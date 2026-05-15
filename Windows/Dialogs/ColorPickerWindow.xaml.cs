using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace CraftSharp.Windows.Dialogs
{
    /// <summary>
    /// 调色板式颜色选择器窗口（HSV模型）
    /// </summary>
    public partial class ColorPickerWindow : Wpf.Ui.Controls.FluentWindow
    {
        // 当前选择的颜色参数（默认红色 #FF0000）
        private double _hue = 0;           // 色相 0-360（红色）
        private double _saturation = 1;    // 饱和度 0-1（最大，纯色）
        private double _value = 1;         // 明度(Value) 0-1（最大，最亮）
        private double _opacity = 1;       // 不透明度 0-1（100%）

        // 颜色状态
        private System.Windows.Media.Color _selectedColor = System.Windows.Media.Colors.Red;
        private bool _isDraggingSpectrum = false;
        private bool _isDraggingHueSpectrum = false;
        private bool _isDraggingOpacity = false;

        // 防止输入框更新时递归触发
        private bool _isUpdatingInputs = false;

        /// <summary>
        /// 用户选择的颜色（十六进制格式，带Alpha）
        /// </summary>
        public string SelectedColorHex { get; private set; } = "#FFFF0000";

        /// <summary>
        /// 用户选择的颜色（Color对象，带Alpha）
        /// </summary>
        public System.Windows.Media.Color SelectedColor { get; private set; } = System.Windows.Media.Color.FromArgb(255, 255, 0, 0);

        /// <summary>
        /// 初始颜色（构造时传入）
        /// </summary>
        private System.Windows.Media.Color _initialColor;

        public ColorPickerWindow()
        {
            _isUpdatingInputs = true;
            InitializeComponent();
            _isUpdatingInputs = false;
            UpdateSpectrumBackground();
            UpdateOpacityGradient();
            UpdateColorDisplay();
            UpdateSelectorPositions();
        }

        public ColorPickerWindow(string initialColorHex)
        {
            _isUpdatingInputs = true;
            InitializeComponent();

            if (TryParseColorHex(initialColorHex, out _initialColor))
            {
                _opacity = _initialColor.A / 255.0;
                RgbToHsv(_initialColor, out _hue, out _saturation, out _value);
                SelectedColor = _initialColor;
                SelectedColorHex = initialColorHex;
                _selectedColor = _initialColor;
            }

            _isUpdatingInputs = false;
            UpdateSpectrumBackground();
            UpdateOpacityGradient();
            UpdateColorDisplay();
            UpdateSelectorPositions();
        }

        #region 鼠标交互

        private void Spectrum_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSpectrum = true;
            CaptureMouse();
            UpdateSpectrumFromMouse(e);
            e.Handled = true;
        }

        private void HueSpectrum_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingHueSpectrum = true;
            CaptureMouse();
            UpdateHueSpectrumFromMouse(e);
            e.Handled = true;
        }

        private void Opacity_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingOpacity = true;
            CaptureMouse();
            UpdateOpacityFromMouse(e);
            e.Handled = true;
        }

        private void Window_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDraggingSpectrum)
                UpdateSpectrumFromMouse(e);
            else if (_isDraggingHueSpectrum)
                UpdateHueSpectrumFromMouse(e);
            else if (_isDraggingOpacity)
                UpdateOpacityFromMouse(e);
        }

        private void Window_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingSpectrum || _isDraggingHueSpectrum || _isDraggingOpacity)
            {
                _isDraggingSpectrum = false;
                _isDraggingHueSpectrum = false;
                _isDraggingOpacity = false;
                ReleaseMouseCapture();
            }
        }

        private void UpdateSpectrumFromMouse(System.Windows.Input.MouseEventArgs e)
        {
            var pos = e.GetPosition(SpectrumBorder);
            double width = SpectrumBorder.ActualWidth;
            double height = SpectrumBorder.ActualHeight;

            if (width > 0 && height > 0)
            {
                _saturation = Math.Clamp(pos.X / width, 0, 1);
                _value = Math.Clamp(1 - pos.Y / height, 0, 1);

                UpdateColorFromHsv();
                UpdateColorDisplay();
                UpdateOpacityGradient();
                UpdateSelectorPositions();
            }
        }

        private void UpdateHueSpectrumFromMouse(System.Windows.Input.MouseEventArgs e)
        {
            var pos = e.GetPosition(HueSpectrumBorder);
            double height = HueSpectrumBorder.ActualHeight;

            if (height > 0)
            {
                _hue = Math.Clamp(pos.Y / height * 360, 0, 360);

                UpdateColorFromHsv();
                UpdateSpectrumBackground();
                UpdateColorDisplay();
                UpdateOpacityGradient();
                UpdateSelectorPositions();
            }
        }

        private void UpdateOpacityFromMouse(System.Windows.Input.MouseEventArgs e)
        {
            var pos = e.GetPosition(OpacityBorder);
            double height = OpacityBorder.ActualHeight;

            if (height > 0)
            {
                _opacity = Math.Clamp(pos.Y / height, 0, 1);
                UpdateColorFromHsv();
                UpdateSelectorPositions();
                UpdateColorDisplay();
            }
        }

        #endregion

        #region 颜色计算

        private void UpdateColorFromHsv()
        {
            var rgb = HsvToRgb(_hue, _saturation, _value);
            byte alpha = (byte)Math.Round(_opacity * 255);
            _selectedColor = System.Windows.Media.Color.FromArgb(alpha, rgb.R, rgb.G, rgb.B);
            SelectedColor = _selectedColor;
            SelectedColorHex = ColorToHex(_selectedColor);
        }

        private System.Windows.Media.Color HsvToRgb(double h, double s, double v)
        {
            double c = v * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = v - c;

            double r1, g1, b1;

            if (h < 60) { r1 = c; g1 = x; b1 = 0; }
            else if (h < 120) { r1 = x; g1 = c; b1 = 0; }
            else if (h < 180) { r1 = 0; g1 = c; b1 = x; }
            else if (h < 240) { r1 = 0; g1 = x; b1 = c; }
            else if (h < 300) { r1 = x; g1 = 0; b1 = c; }
            else { r1 = c; g1 = 0; b1 = x; }

            byte r = (byte)Math.Round((r1 + m) * 255);
            byte g = (byte)Math.Round((g1 + m) * 255);
            byte b = (byte)Math.Round((b1 + m) * 255);

            return System.Windows.Media.Color.FromRgb(r, g, b);
        }

        private void RgbToHsv(System.Windows.Media.Color color, out double h, out double s, out double v)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            v = max;

            if (delta == 0)
            {
                h = 0;
                s = 0;
            }
            else
            {
                s = delta / max;

                if (max == r)
                    h = 60 * (((g - b) / delta) % 6);
                else if (max == g)
                    h = 60 * (((b - r) / delta) + 2);
                else
                    h = 60 * (((r - g) / delta) + 4);

                if (h < 0) h += 360;
            }
        }

        private string ColorToHex(System.Windows.Media.Color color)
        {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        private bool TryParseColorHex(string hex, out System.Windows.Media.Color color)
        {
            color = System.Windows.Media.Colors.White;
            if (string.IsNullOrEmpty(hex)) return false;

            hex = hex.TrimStart('#');

            try
            {
                if (hex.Length == 6)
                {
                    byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    color = System.Windows.Media.Color.FromArgb(255, r, g, b);
                    return true;
                }
                else if (hex.Length == 8)
                {
                    byte a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte r = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                    color = System.Windows.Media.Color.FromArgb(a, r, g, b);
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        #endregion

        #region UI更新

        private void UpdateColorDisplay()
        {
            _isUpdatingInputs = true;

            PreviewColorOverlay.Background = new SolidColorBrush(_selectedColor);
            HexInput.Text = $"#{_selectedColor.R:X2}{_selectedColor.G:X2}{_selectedColor.B:X2}";
            AlphaPercentInput.Text = $"{(int)Math.Round(_opacity * 100)}%";
            RInput.Text = _selectedColor.R.ToString();
            GInput.Text = _selectedColor.G.ToString();
            BInput.Text = _selectedColor.B.ToString();
            AlphaInput.Text = $"{(int)Math.Round(_opacity * 100)}%";

            _isUpdatingInputs = false;
        }

        private void UpdateSpectrumBackground()
        {
            // 色谱背景：横向是透明→纯色（饱和度），纵向是透明→黑色（亮度）
            // 底层是白色背景
            var pureColor = HsvToRgb(_hue, 1, 1);

            // 横向渐变：透明 → 纯色（左白→右纯色）
            var gradient1 = new LinearGradientBrush();
            gradient1.StartPoint = new System.Windows.Point(0, 0);
            gradient1.EndPoint = new System.Windows.Point(1, 0);
            gradient1.GradientStops.Add(new GradientStop(System.Windows.Media.Color.FromArgb(0, pureColor.R, pureColor.G, pureColor.B), 0));
            gradient1.GradientStops.Add(new GradientStop(pureColor, 1));
            HueGradient.Fill = gradient1;
        }

        private void UpdateOpacityGradient()
        {
            var baseColor = HsvToRgb(_hue, _saturation, _value);
            var topColor = System.Windows.Media.Color.FromArgb(0, baseColor.R, baseColor.G, baseColor.B);
            var bottomColor = System.Windows.Media.Color.FromArgb(255, baseColor.R, baseColor.G, baseColor.B);

            var gradient = new LinearGradientBrush();
            gradient.StartPoint = new System.Windows.Point(0, 0);
            gradient.EndPoint = new System.Windows.Point(0, 1);
            gradient.GradientStops.Add(new GradientStop(topColor, 0));
            gradient.GradientStops.Add(new GradientStop(bottomColor, 1));

            OpacityGradient.Fill = gradient;
        }

        private void UpdateSelectorPositions()
        {
            // 色谱选择器：横向饱和度，纵向亮度
            double spectrumX = _saturation * SpectrumBorder.ActualWidth;
            double spectrumY = (1 - _value) * SpectrumBorder.ActualHeight;
            SpectrumSelector.Margin = new Thickness(spectrumX - 6, spectrumY - 6, 0, 0);

            // 色相光谱选择器
            double hueY = (_hue / 360) * HueSpectrumBorder.ActualHeight;
            HueSpectrumSelector.Margin = new Thickness(0, hueY - 2, 0, 0);

            // 不透明度选择器
            double opacityY = _opacity * OpacityBorder.ActualHeight;
            OpacitySelector.Margin = new Thickness(0, opacityY - 2, 0, 0);
        }

        #endregion

        #region 输入框事件

        private void HexInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingInputs) return;

            string text = HexInput.Text;
            if (text.Length != 7 || !text.StartsWith("#")) return;

            string hex = text.Substring(1);
            if (!IsValidHexString(hex)) return;

            if (TryParseHexColor(hex, out byte r, out byte g, out byte b))
            {
                _selectedColor = System.Windows.Media.Color.FromArgb(_selectedColor.A, r, g, b);
                RgbToHsv(_selectedColor, out _hue, out _saturation, out _value);
                UpdateColorFromHsv();
                UpdateSpectrumBackground();
                UpdateOpacityGradient();
                UpdateSelectorPositions();

                _isUpdatingInputs = true;
                PreviewColorOverlay.Background = new SolidColorBrush(_selectedColor);
                AlphaPercentInput.Text = $"{(int)Math.Round(_opacity * 100)}%";
                RInput.Text = _selectedColor.R.ToString();
                GInput.Text = _selectedColor.G.ToString();
                BInput.Text = _selectedColor.B.ToString();
                AlphaInput.Text = $"{(int)Math.Round(_opacity * 100)}%";
                _isUpdatingInputs = false;
            }
        }

        private bool IsValidHexString(string hex)
        {
            if (hex.Length != 6) return false;
            foreach (char c in hex)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                    return false;
            }
            return true;
        }

        private void AlphaPercentInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingInputs) return;

            if (TryParseOpacityValue(AlphaPercentInput.Text, out int opacityPercent))
            {
                _opacity = opacityPercent / 100.0;
                UpdateColorFromHsv();
                UpdateSelectorPositions();

                _isUpdatingInputs = true;
                PreviewColorOverlay.Background = new SolidColorBrush(_selectedColor);
                HexInput.Text = $"#{_selectedColor.R:X2}{_selectedColor.G:X2}{_selectedColor.B:X2}";
                RInput.Text = _selectedColor.R.ToString();
                GInput.Text = _selectedColor.G.ToString();
                BInput.Text = _selectedColor.B.ToString();
                AlphaInput.Text = $"{opacityPercent}%";
                _isUpdatingInputs = false;
            }
        }

        private void RgbInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingInputs) return;

            if (TryParseRgbValueWithClamp(RInput.Text, out byte r, out int rRaw) &&
                TryParseRgbValueWithClamp(GInput.Text, out byte g, out int gRaw) &&
                TryParseRgbValueWithClamp(BInput.Text, out byte b, out int bRaw))
            {
                _selectedColor = System.Windows.Media.Color.FromArgb(_selectedColor.A, r, g, b);
                RgbToHsv(_selectedColor, out _hue, out _saturation, out _value);
                UpdateColorFromHsv();
                UpdateSpectrumBackground();
                UpdateOpacityGradient();
                UpdateSelectorPositions();

                _isUpdatingInputs = true;
                PreviewColorOverlay.Background = new SolidColorBrush(_selectedColor);
                HexInput.Text = $"#{_selectedColor.R:X2}{_selectedColor.G:X2}{_selectedColor.B:X2}";
                AlphaPercentInput.Text = $"{(int)Math.Round(_opacity * 100)}%";
                AlphaInput.Text = $"{(int)Math.Round(_opacity * 100)}%";
                if (rRaw != r) RInput.Text = r.ToString();
                if (gRaw != g) GInput.Text = g.ToString();
                if (bRaw != b) BInput.Text = b.ToString();
                _isUpdatingInputs = false;
            }
        }

        private void AlphaInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingInputs) return;

            if (TryParseOpacityValue(AlphaInput.Text, out int opacityPercent))
            {
                _opacity = opacityPercent / 100.0;
                UpdateColorFromHsv();
                UpdateSelectorPositions();

                _isUpdatingInputs = true;
                PreviewColorOverlay.Background = new SolidColorBrush(_selectedColor);
                HexInput.Text = $"#{_selectedColor.R:X2}{_selectedColor.G:X2}{_selectedColor.B:X2}";
                AlphaPercentInput.Text = $"{opacityPercent}%";
                RInput.Text = _selectedColor.R.ToString();
                GInput.Text = _selectedColor.G.ToString();
                BInput.Text = _selectedColor.B.ToString();
                _isUpdatingInputs = false;
            }
        }

        private bool TryParseHexColor(string hex, out byte r, out byte g, out byte b)
        {
            r = g = b = 0;
            try
            {
                r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                return true;
            }
            catch { return false; }
        }

        private bool TryParseRgbValueWithClamp(string text, out byte clampedValue, out int rawValue)
        {
            clampedValue = 0;
            rawValue = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;

            try
            {
                rawValue = int.Parse(text);
                int clamped = Math.Clamp(rawValue, 0, 255);
                clampedValue = (byte)clamped;
                return true;
            }
            catch { return false; }
        }

        private bool TryParseOpacityValue(string text, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text)) return false;

            try
            {
                text = text.TrimEnd('%');
                int parsed = int.Parse(text);
                parsed = Math.Clamp(parsed, 0, 100);
                value = parsed;
                return true;
            }
            catch { return false; }
        }

        #endregion

        #region 按钮事件

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        #endregion

        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            UpdateSelectorPositions();
        }
    }
}