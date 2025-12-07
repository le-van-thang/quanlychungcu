using System.Threading.Tasks;

public interface IWebSearchClient
{
    Task<WebItem[]> SearchAsync(string query, int max);
}

public class WebItem
{
    public string Title { get; set; }
    public string Snippet { get; set; }
    public string Url { get; set; }
}
