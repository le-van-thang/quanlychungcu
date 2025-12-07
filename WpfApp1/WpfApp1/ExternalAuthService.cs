using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Oauth2.v2;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Newtonsoft.Json.Linq;
using System;
using System.Configuration;
using System.Data.Entity.Core.EntityClient;
using System.Data.SqlTypes;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using WpfApp1.Models;

namespace WpfApp1.Services
{
    public class ExternalAuthService
    {
        private readonly string _connStr;

        public ExternalAuthService()
        {
            var efConn = ConfigurationManager.ConnectionStrings["QuanlychungcuEntities"];
            if (efConn == null)
                throw new InvalidOperationException("Missing connection string 'QuanlychungcuEntities' in App.config.");

            _connStr = new EntityConnectionStringBuilder(efConn.ConnectionString).ProviderConnectionString;
            if (string.IsNullOrWhiteSpace(_connStr))
                throw new InvalidOperationException("Provider connection string is empty. Check 'QuanlychungcuEntities'.");
        }

        private static DateTime? ClampToSqlDateTime(DateTime? dt)
        {
            if (!dt.HasValue) return null;
            var v = dt.Value;
            var min = (DateTime)SqlDateTime.MinValue;
            var max = (DateTime)SqlDateTime.MaxValue;
            if (v < min) v = min;
            if (v > max) v = max;
            return v;
        }

        private static DateTime ClampToSqlDateTimeNonNull(DateTime dt)
        {
            var min = (DateTime)SqlDateTime.MinValue;
            var max = (DateTime)SqlDateTime.MaxValue;
            if (dt < min) return min;
            if (dt > max) return max;
            return dt;
        }

        // GOOGLE
        public async Task<OAuthUserInfo> SignInWithGoogleAsync()
        {
            var clientId = ConfigurationManager.AppSettings["GoogleClientId"];
            var clientSecret = ConfigurationManager.AppSettings["GoogleClientSecret"];
            if (string.IsNullOrWhiteSpace(clientId))
                throw new InvalidOperationException("Thiếu GoogleClientId trong App.config.");

            var scopes = new[] { "openid", "email", "profile" };
            var store = new FileDataStore("GoogleOAuth_" + clientId, true);

            // luôn xoá token cũ để được chọn account mới
            await store.DeleteAsync<TokenResponse>("user");

            var cred = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret ?? string.Empty },
                scopes,
                "user",
                CancellationToken.None,
                store);

            var svc = new Oauth2Service(new BaseClientService.Initializer
            {
                HttpClientInitializer = cred,
                ApplicationName = "CondoLoginApp"
            });

            try
            {
                var me = await svc.Userinfo.Get().ExecuteAsync();

                var issued = (cred.Token.IssuedUtc == default(DateTime) ? DateTime.UtcNow : cred.Token.IssuedUtc);
                var exp = issued.AddSeconds(cred.Token.ExpiresInSeconds ?? 3600).ToLocalTime();

                return new OAuthUserInfo
                {
                    ProviderName = "Google",
                    ProviderUserId = me.Id,
                    Email = me.Email,
                    FullName = me.Name,
                    AccessToken = cred.Token.AccessToken,
                    RefreshToken = cred.Token.RefreshToken,
                    ExpiresAt = ClampToSqlDateTime(exp)
                };
            }
            catch (Google.GoogleApiException ex) when (ex.HttpStatusCode == HttpStatusCode.Unauthorized)
            {
                await store.DeleteAsync<TokenResponse>("user");

                var cred2 = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                    new ClientSecrets { ClientId = clientId, ClientSecret = clientSecret ?? string.Empty },
                    scopes,
                    "user",
                    CancellationToken.None,
                    store);

                var svc2 = new Oauth2Service(new BaseClientService.Initializer
                {
                    HttpClientInitializer = cred2,
                    ApplicationName = "CondoLoginApp"
                });

                var me2 = await svc2.Userinfo.Get().ExecuteAsync();

                var issued2 = (cred2.Token.IssuedUtc == default(DateTime) ? DateTime.UtcNow : cred2.Token.IssuedUtc);
                var exp2 = issued2.AddSeconds(cred2.Token.ExpiresInSeconds ?? 3600).ToLocalTime();

                return new OAuthUserInfo
                {
                    ProviderName = "Google",
                    ProviderUserId = me2.Id,
                    Email = me2.Email,
                    FullName = me2.Name,
                    AccessToken = cred2.Token.AccessToken,
                    RefreshToken = cred2.Token.RefreshToken,
                    ExpiresAt = ClampToSqlDateTime(exp2)
                };
            }
        }

        // FACEBOOK
        public async Task<OAuthUserInfo> GetFacebookUserAsync(string accessToken)
        {
            if (string.IsNullOrWhiteSpace(accessToken))
                throw new ArgumentException("accessToken is required.", nameof(accessToken));

            var url = $"https://graph.facebook.com/me?fields=id,name,email&access_token={Uri.EscapeDataString(accessToken)}";

            using (var http = new HttpClient())
            {
                var json = await http.GetStringAsync(url);
                var obj = JObject.Parse(json);

                return new OAuthUserInfo
                {
                    ProviderName = "Facebook",
                    ProviderUserId = (string)obj["id"],
                    Email = (string)obj["email"],
                    FullName = (string)obj["name"],
                    AccessToken = accessToken
                };
            }
        }

        // Lưu / cập nhật user + OAuthAccount + Token trong DB
        public async Task<TaiKhoan> UpsertExternalUserAsync(OAuthUserInfo u, string defaultRole = "User")
        {
            if (u == null) throw new ArgumentNullException(nameof(u));

            using (var db = new QuanlychungcuEntities())
            {
                var provider = db.OAuthProviders.FirstOrDefault(p => p.Name == u.ProviderName);
                if (provider == null)
                {
                    provider = new OAuthProvider { Name = u.ProviderName, DisplayName = u.ProviderName, IsActive = true };
                    db.OAuthProviders.Add(provider);
                    await db.SaveChangesAsync();
                }

                var oaByUser = db.OAuthAccounts
                                 .Include("TaiKhoan")
                                 .FirstOrDefault(x => x.ProviderID == provider.ProviderID
                                                   && x.ProviderUserId == u.ProviderUserId);

                TaiKhoan tk;

                if (oaByUser != null)
                {
                    tk = oaByUser.TaiKhoan;

                    oaByUser.Email = u.Email ?? oaByUser.Email;
                    oaByUser.FullName = u.FullName ?? oaByUser.FullName;
                    oaByUser.LinkedAt = ClampToSqlDateTimeNonNull(DateTime.Now);
                    await db.SaveChangesAsync();

                    var tok = db.OAuthTokens.FirstOrDefault(t => t.OAuthAccountID == oaByUser.OAuthAccountID);
                    if (tok == null)
                    {
                        tok = new OAuthToken
                        {
                            OAuthAccountID = oaByUser.OAuthAccountID,
                            AccessToken = u.AccessToken,
                            RefreshToken = u.RefreshToken,
                            ExpiresAt = ClampToSqlDateTime(u.ExpiresAt),
                            Scope = "openid email profile",
                            CreatedAt = ClampToSqlDateTimeNonNull(DateTime.UtcNow)
                        };
                        db.OAuthTokens.Add(tok);
                    }
                    else
                    {
                        tok.AccessToken = u.AccessToken;
                        if (!string.IsNullOrEmpty(u.RefreshToken)) tok.RefreshToken = u.RefreshToken;
                        tok.ExpiresAt = ClampToSqlDateTime(u.ExpiresAt);
                        tok.Scope = "openid email profile";
                    }

                    await db.SaveChangesAsync();
                    return tk;
                }

                tk = null;
                if (!string.IsNullOrEmpty(u.Email))
                {
                    var em = u.Email.Trim().ToLowerInvariant();
                    tk = db.TaiKhoans.FirstOrDefault(x => (x.Email ?? "").ToLower() == em);
                }

                if (tk == null)
                {
                    string username = !string.IsNullOrEmpty(u.Email)
                                        ? u.Email.Trim().ToLowerInvariant()
                                        : $"Facebook:{(u.ProviderUserId ?? Guid.NewGuid().ToString("N"))}";
                    username = username.Length > 255 ? username.Substring(0, 255) : username;

                    string baseName = username;
                    int suffix = 1;
                    while (db.TaiKhoans.Any(x => x.Username == username))
                    {
                        var add = "_" + suffix++;
                        var take = Math.Max(1, 255 - add.Length);
                        username = baseName.Substring(0, Math.Min(baseName.Length, take)) + add;
                    }

                    tk = new TaiKhoan
                    {
                        Username = username,
                        Email = u.Email,
                        PasswordHash = null,
                        VaiTro = string.IsNullOrWhiteSpace(defaultRole) ? "User" : defaultRole,
                        IsActive = true
                    };
                    db.TaiKhoans.Add(tk);
                    await db.SaveChangesAsync();
                }

                var oa = new OAuthAccount
                {
                    ProviderID = provider.ProviderID,
                    TaiKhoanID = tk.TaiKhoanID,
                    ProviderUserId = u.ProviderUserId,
                    Email = u.Email,
                    FullName = u.FullName,
                    LinkedAt = ClampToSqlDateTimeNonNull(DateTime.Now)
                };
                db.OAuthAccounts.Add(oa);
                await db.SaveChangesAsync();

                var tokNew = new OAuthToken
                {
                    OAuthAccountID = oa.OAuthAccountID,
                    AccessToken = u.AccessToken,
                    RefreshToken = u.RefreshToken,
                    ExpiresAt = ClampToSqlDateTime(u.ExpiresAt),
                    Scope = "openid email profile",
                    CreatedAt = ClampToSqlDateTimeNonNull(DateTime.UtcNow)
                };
                db.OAuthTokens.Add(tokNew);
                await db.SaveChangesAsync();

                return tk;
            }
        }
    }
}
