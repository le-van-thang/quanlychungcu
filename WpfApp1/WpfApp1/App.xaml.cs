using System.Linq;
using System.Windows;

namespace WpfApp1
{
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            TaiKhoan tk = null;

            var id = SessionStore.Load();
            if (id.HasValue)
            {
                try
                {
                    using (var db = new QuanlychungcuEntities())
                    {
                        tk = db.TaiKhoans
                               .FirstOrDefault(x => x.TaiKhoanID == id.Value && x.IsActive == true);
                    }
                }
                catch
                {
                    tk = null;
                }
            }

            if (tk != null)
            {
                var main = new DashboardWindow(tk);
                MainWindow = main;
                main.Show();
            }
            else
            {
                var login = new LoginWindow();
                MainWindow = login;
                login.Show();
            }
        }
    }
}
