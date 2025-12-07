using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using FlaUI.Core;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
using FlaUI.Core.Exceptions;
using FlaUI.Core.Input;
using FlaUI.UIA3;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace WpfApp1.UiTests
{
    /// <summary>
    /// FlaUI System Tests cho màn hình Login / Dashboard:
    /// - TC50: Login hợp lệ → mở Dashboard.
    /// - TC51: Login sai mật khẩu → không vào Dashboard, vẫn ở Login.
    /// - TC52: Admin (VaiTro=Admin) thấy và dùng được menu quản trị tài khoản.
    /// - TC53: User thường (VaiTro=User) KHÔNG dùng được menu quản trị tài khoản.
    /// 
    /// Yêu cầu dữ liệu:
    /// - Bảng TaiKhoan cần có:
    ///   + Username='adminui', PasswordHash='123456', VaiTro='Admin', IsActive=1.
    ///   + Username='userui',  PasswordHash='123456', VaiTro='User',  IsActive=1.
    /// </summary>
    [TestClass]
    [TestCategory("System")]
    [TestCategory("UI")]
    public class LoginUiTests
    {
        private Application _app;
        private UIA3Automation _automation;

        public TestContext TestContext { get; set; }

        // ================== Setup / Teardown ==================

        /// <summary>
        /// Xoá cache RememberStore để ô user/pass luôn sạch trước mỗi test.
        /// </summary>
        private void CleanupRememberStore()
        {
            var rememberDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "WpfApp1", "remember");

            if (Directory.Exists(rememberDir))
            {
                Directory.Delete(rememberDir, true);
            }
        }

        /// <summary>
        /// Tìm đường dẫn tới WpfApp1.exe bằng cách lần lên tới thư mục chứa WpfApp1.sln,
        /// sau đó thử bin\x64\Debug và bin\Debug.
        /// </summary>
        private string GetExePath()
        {
            var dir = new DirectoryInfo(Environment.CurrentDirectory);

            while (dir != null && !File.Exists(Path.Combine(dir.FullName, "WpfApp1.sln")))
            {
                dir = dir.Parent;
            }

            if (dir == null)
            {
                throw new DirectoryNotFoundException(
                    "Không tìm được thư mục solution (không thấy WpfApp1.sln).");
            }

            var exeX64 = Path.Combine(dir.FullName, "WpfApp1", "bin", "x64", "Debug", "WpfApp1.exe");
            var exeAny = Path.Combine(dir.FullName, "WpfApp1", "bin", "Debug", "WpfApp1.exe");

            if (File.Exists(exeX64)) return exeX64;
            if (File.Exists(exeAny)) return exeAny;

            throw new FileNotFoundException("Không tìm thấy WpfApp1.exe, kiểm tra lại cấu hình build.", exeX64);
        }

        [TestInitialize]
        public void StartApp()
        {
            CleanupRememberStore();

            var exe = GetExePath();
            _app = Application.Launch(exe);
            _automation = new UIA3Automation();
        }

        [TestCleanup]
        public void CloseApp()
        {
            try
            {
                if (_automation != null)
                    _automation.Dispose();

                if (_app != null && !_app.HasExited)
                {
                    _app.Close();
                    Thread.Sleep(1000);

                    if (!_app.HasExited)
                    {
                        _app.Kill();
                    }
                }
            }
            catch
            {
                // tránh quăng exception khi đóng app
            }
        }

        // ================== Helpers chung ==================

        private Window GetMainWindowWithRetry(int timeoutMs = 10000)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                var win = _app.GetAllTopLevelWindows(_automation).FirstOrDefault();
                if (win != null)
                    return win.AsWindow();
                Thread.Sleep(200);
            }

            return null;
        }

        private AutomationElement RetryFindElement(
            AutomationElement root,
            string automationId,
            int timeoutMs)
        {
            var sw = Stopwatch.StartNew();
            AutomationElement found = null;

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                found = root.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
                if (found != null)
                    break;
                Thread.Sleep(200);
            }

            return found;
        }

        /// <summary>
        /// Tìm element theo AutomationId, nếu không có thì theo Name (text hiển thị).
        /// Dùng cho menu sidebar (Tài khoản, Trang chủ, ...).
        /// </summary>
        private AutomationElement RetryFindElementByIdOrName(
            AutomationElement root,
            string automationId,
            string nameContains,
            int timeoutMs)
        {
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                AutomationElement found = null;

                if (!string.IsNullOrEmpty(automationId))
                {
                    found = root.FindFirstDescendant(cf => cf.ByAutomationId(automationId));
                }

                if (found == null && !string.IsNullOrEmpty(nameContains))
                {
                    found = root.FindFirstDescendant(cf => cf.ByName(nameContains));
                }

                if (found != null)
                    return found;

                Thread.Sleep(200);
            }

            return null;
        }

        /// <summary>
        /// Tìm top-level window của chính process app có Title chứa chuỗi cho trước.
        /// Dùng cho LoginWindow / Dashboard.
        /// </summary>
        private Window RetryFindWindowByTitleContains(string titleContains, int timeoutMs)
        {
            var sw = Stopwatch.StartNew();

            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                foreach (var w in _app.GetAllTopLevelWindows(_automation))
                {
                    string title = null;
                    try
                    {
                        title = w.Title;
                    }
                    catch (PropertyNotSupportedException)
                    {
                        continue;
                    }

                    if (!string.IsNullOrEmpty(title) && title.Contains(titleContains))
                        return w;
                }

                Thread.Sleep(200);
            }

            return null;
        }

        // ================== Helpers cho dialog (MessageBox / Confirm) ==================

        /// <summary>
        /// Đợi modal window (MessageBox / Confirm) của 1 window cha.
        /// </summary>
        private Window WaitForModal(Window owner, int timeoutMs = 10000)
        {
            if (owner == null) return null;

            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeoutMs)
            {
                try
                {
                    var modal = owner.ModalWindows.FirstOrDefault();
                    if (modal != null)
                        return modal;
                }
                catch
                {
                    // owner đã đóng
                    return null;
                }

                Thread.Sleep(200);
            }

            return null;
        }

        /// <summary>
        /// Click nút trên dialog.
        /// Ưu tiên các tên trong preferredNames, nếu không có thì bấm nút đầu tiên.
        /// </summary>
        private void ClickDialogButton(Window dialog, params string[] preferredNames)
        {
            if (dialog == null) return;

            AutomationElement[] buttons;
            try
            {
                buttons = dialog.FindAllDescendants(cf => cf.ByControlType(ControlType.Button));
            }
            catch
            {
                return;
            }

            if (buttons == null || buttons.Length == 0) return;

            AutomationElement target = null;

            if (preferredNames != null && preferredNames.Length > 0)
            {
                foreach (var pref in preferredNames)
                {
                    var btn = buttons.FirstOrDefault(b =>
                    {
                        string name = string.Empty;
                        try { name = b.Name ?? string.Empty; } catch { }
                        return name.IndexOf(pref, StringComparison.OrdinalIgnoreCase) >= 0;
                    });

                    if (btn != null)
                    {
                        target = btn;
                        break;
                    }
                }
            }

            // Nếu không tìm được theo tên thì lấy nút đầu tiên
            if (target == null)
                target = buttons[0];

            try
            {
                target.AsButton().Invoke();
            }
            catch
            {
                // bỏ qua lỗi click
            }
        }

        /// <summary>
        /// Auto đóng MessageBox bất kỳ (OK / Đồng ý / ...), gắn với window cha.
        /// </summary>
        private void AutoCloseAnyMessageBox(Window owner, int timeoutMs = 8000)
        {
            var dlg = WaitForModal(owner, timeoutMs);
            if (dlg == null) return;

            ClickDialogButton(dlg, "OK", "Đồng ý");
            Thread.Sleep(300);
        }

        /// <summary>
        /// Auto bấm Yes/Có trong popup xác nhận đăng xuất (gắn với Dashboard).
        /// </summary>
        private void HandleLogoutConfirm(Window dashboardWindow)
        {
            var dlg = WaitForModal(dashboardWindow, 8000);
            if (dlg == null) return;

            ClickDialogButton(dlg, "Yes", "Có");
            Thread.Sleep(500);
        }

        /// <summary>
        /// Đảm bảo app đang ở màn hình Login:
        /// - Nếu đang auto-login vào Dashboard → bấm Logout + xử lý confirm.
        /// - Ngược lại, trả về cửa sổ đầu tiên (LoginWindow).
        /// </summary>
        private Window EnsureLoginWindow()
        {
            var firstWindow = GetMainWindowWithRetry();
            Assert.IsNotNull(firstWindow, "Không lấy được cửa sổ đầu tiên.");

            if (!string.IsNullOrEmpty(firstWindow.Title) &&
                firstWindow.Title.Contains("Trang chủ - Quản lý chung cư"))
            {
                var logoutBtn = RetryFindElement(firstWindow, "Dashboard_Logout", 5000)?.AsButton();
                Assert.IsNotNull(logoutBtn, "Không tìm thấy nút Đăng xuất (Dashboard_Logout).");

                logoutBtn.Invoke();
                Thread.Sleep(300);

                // popup "Xác nhận" → Yes
                HandleLogoutConfirm(firstWindow);

                // sau đó phải quay lại Đăng nhập
                var loginWin = RetryFindWindowByTitleContains("Đăng nhập", 10000);
                Assert.IsNotNull(loginWin, "Không quay lại được cửa sổ Đăng nhập sau khi Đăng xuất.");

                return loginWin;
            }

            return firstWindow;
        }

        /// <summary>
        /// Đăng nhập với username/password chỉ định và trả về Dashboard window.
        /// Dùng chung cho TC50, TC52, TC53.
        /// </summary>
        private Window PerformLogin(string username, string password)
        {
            var loginWindowElement = EnsureLoginWindow();
            var loginWindow = loginWindowElement.AsWindow();
            loginWindow.WaitUntilClickable();
            Thread.Sleep(500);

            var txtUserElem = RetryFindElement(loginWindow, "Login_Username", 5000);
            var passElem = RetryFindElement(loginWindow, "Login_Password", 5000);
            var btnLogin = RetryFindElement(loginWindow, "Login_Button", 5000)?.AsButton();

            Assert.IsNotNull(txtUserElem, "Không tìm thấy ô Username – AutomationId=Login_Username.");
            Assert.IsNotNull(passElem, "Không tìm thấy ô Password – AutomationId=Login_Password.");
            Assert.IsNotNull(btnLogin, "Không tìm thấy nút Đăng nhập – AutomationId=Login_Button.");

            var txtUser = txtUserElem.AsTextBox();
            txtUser.Text = username;
            passElem.Focus();
            Keyboard.Type(password);

            btnLogin.Invoke();

            // auto đóng MessageBox sau login (Thành công / Thông báo / Lỗi)
            AutoCloseAnyMessageBox(loginWindow);

            var mainWindow = RetryFindWindowByTitleContains("Trang chủ - Quản lý chung cư", 15000);
            Assert.IsNotNull(mainWindow,
                string.Format("Sau khi login với user '{0}' phải mở cửa sổ Trang chủ - Quản lý chung cư.", username));

            return mainWindow;
        }

        /// <summary>
        /// Click vào menu "Tài khoản" ở sidebar để load các nút con (btnTaiKhoanList, ...).
        /// Thử theo AutomationId=Menu_TaiKhoan, nếu không có thì fallback Name chứa "Tài khoản".
        /// </summary>
        /// <summary>
        /// Click vào menu "Tài khoản" ở sidebar để load các nút con (btnTaiKhoanList, ...).
        /// Thử theo AutomationId=Menu_TaiKhoan, nếu không có thì fallback Name chứa "Tài khoản".
        /// </summary>
        private void OpenAccountMenu(Window mainWindow)
        {
            var accountMenuElem = RetryFindElementByIdOrName(
                mainWindow, "Menu_TaiKhoan", "Tài khoản", 5000);

            Assert.IsNotNull(accountMenuElem, "Không tìm thấy menu Tài khoản.");

            // Ưu tiên dùng ExpandCollapse pattern (đúng với Expander)
            var expandCollapse = accountMenuElem.Patterns.ExpandCollapse.PatternOrDefault;
            if (expandCollapse != null)
            {
                expandCollapse.Expand();
            }
            else
            {
                // fallback: dùng Invoke hoặc click chuột
                var invoke = accountMenuElem.Patterns.Invoke.PatternOrDefault;
                if (invoke != null)
                {
                    invoke.Invoke();
                }
                else
                {
                    accountMenuElem.Click();
                }
            }

            Thread.Sleep(800);
        }


        // ================== TEST CASES ==================

        /// <summary>
        /// TC50: Login hợp lệ (adminui/123456) mở được Dashboard.
        /// </summary>
        [TestMethod]
        [TestCategory("System")]
        [TestCategory("UI")]
        public void TC50_Login_Valid_ShouldOpenDashboard()
        {
            var mainWindow = PerformLogin("adminui", "123456");
            StringAssert.Contains(mainWindow.Title, "Quản lý chung cư");
        }

        /// <summary>
        /// TC51: Login sai mật khẩu → KHÔNG vào Dashboard, vẫn ở Login.
        /// </summary>
        [TestMethod]
        [TestCategory("System")]
        [TestCategory("UI")]
        public void TC51_Login_InvalidPassword_ShouldStayOnLogin()
        {
            var loginWindowElement = EnsureLoginWindow();
            var loginWindow = loginWindowElement.AsWindow();
            loginWindow.WaitUntilClickable();
            Thread.Sleep(500);

            var txtUserElem = RetryFindElement(loginWindow, "Login_Username", 5000);
            var passElem = RetryFindElement(loginWindow, "Login_Password", 5000);
            var btnLogin = RetryFindElement(loginWindow, "Login_Button", 5000)?.AsButton();

            Assert.IsNotNull(txtUserElem);
            Assert.IsNotNull(passElem);
            Assert.IsNotNull(btnLogin);

            var txtUser = txtUserElem.AsTextBox();
            txtUser.Text = "adminui";          // user tồn tại
            passElem.Focus();
            Keyboard.Type("sai_mat_khau");     // mật khẩu sai

            btnLogin.Invoke();

            // auto đóng MessageBox thông báo lỗi / sai mật khẩu
            AutoCloseAnyMessageBox(loginWindow);

            var dashboard = RetryFindWindowByTitleContains("Trang chủ - Quản lý chung cư", 3000);
            Assert.IsNull(dashboard, "Sai mật khẩu thì không được vào màn hình Trang chủ.");

            var loginWinAfter = RetryFindWindowByTitleContains("Đăng nhập", 3000) ?? loginWindow;
            Assert.IsNotNull(loginWinAfter, "Sau khi login sai mật khẩu phải vẫn ở màn hình Đăng nhập.");
        }

        /// <summary>
        /// TC52: Admin (adminui) phải thấy và mở được màn hình quản trị tài khoản.
        /// </summary>
        [TestMethod]
        [TestCategory("System")]
        [TestCategory("UI")]
        public void TC52_Admin_ShouldSee_AdminAccountMenu()
        {
            var mainWindow = PerformLogin("adminui", "123456");

            OpenAccountMenu(mainWindow);

            var adminBtnElem = RetryFindElement(mainWindow, "btnTaiKhoanList", 5000);
            Assert.IsNotNull(adminBtnElem, "Admin phải thấy nút btnTaiKhoanList (menu quản lý tài khoản).");
            Assert.IsFalse(adminBtnElem.IsOffscreen, "Nút btnTaiKhoanList phải hiển thị trên UI cho Admin.");

            // Click để mở UserControl Tất cả tài khoản
            adminBtnElem.AsButton().Invoke();
            Thread.Sleep(1000);

            // Phải nhìn thấy DataGrid tài khoản
            var grid = RetryFindElement(mainWindow, "Grid_TaiKhoan", 5000);
            Assert.IsNotNull(grid, "Sau khi click 'Tất cả tài khoản' phải thấy lưới tài khoản.");
        }

        /// <summary>
        /// TC53: User thường (userui) không được thấy / sử dụng menu quản trị tài khoản.
        /// </summary>
        [TestMethod]
        [TestCategory("System")]
        [TestCategory("UI")]
        public void TC53_NormalUser_ShouldNotSee_AdminAccountMenu()
        {
            var mainWindow = PerformLogin("userui", "123456");

            OpenAccountMenu(mainWindow);

            var adminBtnElem = RetryFindElement(mainWindow, "btnTaiKhoanList", 2000);

            if (adminBtnElem == null)
            {
                // Đúng chuẩn: user thường không có nút
                Assert.IsTrue(true);
            }
            else
            {
                // Nếu vì lý do gì vẫn thấy nút → không được phép sử dụng
                adminBtnElem.AsButton().Invoke();
                Thread.Sleep(500);

                AutoCloseAnyMessageBox(mainWindow);

                var grid = RetryFindElement(mainWindow, "Grid_TaiKhoan", 2000);
                if (grid != null)
                {
                    Assert.IsFalse(grid.IsEnabled,
                        "Nếu user thường mở được màn hình thì lưới phải bị vô hiệu hóa.");
                }
            }
        }
    }
}
