using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace WpfApp1
{
    public partial class AIWindow : Window
    {
        private readonly TaiKhoan _user;
        private readonly AiService _ai;
        private bool _dark = true;
        private bool _sidebarOpen = false;

        // Search view
        private ICollectionView _convView;

        // Speech hooks (optional)
        partial void InitSpeech();
        partial void ToggleMic();

        public ObservableCollection<ChatMessage> Messages { get; } = new ObservableCollection<ChatMessage>();
        public ObservableCollection<FileAttachment> PendingAttachments { get; } = new ObservableCollection<FileAttachment>();

        private readonly ConversationStore _store = new ConversationStore();
        private Conversation _current;

        public AIWindow(TaiKhoan user)
        {
            InitializeComponent();
            _user = user;
            _ai = new AiService();

            lblTitle.Text = (_user != null) ? ("AI trợ lý — xin chào " + _user.Username) : "AI trợ lý";
            lblMode.Text = _ai.IsDemo ? "(Demo mode: không gọi API thật)" : "(Online)";

            icChat.ItemsSource = Messages;
            icPending.ItemsSource = PendingAttachments;

            // nguồn dữ liệu cho lịch sử + view để filter
            lstConversations.ItemsSource = _store.LoadAll();
            _convView = CollectionViewSource.GetDefaultView(lstConversations.ItemsSource);

            StartNewConversation();

            txtQuestion.Focus();
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Paste, OnPasteExecute));

            InitSpeech();
        }

        /* ===== Sidebar / conversations ===== */

        private void StartNewConversation()
        {
            _current = _store.CreateNew();
            ShowConversation(_current);

            if (Messages.Count == 0)
            {
                Messages.Add(new ChatMessage
                {
                    IsUser = false,
                    Message = "Chào bạn! Tôi là trợ lý cho ứng dụng QUẢN LÝ CHUNG CƯ. Tôi có thể giúp gì cho bạn hôm nay?"
                });
            }
        }

        private void ShowConversation(Conversation cv)
        {
            Messages.Clear();
            foreach (var m in cv.Messages) Messages.Add(m);
            svChat.ScrollToEnd();
        }

        private void SaveConversation()
        {
            if (_current == null) return;
            _current.Messages = Messages.ToList();
            _store.Save(_current);

            // refresh danh sách + filter đang áp dụng
            lstConversations.ItemsSource = _store.LoadAll();
            _convView = CollectionViewSource.GetDefaultView(lstConversations.ItemsSource);
            ApplySearchFilter(txtSearchHist.Text);
        }

        private void btnMenu_Click(object sender, RoutedEventArgs e)
        {
            _sidebarOpen = !_sidebarOpen;
            // mở ~45% chiều rộng cửa sổ, tối đa 500px
            double target = _sidebarOpen ? Math.Min(500, Math.Max(320, this.ActualWidth * 0.45)) : 0;
            colSidebar.Width = new GridLength(target);
        }

        private void lstConversations_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            var cv = lstConversations.SelectedItem as Conversation;
            if (cv != null)
            {
                _current = cv;
                ShowConversation(cv);
            }
        }

        private void txtSearchHist_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            ApplySearchFilter(txtSearchHist.Text);
        }

        private void ApplySearchFilter(string raw)
        {
            string q = (raw ?? "").Trim().ToLowerInvariant();

            if (_convView == null) return;

            if (string.IsNullOrEmpty(q))
            {
                _convView.Filter = null;
            }
            else
            {
                _convView.Filter = obj =>
                {
                    var c = obj as Conversation;               // C# 7.3: tránh dùng "is not"
                    if (c == null) return false;

                    string title = (c.Title ?? "").ToLowerInvariant();
                    string first = (c.Messages != null && c.Messages.FirstOrDefault() != null
                                    ? c.Messages.FirstOrDefault().Message ?? "" : "").ToLowerInvariant();
                    string all = string.Join(" ",
                                    (c.Messages ?? new List<ChatMessage>())
                                        .Select(m => m.Message ?? "")).ToLowerInvariant();

                    return title.Contains(q) || first.Contains(q) || all.Contains(q);
                };
            }
            _convView.Refresh();
        }

        /* ===== Input & send ===== */

        private void TxtQuestion_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter && (Keyboard.Modifiers & ModifierKeys.Control) == ModifierKeys.Control)
            {
                BtnSend_Click(sender, new RoutedEventArgs());
                e.Handled = true;
            }
        }

        private async void BtnSend_Click(object sender, RoutedEventArgs e)
        {
            string q = (txtQuestion.Text ?? "").Trim();
            if (q.Length == 0 && PendingAttachments.Count == 0)
            {
                MessageBox.Show("Bạn hãy nhập câu hỏi hoặc đính kèm tệp nhé.");
                return;
            }

            Messages.Add(new ChatMessage
            {
                IsUser = true,
                Message = q,
                Attachments = PendingAttachments.Select(CloneAttachment).ToList()
            });

            txtQuestion.Clear();
            PendingAttachments.Clear();
            txtQuestion.Focus();
            svChat.ScrollToEnd();
            SaveConversation();

            try
            {
                btnSend.IsEnabled = false;
                btnAttach.IsEnabled = false;
                btnMic.IsEnabled = false;

                var userMsg = Messages.LastOrDefault(m => m.IsUser);
                IEnumerable<FileAttachment> atts = (userMsg != null) ? (userMsg.Attachments ?? new List<FileAttachment>()) : new List<FileAttachment>();

                string ans = await _ai.AskAsync(q, atts);
                Messages.Add(new ChatMessage { IsUser = false, Message = ans });
                svChat.ScrollToEnd();
                SaveConversation();
            }
            catch (Exception ex)
            {
                Messages.Add(new ChatMessage
                {
                    IsUser = false,
                    Message = "Lỗi: " + (ex.InnerException != null ? ex.InnerException.Message : ex.Message)
                });
                svChat.ScrollToEnd();
                SaveConversation();
            }
            finally
            {
                btnSend.IsEnabled = true;
                btnAttach.IsEnabled = true;
                btnMic.IsEnabled = true;
            }
        }

        private static FileAttachment CloneAttachment(FileAttachment a)
        {
            return new FileAttachment
            {
                FilePath = a.FilePath,
                DisplayName = a.DisplayName,
                IsImage = a.IsImage,
                Thumbnail = a.Thumbnail
            };
        }

        private void BtnAttach_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new OpenFileDialog
            {
                Title = "Chọn tệp đính kèm",
                Multiselect = true,
                Filter = "Tất cả|*.*|Ảnh|*.png;*.jpg;*.jpeg;*.webp;*.gif;*.bmp|Văn bản|*.txt;*.csv;*.json;*.md"
            };
            if (dlg.ShowDialog() == true) AddFiles(dlg.FileNames);
        }

        private void AddFiles(IEnumerable<string> paths)
        {
            string[] imgExts = new[] { ".png", ".jpg", ".jpeg", ".bmp", ".gif", ".webp" };
            foreach (string p in paths ?? Enumerable.Empty<string>())
            {
                if (!File.Exists(p)) continue;

                string ext = System.IO.Path.GetExtension(p).ToLowerInvariant();
                bool img = imgExts.Contains(ext);

                PendingAttachments.Add(new FileAttachment
                {
                    FilePath = p,
                    DisplayName = System.IO.Path.GetFileName(p),
                    IsImage = img,
                    Thumbnail = img ? LoadThumb(p, 90) : null
                });
            }
        }

        private static ImageSource LoadThumb(string file, int size)
        {
            try
            {
                var b = new BitmapImage();
                b.BeginInit();
                b.CacheOption = BitmapCacheOption.OnLoad;
                b.UriSource = new Uri(file);
                b.DecodePixelWidth = size;
                b.DecodePixelHeight = size;
                b.EndInit();
                b.Freeze();
                return b;
            }
            catch { return null; }
        }

        private void RemovePending_Click(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var a = fe != null ? fe.DataContext as FileAttachment : null;
            if (a != null) PendingAttachments.Remove(a);
        }

        private void OpenAttachment_Click(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            var a = fe != null ? fe.DataContext as FileAttachment : null;
            if (a == null) return;

            try { Process.Start(new ProcessStartInfo(a.FilePath) { UseShellExecute = true }); }
            catch (Exception ex) { MessageBox.Show("Không mở được tệp: " + ex.Message); }
        }

        private void Window_PreviewDragOver(object sender, DragEventArgs e)
        {
            e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
            e.Handled = true;
        }

        private void Window_Drop(object sender, DragEventArgs e)
        {
            if (!e.Data.GetDataPresent(DataFormats.FileDrop)) return;
            var files = e.Data.GetData(DataFormats.FileDrop) as string[];
            if (files != null) AddFiles(files);
        }

        private void OnPasteExecute(object sender, ExecutedRoutedEventArgs e)
        {
            try
            {
                if (Clipboard.ContainsImage())
                {
                    var img = Clipboard.GetImage();
                    if (img != null)
                    {
                        string temp = System.IO.Path.Combine(System.IO.Path.GetTempPath(),
                            "clip_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png");

                        using (var fs = new FileStream(temp, FileMode.Create, FileAccess.Write))
                        {
                            var enc = new PngBitmapEncoder();
                            enc.Frames.Add(BitmapFrame.Create(img));
                            enc.Save(fs);
                        }

                        AddFiles(new[] { temp });
                        e.Handled = true;
                    }
                }
            }
            catch { }
        }

        private void BtnNewChat_Click(object sender, RoutedEventArgs e)
        {
            StartNewConversation();
        }

        private void btnTheme_Click(object sender, RoutedEventArgs e)
        {
            // đảo trạng thái
            _dark = !_dark;

            if (_dark)
            {
                // đang dark mode
                btnTheme.Content = "☀️"; // icon: bấm để chuyển sang sáng

                Resources["WindowBg"] = new SolidColorBrush(Color.FromRgb(5, 8, 22));   // #050816
                Resources["CardBg"] = new SolidColorBrush(Color.FromRgb(11, 16, 32)); // #0B1020
                Resources["TitleFg"] = new SolidColorBrush(Color.FromRgb(249, 250, 251)); // #F9FAFB
                Resources["SubFg"] = new SolidColorBrush(Color.FromRgb(156, 163, 175)); // #9CA3AF
                Resources["InputBg"] = new SolidColorBrush(Color.FromRgb(2, 6, 23));   // #020617
                Resources["InputBrd"] = new SolidColorBrush(Color.FromRgb(79, 70, 229)); // #4F46E5
            }
            else
            {
                // light mode
                btnTheme.Content = "🌙"; // icon: bấm để chuyển lại dark

                Resources["WindowBg"] = (SolidColorBrush)new BrushConverter().ConvertFromString("#F4F6F7");
                Resources["CardBg"] = Brushes.White;
                Resources["TitleFg"] = (SolidColorBrush)new BrushConverter().ConvertFromString("#111827");
                Resources["SubFg"] = (SolidColorBrush)new BrushConverter().ConvertFromString("#6B7280");
                Resources["InputBg"] = Brushes.White;
                // viền giữ màu xanh đậm cho rõ trên nền trắng
                Resources["InputBrd"] = (SolidColorBrush)new BrushConverter().ConvertFromString("#4F46E5");
            }
        }


        private void BtnMic_Click(object sender, RoutedEventArgs e)
        {
            ToggleMic();
        }
    }
}
