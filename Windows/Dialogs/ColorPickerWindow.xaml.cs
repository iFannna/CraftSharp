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

        // 颜色状态
        private System.Windows.Media.Color _selectedColor = System.Windows.Media.Colors.White;
        private bool _isDraggingSpectrum = false;
        private bool _isDraggingBrightness = false;

        /// <summary>
        /// 用户选择的颜色（十六进制格式）
        /// </summary>
        public string SelectedColorHex { get; private set; } = "#FFFFFF";

        /// <summary>
        /// 用户选择的颜色（Color对象）
        /// </summary>
        public System.Windows.Media.Color SelectedColor { get; private set; } = System.Windows.Media.Color.FromRgb(255, 255, 255);

        /// <summary>
        /// 初始颜色（构造时传入）
        /// </summary>
        private System.Windows.Media.Color _initialColor;

        public ColorPickerWindow()
        {
            InitializeComponent();
            UpdateColorDisplay();
            UpdateBrightnessGradient();
        }

        /// <summary>
        /// 构造函数：传入初始颜色
        /// </summary>
        public ColorPickerWindow(string initialColorHex)
        {
            InitializeComponent();

            // 解析初始颜色
            if (TryParseColorHex(initialColorHex, out _initialColor))
            {
                // 将RGB转换为HSL
                RgbToHsl(_initialColor, out _hue, out _saturation, out _brightness);
                SelectedColor = _initialColor;
                SelectedColorHex = initialColorHex;
                _selectedColor = _initialColor;
            }

            UpdateColorDisplay();
            UpdateBrightnessGradient();
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
                UpdateSelectorPositions();
            }
        }

        #endregion

        #region 颜色计算

        /// <summary>
        /// 根据HSL值更新颜色
        /// </summary>
        private void UpdateColorFromHsl()
        {
            _selectedColor = HslToRgb(_hue, _saturation, _brightness);
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
        /// 颜色转十六进制字符串
        /// </summary>
        private string ColorToHex(System.Windows.Media.Color color)
        {
            return $"#{color.R:X2}{color.G:X2}{color.B:X2}";
        }

        /// <summary>
        /// 解析十六进制颜色字符串
        /// </summary>
        private bool TryParseColorHex(string hex, out System.Windows.Media.Color color)
        {
            color = System.Windows.Media.Colors.White;

            if (string.IsNullOrEmpty(hex))
                return false;

            hex = hex.TrimStart('#');

            if (hex.Length != 6)
                return false;

            try
            {
                byte r = byte.Parse(hex.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hex.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hex.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                color = System.Windows.Media.Color.FromRgb(r, g, b);
                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion

        #region UI更新

        /// <summary>
        /// 更新颜色显示（预览、十六进制、RGB）
        /// </summary>
        private void UpdateColorDisplay()
        {
            PreviewBorder.Background = new SolidColorBrush(_selectedColor);
            HexValueText.Text = SelectedColorHex;
            RgbValueText.Text = $"R:{_selectedColor.R} G:{_selectedColor.G} B:{_selectedColor.B}";
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