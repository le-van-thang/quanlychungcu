using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WpfApp1.Tests.Unit
{
    /// <summary>
    /// Bao phủ:
    /// - TC07: Remember-me lưu credential.
    /// - TC08: Bỏ tick Remember xóa credential.
    /// - TC09: Toggle eye icon hiển thị/mask password.
    /// Hiện chưa viết test UI → đánh FAIL để không bị N/A.
    /// </summary>
    [TestClass]
    [TestCategory("Unit")]
    public class RememberAndPasswordUiTests
    {
        [TestMethod]
        [Ignore("TODO – TC07 kiểm thử RememberStore bằng tay.")]
        public void TC07_RememberMe_SaveAndReload_ShouldPrefill() { }

        [TestMethod]
        [Ignore("TODO – TC08 kiểm thử bỏ tick Remember bằng tay.")]
        public void TC08_UncheckRemember_ShouldClearOnNextStart() { }

        [TestMethod]
        [Ignore("TODO – TC09 kiểm thử eye icon bằng tay.")]
        public void TC09_PasswordToggle_ShouldMaskAndUnmask() { }
    }
}
