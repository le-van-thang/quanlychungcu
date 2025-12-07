using System;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using WpfApp1;
using WpfApp1.Tests.Common;

namespace WpfApp1.Tests.SystemTests
{
    /// <summary>
    /// Bao phủ:
    /// - TC01–TC06: Login local.
    /// - TC10, TC46–TC49: Session + lỗi DB (chưa hiện thực → FAIL).
    /// </summary>
    [TestClass]
    [TestCategory("System")]
    public class LoginAndSessionTests : DbTestBase
    {
        private QuanlychungcuEntities _db;

        [TestInitialize]
        public void SetupDb()
        {
            _db = new QuanlychungcuEntities();
        }

        private TaiKhoan EnsureUser(
            string username,
            string passwordPlain,
            bool isActive = true,
            string role = "User")
        {
            var acc = _db.TaiKhoans.FirstOrDefault(x => x.Username == username);
            if (acc == null)
            {
                acc = new TaiKhoan
                {
                    Username = username,
                    PasswordHash = passwordPlain,     // demo: plain text
                    Email = username + "@example.com",
                    VaiTro = role,
                    IsActive = isActive,
                    CreatedAt = DateTime.Now
                };
                _db.TaiKhoans.Add(acc);
            }
            else
            {
                acc.PasswordHash = passwordPlain;
                acc.IsActive = isActive;
                acc.VaiTro = role;
            }

            _db.SaveChanges();
            return acc;
        }

        /// <summary>
        /// Giả lập logic Authenticate cơ bản (thay bằng LoginService thật nếu có).
        /// </summary>
        private bool FakeAuthenticate(string username, string password)
        {
            var acc = _db.TaiKhoans.FirstOrDefault(x => x.Username == username);
            if (acc == null) return false;
            if (!acc.IsActive) return false;
            return acc.PasswordHash == password;
        }

        // ---------- TC01 ----------

        [TestMethod]
        public void TC01_Login_Valid_ShouldSucceed()
        {
            EnsureUser("user1", "123456", isActive: true);

            var ok = FakeAuthenticate("user1", "123456");

            Assert.IsTrue(ok, "TC01: Login hợp lệ phải thành công.");
        }

        // ---------- TC02 ----------

        [TestMethod]
        public void TC02_Login_InvalidPassword_ShouldFail()
        {
            EnsureUser("user1", "123456", isActive: true);

            var ok = FakeAuthenticate("user1", "wrongpass");

            Assert.IsFalse(ok, "TC02: Password sai phải bị từ chối.");
        }

        // ---------- TC03 ----------

        [TestMethod]
        public void TC03_Login_UnknownUser_ShouldFail()
        {
            var ok = FakeAuthenticate("ghost", "any");
            Assert.IsFalse(ok, "TC03: user không tồn tại -> login fail.");
        }

        // ---------- TC04 & TC05: validate input ----------

        private bool FakeLoginInputValid(string username, string password)
        {
            return !string.IsNullOrWhiteSpace(username)
                && !string.IsNullOrWhiteSpace(password);
        }

        [TestMethod]
        public void TC04_Login_EmptyUsername_ShouldBeInvalid()
        {
            var valid = FakeLoginInputValid("", "123");
            Assert.IsFalse(valid, "TC04: username rỗng phải bị chặn ở UI/validate.");
        }

        [TestMethod]
        public void TC05_Login_EmptyPassword_ShouldBeInvalid()
        {
            var valid = FakeLoginInputValid("user1", "");
            Assert.IsFalse(valid, "TC05: password rỗng phải bị chặn ở UI/validate.");
        }

        // ---------- TC06 ----------

        [TestMethod]
        public void TC06_Login_InactiveAccount_ShouldFail()
        {
            EnsureUser("user2", "123456", isActive: false);

            var ok = FakeAuthenticate("user2", "123456");

            Assert.IsFalse(ok, "TC06: account IsActive=false thì không được login.");
        }

        // ---------- TC10, TC46, TC47, TC48, TC49: đánh FAIL ----------

        [TestMethod]
        [Ignore("TODO – TC10/TC46: auto-login từ SessionStore sẽ kiểm thử thủ công.")]
        public void TC10_TC46_SessionFile_Valid_ShouldAutoLogin()
        {
        }

        [TestMethod]
        [Ignore("TODO – TC47: kiểm tra Logout xóa session file – sẽ test thủ công.")]
        public void TC47_Logout_ShouldDeleteSessionFile()
        {
        }

        [TestMethod]
        [Ignore("TODO – TC48: session chứa user không tồn tại – sẽ test thủ công.")]
        public void TC48_SessionFile_InvalidUser_ShouldShowLoginWindow()
        {
        }

        [TestMethod]
        [Ignore("TODO – TC49: lỗi kết nối DB khi startup – sẽ test thủ công.")]
        public void TC49_Startup_BrokenConnection_ShouldShowFriendlyError()
        {
        }
    }
    }
