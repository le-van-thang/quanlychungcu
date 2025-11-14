using Microsoft.Web.WebView2.Core;
using System;
using System.Configuration;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace WpfApp1.Windows
{
    public partial class FacebookLoginWindow : Window
    {
        public string AccessToken { get; private set; }

        private string _appId = "";
        private string _redirect = "";

        private const bool ForceClearFacebookCookiesOnOpen = true;

        public FacebookLoginWindow()
        {
            InitializeComponent();
            Loaded += FacebookLoginWindow_Loaded;
            Closed += FacebookLoginWindow_Closed;
        }

        private async void FacebookLoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            _appId = ConfigurationManager.AppSettings["FacebookAppId"] ?? "";
            _redirect = ConfigurationManager.AppSettings["FacebookRedirectUri"] ?? "";

            if (string.IsNullOrWhiteSpace(_appId) || string.IsNullOrWhiteSpace(_redirect))
            {
                MessageBox.Show("Thiếu FacebookAppId hoặc FacebookRedirectUri trong App.config.");
                DialogResult = false;
                Close();
                return;
            }

            await EnsureWebView2Async();

            if (ForceClearFacebookCookiesOnOpen)
            {
                try { await ClearFacebookCookiesAsync(); } catch { }
            }

            webView.CoreWebView2.NavigationStarting += Core_NavigationStarting;
            webView.CoreWebView2.SourceChanged += Core_SourceChanged;

            var scope = "public_profile,email";
            var url =
                "https://www.facebook.com/v19.0/dialog/oauth" +
                "?client_id=" + Uri.EscapeDataString(_appId) +
                "&redirect_uri=" + Uri.EscapeDataString(_redirect) +
                "&response_type=token" +
                "&display=popup" +
                "&scope=" + Uri.EscapeDataString(scope);

            webView.Source = new Uri(url);
        }

        private async Task EnsureWebView2Async()
        {
            if (webView.CoreWebView2 == null)
            {
                await webView.EnsureCoreWebView2Async();
            }
        }

        private async Task ClearFacebookCookiesAsync()
        {
            if (webView.CoreWebView2 == null) return;

            var mgr = webView.CoreWebView2.CookieManager;

            await DeleteAllFor(mgr, "https://facebook.com/");
            await DeleteAllFor(mgr, "https://www.facebook.com/");
            await DeleteAllFor(mgr, "https://m.facebook.com/");
        }

        private static async Task DeleteAllFor(CoreWebView2CookieManager mgr, string uri)
        {
            var cookies = await mgr.GetCookiesAsync(uri);
            foreach (var ck in cookies)
            {
                var del = mgr.CreateCookie(ck.Name, ck.Value, ck.Domain, ck.Path);
                del.Expires = ck.Expires;
                del.IsHttpOnly = ck.IsHttpOnly;
                del.IsSecure = ck.IsSecure;
                del.SameSite = ck.SameSite;
                mgr.DeleteCookie(del);
            }
        }

        private void Core_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (TryHandleRedirectUrl(e.Uri))
            {
                e.Cancel = true;
            }
        }

        private async void Core_SourceChanged(object sender, object e)
        {
            try
            {
                string hrefJson = await webView.ExecuteScriptAsync("location.href");
                string href = Unquote(hrefJson);
                TryHandleRedirectUrl(href);
            }
            catch
            {
            }
        }

        private static string Unquote(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString)) return jsonString;

            if (jsonString.Length >= 2 && jsonString[0] == '"' &&
                jsonString[jsonString.Length - 1] == '"')
            {
                string s = jsonString.Substring(1, jsonString.Length - 2);
                s = s.Replace("\\u0026", "&")
                     .Replace("\\/", "/");
                return s;
            }
            return jsonString;
        }

        private bool TryHandleRedirectUrl(string url)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(_redirect)) return false;

            if (!url.StartsWith(_redirect, StringComparison.OrdinalIgnoreCase))
                return false;

            Uri uri;
            try { uri = new Uri(url); }
            catch { return false; }

            string fragment = uri.Fragment;
            string query = uri.Query;

            if ((!string.IsNullOrEmpty(fragment) && fragment.Contains("error=access_denied")) ||
                (!string.IsNullOrEmpty(query) && query.Contains("error=access_denied")))
            {
                DialogResult = false;
                Close();
                return true;
            }

            string token = null;

            if (!string.IsNullOrEmpty(fragment))
            {
                var m = Regex.Match(fragment, @"access_token=([^&]+)");
                if (m.Success) token = Uri.UnescapeDataString(m.Groups[1].Value);
            }

            if (token == null && !string.IsNullOrEmpty(query))
            {
                var m = Regex.Match(query, @"access_token=([^&]+)");
                if (m.Success) token = Uri.UnescapeDataString(m.Groups[1].Value);
            }

            if (string.IsNullOrEmpty(token))
                return false;

            AccessToken = token;
            DialogResult = true;
            Close();
            return true;
        }

        private void FacebookLoginWindow_Closed(object sender, EventArgs e)
        {
            try
            {
                if (webView != null && webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.NavigationStarting -= Core_NavigationStarting;
                    webView.CoreWebView2.SourceChanged -= Core_SourceChanged;
                }
            }
            catch
            {
            }
        }
    }
}
