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
        [Ignore("TODO – TC11 sẽ kiểm thử thủ công, chưa tự động hoá.")]
        public void TC11_Google_FirstLogin_ShouldCreateNewUserAndOAuthRows()
        {
            // Chỉ để mapping với Test Case Excel.
        }

        [TestMethod]
        [Ignore("TODO – TC12 sẽ kiểm thử thủ công, chưa tự động hoá.")]
        public void TC12_Google_Relogin_ShouldReuseExistingTaiKhoan()
        {
        }

        [TestMethod]
        [Ignore("TODO – TC13 sẽ kiểm thử thủ công, chưa tự động hoá.")]
        public void TC13_Google_LoginCanceled_ShouldStayOnLogin_NoDbChanges()
        {
        }

        [TestMethod]
        [Ignore("TODO – TC14 sẽ kiểm thử thủ công, chưa tự động hoá.")]
        public void TC14_UpsertExternalUser_NullRefreshToken_ShouldKeepOld()
        {
        }

        [TestMethod]
        [Ignore("TODO – TC15 sẽ kiểm thử thủ công, chưa tự động hoá.")]
        public void TC15_Facebook_Login_Success_ShouldCreateOrUpdateRows()
        {
        }

        [TestMethod]
        [Ignore("TODO – TC16 sẽ kiểm thử thủ công, chưa tự động hoá.")]
        public void TC16_Facebook_LoginCanceled_ShouldReturnFalseAndNoDbChanges()
        {
        }

        [TestMethod]
        [Ignore("TODO – TC17 sẽ kiểm thử thủ công, chưa tự động hoá.")]
        public void TC17_Facebook_EmailMatchExistingTaiKhoan_ShouldLinkExistingUser()
        {
        }

        [TestMethod]
        [Ignore("TODO – TC18 sẽ kiểm thử thủ công, chưa tự động hoá.")]
        public void TC18_UpsertExternalUser_NoProvider_ShouldAutoCreateProvider()
        {
        }
}
}
