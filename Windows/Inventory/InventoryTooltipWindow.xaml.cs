using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using CraftSharp.Controls;
using CraftSharp.Helpers;
using CraftSharp.Models;
using CraftSharp.Services;

namespace CraftSharp.Windows.Inventory
{
    /// <summary>
    /// 物品栏格子 Tooltip 窗口
    /// </summary>
    public partial class InventoryTooltipWindow : Window
    {
        private double _scaleFactor;
        private System.Windows.Media.FontFamily _fontFamily;
        private double _contentMaxWidth; // 内容最大宽度缓存
        private const double MinWidthBase = 0; // 最小宽度
        private const double MaxWidthBase = 150; // 最大宽度
        private const double PaddingBase = 3;
        private const double FontSizeBase = 8;
        private const double ComponentSpacingBase = 0;

        // 预定义颜色
        private static readonly string ColorOriginalName = "#FCFC54";    // 黄色
        private static readonly string ColorFilePath = "#A8A8A8";        // 灰色
        private static readonly string ColorFileType = "#5454FC";       // 蓝色
        private static readonly string ColorFileMissing = "#FC5454";    // 红色

        public InventoryTooltipWindow(double scaleFactor)
        {
            InitializeComponent();

            _scaleFactor = scaleFactor;

            // 加载背景和边框图片
            LoadImages();

            // 设置字体
            _fontFamily = new System.Windows.Media.FontFamily(new Uri("pack://application:,,,/"), "/assets/fonts/unifont-16.0.04.ttf#Unifont");
        }

        private void LoadImages()
        {
            try
            {
                var bgBitmap = Services.ImageService.Instance.LoadBitmapImage("assets/minecraft/textures/gui/sprites/tooltip/background.png");
                if (bgBitmap != null)
                {
                    BackgroundImage.Source = bgBitmap;
                    BackgroundImage.BorderSizeBase = 3; // 九宫格边框宽度（3像素）
                    BackgroundImage.ScaleFactor = _scaleFactor;
                }

                var frameBitmap = Services.ImageService.Instance.LoadBitmapImage("assets/minecraft/textures/gui/sprites/tooltip/frame.png");
                if (frameBitmap != null)
                {
                    FrameImage.Source = frameBitmap;
                    FrameImage.BorderSizeBase = 3; // 九宫格边框宽度（3像素）
                    FrameImage.ScaleFactor = _scaleFactor;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// 设置 Tooltip 内容
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="isMissing">是否丢失</param>
        /// <param name="fileNameColor">文件名颜色配置</param>
        /// <param name="showFileName">显示文件名</param>
        /// <param name="showOriginalName">显示原文件名</param>
        /// <param name="showFilePath">显示文件路径</param>
        /// <param name="showFileType">显示文件类型</param>
        public void SetContent(string filePath, bool isMissing, string fileNameColor,
            bool showFileName, bool showOriginalName, bool showFilePath, bool showFileType)
        {
            ContentPanel.Children.Clear();

            // 重置尺寸状态（避免复用窗口时保留旧尺寸）
            Width = 0;
            Height = 0;

            double padding = PaddingBase * _scaleFactor;
            double fontSize = FontSizeBase * _scaleFactor;
            double componentSpacing = ComponentSpacingBase * _scaleFactor;
            double maxWidth = MaxWidthBase * _scaleFactor;
            double minWidth = MinWidthBase * _scaleFactor;

            // 计算内容最大宽度（用于文本换行）
            _contentMaxWidth = maxWidth - padding * 2;

            // 设置内容面板 Margin
            ContentPanel.Margin = new Thickness(padding);

            if (isMissing)
            {
                // 占位图：只显示文件原始名 + 文件已丢失（两个组件）
                AddTextComponent(Path.GetFileName(filePath), ColorOriginalName, fontSize, true);
                AddSpacing(componentSpacing);
                AddTextComponent("文件已丢失", ColorFileMissing, fontSize, true);
            }
            else
            {
                // 正常文件：根据开关显示组件
                bool hasContent = false;

                // 组件1：文件名（不含后缀）
                if (showFileName)
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                    string line1Color = GetDisplayColor(fileNameColor, fileNameWithoutExt);
                    AddTextComponent(fileNameWithoutExt, line1Color, fontSize, true);
                    hasContent = true;
                }

                // 组件2：文件原始名（含后缀）
                if (showOriginalName)
                {
                    if (hasContent) AddSpacing(componentSpacing);
                    AddTextComponent(Path.GetFileName(filePath), ColorOriginalName, fontSize, true);
                    hasContent = true;
                }

                // 组件3：文件路径
                if (showFilePath)
                {
                    if (hasContent) AddSpacing(componentSpacing);
                    AddTextComponent(filePath, ColorFilePath, fontSize, true);
                    hasContent = true;
                }

                // 组件4：文件类型（后缀）
                if (showFileType)
                {
                    if (hasContent) AddSpacing(componentSpacing);
                    string extension = Path.GetExtension(filePath);
                    if (!string.IsNullOrEmpty(extension))
                    {
                        AddTextComponent(extension, ColorFileType, fontSize, true);
                    }
                    else
                    {
                        AddTextComponent("(无后缀)", ColorFileType, fontSize, true);
                    }
                }

                // 如果所有开关都关闭，至少显示文件名
                if (!hasContent)
                {
                    string fileNameWithoutExt = Path.GetFileNameWithoutExtension(filePath);
                    string line1Color = GetDisplayColor(fileNameColor, fileNameWithoutExt);
                    AddTextComponent(fileNameWithoutExt, line1Color, fontSize, true);
                }
            }

            // 计算并设置窗口尺寸（动态宽度）
            UpdateWindowSize(minWidth, maxWidth);
        }

        /// <summary>
        /// 根据颜色配置获取实际显示颜色
        /// </summary>
        private string GetDisplayColor(string colorConfig, string fileName)
        {
            if (colorConfig == "auto")
            {
                return ColorPickerHelper.GetAutoColorForFileName(fileName);
            }
            return colorConfig;
        }

        /// <summary>
        /// 添加文本组件（带阴影效果）
        /// </summary>
        private void AddTextComponent(string text, string colorHex, double fontSize, bool allowWrap = false)
        {
            var textBlock = new TextBlock
            {
                Text = text,
                FontSize = fontSize,
                FontFamily = _fontFamily,
                Foreground = new SolidColorBrush(ColorPickerHelper.ParseColorHex(colorHex)),
                TextWrapping = allowWrap ? TextWrapping.Wrap : TextWrapping.NoWrap
            };

            // 添加阴影效果（与快捷栏文件名一致）
            var textColor = ColorPickerHelper.ParseColorHex(colorHex);
            var shadowColor = CalculateShadowColor(textColor);
            textBlock.Effect = new DropShadowEffect
            {
                Color = shadowColor,
                Direction = 315,
                ShadowDepth = 0.75 * _scaleFactor,
                BlurRadius = 0,
                Opacity = 1.0
            };

            ContentPanel.Children.Add(textBlock);
        }

        /// <summary>
        /// 根据文本颜色计算阴影颜色（加深）
        /// </summary>
        private System.Windows.Media.Color CalculateShadowColor(System.Windows.Media.Color textColor)
        {
            double darkenFactor = 0.5;
            byte r = (byte)Math.Round(textColor.R * darkenFactor);
            byte g = (byte)Math.Round(textColor.G * darkenFactor);
            byte b = (byte)Math.Round(textColor.B * darkenFactor);
            return System.Windows.Media.Color.FromRgb(r, g, b);
        }

        /// <summary>
        /// 添加行间距
        /// </summary>
        private void AddSpacing(double spacing)
        {
            var spacer = new FrameworkElement
            {
                Height = spacing
            };
            ContentPanel.Children.Add(spacer);
        }

        /// <summary>
        /// 更新窗口尺寸（动态宽度）
        /// </summary>
        private void UpdateWindowSize(double minWidth, double maxWidth)
        {
            double padding = PaddingBase * _scaleFactor;

            // 先测量内容需要的尺寸（无宽度限制）
            ContentPanel.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            double naturalWidth = ContentPanel.DesiredSize.Width;

            // 如果自然宽度超过最大宽度，需要用最大宽度重新测量（触发换行）
            if (naturalWidth > maxWidth - padding * 2)
            {
                ContentPanel.Measure(new System.Windows.Size(maxWidth - padding * 2, double.PositiveInfinity));
                ContentPanel.Arrange(new System.Windows.Rect(0, 0, maxWidth - padding * 2, ContentPanel.DesiredSize.Height));
                Width = maxWidth;
            }
            else
            {
                // 自然宽度在范围内，直接使用
                ContentPanel.Arrange(new System.Windows.Rect(0, 0, naturalWidth, ContentPanel.DesiredSize.Height));
                double dynamicWidth = naturalWidth + padding * 2;
                Width = Math.Max(minWidth, dynamicWidth);
            }

            Height = ContentPanel.ActualHeight + padding * 2;

            // 强制窗口布局更新
            UpdateLayout();

            // 背景和边框使用 Grid 的实际尺寸
            // Grid 会自动填充窗口，NineSliceImage 会填充 Grid
        }

        /// <summary>
        /// 显示 Tooltip（定位到格子右侧，垂直居中）
        /// </summary>
        /// <param name="cellLeft">格子左侧屏幕坐标</param>
        /// <param name="cellTop">格子顶部屏幕坐标</param>
        /// <param name="cellWidth">格子宽度</param>
        /// <param name="cellHeight">格子高度</param>
        public void ShowAtCellPosition(double cellLeft, double cellTop, double cellWidth, double cellHeight)
        {
            // Tooltip 显示在格子右侧，偏移 2*_scaleFactor
            double offsetX = 2 * _scaleFactor;

            // 获取屏幕工作区域
            var mousePos = System.Windows.Forms.Control.MousePosition;
            var screen = System.Windows.Forms.Screen.FromPoint(mousePos);
            double screenRight = screen.WorkingArea.Right;
            double screenBottom = screen.WorkingArea.Bottom;
            double screenTop = screen.WorkingArea.Top;

            // 计算 Tooltip 位置（格子右侧，垂直居中）
            double x = cellLeft + cellWidth + offsetX;
            double y = cellTop + (cellHeight - Height) / 2; // 垂直居中

            // 检查是否超出屏幕右侧
            if (x + Width > screenRight)
            {
                x = cellLeft - Width - offsetX;
            }

            // 检查是否超出屏幕底部
            if (y + Height > screenBottom)
            {
                y = screenBottom - Height - 5;
            }

            // 确保不超出屏幕顶部
            if (y < screenTop)
            {
                y = screenTop + 5;
            }

            Left = x;
            Top = y;

            Show();
        }
    }
}