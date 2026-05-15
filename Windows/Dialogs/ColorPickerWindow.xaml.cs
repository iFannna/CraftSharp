using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CraftSharp.Windows.Dialogs
{
    /// <summary>
    /// 颜色选择弹窗
    /// </summary>
    public partial class ColorPickerWindow : Wpf.Ui.Controls.FluentWindow
    {
        /// <summary>
        /// 用户选择的颜色（十六进制格式）
        /// </summary>
        public string SelectedColorHex { get; private set; } = "#ECECEC";

        /// <summary>
        /// 用户选择的颜色（Color对象）
        /// </summary>
        public System.Windows.Media.Color SelectedColor { get; private set; } = System.Windows.Media.Color.FromRgb(236, 236, 236);

        public ColorPickerWindow()
        {
            InitializeComponent();

            // 监听滑块变化
            RedSlider.ValueChanged += Slider_ValueChanged;
            GreenSlider.ValueChanged += Slider_ValueChanged;
            BlueSlider.ValueChanged += Slider_ValueChanged;
        }

        /// <summary>
        /// 构造函数：传入初始颜色
        /// </summary>
        public ColorPickerWindow(string initialColorHex)
        {
            InitializeComponent();

            // 解析初始颜色
            if (TryParseColorHex(initialColorHex, out var initialColor))
            {
                RedSlider.Value = initialColor.R;
                GreenSlider.Value = initialColor.G;
                BlueSlider.Value = initialColor.B;
                SelectedColor = initialColor;
                SelectedColorHex = initialColorHex;
                UpdatePreview(initialColor);
            }

            // 监听滑块变化
            RedSlider.ValueChanged += Slider_ValueChanged;
            GreenSlider.ValueChanged += Slider_ValueChanged;
            BlueSlider.ValueChanged += Slider_ValueChanged;
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // 更新数值显示
            RedValue.Text = ((int)RedSlider.Value).ToString();
            GreenValue.Text = ((int)GreenSlider.Value).ToString();
            BlueValue.Text = ((int)BlueSlider.Value).ToString();

            // 更新颜色
            System.Windows.Media.Color color = System.Windows.Media.Color.FromRgb(
                (byte)RedSlider.Value,
                (byte)GreenSlider.Value,
                (byte)BlueSlider.Value
            );
            SelectedColor = color;
            SelectedColorHex = ColorToHex(color);
            UpdatePreview(color);
        }

        /// <summary>
        /// 更新预览区域
        /// </summary>
        private void UpdatePreview(System.Windows.Media.Color color)
        {
            PreviewBorder.Background = new SolidColorBrush(color);
            HexValueText.Text = SelectedColorHex;
        }

        /// <summary>
        /// 将颜色转换为十六进制字符串
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

            // 移除 # 前缀
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

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }
    }
}