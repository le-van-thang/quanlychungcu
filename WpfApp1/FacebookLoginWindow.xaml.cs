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
        public string AccessToken { get; private set; }  // có thể null trước khi login xong

        private string _appId = "";
        private string _redirect = "";

        public FacebookLoginWindow()
        {
            InitializeComponent();
            Loaded += FacebookLoginWindow_Loaded;
            Closed += FacebookLoginWindow_Closed;
        }

        private async void FacebookLoginWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Đọc cấu hình
            _appId = ConfigurationManager.AppSettings["FacebookAppId"] ?? "";
            _redirect = ConfigurationManager.AppSettings["FacebookRedirectUri"] ?? "";

            if (string.IsNullOrWhiteSpace(_appId) || string.IsNullOrWhiteSpace(_redirect))
            {
                MessageBox.Show("Thiếu FacebookAppId hoặc FacebookRedirectUri trong App.config.");
                DialogResult = false;
                Close();
                return;
            }

            // Khởi tạo WebView2
            await EnsureWebView2Async();

            // Gắn sự kiện để bắt token
            webView.CoreWebView2.NavigationStarting += Core_NavigationStarting; // bắt query (không có fragment)
            webView.CoreWebView2.SourceChanged += Core_SourceChanged;           // đọc fragment qua JS

            // Điều hướng đến OAuth
            var scope = "email,public_profile";
            var url =
                "https://www.facebook.com/v19.0/dialog/oauth" +
                "?client_id=" + Uri.EscapeDataString(_appId) +
                "&redirect_uri=" + Uri.EscapeDataString(_redirect) +
                "&response_type=token" +
                "&scope=" + Uri.EscapeDataString(scope);

            webView.Source = new Uri(url);
        }

        private async Task EnsureWebView2Async()
        {
            if (webView.CoreWebView2 == null)
            {
                await webView.EnsureCoreWebView2Async();
                // Tuỳ chọn: cấu hình settings tại đây nếu cần
                // webView.CoreWebView2.Settings.UserAgent = webView.CoreWebView2.Settings.UserAgent + " WpfApp1";
            }
        }

        // 1) Bắt ở NavigationStarting (URI không chứa fragment #...)
        private void Core_NavigationStarting(object sender, CoreWebView2NavigationStartingEventArgs e)
        {
            if (TryHandleRedirectUrl(e.Uri))
            {
                e.Cancel = true; // đã lấy token -> không cần tải trang nữa
            }
        }

        // 2) Bắt ở SourceChanged: dùng JS lấy location.href (có fragment)
        private async void Core_SourceChanged(object sender, object e)
        {
            try
            {
                string hrefJson = await webView.ExecuteScriptAsync("location.href"); // trả về chuỗi JSON có ngoặc kép
                string href = Unquote(hrefJson);
                TryHandleRedirectUrl(href);
            }
            catch
            {
                // bỏ qua lỗi nhỏ của script/designer
            }
        }

        // Bóc bỏ dấu ngoặc kép JSON và các escape cơ bản (không dùng index ^1)
        private static string Unquote(string jsonString)
        {
            if (string.IsNullOrEmpty(jsonString)) return jsonString;

            if (jsonString.Length >= 2 && jsonString[0] == '\"' && jsonString[jsonString.Length - 1] == '\"')
            {
                string s = jsonString.Substring(1, jsonString.Length - 2);
                s = s.Replace("\\u0026", "&").Replace("\\/", "/");
                return s;
            }
            return jsonString;
        }

        // Trả true nếu lấy được access_token và đóng cửa sổ
        private bool TryHandleRedirectUrl(string url)
        {
            if (string.IsNullOrEmpty(url) || string.IsNullOrEmpty(_redirect)) return false;
            if (!url.StartsWith(_redirect, StringComparison.OrdinalIgnoreCase)) return false;

            Uri uri;
            try { uri = new Uri(url); }
            catch { return false; }

            string fragment = uri.Fragment; // "#access_token=..."
            string query = uri.Query;

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
            {
                // Có thể kiểm tra error=access_denied trong fragment/query để biết user hủy
                return false;
            }

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
            catch { }
        }
    }
}
