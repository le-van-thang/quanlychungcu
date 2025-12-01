using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class ActivityLogControl : UserControl
    {
        private TaiKhoan _currentUser;

        // Dùng cho DESIGN-TIME (mở XAML trong VS) + runtime mini log
        public ActivityLogControl()
        {
            InitializeComponent();

            // TUYỆT ĐỐI KHÔNG ĐỤNG DB KHI DESIGN
            if (DesignerProperties.GetIsInDesignMode(this))
            {
                // có thể gán demo rỗng cho đỡ lỗi
                dgLogs.ItemsSource = new List<ActivityLog>();
            }
            else
            {
                LoadData();
            }
        }

        // Dùng khi chạy app full-screen, truyền user vào
        public ActivityLogControl(TaiKhoan user) : this()
        {
            _currentUser = user;

            if (!DesignerProperties.GetIsInDesignMode(this))
            {
                LoadData();
            }
        }

        private void LoadData()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var query = db.ActivityLogs.AsQueryable();

                // User thường: không được xem
                if (RoleHelper.IsUser(_currentUser))
                {
                    dgLogs.ItemsSource = null;
                    return;
                }

                // Manager: chỉ log nghiệp vụ
                if (RoleHelper.IsManager(_currentUser))
                {
                    var allowedEntities = new[]
                    {
                        "CuDan",
                        "CanHo",
                        "XeOTo",
                        "XeMay",
                        "XeDap",
                        "PhuongTien",
                        "MatBangThuongMai",
                        "HoaDonCuDan",
                        "HoaDonTM",
                        "HoaDon",
                        "VatTu"
                    };

                    query = query.Where(x => allowedEntities.Contains(x.EntityName));
                }

                // Admin: xem tất
                var data = query
                           .OrderByDescending(x => x.CreatedAt)
                           .Take(500)
                           .ToList();

                dgLogs.ItemsSource = data;
            }
        }
    }
}
