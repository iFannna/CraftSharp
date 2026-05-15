using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace CraftSharp.Windows.Dialogs
{
    /// <summary>
    /// 调色板式颜色选择器窗口
    /// </summary>
    public partial class ColorPickerWindow : Wpf.Ui.Controls.FluentWindow
    {
        // 当前选择的颜色参数（默认红色 #FF0000）
        private double _hue = 0;           // 色相 0-360（红色）
        private double _saturation = 1;    // 饱和度 0-1（最大）
        private double _brightness = 0.5;  // 明度 0-1（纯色）
        private double _opacity = 1;       // 不透明度 0-1（100%）

        // 颜色状态
        private System.Windows.Media.Color _selectedColor = System.Windows.Media.Colors.Red;
        private bool _isDraggingSpectrum = false;
        private bool _isDraggingBrightness = false;
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
            _isUpdatingInputs = true;  // 防止初始化时触发事件
            InitializeComponent();
            _isUpdatingInputs = false;
            UpdateColorDisplay();
            UpdateBrightnessGradient();
            UpdateOpacityGradient();
        }

        /// <summary>
        /// 构造函数：传入初始颜色
        /// </summary>
        public ColorPickerWindow(string initialColorHex)
        {
            _isUpdatingInputs = true;  // 防止初始化时触发事件
            InitializeComponent();

            // 解析初始颜色（支持 #RRGGBB 或 #AARRGGBB 格式）
            if (TryParseColorHex(initialColorHex, out _initialColor))
            {
                // 提取透明度
                _opacity = _initialColor.A / 255.0;

                // 将RGB转换为HSL
                RgbToHsl(_initialColor, out _hue, out _saturation, out _brightness);
                SelectedColor = _initialColor;
                SelectedColorHex = initialColorHex;
                _selectedColor = _initialColor;
            }

            _isUpdatingInputs = false;
            UpdateColorDisplay();
            UpdateBrightnessGradient();
            UpdateOpacityGradient();
            UpdateSelectorPositions();
        }

        #region 鼠标交互

        /// <summary>
        /// 色谱区域鼠标按下
        /// </summary>
        private void Spectrum_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSpectrum = true;
            CaptureMouse();
            UpdateSpectrumFromMouse(e);
            e.Handled = true;
        }

        /// <summary>
        /// 明度条鼠标按下
        /// </summary>
        private void Brightness_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingBrightness = true;
            CaptureMouse();
            UpdateBrightnessFromMouse(e);
            e.Handled = true;
        }

        /// <summary>
        /// 不透明度条鼠标按下
        /// </summary>
        private void Opacity_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingOpacity = true;
            CaptureMouse();
            UpdateOpacityFromMouse(e);
            e.Handled = true;
        }

        /// <summary>
        /// 窗口级别的鼠标移动处理（用于拖拽操作）
        /// </summary>
        private void Window_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDraggingSpectrum)
            {
                UpdateSpectrumFromMouse(e);
            }
            else if (_isDraggingBrightness)
            {
                UpdateBrightnessFromMouse(e);
            }
            else if (_isDraggingOpacity)
            {
                UpdateOpacityFromMouse(e);
            }
        }

        /// <summary>
        /// 窗口级别的鼠标释放处理
        /// </summary>
        private void Window_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (_isDraggingSpectrum || _isDraggingBrightness || _isDraggingOpacity)
            {
                _isDraggingSpectrum = false;
                _isDraggingBrightness = false;
                _isDraggingOpacity = false;
                ReleaseMouseCapture();
            }
        }

        /// <summary>
        /// 根据鼠标位置更新色谱选择
        /// </summary>
        private void UpdateSpectrumFromMouse(System.Windows.Input.MouseEventArgs e)
        {
            var pos = e.GetPosition(SpectrumBorder);
            double width = SpectrumBorder.ActualWidth;
            double height = SpectrumBorder.ActualHeight;

            if (width > 0 && height > 0)
            {
                // 色相 0-360（横向）
                _hue = Math.Clamp(pos.X / width * 360, 0, 360);
                // 饱和度 0-1（纵向，从上到下递减）
                _saturation = Math.Clamp(1 - pos.Y / height, 0, 1);

                UpdateColorFromHsl();
                UpdateColorDisplay();
                UpdateBrightnessGradient();
                UpdateOpacityGradient();
                UpdateSelectorPositions();
            }
        }

        /// <summary>
        /// 根据鼠标位置更新明度选择
        /// </summary>
        private void UpdateBrightnessFromMouse(System.Windows.Input.MouseEventArgs e)
        {
            var pos = e.GetPosition(BrightnessBorder);
            double height = BrightnessBorder.ActualHeight;

            if (height > 0)
            {
                // 明度 1-0（纵向，从上到下）
                _brightness = Math.Clamp(1 - pos.Y / height, 0, 1);

                UpdateColorFromHsl();
                UpdateColorDisplay();
                UpdateOpacityGradient();
                UpdateSelectorPositions();
            }
        }

        /// <summary>
        /// 根据鼠标位置更新不透明度选择
        /// </summary>
        private void UpdateOpacityFromMouse(System.Windows.Input.MouseEventArgs e)
        {
            var pos = e.GetPosition(OpacityBorder);
            double height = OpacityBorder.ActualHeight;

            if (height > 0)
            {
                // 不透明度 0-1（纵向，从上到下递增）
                _opacity = Math.Clamp(pos.Y / height, 0, 1);

                UpdateColorFromHsl();
                UpdateColorDisplay();
                UpdateSelectorPositions();
            }
        }

        #endregion

        #region 颜色计算

        /// <summary>
        /// 根据HSL值和透明度更新颜色
        /// </summary>
        private void UpdateColorFromHsl()
        {
            var rgb = HslToRgb(_hue, _saturation, _brightness);
            byte alpha = (byte)Math.Round(_opacity * 255);
            _selectedColor = System.Windows.Media.Color.FromArgb(alpha, rgb.R, rgb.G, rgb.B);
            SelectedColor = _selectedColor;
            SelectedColorHex = ColorToHex(_selectedColor);
        }

        /// <summary>
        /// HSL转RGB
        /// </summary>
        private System.Windows.Media.Color HslToRgb(double h, double s, double l)
        {
            double c = (1 - Math.Abs(2 * l - 1)) * s;
            double x = c * (1 - Math.Abs((h / 60) % 2 - 1));
            double m = l - c / 2;

            double r1, g1, b1;

            if (h < 60)
            {
                r1 = c; g1 = x; b1 = 0;
            }
            else if (h < 120)
            {
                r1 = x; g1 = c; b1 = 0;
            }
            else if (h < 180)
            {
                r1 = 0; g1 = c; b1 = x;
            }
            else if (h < 240)
            {
                r1 = 0; g1 = x; b1 = c;
            }
            else if (h < 300)
            {
                r1 = x; g1 = 0; b1 = c;
            }
            else
            {
                r1 = c; g1 = 0; b1 = x;
            }

            byte r = (byte)Math.Round((r1 + m) * 255);
            byte g = (byte)Math.Round((g1 + m) * 255);
            byte b = (byte)Math.Round((b1 + m) * 255);

            return System.Windows.Media.Color.FromRgb(r, g, b);
        }

        /// <summary>
        /// RGB转HSL
        /// </summary>
        private void RgbToHsl(System.Windows.Media.Color color, out double h, out double s, out double l)
        {
            double r = color.R / 255.0;
            double g = color.G / 255.0;
            double b = color.B / 255.0;

            double max = Math.Max(r, Math.Max(g, b));
            double min = Math.Min(r, Math.Min(g, b));
            double delta = max - min;

            l = (max + min) / 2;

            if (delta == 0)
            {
                h = 0;
                s = 0;
            }
            else
            {
                s = delta / (1 - Math.Abs(2 * l - 1));

                if (max == r)
                {
                    h = 60 * (((g - b) / delta) % 6);
                }
                else if (max == g)
                {
                    h = 60 * (((b - r) / delta) + 2);
                }
                else
                {
                    h = 60 * (((r - g) / delta) + 4);
                }

                if (h < 0) h += 360;
            }
        }

        /// <summary>
        /// 颜色转十六进制字符串（带Alpha）
        /// </summary>
        private string ColorToHex(System.Windows.Media.Color color)
        {
            return $"#{color.A:X2}{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        /// <summary>
        /// 解析十六进制颜色字符串（支持 #RRGGBB 和 #AARRGGBB 格式）
        /// </summary>
        private bool TryParseColorHex(string hex, out System.Windows.Media.Color color)
        {
            color = System.Windows.Media.Colors.White;

            if (string.IsNullOrEmpty(hex))
                return false;

            hex = hex.TrimStart('#');

            try
            {
                if (hex.Length == 6)
                {
                    // #RRGGBB 格式，默认 Alpha=255
                    byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    color = System.Windows.Media.Color.FromArgb(255, r, g, b);
                    return true;
                }
                else if (hex.Length == 8)
                {
                    // #AARRGGBB 格式
                    byte a = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                    byte r = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                    byte g = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                    byte b = byte.Parse(hex.Substring(6, 2), System.Globalization.NumberStyles.HexNumber);
                    color = System.Windows.Media.Color.FromArgb(a, r, g, b);
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region UI更新

        /// <summary>
        /// 更新颜色显示（预览、输入框）
        /// </summary>
        private void UpdateColorDisplay()
        {
            _isUpdatingInputs = true;

            // 更新预览
            PreviewColorOverlay.Background = new SolidColorBrush(_selectedColor);

            // 更新 HEX 输入框（不带 Alpha）
            HexInput.Text = $"#{_selectedColor.R:X2}{_selectedColor.G:X2}{_selectedColor.B:X2}";

            // HEX 行的 Alpha 输入框（百分比格式）
            AlphaPercentInput.Text = $"{(int)Math.Round(_opacity * 100)}%";

            // 更新 RGB 输入框
            RInput.Text = _selectedColor.R.ToString();
            GInput.Text = _selectedColor.G.ToString();
            BInput.Text = _selectedColor.B.ToString();

            // RGB 行的 Alpha 输入框（百分比格式）
            AlphaInput.Text = $"{(int)Math.Round(_opacity * 100)}%";

            _isUpdatingInputs = false;
        }

        /// <summary>
        /// 更新明度条渐变（根据当前色相）
        /// </summary>
        private void UpdateBrightnessGradient()
        {
            // 明度条从当前色相的纯色（饱和度=1，明度=0.5）到白色和黑色
            System.Windows.Media.Color topColor = HslToRgb(_hue, _saturation, 1);    // 最亮
            System.Windows.Media.Color bottomColor = HslToRgb(_hue, _saturation, 0); // 最暗

            var gradient = new LinearGradientBrush();
            gradient.StartPoint = new System.Windows.Point(0, 0);
            gradient.EndPoint = new System.Windows.Point(0, 1);
            gradient.GradientStops.Add(new GradientStop(topColor, 0));
            gradient.GradientStops.Add(new GradientStop(bottomColor, 1));

            BrightnessGradient.Fill = gradient;
        }

        /// <summary>
        /// 更新不透明度条渐变（根据当前颜色）
        /// </summary>
        private void UpdateOpacityGradient()
        {
            // 不透明度条从当前颜色的完全透明到完全不透明（从上到下递增）
            System.Windows.Media.Color baseColor = HslToRgb(_hue, _saturation, _brightness);
            System.Windows.Media.Color topColor = System.Windows.Media.Color.FromArgb(0, baseColor.R, baseColor.G, baseColor.B);
            System.Windows.Media.Color bottomColor = System.Windows.Media.Color.FromArgb(255, baseColor.R, baseColor.G, baseColor.B);

            var gradient = new LinearGradientBrush();
            gradient.StartPoint = new System.Windows.Point(0, 0);
            gradient.EndPoint = new System.Windows.Point(0, 1);
            gradient.GradientStops.Add(new GradientStop(topColor, 0));
            gradient.GradientStops.Add(new GradientStop(bottomColor, 1));

            OpacityGradient.Fill = gradient;
        }

        /// <summary>
        /// 更新选择器位置
        /// </summary>
        private void UpdateSelectorPositions()
        {
            // 色谱选择器位置
            double spectrumX = (_hue / 360) * SpectrumBorder.ActualWidth;
            double spectrumY = (1 - _saturation) * SpectrumBorder.ActualHeight;

            SpectrumSelector.Margin = new Thickness(spectrumX - 6, spectrumY - 6, 0, 0);

            // 明度选择器位置
            double brightnessY = (1 - _brightness) * BrightnessBorder.ActualHeight;

            BrightnessSelector.Margin = new Thickness(0, brightnessY - 2, 0, 0);

            // 不透明度选择器位置（从上到下递增）
            double opacityY = _opacity * OpacityBorder.ActualHeight;

            OpacitySelector.Margin = new Thickness(0, opacityY - 2, 0, 0);
        }

        #endregion

        #region 输入框事件

        /// <summary>
        /// HEX 输入框文本变化时更新颜色（仅在输入完整时生效）
        /// </summary>
        private void HexInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingInputs) return;

            string text = HexInput.Text;

            // 只处理完整的6位十六进制输入（#RRGGBB 格式）
            // 不完整的输入不做任何处理，让用户继续输入
            if (text.Length != 7 || !text.StartsWith("#"))
                return;

            string hex = text.Substring(1);

            // 验证是否为有效的6位十六进制
            if (!IsValidHexString(hex))
                return;

            // 输入完整且有效，更新颜色
            if (TryParseHexColor(hex, out byte r, out byte g, out byte b))
            {
                _selectedColor = System.Windows.Media.Color.FromArgb(_selectedColor.A, r, g, b);
                RgbToHsl(_selectedColor, out _hue, out _saturation, out _brightness);
                UpdateColorFromHsl();
                UpdateBrightnessGradient();
                UpdateOpacityGradient();
                UpdateSelectorPositions();

                // 只更新其他输入框，不更新HexInput（避免干扰用户输入）
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

        /// <summary>
        /// 验证字符串是否为有效的十六进制字符串（6位）
        /// </summary>
        private bool IsValidHexString(string hex)
        {
            if (hex.Length != 6)
                return false;

            foreach (char c in hex)
            {
                if (!((c >= '0' && c <= '9') || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f')))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// HEX 行的 Alpha 百分比输入框文本变化时更新
        /// </summary>
        private void AlphaPercentInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingInputs) return;

            // 尝试解析，无效时不处理（让用户继续输入）
            if (TryParseOpacityValue(AlphaPercentInput.Text, out int opacityPercent))
            {
                _opacity = opacityPercent / 100.0;
                UpdateColorFromHsl();
                UpdateSelectorPositions();

                // 只更新其他输入框和预览
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

        /// <summary>
        /// RGB 输入框文本变化时更新颜色（仅在所有值有效时生效）
        /// </summary>
        private void RgbInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingInputs) return;

            // 检查所有RGB输入框是否都有有效数字
            if (TryParseRgbValueWithClamp(RInput.Text, out byte r, out int rRaw) &&
                TryParseRgbValueWithClamp(GInput.Text, out byte g, out int gRaw) &&
                TryParseRgbValueWithClamp(BInput.Text, out byte b, out int bRaw))
            {
                _selectedColor = System.Windows.Media.Color.FromArgb(_selectedColor.A, r, g, b);
                RgbToHsl(_selectedColor, out _hue, out _saturation, out _brightness);
                UpdateColorFromHsl();
                UpdateBrightnessGradient();
                UpdateOpacityGradient();
                UpdateSelectorPositions();

                // 更新其他输入框，同时矫正超范围的RGB值
                _isUpdatingInputs = true;
                PreviewColorOverlay.Background = new SolidColorBrush(_selectedColor);
                HexInput.Text = $"#{_selectedColor.R:X2}{_selectedColor.G:X2}{_selectedColor.B:X2}";
                AlphaPercentInput.Text = $"{(int)Math.Round(_opacity * 100)}%";
                AlphaInput.Text = $"{(int)Math.Round(_opacity * 100)}%";

                // 矫正超范围的RGB输入框显示值
                if (rRaw != r) RInput.Text = r.ToString();
                if (gRaw != g) GInput.Text = g.ToString();
                if (bRaw != b) BInput.Text = b.ToString();

                _isUpdatingInputs = false;
            }
        }

        /// <summary>
        /// RGB 行的 Alpha 百分比输入框文本变化时更新
        /// </summary>
        private void AlphaInput_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isUpdatingInputs) return;

            // 尝试解析，无效时不处理（让用户继续输入）
            if (TryParseOpacityValue(AlphaInput.Text, out int opacityPercent))
            {
                _opacity = opacityPercent / 100.0;
                UpdateColorFromHsl();
                UpdateSelectorPositions();

                // 只更新其他输入框和预览
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

        /// <summary>
        /// 解析十六进制颜色值
        /// </summary>
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
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 解析 RGB 值（0-255），返回矫正后的值和原始值
        /// </summary>
        private bool TryParseRgbValueWithClamp(string text, out byte clampedValue, out int rawValue)
        {
            clampedValue = 0;
            rawValue = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            try
            {
                rawValue = int.Parse(text);
                int clamped = Math.Clamp(rawValue, 0, 255);
                clampedValue = (byte)clamped;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 解析 RGB 值（0-255）
        /// </summary>
        private bool TryParseRgbValue(string text, out byte value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            try
            {
                int parsed = int.Parse(text);
                parsed = Math.Clamp(parsed, 0, 255);
                value = (byte)parsed;
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 解析不透明度百分比值（支持 "100", "100%", "50%" 等格式）
        /// </summary>
        private bool TryParseOpacityValue(string text, out int value)
        {
            value = 0;
            if (string.IsNullOrWhiteSpace(text))
                return false;

            try
            {
                // 移除百分号
                text = text.TrimEnd('%');
                int parsed = int.Parse(text);
                parsed = Math.Clamp(parsed, 0, 100);
                value = parsed;
                return true;
            }
            catch
            {
                return false;
            }
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

        /// <summary>
        /// 窗口加载完成后更新选择器位置
        /// </summary>
        protected override void OnContentRendered(EventArgs e)
        {
            base.OnContentRendered(e);
            UpdateSelectorPositions();
        }
    }
}