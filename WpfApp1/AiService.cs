using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Data.Entity.Core.EntityClient;
using WpfApp1.Services;                      // GeminiClient, BingWebSearchClient
using WpfApp1;                               // FileAttachment
using System.IO;
using System.Linq;

namespace WpfApp1
{
    public class AiService
    {
        private readonly GeminiClient _llm = new GeminiClient();
        private readonly IWebSearchClient _web; // optional (Bing)
        private readonly string _conn;

        public bool IsDemo => false;

        public AiService()
        {
            var ef = System.Configuration.ConfigurationManager.ConnectionStrings["QuanlychungcuEntities"];
            _conn = new EntityConnectionStringBuilder(ef.ConnectionString).ProviderConnectionString;

            var bingKey = System.Configuration.ConfigurationManager.AppSettings["Bing_ApiKey"];

            if (!string.IsNullOrWhiteSpace(bingKey))
            {
                _web = new BingWebSearchClient(bingKey);          // Có key Azure Bing -> dùng Bing API (dữ liệu ngoài “xịn”)
            }
            else
            {
                _web = new DuckDuckGoLiteClient();                // Không có key -> dùng DuckDuckGo HTML (không cần key, vẫn là dữ liệu thật)
            }
        }

        // ================== Public entry (attachments) ==================
        // Gửi prompt (đã gồm dữ liệu nội bộ + web) và KÈM attachments (ảnh/tệp) vào Gemini
        public async Task<string> AskAsync(string q, IEnumerable<FileAttachment> attachments)
        {
            var prompt = await BuildPromptAsync(q);
            var atts = (attachments ?? Enumerable.Empty<FileAttachment>()).ToList();
            return await _llm.AskAsync(prompt, atts);
        }

        // ================== Public entry (text-only) ==================
        public async Task<string> AskAsync(string q)
        {
            var prompt = await BuildPromptAsync(q);
            return await _llm.AskAsync(prompt);
        }

        // ===== Dùng chung: build prompt (giữ toàn bộ logic search nội bộ + web) =====
        private async Task<string> BuildPromptAsync(string q)
        {
            var topN = int.Parse(System.Configuration.ConfigurationManager.AppSettings["AI_SearchTopN"] ?? "5");
            var noAccent = RemoveDiacritics(q).ToLowerInvariant();

            // --- các pattern đặc biệt ---
            var mPhone = Regex.Match(noAccent, @"(dien thoai|sdt)\s+(bat dau|batdau)\s+(\d{2,6})");
            if (mPhone.Success)
            {
                var prefix = mPhone.Groups[3].Value;
                var tbPhone = await SearchCuDanByPhonePrefixAsync(prefix, topN);
                if (tbPhone.Rows.Count == 0) return $"Không tìm thấy cư dân có SĐT bắt đầu \"{prefix}\".";
                return RenderList("Cư dân có SĐT bắt đầu \"" + prefix + "\"", tbPhone);
            }

            var mPlate = Regex.Match(noAccent, @"(bien so|bks)\s+(bat dau|batdau)\s+([a-z0-9\-]{2,8})");
            if (mPlate.Success)
            {
                var prefix = mPlate.Groups[3].Value.ToUpperInvariant();
                var tbPlate = await SearchXeByPlatePrefixAsync(prefix, topN);
                if (tbPlate.Rows.Count == 0) return $"Không thấy phương tiện có biển số bắt đầu \"{prefix}\".";
                return RenderList("Phương tiện có biển số bắt đầu \"" + prefix + "\"", tbPlate);
            }

            var mMail = Regex.Match(noAccent, @"email\s+(ket thuc|ketthuc)\s+@?([a-z0-9\.\-]+)");
            if (mMail.Success)
            {
                var domain = mMail.Groups[2].Value;
                var tbMail = await SearchCuDanByEmailDomainAsync(domain, topN);
                if (tbMail.Rows.Count == 0) return $"Không thấy cư dân có email kết thúc @{domain}.";
                return RenderList("Cư dân có email @" + domain, tbMail);
            }

            if (ContainsNoAccent(q, "hoa don", "hoa don cu dan") && ContainsNoAccent(q, "chua thanh toan"))
            {
                var tbDebt = await SearchHoaDonCuDanByTrangThaiAsync("ChuaThanhToan", topN);
                if (tbDebt.Rows.Count == 0) return "Không có hóa đơn cư dân trạng thái ChưaThanhToan.";
                return RenderList("Hóa đơn cư dân chưa thanh toán", tbDebt);
            }

            // --- intent search + build context nội bộ ---
            bool isSearch =
                ContainsNoAccent(q, "tim", "tra cuu", "tim kiem", "liet ke", "danh sach", "ke hoach") ||
                ContainsNoAccent(q, "can ho", "canho") ||
                ContainsNoAccent(q, "cu dan", "dan cu") ||
                ContainsNoAccent(q, "o to", "oto", "xe may", "xe dap", "phuong tien") ||
                ContainsNoAccent(q, "hoa don", "chua thanh toan") ||
                ContainsNoAccent(q, "vat tu", "mat bang", "nhan vien", "tai khoan", "vai tro");

            var prompt = new StringBuilder();
            if (isSearch)
            {
                var keyword = BuildSqlKeyword(q);
                var entityType = ResolveEntityType(q);

                var tb = await SearchByProcAsync(keyword, topN, entityType);
                if (tb.Rows.Count == 0 && entityType != null)
                    tb = await SearchByProcAsync(keyword, topN, null);
                if (tb.Rows.Count == 0)
                    tb = await SearchByProcAsync(string.Empty, topN, entityType);

                if (tb.Rows.Count == 0)
                {
                    // không có dữ liệu nội bộ — vẫn để LLM trả lời chung + web
                    prompt.AppendLine("Bạn là trợ lý cho ứng dụng QUẢN LÝ CHUNG CƯ. Trả lời ngắn gọn, rõ ràng.");
                    prompt.AppendLine($"### CÂU HỎI NGƯỜI DÙNG: {q}");
                }
                else
                {
                    var ctx = new StringBuilder();
                    ctx.AppendLine("### DỮ LIỆU NỘI BỘ (Top " + tb.Rows.Count + "):");
                    foreach (DataRow r in tb.Rows)
                        ctx.AppendLine($"- [{r["EntityType"]}] {r["Title"]} — {r["Detail"]} (Id={r["EntityId"]})");

                    // thay bằng:
                    prompt.AppendLine("Bạn là trợ lý cho ứng dụng QUẢN LÝ CHUNG CƯ.");
                    prompt.AppendLine("- Nếu câu hỏi liên quan chung cư: trả lời ngắn gọn, chính xác theo dữ liệu nội bộ.");
                    prompt.AppendLine("- Nếu KHÔNG liên quan: vẫn trả lời như trợ lý tổng quát, súc tích, hữu ích.");
                    prompt.AppendLine("- Có trích dẫn web ở dưới thì tham khảo để bổ sung.");
                    prompt.AppendLine();
                    prompt.AppendLine($"### CÂU HỎI NGƯỜI DÙNG: {q}");
                    prompt.AppendLine();
                    prompt.AppendLine(ctx.ToString());
                    prompt.AppendLine();
                    prompt.AppendLine("### YÊU CẦU");
                    prompt.AppendLine("- Tóm tắt kết quả phù hợp với câu hỏi.");
                    prompt.AppendLine("- Nếu có thể, gợi ý thao tác: mở CanHoList/CuDanList/… (chỉ nêu filter).");
                    prompt.AppendLine("- Không bịa.");
                }
            }
            else
            {
                prompt.AppendLine("Bạn là trợ lý cho ứng dụng QUẢN LÝ CHUNG CƯ. Trả lời ngắn gọn, rõ ràng.");
                prompt.AppendLine($"### CÂU HỎI NGƯỜI DÙNG: {q}");
            }

            // --- web snippets (nếu có Bing key) ---
            var webCtx = await BuildWebContextAsync(q, 3);
            if (!string.IsNullOrEmpty(webCtx))
            {
                prompt.AppendLine();
                prompt.AppendLine(webCtx);
                prompt.AppendLine("Dựa trên trích dẫn web ở trên, bổ sung kiến thức bên ngoài nếu cần.");
            }

            return prompt.ToString();
        }

        // ================== Helpers ==================
        private static string RemoveDiacritics(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            var norm = s.Normalize(NormalizationForm.FormD);
            var sb = new StringBuilder(norm.Length);
            foreach (var ch in norm)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(ch);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(ch);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        private static bool ContainsNoAccent(string haystack, params string[] needles)
        {
            var h = RemoveDiacritics(haystack).ToLowerInvariant();
            foreach (var n in needles)
                if (h.Contains(RemoveDiacritics(n).ToLowerInvariant())) return true;
            return false;
        }

        private static string NormalizeQuotesAndPunct(string s)
        {
            if (string.IsNullOrEmpty(s)) return s;
            return s.Replace('“', '"').Replace('”', '"')
                    .Replace('‘', '\'').Replace('’', '\'')
                    .Replace("?", " ").Replace("!", " ").Replace(",", " ").Replace(".", " ")
                    .Trim();
        }

        private static string BuildSqlKeyword(string q)
        {
            var raw = NormalizeQuotesAndPunct(q);
            var noAccent = RemoveDiacritics(raw).ToLowerInvariant();

            if (noAccent.Contains("dan cu") || noAccent.Contains("cu dan")) return "cư dân";
            if (noAccent.Contains("can ho") || noAccent.Contains("canho")) return "căn hộ";
            if (noAccent.Contains("o to") || noAccent.Contains("oto")) return "ô tô";
            if (noAccent.Contains("xe may")) return "xe máy";
            if (noAccent.Contains("xe dap")) return "xe đạp";
            if (noAccent.Contains("vat tu") || noAccent.Contains("vattu")) return "vật tư";
            if (noAccent.Contains("mat bang")) return "mặt bằng";
            if (noAccent.Contains("hoa don")) return "hóa đơn";   // <-- dòng lỗi, dùng Contains (hoa)
            if (noAccent.Contains("chua thanh toan")) return "chưa thanh toán";

            var tokens = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            var sb = new StringBuilder();
            int picked = 0;
            foreach (var t in tokens)
            {
                if (RemoveDiacritics(t).Length >= 3)
                {
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(t);
                    picked++;
                    if (picked >= 3) break;
                }
            }
            return sb.ToString();
        }


        private static string ResolveEntityType(string q)
        {
            var s = RemoveDiacritics(q).ToLowerInvariant();

            if (s.Contains("xe may")) return "XeMay";
            if (s.Contains("xe dap")) return "XeDap";
            if (s.Contains("o to") || s.Contains("oto")) return "XeOTo";
            if (s.Contains("phuong tien")) return "XeMay";

            if (s.Contains("hoa don thuong mai") || s.Contains("hd tm")) return "HoaDonTM";
            if (s.Contains("hoa don cu dan") || (s.Contains("hoa don") && s.Contains("cu dan"))) return "HoaDonCuDan";
            if (s.Contains("hoa don")) return null;

            if (s.Contains("can ho")) return "CanHo";
            if (s.Contains("cu dan") || s.Contains("dan cu")) return "CuDan";
            if (s.Contains("vat tu") || s.Contains("vattu")) return "VatTu";
            if (s.Contains("mat bang")) return "MatBang";
            if (s.Contains("nhan vien")) return "NhanVien";
            if (s.Contains("tai khoan")) return "TaiKhoan";
            if (s.Contains("user ")) return "User";
            if (s.Contains("vai tro")) return "VaiTro";
            return null;
        }

        // ============ Render helper ============
        private static string RenderList(string title, DataTable tb)
        {
            var sb = new StringBuilder();
            sb.AppendLine(title + ":");
            foreach (DataRow r in tb.Rows)
                sb.AppendLine($"- [{r["EntityType"]}] {r["Title"]} — {r["Detail"]} (Id={r["EntityId"]})");
            return sb.ToString();
        }

        // ============ Gọi sp_Search ============
        private async Task<DataTable> SearchByProcAsync(string keyword, int top, string entityType)
        {
            var dt = new DataTable();
            using (var c = new SqlConnection(_conn))
            using (var cmd = new SqlCommand("ai.sp_Search", c) { CommandType = CommandType.StoredProcedure })
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@q", keyword ?? "");
                cmd.Parameters.AddWithValue("@top", top);
                var p = cmd.Parameters.Add("@entityType", SqlDbType.NVarChar, 50);
                p.Value = string.IsNullOrEmpty(entityType) ? (object)DBNull.Value : entityType;

                await c.OpenAsync();
                da.Fill(dt);
            }
            return dt;
        }

        // ============ Patterned queries ============
        private async Task<DataTable> SearchCuDanByPhonePrefixAsync(string prefix, int top)
        {
            var dt = new DataTable();
            using (var c = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
                SELECT TOP(@top)
                    N'CuDan' AS EntityType,
                    CAST(cd.CuDanID AS nvarchar(50)) AS EntityId,
                    N'Cư dân ' + cd.HoTen AS Title,
                    N'Email: ' + ISNULL(cd.Email,'') + N', SĐT: ' + ISNULL(cd.DienThoai,'') +
                    N', Căn hộ: ' + ISNULL(ch.SoCanHo, N'') AS Detail
                FROM dbo.CuDan cd
                LEFT JOIN dbo.CanHo ch ON ch.CanHoID = cd.CanHoID
                WHERE cd.DienThoai LIKE @pre + '%'
                ORDER BY cd.CuDanID DESC;", c))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@pre", prefix);
                cmd.Parameters.AddWithValue("@top", top);
                await c.OpenAsync();
                da.Fill(dt);
            }
            return dt;
        }

        private async Task<DataTable> SearchXeByPlatePrefixAsync(string prefix, int top)
        {
            var dt = new DataTable();
            using (var c = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
                SELECT TOP(@top) * FROM (
                    SELECT N'XeOTo' AS EntityType, CAST(x.XeOToID AS nvarchar(50)) AS EntityId,
                           N'Ô tô ' + ISNULL(x.BKS,N'') AS Title,
                           N'Chủ: ' + cd.HoTen + N'. SĐT ' + ISNULL(cd.DienThoai,N'') AS Detail
                    FROM dbo.XeOTo x JOIN dbo.CuDan cd ON cd.CuDanID = x.CuDanID
                    WHERE ISNULL(x.BKS,'') LIKE @pre + '%'
                    UNION ALL
                    SELECT N'XeMay', CAST(m.XeMayID AS nvarchar(50)),
                           N'Xe máy ' + ISNULL(m.BKS,N''),
                           N'Chủ: ' + cd.HoTen + N'. SĐT ' + ISNULL(cd.DienThoai,N'')
                    FROM dbo.XeMay m JOIN dbo.CuDan cd ON cd.CuDanID = m.CuDanID
                    WHERE ISNULL(m.BKS,'') LIKE @pre + '%'
                    UNION ALL
                    SELECT N'XeDap', CAST(d.XeDapID AS nvarchar(50)),
                           N'Xe đạp ' + ISNULL(d.BKS,N''),
                           N'Chủ: ' + cd.HoTen + N'. SĐT ' + ISNULL(cd.DienThoai,N'')
                    FROM dbo.XeDap d JOIN dbo.CuDan cd ON cd.CuDanID = d.CuDanID
                    WHERE ISNULL(d.BKS,'') LIKE @pre + '%'
                ) t
                ORDER BY t.Title DESC;", c))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@pre", prefix);
                cmd.Parameters.AddWithValue("@top", top);
                await c.OpenAsync();
                da.Fill(dt);
            }
            return dt;
        }

        private async Task<DataTable> SearchCuDanByEmailDomainAsync(string domain, int top)
        {
            var dt = new DataTable();
            using (var c = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
                SELECT TOP(@top)
                    N'CuDan' AS EntityType,
                    CAST(cd.CuDanID AS nvarchar(50)) AS EntityId,
                    N'Cư dân ' + cd.HoTen AS Title,
                    N'Email: ' + ISNULL(cd.Email,'') + N', SĐT: ' + ISNULL(cd.DienThoai,'') +
                    N', Căn hộ: ' + ISNULL(ch.SoCanHo, N'') AS Detail
                FROM dbo.CuDan cd
                LEFT JOIN dbo.CanHo ch ON ch.CanHoID = cd.CanHoID
                WHERE cd.Email LIKE '%@' + @dom
                ORDER BY cd.CuDanID DESC;", c))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@dom", domain);
                cmd.Parameters.AddWithValue("@top", top);
                await c.OpenAsync();
                da.Fill(dt);
            }
            return dt;
        }

        private async Task<DataTable> SearchHoaDonCuDanByTrangThaiAsync(string trangThai, int top)
        {
            var dt = new DataTable();
            using (var c = new SqlConnection(_conn))
            using (var cmd = new SqlCommand(@"
                SELECT TOP(@top)
                    N'HoaDonCuDan' AS EntityType,
                    CAST(h.HoaDonID AS nvarchar(50)) AS EntityId,
                    N'Hóa đơn ' + h.LoaiDichVu + N' - ' + CONVERT(nvarchar(10), h.NgayLap, 120) AS Title,
                    N'Cư dân: ' + cd.HoTen + N', Căn hộ: ' + ch.SoCanHo +
                    N', Số tiền: ' + CONVERT(nvarchar(50), h.SoTien) +
                    N', Trạng thái: ' + ISNULL(h.TrangThai,N'') AS Detail
                FROM dbo.HoaDonCuDan h
                JOIN dbo.CuDan cd ON cd.CuDanID = h.CuDanID
                JOIN dbo.CanHo ch ON ch.CanHoID = h.CanHoID
                WHERE h.TrangThai = @tt
                ORDER BY h.NgayLap DESC;", c))
            using (var da = new SqlDataAdapter(cmd))
            {
                cmd.Parameters.AddWithValue("@tt", trangThai);
                cmd.Parameters.AddWithValue("@top", top);
                await c.OpenAsync();
                da.Fill(dt);
            }
            return dt;
        }

        // ============ Web snippets (optional) ============
        private async Task<string> BuildWebContextAsync(string q, int max)
        {
            if (_web == null) return null;
            try
            {
                var items = await _web.SearchAsync(q, max);
                if (items == null || items.Length == 0) return null;

                var sb = new StringBuilder();
                sb.AppendLine("### TRÍCH DẪN WEB (tham khảo):");
                for (int i = 0; i < items.Length; i++)
                {
                    var it = items[i];
                    sb.AppendLine($"[{i + 1}] {it.Title} — {it.Snippet} ({it.Url})");
                }
                return sb.ToString();
            }
            catch
            {
                return null; // im lặng nếu web search lỗi
            }
        }
    }
}
