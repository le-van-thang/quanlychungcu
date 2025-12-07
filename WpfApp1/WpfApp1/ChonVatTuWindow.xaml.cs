using System.Linq;
using System.Windows;

namespace WpfApp1
{
    public partial class ChonVatTuWindow : Window
    {
        public System.Collections.Generic.List<VatTuSelection> SelectedVatTus { get; set; }

        public ChonVatTuWindow()
        {
            InitializeComponent();
            LoadVatTu();
        }

        private void LoadVatTu()
        {
            using (var db = new QuanlychungcuEntities())
            {
                var list = db.VatTus
                             .Select(v => new VatTuSelection
                             {
                                 VatTuID = v.VatTuID,
                                 TenVatTu = v.TenVatTu,
                                 SoLuongTon = v.SoLuong ?? 0,   // ✅ lấy từ VatTu.SoLuong
                                 Gia = v.Gia ?? 0,
                                 IsSelected = false,
                                 SoLuongChon = 0
                             })
                             .ToList();

                dgVatTu.ItemsSource = list;
            }
        }

        private void BtnOk_Click(object sender, RoutedEventArgs e)
        {
            SelectedVatTus = dgVatTu.ItemsSource.Cast<VatTuSelection>()
                                   .Where(v => v.IsSelected && v.SoLuongChon > 0)
                                   .ToList();

            if (SelectedVatTus.Any())
            {
                this.DialogResult = true;
            }
            else
            {
                MessageBox.Show("Vui lòng chọn ít nhất 1 vật tư và nhập số lượng!", "Thông báo");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}

namespace WpfApp1
{
    public class VatTuSelection
    {
        public int VatTuID { get; set; }
        public string TenVatTu { get; set; }
        public int SoLuongTon { get; set; }   // tồn kho trong DB
        public decimal Gia { get; set; }      // giá trong DB

        public bool IsSelected { get; set; }
        public int SoLuongChon { get; set; }  // số lượng user chọn
    }
}

