using System;
using System.Globalization;
using System.Windows;

namespace WpfApp1
{
#if SYSTEM_SPEECH
    using System.Speech.Recognition;

    public partial class AIWindow
    {
        private SpeechRecognitionEngine _sr;
        private bool _micOn = false;

        partial void InitSpeech()
        {
            try
            {
                var ci = new CultureInfo("vi-VN");
                _sr = new SpeechRecognitionEngine(ci);
            }
            catch
            {
                // fallback EN-US nếu máy không có vi-VN
                _sr = new SpeechRecognitionEngine(new CultureInfo("en-US"));
            }

            try
            {
                _sr.SetInputToDefaultAudioDevice();
                _sr.LoadGrammar(new DictationGrammar());

                _sr.SpeechRecognized += (s, e) =>
                {
                    if (e?.Result == null || e.Result.Confidence < 0.60) return;
                    Dispatcher.Invoke(() =>
                    {
                        if (!string.IsNullOrWhiteSpace(txtQuestion.Text))
                            txtQuestion.AppendText(" ");
                        txtQuestion.AppendText(e.Result.Text);
                        txtQuestion.CaretIndex = txtQuestion.Text.Length;
                        txtQuestion.Focus();
                    });
                };

                _sr.RecognizeCompleted += (s, e) =>
                {
                    Dispatcher.Invoke(() =>
                    {
                        _micOn = false;
                        btnMic.Content = "🎤";
                        btnMic.ToolTip = "Bật micro (nhấn để nói)";
                        lblMic.Text = "Mic: Tắt";
                    });
                };
            }
            catch (Exception ex)
            {
                MessageBox.Show("Không khởi tạo được micro: " + ex.Message);
            }
        }

        partial void ToggleMic()
        {
            if (_sr == null)
            {
                MessageBox.Show("Thiết bị micro chưa sẵn sàng.");
                return;
            }

            if (_micOn)
            {
                try { _sr.RecognizeAsyncStop(); } catch { }
                _micOn = false;
                btnMic.Content = "🎤";
                btnMic.ToolTip = "Bật micro (nhấn để nói)";
                lblMic.Text = "Mic: Tắt";
            }
            else
            {
                try
                {
                    _sr.RecognizeAsync(RecognizeMode.Multiple);
                    _micOn = true;
                    btnMic.Content = "⏺";
                    btnMic.ToolTip = "Đang nghe… nhấn để tắt";
                    lblMic.Text = "Mic: Đang nghe…";
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Không bật được micro: " + ex.Message);
                }
            }
        }
    }
#else
    // ===== Bản giả (không cần System.Speech) — build vẫn xanh =====
    public partial class AIWindow
    {
        partial void InitSpeech() { /* no-op khi chưa bật micro */ }

        partial void ToggleMic()
        {
            MessageBox.Show(
                "Tính năng micro chưa bật.\n\n" +
                "Để kích hoạt:\n" +
                "1) Project → Add Reference… → Assemblies → tick System.Speech\n" +
                "2) Project → Properties → Build → Conditional compilation symbols: thêm SYSTEM_SPEECH\n" +
                "3) Clean & Rebuild",
                "Micro chưa bật",
                MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }
#endif
}
