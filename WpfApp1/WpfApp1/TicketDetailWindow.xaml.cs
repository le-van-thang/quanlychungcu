using System;
using System.Linq;
using System.Windows;

namespace WpfApp1
{
    public partial class TicketDetailWindow : Window
    {
        private readonly int? _ticketId;
        private readonly TaiKhoan _currentUser;
        private readonly bool _readOnly;
        private readonly bool _canManage;

        public TicketDetailWindow(int? ticketId, TaiKhoan currentUser, bool readOnly = false)
        {
            InitializeComponent();

            _ticketId = ticketId;
            _currentUser = currentUser;
            _readOnly = readOnly;
            _canManage = RoleHelper.CanManageTicket(_currentUser);

            Loaded += TicketDetailWindow_Loaded;
        }

        private void TicketDetailWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Danh sách trạng thái
            cboStatus.ItemsSource = new[] { "Mới", "Đang xử lý", "Hoàn tất" };

            using (var db = new QuanlychungcuEntities())
            {
                // Lấy tất cả rồi lọc bằng RoleHelper trên bộ nhớ (LINQ to Objects)
                var allAccounts = db.TaiKhoans.ToList();

                var handlers = allAccounts
                    .Where(t => RoleHelper.IsAdmin(t) || RoleHelper.IsManager(t))
                    .Select(t => new
                    {
                        t.TaiKhoanID,
                        Display = !string.IsNullOrWhiteSpace(t.Email)
                                    ? t.Email
                                    : (t.Username ?? ("TK#" + t.TaiKhoanID))
                    })
                    .ToList();

                cboAssigned.ItemsSource = handlers;
                cboAssigned.DisplayMemberPath = "Display";
                cboAssigned.SelectedValuePath = "TaiKhoanID";
            }

            if (_ticketId == null)
            {
                // Tạo mới
                cboStatus.SelectedItem = "Mới";

                // User thường: không được đổi status/gán
                if (!_canManage)
                {
                    cboStatus.IsEnabled = false;
                    cboAssigned.IsEnabled = false;
                }
            }
            else
            {
                LoadTicket();
            }

            if (_readOnly)
                SetReadOnly();
        }
        private void LoadTicket()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var t = db.Tickets.FirstOrDefault(x => x.TicketID == _ticketId.Value);
                if (t == null)
                {
                    MessageBox.Show("Không tìm thấy phản ánh.", "Lỗi",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    DialogResult = false;
                    Close();
                    return;
                }

                txtTitle.Text = t.Title;
                txtContent.Text = t.Content;
                cboStatus.SelectedItem = string.IsNullOrEmpty(t.Status) ? "Mới" : t.Status;
                if (t.AssignedTo != null)
                    cboAssigned.SelectedValue = t.AssignedTo.Value;

                // User thường: chỉ được xem, không sửa trạng thái/gán
                if (!_canManage)
                {
                    cboStatus.IsEnabled = false;
                    cboAssigned.IsEnabled = false;
                }
            }
        }

        private void SetReadOnly()
        {
            txtTitle.IsReadOnly = true;
            txtContent.IsReadOnly = true;
            cboStatus.IsEnabled = false;
            cboAssigned.IsEnabled = false;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (_readOnly)
            {
                Close();
                return;
            }

            string title = (txtTitle.Text ?? "").Trim();
            string content = (txtContent.Text ?? "").Trim();

            if (string.IsNullOrEmpty(title))
            {
                MessageBox.Show("Vui lòng nhập tiêu đề.", "Thiếu dữ liệu",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(content))
            {
                MessageBox.Show("Vui lòng nhập nội dung.", "Thiếu dữ liệu",
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            using (var db = new QuanlychungcuEntities())
            {
                Ticket entity;

                if (_ticketId == null)
                {
                    if (_currentUser == null)
                    {
                        MessageBox.Show("Không xác định được tài khoản hiện tại.",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    // Map cư dân theo Email tài khoản
                    var email = _currentUser.Email;
                    if (string.IsNullOrWhiteSpace(email))
                    {
                        MessageBox.Show("Tài khoản hiện tại không có email, không thể gắn cư dân.",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    var cuDan = db.CuDans.FirstOrDefault(c => c.Email == email);
                    if (cuDan == null)
                    {
                        MessageBox.Show("Không tìm thấy cư dân có email trùng với tài khoản hiện tại.",
                            "Lỗi", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }

                    entity = new Ticket
                    {
                        CuDanID = cuDan.CuDanID,
                        CreatedAt = DateTime.Now,
                        Status = "Mới"
                    };

                    db.Tickets.Add(entity);
                }
                else
                {
                    entity = db.Tickets.FirstOrDefault(t => t.TicketID == _ticketId.Value);
                    if (entity == null)
                    {
                        MessageBox.Show("Không tìm thấy phản ánh.", "Lỗi",
                            MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                entity.Title = title;
                entity.Content = content;
                entity.UpdatedAt = DateTime.Now;

                // Chỉ Admin/Manager được đổi trạng thái & gán người xử lý
                if (_canManage)
                {
                    entity.Status = cboStatus.SelectedItem as string ?? "Mới";

                    if (cboAssigned.SelectedValue is int taiKhoanId)
                        entity.AssignedTo = taiKhoanId;
                    else
                        entity.AssignedTo = null;
                }

                db.SaveChanges();

                // Ghi log
                string act = _ticketId == null ? "Create" : "Update";
                AuditLogger.Log(act, "Ticket",
                    entity.TicketID.ToString(),
                    $"{act} ticket: {entity.Title}");

                DialogResult = true;
                Close();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
            => Close();
    }
}
