using System.Configuration;

public static class WebSearchFactory
{
    public static IWebSearchClient TryCreateFromConfig()
    {
        // Ưu tiên Bing
        var bingKey = ConfigurationManager.AppSettings["Bing_Search_ApiKey"];
        if (!string.IsNullOrWhiteSpace(bingKey))
            return new BingWebSearchClient(bingKey);

        // (Nếu dùng Google CSE, bạn tự thêm tương tự)
        return null;
    }
}
