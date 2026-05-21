using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using CraftSharp.Services.Core;
using CraftSharp.Services.Hud;
using CraftSharp.Services.Slot;
using CraftSharp.Services.Resource;

namespace CraftSharp.Windows.Dialogs
{
    /// <summary>
    /// 样式预览弹窗 - 走马灯展示物品栏样式图片
    /// </summary>
    public partial class StylePreviewWindow : Wpf.Ui.Controls.FluentWindow
    {
        private List<string> _styleFiles = new();
        private int _currentIndex = 0;
        private double _scaleFactor;

        /// <summary>
        /// 选中的样式文件名（如 inventory.png）
        /// </summary>
        public string SelectedStyle { get; private set; } = "inventory.png";

        /// <summary>
        /// 样式选择确认事件
        /// </summary>
        public event EventHandler<string>? StyleSelected;

        public StylePreviewWindow(string currentStyle = "inventory.png")
        {
            InitializeComponent();

            // 初始化缩放因子
            ScaleService.Instance.Initialize();
            _scaleFactor = ScaleService.Instance.ScaleFactor;

            // 加载样式文件列表
            LoadStyleFiles();

            // 设置当前样式索引
            int targetIndex = _styleFiles.IndexOf(currentStyle);
            if (targetIndex >= 0)
                _currentIndex = targetIndex;
            else
                _currentIndex = 0;

            // 显示当前样式
            UpdateDisplay();
        }

        /// <summary>
        /// 加载 assets/minecraft/textures/gui/container 目录下的 PNG 文件
        /// </summary>
        private void LoadStyleFiles()
        {
            var containerPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "assets/minecraft/textures/gui/container");

            if (Directory.Exists(containerPath))
            {
                var files = Directory.GetFiles(containerPath, "*.png");
                foreach (var file in files)
                {
                    // 只添加 PNG 文件名（不含路径）
                    string fileName = Path.GetFileName(file);
                    _styleFiles.Add(fileName);
                }

                // 排序：inventory.png 放在最前面
                _styleFiles.Sort((a, b) =>
                {
                    if (a == "inventory.png") return -1;
                    if (b == "inventory.png") return 1;
                    return string.Compare(a, b, StringComparison.Ordinal);
                });
            }

            if (_styleFiles.Count == 0)
            {
                _styleFiles.Add("inventory.png");
            }
        }

        /// <summary>
        /// 更新显示内容（图片、序号、样式名称）
        /// </summary>
        private void UpdateDisplay()
        {
            if (_styleFiles.Count == 0 || _currentIndex < 0 || _currentIndex >= _styleFiles.Count)
                return;

            string fileName = _styleFiles[_currentIndex];
            string filePath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "assets/minecraft/textures/gui/container",
                fileName);

            // 加载图片
            if (File.Exists(filePath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(filePath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                StyleImage.Source = bitmap;

                // 设置 Viewbox 最大尺寸：原图尺寸 * scaleFactor
                if (bitmap.PixelWidth > 0 && bitmap.PixelHeight > 0)
                {
                    double maxWidth = bitmap.PixelWidth * _scaleFactor;
                    double maxHeight = bitmap.PixelHeight * _scaleFactor;
                    ImageViewbox.MaxWidth = Math.Min(maxWidth, 400);
                    ImageViewbox.MaxHeight = Math.Min(maxHeight, 280);
                }
            }
            else
            {
                StyleImage.Source = null;
            }

            // 更新序号
            IndexText.Text = $"{_currentIndex + 1} / {_styleFiles.Count}";

            // 更新样式名称（国际化）
            string styleNameKey = GetStyleNameKey(fileName);
            string? styleName = System.Windows.Application.Current.TryFindResource(styleNameKey) as string;
            if (string.IsNullOrEmpty(styleName))
            {
                // 如果没有找到国际化字符串，使用文件名
                styleName = Path.GetFileNameWithoutExtension(fileName);
            }
            StyleNameText.Text = styleName;

            // 更新箭头按钮状态
            PrevButton.IsEnabled = _currentIndex > 0;
            NextButton.IsEnabled = _currentIndex < _styleFiles.Count - 1;
        }

        /// <summary>
        /// 根据文件名获取国际化字符串 Key
        /// </summary>
        private string GetStyleNameKey(string fileName)
        {
            // inventory.png → InventoryStyleInventory
            // brewing_stand.png → InventoryStyleBrewingStand
            string baseName = Path.GetFileNameWithoutExtension(fileName);
            // 转换为 PascalCase
            string pascalName = ConvertToPascalCase(baseName);
            return $"InventoryStyle{pascalName}";
        }

        /// <summary>
        /// 将 snake_case 转换为 PascalCase
        /// </summary>
        private string ConvertToPascalCase(string snakeCase)
        {
            if (string.IsNullOrEmpty(snakeCase))
                return "";

            var parts = snakeCase.Split('_');
            var result = new System.Text.StringBuilder();
            foreach (var part in parts)
            {
                if (part.Length > 0)
                {
                    result.Append(char.ToUpperInvariant(part[0]));
                    if (part.Length > 1)
                        result.Append(part.Substring(1));
                }
            }
            return result.ToString();
        }

        /// <summary>
        /// 上一页按钮点击
        /// </summary>
        private void PrevButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex > 0)
            {
                _currentIndex--;
                UpdateDisplay();
            }
        }

        /// <summary>
        /// 下一页按钮点击
        /// </summary>
        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentIndex < _styleFiles.Count - 1)
            {
                _currentIndex++;
                UpdateDisplay();
            }
        }

        /// <summary>
        /// 图片点击选择样式
        /// </summary>
        private void StyleImage_Click(object sender, MouseButtonEventArgs e)
        {
            if (_styleFiles.Count == 0 || _currentIndex < 0 || _currentIndex >= _styleFiles.Count)
                return;

            SelectedStyle = _styleFiles[_currentIndex];
            StyleSelected?.Invoke(this, SelectedStyle);
            DialogResult = true;
            Close();
        }
    }
}