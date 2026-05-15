using System;
using System.Windows;
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
        // 当前选择的颜色参数
        private double _hue = 0;           // 色相 0-360
        private double _saturation = 1;    // 饱和度 0-1
        private double _brightness = 1;    // 明度 0-1
        private double _opacity = 1;       // 不透明度 0-1

        // 颜色状态
        private System.Windows.Media.Color _selectedColor = System.Windows.Media.Colors.White;
        private bool _isDraggingSpectrum = false;
        private bool _isDraggingBrightness = false;
        private bool _isDraggingOpacity = false;

        /// <summary>
        /// 用户选择的颜色（十六进制格式，带Alpha）
        /// </summary>
        public string SelectedColorHex { get; private set; } = "#FFFFFFFF";

        /// <summary>
        /// 用户选择的颜色（Color对象，带Alpha）
        /// </summary>
        public System.Windows.Media.Color SelectedColor { get; private set; } = System.Windows.Media.Color.FromArgb(255, 255, 255, 255);

        /// <summary>
        /// 初始颜色（构造时传入）
        /// </summary>
        private System.Windows.Media.Color _initialColor;

        public ColorPickerWindow()
        {
            InitializeComponent();
            UpdateColorDisplay();
            UpdateBrightnessGradient();
            UpdateOpacityGradient();
        }

        /// <summary>
        /// 构造函数：传入初始颜色
        /// </summary>
        public ColorPickerWindow(string initialColorHex)
        {
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
            UpdateSpectrumFromMouse(e);
        }

        /// <summary>
        /// 色谱区域鼠标移动
        /// </summary>
        private void Spectrum_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDraggingSpectrum)
            {
                UpdateSpectrumFromMouse(e);
            }
        }

        /// <summary>
        /// 色谱区域鼠标释放
        /// </summary>
        private void Spectrum_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingSpectrum = false;
        }

        /// <summary>
        /// 明度条鼠标按下
        /// </summary>
        private void Brightness_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingBrightness = true;
            UpdateBrightnessFromMouse(e);
        }

        /// <summary>
        /// 明度条鼠标移动
        /// </summary>
        private void Brightness_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDraggingBrightness)
            {
                UpdateBrightnessFromMouse(e);
            }
        }

        /// <summary>
        /// 明度条鼠标释放
        /// </summary>
        private void Brightness_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingBrightness = false;
        }

        /// <summary>
        /// 不透明度条鼠标按下
        /// </summary>
        private void Opacity_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDraggingOpacity = true;
            UpdateOpacityFromMouse(e);
        }

        /// <summary>
        /// 不透明度条鼠标移动
        /// </summary>
        private void Opacity_MouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDraggingOpacity)
            {
                UpdateOpacityFromMouse(e);
            }
        }

        /// <summary>
        /// 不透明度条鼠标释放
        /// </summary>
        private void Opacity_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDraggingOpacity = false;
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
        /// 更新颜色显示（预览、十六进制、RGB、Alpha）
        /// </summary>
        private void UpdateColorDisplay()
        {
            PreviewColorOverlay.Background = new SolidColorBrush(_selectedColor);
            HexValueText.Text = SelectedColorHex;
            int opacityPercent = (int)Math.Round(_opacity * 100);
            RgbValueText.Text = $"R:{_selectedColor.R} G:{_selectedColor.G} B:{_selectedColor.B} A:{opacityPercent}%";
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