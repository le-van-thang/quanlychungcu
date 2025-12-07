using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WpfApp1.Tests.SystemTests
{
    /// <summary>
    /// Bao phủ:
    /// - TC39–TC45: Dashboard + phân quyền menu (UI).
    /// Hiện tại chưa viết UI automation → cho FAIL để không bị N/A.
    /// </summary>
    [TestClass]
    [TestCategory("System")]
    public class DashboardAndAuthTests
    {

        [TestMethod]
        [Ignore("TODO – TC39 sẽ kiểm thử thủ công.")]
        public void TC39_Dashboard_ApartmentCount_ShouldMatchDb() { }

        [TestMethod]
        [Ignore("TODO – TC40 sẽ kiểm thử thủ công.")]
        public void TC40_Dashboard_ResidentCount_ShouldMatchDb() { }

        [TestMethod]
        [Ignore("TODO – TC41 sẽ kiểm thử thủ công.")]
        public void TC41_Dashboard_ResidentCount_AfterAdd_ShouldIncrease() { }

        [TestMethod]
        [Ignore("TODO – TC42 sẽ kiểm thử thủ công.")]
        public void TC42_AiButton_Click_ShouldOpenAiWindow() { }

        [TestMethod]
        [Ignore("TODO – TC43 sẽ kiểm thử thủ công.")]
        public void TC43_Admin_ShouldSee_AdminMenus() { }

        [TestMethod]
        [Ignore("TODO – TC44 sẽ kiểm thử thủ công.")]
        public void TC44_NormalUser_ShouldNotSee_AdminMenus() { }

        [TestMethod]
        [Ignore("TODO – TC45 sẽ kiểm thử thủ công.")]
        public void TC45_NormalUser_DirectOpenAdminScreen_ShouldBeBlocked() { }

}
}
