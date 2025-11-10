using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

public class BingWebSearchClient : IWebSearchClient
{
    private readonly string _key;
    private readonly HttpClient _http = new HttpClient();

    public BingWebSearchClient(string apiKey) => _key = apiKey;

    public async Task<WebItem[]> SearchAsync(string query, int max)
    {
        var url = $"https://api.bing.microsoft.com/v7.0/search?q={Uri.EscapeDataString(query)}&count={max}";
        // Đúng cho C# 7.3 trở xuống
        using (var req = new HttpRequestMessage(HttpMethod.Get, url))
        {
            req.Headers.Add("Ocp-Apim-Subscription-Key", _key);

            var resp = await _http.SendAsync(req);
            var body = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) return Array.Empty<WebItem>();

            var json = JObject.Parse(body);
            var webPages = json["webPages"]?["value"] as JArray;
            if (webPages == null) return Array.Empty<WebItem>();

            return webPages.Select(x => new WebItem
            {
                Title = x["name"]?.ToString(),
                Url = x["url"]?.ToString(),
                Snippet = x["snippet"]?.ToString()
            }).ToArray();
        }
    }
}
