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
    public class RememberAndPasswordUiTests
    {
        [TestMethod]
        public void TC07_RememberMe_SaveAndReload_ShouldPrefill()
        {
            Assert.Fail(
                "TC07 TODO – Chưa tích hợp RememberStore thật để kiểm tra auto-fill Username/Password.");
        }

        [TestMethod]
        public void TC08_UncheckRemember_ShouldClearOnNextStart()
        {
            Assert.Fail(
                "TC08 TODO – Chưa viết test bỏ tick Remember rồi start lại app, textbox phải rỗng và file bị xóa.");
        }

        [TestMethod]
        public void TC09_PasswordToggle_ShouldMaskAndUnmask()
        {
            Assert.Fail(
                "TC09 TODO – Chưa viết test code-behind cho eye icon: mask/unmask PasswordBox.");
        }
    }
}
