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
        private readonly GeminiClient _llm;      // không khởi tạo ở đây nữa
        private readonly IWebSearchClient _web;
        private readonly string _conn;

        public bool IsDemo { get; }

        public AiService(bool isDemo = false)
        {
            IsDemo = isDemo;

            // DB + web search vẫn đọc như cũ (OK cho unit test)
            var ef = System.Configuration.ConfigurationManager
                         .ConnectionStrings["QuanlychungcuEntities"];
            _conn = new EntityConnectionStringBuilder(ef.ConnectionString)
                        .ProviderConnectionString;

            var bingKey = System.Configuration.ConfigurationManager.AppSettings["Bing_ApiKey"];
            if (!string.IsNullOrWhiteSpace(bingKey))
                _web = new BingWebSearchClient(bingKey);
            else
                _web = new DuckDuckGoLiteClient();

            // ❗Chỉ tạo GeminiClient khi không demo
            if (!IsDemo)
            {
                _llm = new GeminiClient();
            }
        }

        // ================== Public entry (attachments) ==================
        // Gửi prompt (đã gồm dữ liệu nội bộ + web) và KÈM attachments (ảnh/tệp) vào Gemini
        public async Task<string> AskAsync(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
                throw new ArgumentException("Question must not be empty.", nameof(q));

            if (IsDemo)
            {
                if (q.Length > 2000)
                    return "Câu hỏi của bạn quá dài, demo chỉ hỗ trợ <= 2000 ký tự.";
                return "[DEMO] Bạn vừa hỏi: " + q;
            }

            if (_llm == null)
                throw new InvalidOperationException("AI client chưa được khởi tạo.");

            var prompt = await BuildPromptAsync(q);
            return await _llm.AskAsync(prompt);
        }

        public async Task<string> AskAsync(string q, IEnumerable<FileAttachment> attachments)
        {
            if (string.IsNullOrWhiteSpace(q) && (attachments == null || !attachments.Any()))
                throw new ArgumentException("Bạn phải nhập câu hỏi hoặc đính kèm tệp.");

            if (IsDemo)
            {
                if (!string.IsNullOrWhiteSpace(q) && q.Length > 2000)
                    return "Câu hỏi của bạn quá dài, demo chỉ hỗ trợ <= 2000 ký tự.";

                return "[DEMO] Trả lời demo cho câu hỏi: " + (q ?? "(không có)") +
                       (attachments != null && attachments.Any() ? " (kèm tệp)" : "");
            }

            if (_llm == null)
                throw new InvalidOperationException("AI client chưa được khởi tạo.");

            var prompt = await BuildPromptAsync(q);
            var atts = (attachments ?? Enumerable.Empty<FileAttachment>()).ToList();
            return await _llm.AskAsync(prompt, atts);
        }


        // ================== BUILD PROMPT: trộn data nội bộ + web ==================
        private async Task<string> BuildPromptAsync(string q)
        {
            var topN = int.Parse(System.Configuration.ConfigurationManager.AppSettings["AI_SearchTopN"] ?? "5");
            var noAccent = RemoveDiacritics(q).ToLowerInvariant();
            var intent = NormalizeForIntent(q);

            var prompt = new StringBuilder();

            // --- các pattern đặc biệt (ưu tiên) ---

            // 1) Tìm cư dân theo SĐT bắt đầu
            var mPhone = Regex.Match(noAccent, @"(dien thoai|sdt)\s+(bat dau|batdau)\s+(\d{2,6})");
            if (mPhone.Success)
            {
                var prefix = mPhone.Groups[3].Value;
                var tbPhone = await SearchCuDanByPhonePrefixAsync(prefix, topN);
                if (tbPhone.Rows.Count == 0) return $"Không tìm thấy cư dân có SĐT bắt đầu \"{prefix}\".";
                return RenderList("Cư dân có SĐT bắt đầu \"" + prefix + "\"", tbPhone);
            }

            // 2) Tìm xe theo biển số bắt đầu
            var mPlate = Regex.Match(noAccent, @"(bien so|bks)\s+(bat dau|batdau)\s+([a-z0-9\-]{2,8})");
            if (mPlate.Success)
            {
                var prefix = mPlate.Groups[3].Value.ToUpperInvariant();
                var tbPlate = await SearchXeByPlatePrefixAsync(prefix, topN);
                if (tbPlate.Rows.Count == 0) return $"Không thấy phương tiện có biển số bắt đầu \"{prefix}\".";
                return RenderList("Phương tiện có biển số bắt đầu \"" + prefix + "\"", tbPlate);
            }

            // 3) Tìm cư dân theo domain email
            var mMail = Regex.Match(noAccent, @"email\s+(ket thuc|ketthuc)\s+@?([a-z0-9\.\-]+)");
            if (mMail.Success)
            {
                var domain = mMail.Groups[2].Value;
                var tbMail = await SearchCuDanByEmailDomainAsync(domain, topN);
                if (tbMail.Rows.Count == 0) return $"Không thấy cư dân có email kết thúc @{domain}.";
                return RenderList("Cư dân có email @" + domain, tbMail);
            }

            // 4) Hóa đơn cư dân chưa thanh toán
            // (Chú ý TrangThai trong DB phải đúng = 'ChuaThanhToan')
            if (intent.Contains("hoa don") && intent.Contains("chua thanh toan"))
            {
                var tbDebt = await SearchHoaDonCuDanByTrangThaiAsync("ChuaThanhToan", topN);
                if (tbDebt.Rows.Count == 0) return "Không có hóa đơn cư dân trạng thái ChuaThanhToan.";
                return RenderList("Hóa đơn cư dân chưa thanh toán", tbDebt);
            }

            // --- intent search + build context nội bộ ---
            bool isSearch =
                intent.Contains("tim ") || intent.Contains("tra cuu") ||
                intent.Contains("tim kiem") || intent.Contains("liet ke") ||
                intent.Contains("danh sach") || intent.Contains("ke hoach") ||
                intent.Contains("can ho") || intent.Contains("cu dan") ||
                intent.Contains("hoa don") || intent.Contains("vat tu") ||
                intent.Contains("xe may") || intent.Contains("xe o to") ||
                intent.Contains("xe dap") || intent.Contains("mat bang") ||
                intent.Contains("nhan vien") || intent.Contains("tai khoan");

            DataTable tb = null;

            if (isSearch)
            {
                var keyword = BuildSqlKeyword(q);
                var entityType = ResolveEntityType(q);

                // Gọi sp_Search với keyword + entityType đoán
                tb = await SearchByProcAsync(keyword, topN, entityType);

                // Nếu không có kết quả mà đã fix entityType -> thử bỏ entityType
                if (tb.Rows.Count == 0 && entityType != null)
                    tb = await SearchByProcAsync(keyword, topN, null);

                // Nếu vẫn trống -> thử lấy default top theo entityType (hoặc all)
                if (tb.Rows.Count == 0)
                    tb = await SearchByProcAsync(string.Empty, topN, entityType);
            }

            // ====== BUILD PROMPT CHO LLM ======
            if (isSearch && tb != null && tb.Rows.Count > 0)
            {
                var ctx = new StringBuilder();
                ctx.AppendLine("### DỮ LIỆU NỘI BỘ (Top " + tb.Rows.Count + " bản ghi phù hợp):");
                foreach (DataRow r in tb.Rows)
                    ctx.AppendLine($"- [{r["EntityType"]}] {r["Title"]} — {r["Detail"]} (Id={r["EntityId"]})");

                prompt.AppendLine("Bạn là trợ lý cho ứng dụng QUẢN LÝ CHUNG CƯ.");
                prompt.AppendLine("- ƯU TIÊN dùng dữ liệu nội bộ để trả lời nếu câu hỏi liên quan đến cư dân, căn hộ, hóa đơn, xe, vật tư, mặt bằng, nhân viên, tài khoản, v.v.");
                prompt.AppendLine("- Nếu câu hỏi chung chung (kiến thức đời sống, thế giới, lập trình, v.v.), hãy trả lời như trợ lý tổng quát.");
                prompt.AppendLine("- Nếu có trích dẫn web ở dưới, dùng để bổ sung, nhưng không bịa URL hay số liệu.");
                prompt.AppendLine();
                prompt.AppendLine($"### CÂU HỎI NGƯỜI DÙNG: {q}");
                prompt.AppendLine();
                prompt.AppendLine(ctx.ToString());
                prompt.AppendLine();
                prompt.AppendLine("### YÊU CẦU");
                prompt.AppendLine("- Tóm tắt câu trả lời dựa trên dữ liệu nội bộ (nếu liên quan).");
                prompt.AppendLine("- Nếu phù hợp, gợi ý thao tác trong app, ví dụ: 'Mở màn hình CanHoList, lọc theo SoCanHo chứa 101'.");
                prompt.AppendLine("- Không bịa thông tin về dữ liệu nội bộ.");
            }
            else
            {
                // Không có data nội bộ, hoặc không phải câu dạng search => trả lời kiểu trợ lý chung
                prompt.AppendLine("Bạn là trợ lý cho ứng dụng QUẢN LÝ CHUNG CƯ. Trả lời ngắn gọn, rõ ràng.");
                prompt.AppendLine($"### CÂU HỎI NGƯỜI DÙNG: {q}");
            }

            // --- web snippets (nếu có web search) ---
            var webCtx = await BuildWebContextAsync(q, 3);
            if (!string.IsNullOrEmpty(webCtx))
            {
                prompt.AppendLine();
                prompt.AppendLine(webCtx);
                prompt.AppendLine("Dựa trên trích dẫn web ở trên, bổ sung kiến thức bên ngoài nếu câu hỏi không chỉ liên quan đến dữ liệu nội bộ.");
            }

            return prompt.ToString();
        }

        // ================== Helpers: chuẩn hóa / intent / viết tắt ==================

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

            // Chuẩn hóa ngoặc cong về ngoặc thẳng
            s = s.Replace('“', '"')
                 .Replace('”', '"')
                 .Replace('‘', '\'')
                 .Replace('’', '\'');

            // XÓA luôn ngoặc kép, ? ! , . (đưa về khoảng trắng)
            s = s.Replace('"', ' ')
                 .Replace('?', ' ')
                 .Replace('!', ' ')
                 .Replace(',', ' ')
                 .Replace('.', ' ');

            // Gom nhiều khoảng trắng thành 1
            s = Regex.Replace(s, @"\s+", " ").Trim();

            return s;
        }


        // Chuẩn hóa câu hỏi để hiểu intent: bỏ dấu + thường + expand viết tắt
        private static string NormalizeForIntent(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return string.Empty;

            var s = RemoveDiacritics(q).ToLowerInvariant();
            s = Regex.Replace(s, @"\s+", " ").Trim();

            // ==== Expand viết tắt / alias ====

            // Hóa đơn
            s = s.Replace(" hdtm", " hoa don thuong mai")
                 .Replace(" hd tm", " hoa don thuong mai")
                 .Replace(" hoa don tm", " hoa don thuong mai")
                 .Replace(" hdc ", " hoa don cu dan ")
                 .Replace(" hd cd", " hoa don cu dan");

            // Cư dân, căn hộ
            s = s.Replace(" cd ", " cu dan ")
                 .Replace(" cu dan ", " cu dan ")
                 .Replace(" ch ", " can ho ");

            // Xe
            s = s.Replace(" oto", " o to")
                 .Replace(" xe oto", " xe o to")
                 .Replace(" xomay", " xe may")
                 .Replace(" xedap", " xe dap");

            // Nhân viên, vai trò
            s = s.Replace(" nv ", " nhan vien ")
                 .Replace(" vtro", " vai tro");

            return s;
        }

        private static string BuildSqlKeyword(string q)
        {
            var raw = NormalizeQuotesAndPunct(q);
            var intent = NormalizeForIntent(raw);   // đã bỏ dấu + expand viết tắt
            var intentNoAcc = RemoveDiacritics(intent).ToLowerInvariant();

            // 1) Ưu tiên bắt "tên X" / "có tên X"
            var tokens = raw.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length > 0)
            {
                var rawNoAccTokens = tokens
                    .Select(t => RemoveDiacritics(t).ToLowerInvariant())
                    .ToArray();

                // "tên Huy" hoặc "có tên Huy"
                for (int i = 0; i < rawNoAccTokens.Length; i++)
                {
                    if (rawNoAccTokens[i] == "ten" ||
                       (rawNoAccTokens[i] == "co" && i + 1 < rawNoAccTokens.Length && rawNoAccTokens[i + 1] == "ten"))
                    {
                        int idx = (rawNoAccTokens[i] == "ten") ? i + 1 : i + 2;
                        if (idx < tokens.Length)
                            return tokens[idx];      // ví dụ "Huy"
                    }
                }

                // "số căn A101" / "số căn hộ A101"
                for (int i = 0; i < rawNoAccTokens.Length - 1; i++)
                {
                    if (rawNoAccTokens[i] == "so" &&
                        (rawNoAccTokens[i + 1] == "can" || rawNoAccTokens[i + 1] == "canho"))
                    {
                        int idx = i + 2;
                        if (idx < tokens.Length)
                            return tokens[idx];      // ví dụ "A101"
                    }
                }
            }

            // 2) Không bắt được tên cụ thể -> dùng keyword theo loại entity
            if (intentNoAcc.Contains("cu dan")) return "cư dân";
            if (intentNoAcc.Contains("can ho")) return "căn hộ";
            if (intentNoAcc.Contains("o to")) return "ô tô";
            if (intentNoAcc.Contains("xe may")) return "xe máy";
            if (intentNoAcc.Contains("xe dap")) return "xe đạp";
            if (intentNoAcc.Contains("vat tu")) return "vật tư";
            if (intentNoAcc.Contains("mat bang thuong mai")) return "mặt bằng thương mại";
            if (intentNoAcc.Contains("mat bang")) return "mặt bằng";
            if (intentNoAcc.Contains("hoa don thuong mai")) return "hóa đơn thương mại";
            if (intentNoAcc.Contains("hoa don cu dan")) return "hóa đơn cư dân";
            if (intentNoAcc.Contains("hoa don")) return "hóa đơn";
            if (intentNoAcc.Contains("chua thanh toan")) return "chưa thanh toán";

            // 3) Fallback: lấy 3 token >= 3 ký tự
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
            var s = NormalizeForIntent(q);   // đã bỏ dấu + expand viết tắt

            // Xe
            if (s.Contains("xe may")) return "XeMay";
            if (s.Contains("xe dap")) return "XeDap";
            if (s.Contains("xe o to") || s.Contains(" o to")) return "XeOTo";
            if (s.Contains("phuong tien")) return "XeMay"; // default

            // Hóa đơn
            if (s.Contains("hoa don thuong mai")) return "HoaDonTM";
            if (s.Contains("hoa don cu dan") || (s.Contains("hoa don") && s.Contains("cu dan"))) return "HoaDonCuDan";
            if (s.Contains("hoa don tm") || s.Contains("hdtm")) return "HoaDonTM";
            if (s.Contains("hoa don cd") || s.Contains("hd cd")) return "HoaDonCuDan";
            if (s.Contains("hoa don")) return null; // để sp_Search tự đoán

            // Căn hộ / cư dân
            if (s.Contains("can ho")) return "CanHo";
            if (s.Contains("cu dan") || s.Contains("dan cu")) return "CuDan";

            // Vật tư / mặt bằng
            if (s.Contains("vat tu") || s.Contains("vattu")) return "VatTu";
            if (s.Contains("mat bang thuong mai") || s.Contains("mbtm")) return "MatBangThuongMai";
            if (s.Contains("mat bang")) return "MatBangThuongMai";

            // Nhân viên / tài khoản / vai trò / user
            if (s.Contains("nhan vien") || s.Contains("nv ")) return "NhanVien";
            if (s.Contains("tai khoan")) return "TaiKhoan";
            if (s.Contains("user ")) return "User";
            if (s.Contains("vai tro") || s.Contains("vtro")) return "VaiTro";
            if (s.Contains("admin")) return "Admin";

            // Inventory / model / AI liên quan
            if (s.Contains("vattu ton kho") || s.Contains("ton kho") || s.Contains("inventory")) return "VatTu";
            if (s.Contains("du bao") || s.Contains("forecast")) return "InventoryForecast";
            if (s.Contains("model ai") || s.Contains("modelregistry")) return "ModelRegistry";
            if (s.Contains("jobrun") || s.Contains("job run") || s.Contains("lich su job")) return "JobRun";

            return null; // không đoán được -> sp_Search dùng all
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

        // ============ Gọi sp_Search chung ============

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

        // ============ Patterned queries riêng ============

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
                // nếu web search lỗi thì im lặng, không ảnh hưởng AI
                return null;
            }
        }
    }
}
