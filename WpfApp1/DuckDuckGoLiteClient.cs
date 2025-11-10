using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using HtmlAgilityPack;

namespace WpfApp1.Services
{
    public class DuckDuckGoLiteClient : IWebSearchClient
    {
        private static HttpClient CreateClient()
        {
            var c = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30) // tăng timeout
            };
            c.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            c.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "vi,en;q=0.8");
            return c;
        }

        public async Task<WebItem[]> SearchAsync(string query, int max = 3)
        {
            if (string.IsNullOrWhiteSpace(query)) return Array.Empty<WebItem>();
            var items = new List<WebItem>();

            // ƯU TIÊN: duckduckgo.com/html (ít bị chặn hơn html.duckduckgo.com)
            var urls = new[]
            {
                "https://duckduckgo.com/html/?q=" + Uri.EscapeDataString(query),
                "https://html.duckduckgo.com/html/?q=" + Uri.EscapeDataString(query)
            };

            foreach (var url in urls)
            {
                try
                {
                    string html;
                    using (var http = CreateClient())
                        html = await http.GetStringAsync(url);

                    var doc = new HtmlDocument();
                    doc.LoadHtml(html);

                    var anchors = doc.DocumentNode.SelectNodes("//a[@class='result__a']");
                    if (anchors != null)
                    {
                        foreach (var a in anchors)
                        {
                            var title = a.InnerText?.Trim() ?? "";
                            var link = a.GetAttributeValue("href", "");

                            // snippet nằm gần thẻ tiêu đề
                            var parent = a.ParentNode;
                            var snippetNode = parent?.SelectSingleNode(".//*[contains(@class,'result__snippet')]");
                            var snippet = snippetNode?.InnerText?.Trim() ?? "";

                            if (!string.IsNullOrWhiteSpace(title) && !string.IsNullOrWhiteSpace(link))
                            {
                                items.Add(new WebItem { Title = title, Url = link, Snippet = snippet });
                                if (items.Count >= max) return items.ToArray();
                            }
                        }
                    }
                }
                catch (TaskCanceledException)
                {
                    // timeout -> thử URL kế
                }
                catch
                {
                    // lỗi khác -> thử URL kế
                }
            }

            return items.ToArray();
        }
    }
}
