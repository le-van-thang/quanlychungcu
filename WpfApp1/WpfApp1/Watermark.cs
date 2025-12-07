using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;

namespace WpfApp1   // đổi namespace cho khớp project của bạn
{
    /// <summary>
    /// Adorner hiển thị watermark (TextBlock) lên control.
    /// </summary>
    class WatermarkAdorner : Adorner
    {
        private readonly ContentPresenter _contentPresenter;

        public WatermarkAdorner(UIElement adornedElement, object watermark) : base(adornedElement)
        {
            IsHitTestVisible = false;

            var tb = new TextBlock
            {
                Text = watermark?.ToString() ?? string.Empty,
                Foreground = Brushes.LightGray,
                Margin = new Thickness(5, 0, 0, 0),
                VerticalAlignment = VerticalAlignment.Center,
                Opacity = 0.8,
            };

            _contentPresenter = new ContentPresenter
            {
                Content = tb,
                Opacity = 1.0,
                IsHitTestVisible = false
            };

            AddVisualChild(_contentPresenter);
        }

        protected override int VisualChildrenCount => 1;

        protected override Visual GetVisualChild(int index) => _contentPresenter;

        protected override Size MeasureOverride(Size constraint)
        {
            _contentPresenter.Measure(constraint);
            return constraint;
        }

        protected override Size ArrangeOverride(Size finalSize)
        {
            _contentPresenter.Arrange(new Rect(finalSize));
            return finalSize;
        }
    }

    /// <summary>
    /// Attached property WatermarkService.Watermark
    /// </summary>
    public static class WatermarkService
    {
        static readonly Dictionary<FrameworkElement, WatermarkAdorner> _adorners = new Dictionary<FrameworkElement, WatermarkAdorner>();

        public static readonly DependencyProperty WatermarkProperty =
            DependencyProperty.RegisterAttached("Watermark", typeof(string), typeof(WatermarkService),
                new PropertyMetadata(null, OnWatermarkChanged));

        public static void SetWatermark(DependencyObject element, string value) => element.SetValue(WatermarkProperty, value);
        public static string GetWatermark(DependencyObject element) => (string)element.GetValue(WatermarkProperty);

        private static void OnWatermarkChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (!(d is FrameworkElement fe)) return;

            fe.Loaded -= Fe_Loaded;
            fe.Loaded += Fe_Loaded;

            fe.Unloaded -= Fe_Unloaded;
            fe.Unloaded += Fe_Unloaded;

            // focus and content change handlers
            fe.GotFocus -= Fe_GotFocus;
            fe.LostFocus -= Fe_LostFocus;
            fe.GotFocus += Fe_GotFocus;
            fe.LostFocus += Fe_LostFocus;

            if (fe is TextBox tb)
            {
                tb.TextChanged -= ContentChanged;
                tb.TextChanged += ContentChanged;
            }
            else if (fe is PasswordBox pb)
            {
                pb.PasswordChanged -= ContentChanged;
                pb.PasswordChanged += ContentChanged;
            }
        }

        private static void Fe_Unloaded(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            RemoveAdorner(fe);
        }

        private static void Fe_Loaded(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            UpdateWatermark(fe);
        }

        private static void Fe_LostFocus(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            UpdateWatermark(fe);
        }

        private static void Fe_GotFocus(object sender, RoutedEventArgs e)
        {
            var fe = sender as FrameworkElement;
            UpdateWatermark(fe);
        }

        private static void ContentChanged(object sender, RoutedEventArgs e)
        {
            if (sender is FrameworkElement fe)
                UpdateWatermark(fe);
        }

        private static void UpdateWatermark(FrameworkElement fe)
        {
            if (fe == null) return;

            var layer = AdornerLayer.GetAdornerLayer(fe);
            if (layer == null) return; // chưa có trong visual tree

            bool hasText = false;
            if (fe is TextBox tb) hasText = !string.IsNullOrEmpty(tb.Text);
            else if (fe is PasswordBox pb) hasText = !string.IsNullOrEmpty(pb.Password);

            // show if empty and not focused
            if (!hasText && !fe.IsFocused)
            {
                if (!_adorners.ContainsKey(fe))
                {
                    var wm = GetWatermark(fe);
                    var adorner = new WatermarkAdorner(fe, wm);
                    layer.Add(adorner);
                    _adorners[fe] = adorner;
                }
            }
            else
            {
                // remove if exists
                if (_adorners.TryGetValue(fe, out var existing))
                {
                    layer.Remove(existing);
                    _adorners.Remove(fe);
                }
            }
        }

        private static void RemoveAdorner(FrameworkElement fe)
        {
            if (fe == null) return;
            var layer = AdornerLayer.GetAdornerLayer(fe);
            if (layer == null) return;
            if (_adorners.TryGetValue(fe, out var existing))
            {
                layer.Remove(existing);
                _adorners.Remove(fe);
            }
        }
    }
}
