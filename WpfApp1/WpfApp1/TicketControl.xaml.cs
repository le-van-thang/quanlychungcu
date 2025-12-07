using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace WpfApp1
{
    public partial class TicketControl : UserControl
    {
        private readonly TaiKhoan _currentUser;
        private readonly bool _canManage;

        public TicketControl() : this(null)
        {
        }

        public TicketControl(TaiKhoan currentUser)
        {
            InitializeComponent();

            _currentUser = currentUser;
            _canManage = RoleHelper.CanManageTicket(_currentUser);

            ApplyRoleUi();

            Loaded += (s, e) => LoadData();
        }

        private void ApplyRoleUi()
        {
            // User thường: không sửa / xoá
            if (!_canManage)
            {
                btnEdit.IsEnabled = false;
                btnDelete.IsEnabled = false;
            }
        }

        private void LoadData()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var q = db.Tickets.AsQueryable();

                // User thường: chỉ thấy ticket của chính mình (theo Email cư dân = Email tài khoản)
                if (RoleHelper.IsUser(_currentUser))
                {
                    var email = _currentUser?.Email;
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        q = q.Where(t => t.CuDan.Email == email);
                    }
                    else
                    {
                        // Không có email -> không cho xem gì
                        q = q.Where(t => false);
                    }
                }

                // Search
                var kw = (txtSearch.Text ?? "").Trim();
                if (!string.IsNullOrEmpty(kw))
                {
                    q = q.Where(t =>
                        t.Title.Contains(kw) ||
                        t.Content.Contains(kw) ||
                        t.Status.Contains(kw) ||
                        t.CuDan.HoTen.Contains(kw));
                }

                var data = q
                    .OrderByDescending(t => t.CreatedAt)
                    .Select(t => new
                    {
                        t.TicketID,
                        CuDanName = t.CuDan.HoTen,
                        t.Title,
                        t.Status,
                        AssignedName =
                            (t.TaiKhoan != null
                                ? (t.TaiKhoan.Email ?? t.TaiKhoan.Username)
                                : null),
                        t.CreatedAt
                    })
                    .ToList();

                dgTickets.ItemsSource = data;
            }
        }

        private int? GetSelectedTicketId()
        {
            if (dgTickets.SelectedItem == null) return null;

            dynamic row = dgTickets.SelectedItem;
            return (int?)row.TicketID;
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
            => LoadData();

        private void BtnReload_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Text = "";
            LoadData();
        }

        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var win = new TicketDetailWindow(null, _currentUser);
            win.Owner = Window.GetWindow(this);

            if (win.ShowDialog() == true)
                LoadData();
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            var id = GetSelectedTicketId();
            if (id == null)
            {
                MessageBox.Show("Vui lòng chọn một phản ánh.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!_canManage)
            {
                MessageBox.Show("Bạn không có quyền xử lý / sửa phản ánh.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var win = new TicketDetailWindow(id.Value, _currentUser);
            win.Owner = Window.GetWindow(this);

            if (win.ShowDialog() == true)
                LoadData();
        }

        private void BtnView_Click(object sender, RoutedEventArgs e)
        {
            var id = GetSelectedTicketId();
            if (id == null)
            {
                MessageBox.Show("Vui lòng chọn một phản ánh.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var win = new TicketDetailWindow(id.Value, _currentUser, readOnly: true);
            win.Owner = Window.GetWindow(this);
            win.ShowDialog();
        }

        private void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            var id = GetSelectedTicketId();
            if (id == null)
            {
                MessageBox.Show("Vui lòng chọn một phản ánh.", "Thông báo",
                    MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (!_canManage)
            {
                MessageBox.Show("Bạn không có quyền xoá phản ánh.",
                    "Không có quyền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (MessageBox.Show("Bạn chắc chắn muốn xoá phản ánh này?",
                    "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Question) != MessageBoxResult.Yes)
                return;

            using (var db = new QuanlychungcuEntities())
            {
                var t = db.Tickets.FirstOrDefault(x => x.TicketID == id.Value);
                if (t == null)
                {
                    MessageBox.Show("Không tìm thấy phản ánh.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                db.Tickets.Remove(t);
                db.SaveChanges();

                AuditLogger.Log("Delete", "Ticket", id.Value.ToString(),
                    $"Xoá ticket: {t.Title}");

                LoadData();
            }
        }
    }
}
