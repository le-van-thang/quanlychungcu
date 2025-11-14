using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WpfApp1.Tests.Integration
{
    /// <summary>
    /// Bao phủ:
    /// - TC11–TC13: Google OAuth.
    /// - TC14 & TC18: UpsertExternalUser logic.
    /// - TC15–TC17: Facebook OAuth.
    /// </summary>
    [TestClass]
    public class OAuthTests
    {
        [TestMethod]
        public void TC11_Google_FirstLogin_ShouldCreateNewUserAndOAuthRows()
        {
            Assert.Fail(
                "TC11 TODO – Chưa hiện thực mô phỏng Google login lần đầu & kiểm tra TaiKhoan/OAuthAccount/OAuthToken.");
        }

        [TestMethod]
        public void TC12_Google_Relogin_ShouldReuseExistingTaiKhoan()
        {
            Assert.Fail(
                "TC12 TODO – Chưa hiện thực seed TaiKhoan+OAuthAccount và kiểm tra login Google không tạo user mới.");
        }

        [TestMethod]
        public void TC13_Google_LoginCanceled_ShouldStayOnLogin_NoDbChanges()
        {
            Assert.Fail(
                "TC13 TODO – Chưa mô phỏng người dùng cancel popup Google & assert DB không đổi.");
        }

        [TestMethod]
        public void TC14_UpsertExternalUser_NullRefreshToken_ShouldKeepOld()
        {
            Assert.Fail(
                "TC14 TODO – Chưa hiện thực UpsertExternalUserAsync với RefreshToken=null để kiểm tra giữ RefreshToken cũ.");
        }

        [TestMethod]
        public void TC15_Facebook_Login_Success_ShouldCreateOrUpdateRows()
        {
            Assert.Fail(
                "TC15 TODO – Chưa hiện thực login Facebook thành công & kiểm tra tạo/cập nhật TaiKhoan/OAuthAccount/OAuthToken.");
        }

        [TestMethod]
        public void TC16_Facebook_LoginCanceled_ShouldReturnFalseAndNoDbChanges()
        {
            Assert.Fail(
                "TC16 TODO – Chưa mô phỏng đóng popup Facebook & assert DB không đổi.");
        }

        [TestMethod]
        public void TC17_Facebook_EmailMatchExistingTaiKhoan_ShouldLinkExistingUser()
        {
            Assert.Fail(
                "TC17 TODO – Chưa seed TaiKhoan email trùng và kiểm tra OAuthAccount link vào user sẵn có.");
        }

        [TestMethod]
        public void TC18_UpsertExternalUser_NoProvider_ShouldAutoCreateProvider()
        {
            Assert.Fail(
                "TC18 TODO – Chưa mô phỏng ProviderName='Google' khi bảng OAuthProvider chưa có, để kiểm tra tự tạo Provider.");
        }
    }
}
