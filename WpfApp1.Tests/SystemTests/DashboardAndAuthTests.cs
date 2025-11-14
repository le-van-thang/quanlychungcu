using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WpfApp1.Tests.SystemTests
{
    /// <summary>
    /// Bao phủ:
    /// - TC39–TC45: Dashboard + phân quyền menu (UI).
    /// Hiện tại chưa viết UI automation → cho FAIL để không bị N/A.
    /// </summary>
    [TestClass]
    public class DashboardAndAuthTests
    {
        [TestMethod]
        public void TC39_Dashboard_ApartmentCount_ShouldMatchDb()
        {
            Assert.Fail(
                "TC39 TODO – Chưa hiện thực lấy ApartmentCount trên HomeControl và so với COUNT(*) CanHoes.");
        }

        [TestMethod]
        public void TC40_Dashboard_ResidentCount_ShouldMatchDb()
        {
            Assert.Fail(
                "TC40 TODO – Chưa hiện thực lấy ResidentCount trên HomeControl và so với COUNT(*) CuDans.");
        }

        [TestMethod]
        public void TC41_Dashboard_ResidentCount_AfterAdd_ShouldIncrease()
        {
            Assert.Fail(
                "TC41 TODO – Chưa mô phỏng thêm cư dân từ CuDanControl rồi quay lại HomeControl kiểm tra count +1.");
        }

        [TestMethod]
        public void TC42_AiButton_Click_ShouldOpenAiWindow()
        {
            Assert.Fail(
                "TC42 TODO – Chưa simulate click nút AI trên HomeControl để kiểm tra AIWindow.ShowDialog().");
        }

        [TestMethod]
        public void TC43_Admin_ShouldSee_AdminMenus()
        {
            Assert.Fail(
                "TC43 TODO – Chưa login tài khoản VaiTro='Admin' và kiểm tra các nút/expander admin Visible/IsEnabled.");
        }

        [TestMethod]
        public void TC44_NormalUser_ShouldNotSee_AdminMenus()
        {
            Assert.Fail(
                "TC44 TODO – Chưa login user VaiTro='User' để kiểm tra các nút admin bị ẩn/disabled.");
        }

        [TestMethod]
        public void TC45_NormalUser_DirectOpenAdminScreen_ShouldBeBlocked()
        {
            Assert.Fail(
                "TC45 TODO – Chưa simulate mở TaiKhoanListControl bằng user thường và assert bị chặn/exception.");
        }
    }
}
