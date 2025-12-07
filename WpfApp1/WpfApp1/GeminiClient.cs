using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace WpfApp1.Services
{
    public class GeminiClient
    {
        private readonly string _apiKey;
        private string _model;
        // Nếu API trả 404 cho v1, bạn đổi thành v1beta
        private const string BaseUrl = "https://generativelanguage.googleapis.com/v1";

        public GeminiClient()
        {
            _apiKey = ConfigurationManager.AppSettings["Gemini_ApiKey"];
            _model = NormalizeModel(ConfigurationManager.AppSettings["Gemini_Model"] ?? "gemini-1.5-flash");
            if (string.IsNullOrWhiteSpace(_apiKey))
                throw new InvalidOperationException("Thiếu Gemini_ApiKey trong App.config.");
        }

        private static string NormalizeModel(string name)
        {
            if (string.IsNullOrWhiteSpace(name)) return name;
            var prefix = "models/";
            if (name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                return name.Substring(prefix.Length);
            return name;
        }

        private static HttpClient CreateClient()
        {
            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12;
            return new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
        }

        // -------- TEXT ONLY --------
        public Task<string> AskAsync(string prompt)
        {
            return AskAsync(prompt, new WpfApp1.FileAttachment[0]);
        }

        // -------- TEXT + ATTACHMENTS --------
        public async Task<string> AskAsync(string prompt, System.Collections.Generic.IEnumerable<WpfApp1.FileAttachment> attachments)
        {
            var parts = new System.Collections.Generic.List<object>();
            parts.Add(new { text = prompt });

            foreach (var a in (attachments ?? new WpfApp1.FileAttachment[0]))
            {
                if (a == null || string.IsNullOrEmpty(a.FilePath) || !File.Exists(a.FilePath))
                    continue;

                var ext = Path.GetExtension(a.FilePath).ToLowerInvariant();

                // Ảnh: gửi base64 đúng MIME (Gemini đọc được)
                if (a.IsImage || IsImageExt(ext))
                {
                    var mime = GetMimeFromExt(ext);
                    // Dùng bản sync để tương thích C# 7.3 / .NET cũ
                    var bytes = File.ReadAllBytes(a.FilePath);
                    parts.Add(new
                    {
                        inlineData = new
                        {
                            mimeType = mime,
                            data = Convert.ToBase64String(bytes)
                        }
                    });
                }
                // File text nhỏ: trích 8KB đầu
                else if (IsSmallTextExt(ext))
                {
                    var text = File.ReadAllText(a.FilePath, Encoding.UTF8);
                    if (text.Length > 8192) text = text.Substring(0, 8192) + "\n...[truncated]";
                    parts.Add(new { text = "[Tệp văn bản: " + (a.DisplayName ?? Path.GetFileName(a.FilePath)) + "]\n" + text });
                }
                // File khác: chỉ thông báo tên
                else
                {
                    parts.Add(new { text = "[Tệp đính kèm: " + (a.DisplayName ?? Path.GetFileName(a.FilePath)) + "] (không gửi nội dung)" });
                }
            }

            var payload = new
            {
                contents = new[] { new { parts = parts.ToArray() } }
            };

            try
            {
                return await CallGenerateAsync(NormalizeModel(_model), payload);
            }
            catch (GeminiHttpException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // Thử model -latest, sau đó autodiscover
                var alt = _model.EndsWith("-latest", StringComparison.OrdinalIgnoreCase) ? _model : _model + "-latest";
                try
                {
                    var ans = await CallGenerateAsync(NormalizeModel(alt), payload);
                    _model = alt;
                    return ans;
                }
                catch
                {
                    var discovered = await AutoDiscoverModelAsync();
                    if (!string.IsNullOrEmpty(discovered))
                    {
                        _model = discovered;
                        return await CallGenerateAsync(NormalizeModel(_model), payload);
                    }
                    throw;
                }
            }
        }

        private async Task<string> CallGenerateAsync(string model, object payload)
        {
            using (var client = CreateClient())
            {
                var url = BaseUrl + "/models/" + model + ":generateContent?key=" + _apiKey;
                var json = JsonConvert.SerializeObject(payload);

                // Thử tối đa 2 lần (lần 1 lỗi 503 thì retry)
                for (int attempt = 1; attempt <= 2; attempt++)
                {
                    var resp = await client.PostAsync(
                        url,
                        new StringContent(json, Encoding.UTF8, "application/json")
                    );

                    var body = await resp.Content.ReadAsStringAsync();

                    if (resp.IsSuccessStatusCode)
                    {
                        var obj = JObject.Parse(body);
                        var text = obj["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();
                        return string.IsNullOrWhiteSpace(text)
                            ? "Không có phản hồi từ Gemini."
                            : text;
                    }

                    // Nếu 503 và mới là lần 1 -> nghỉ 1.5s rồi thử lại
                    if (resp.StatusCode == HttpStatusCode.ServiceUnavailable && attempt == 1)
                    {
                        await Task.Delay(1500);
                        continue;
                    }

                    // Lỗi khác, hoặc 503 sau khi đã retry => ném exception gọn
                    throw new GeminiHttpException(resp.StatusCode, body);
                }

                // Về lý thuyết không tới đây, nhưng để cho chắc
                return "Hiện tại Gemini đang quá tải, vui lòng thử lại sau.";
            }
        }


        private async Task<string> AutoDiscoverModelAsync()
        {
            using (var client = CreateClient())
            {
                var url = BaseUrl + "/models?key=" + _apiKey;
                var resp = await client.GetAsync(url);
                var body = await resp.Content.ReadAsStringAsync();
                if (!resp.IsSuccessStatusCode) return null;

                var obj = JObject.Parse(body);
                var models = obj["models"] as JArray;

                if (models == null || models.Count == 0) return null;

                Func<JToken, bool> hasGenerate = m =>
                {
                    var arr = m["supportedGenerationMethods"] as JArray;
                    return arr != null &&
                           arr.ToString().IndexOf("generateContent", StringComparison.OrdinalIgnoreCase) >= 0;
                };

                JToken pick = null;
                pick = models.FirstOrDefault(m => hasGenerate(m) && ((m["name"] + "").IndexOf("1.5", StringComparison.OrdinalIgnoreCase) >= 0));
                if (pick == null) pick = models.FirstOrDefault(m => hasGenerate(m));

                return pick == null ? null : NormalizeModel(pick["name"] + "");
            }
        }

        private static bool IsImageExt(string ext)
        {
            var imgs = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" };
            return imgs.Contains(ext);
        }

        private static bool IsSmallTextExt(string ext)
        {
            var txts = new[] { ".txt", ".csv", ".json", ".md" };
            return txts.Contains(ext);
        }

        private static string GetMimeFromExt(string ext)
        {
            switch (ext)
            {
                case ".png": return "image/png";
                case ".jpg":
                case ".jpeg": return "image/jpeg";
                case ".gif": return "image/gif";
                case ".bmp": return "image/bmp";
                case ".webp": return "image/webp";
                default: return "application/octet-stream";
            }
        }

        private class GeminiHttpException : Exception
        {
            public HttpStatusCode StatusCode { get; private set; }

            public GeminiHttpException(HttpStatusCode code, string body)
                : base(BuildMessage(code, body))
            {
                StatusCode = code;
            }

            private static string BuildMessage(HttpStatusCode code, string body)
            {
                // Trường hợp quá tải (503) -> thông báo thân thiện
                if (code == HttpStatusCode.ServiceUnavailable)
                {
                    return "Hiện tại dịch vụ Gemini đang quá tải hoặc tạm thời không khả dụng (503). " +
                           "Bạn thử lại sau vài phút nhé.";
                }

                // Cắt ngắn body để không show nguyên JSON
                string shortBody = body ?? "";
                if (shortBody.Length > 300)
                    shortBody = shortBody.Substring(0, 300) + "...";

                return $"AI hiện không trả lời được (HTTP {(int)code} - {code}). " +
                       $"Chi tiết: {shortBody}";
            }
        }

    }
}
