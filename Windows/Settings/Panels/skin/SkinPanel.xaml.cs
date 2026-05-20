using CraftSharp.Models;
using CraftSharp.Windows.Settings.Panels.Skin.Components;
using CraftSharp.Windows.Skin;
using System.Collections.ObjectModel;
using System.Linq;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CraftSharp.Windows.Settings.Panels.Skin
{
    public partial class SkinPanel : global::System.Windows.Controls.UserControl
    {
        private AppSettings _settings;
        private global::System.Windows.Controls.Border[] _optionBorders;
        private global::System.Windows.Controls.TextBlock[] _optionTexts;
        private ObservableCollection<SkinItem> _skinItems;
        private SkinItemControl? _selectedSkinControl;

        // 当前选中的类型：wide 或 slim
        private bool _isWide = true;

        // 加载计数器
        private int _loadedCount = 0;
        private int _totalSkinCount = 0;

        // 父窗口引用（用于打开弹窗）
        private global::System.Windows.Window? _parentWindow;

        private static readonly string WideSkinFolder = "assets/minecraft/textures/entity/player/wide";
        private static readonly string SlimSkinFolder = "assets/minecraft/textures/entity/player/slim";
        private static readonly string WideUvPath = "assets/minecraft/textures/entity/player/uv/wide.json";
        private static readonly string SlimUvPath = "assets/minecraft/textures/entity/player/uv/slim.json";

        public SkinPanel(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            _optionBorders = new global::System.Windows.Controls.Border[] { BorderSteve, BorderAlex, BorderUpload };
            _optionTexts = new global::System.Windows.Controls.TextBlock[] { TextSteve, TextAlex, TextUpload };
            _skinItems = new ObservableCollection<SkinItem>();
            SkinGrid.ItemsSource = _skinItems;

            // 根据设置初始化皮肤类型
            _isWide = _settings.Player.SkinType == "wide";

            // 初始化选项按钮状态
            UpdateOptionButtonState(_isWide);

            LoadSkinsAsync();
        }

        public void SetParentWindow(global::System.Windows.Window parent)
        {
            _parentWindow = parent;
        }

        private void RootGrid_PreviewMouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            // 关闭可能打开的右键菜单
            foreach (var item in _skinItems)
            {
                var container = SkinGrid.ItemContainerGenerator.ContainerFromItem(item);
                if (container is System.Windows.FrameworkElement element)
                {
                    var control = FindSkinItemControl(element);
                    if (control?.ItemBorder.ContextMenu?.IsOpen == true)
                        control.ItemBorder.ContextMenu.IsOpen = false;
                }
            }
        }

        private async void LoadSkinsAsync()
        {
            // 显示加载提示，隐藏皮肤网格
            LoadingOverlay.Visibility = Visibility.Visible;
            SkinGrid.Visibility = Visibility.Hidden;
            SkinPreview.Clear();
            _selectedSkinControl = null;
            _loadedCount = 0;

            // 强制 UI 更新，让 LoadingOverlay 先显示出来
            await System.Windows.Threading.Dispatcher.Yield(System.Windows.Threading.DispatcherPriority.Background);

            var skinFolder = _isWide ? WideSkinFolder : SlimSkinFolder;
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var fullPath = Path.Combine(basePath, skinFolder);
            var skinsDir = Path.Combine(fullPath, "skins");

            // 先加载上传皮肤（skins 子目录），再加载内置皮肤
            var newItems = new ObservableCollection<SkinItem>();

            // 上传皮肤
            if (Directory.Exists(skinsDir))
            {
                foreach (var file in Directory.GetFiles(skinsDir, "*.png"))
                {
                    newItems.Add(new SkinItem
                    {
                        Name = Path.GetFileNameWithoutExtension(file),
                        Path = file,
                        IsWide = _isWide,
                        IsCustom = true
                    });
                }
            }

            // 内置皮肤
            if (Directory.Exists(fullPath))
            {
                foreach (var file in Directory.GetFiles(fullPath, "*.png"))
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    var displayName = string.IsNullOrEmpty(name) ? name :
                        name.Substring(0, 1).ToUpper() + (name.Length > 1 ? name.Substring(1) : "");

                    newItems.Add(new SkinItem
                    {
                        Name = displayName,
                        Path = file,
                        IsWide = _isWide,
                        IsCustom = false
                    });
                }
            }

            _totalSkinCount = newItems.Count;
            _skinItems = newItems;
            SkinGrid.ItemsSource = _skinItems;

            if (_totalSkinCount == 0)
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                SkinGrid.Visibility = Visibility.Visible;
            }
        }

        private void OptionBorder_Click(object sender, global::System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is global::System.Windows.Controls.Border clickedBorder)
            {
                for (int i = 0; i < _optionBorders.Length; i++)
                {
                    var border = _optionBorders[i];
                    var text = _optionTexts[i];

                    if (border == clickedBorder)
                    {
                        border.Background = TryFindResource("AccentBrush") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(0, 120, 215));
                        text.Foreground = TryFindResource("TextPrimaryBrush") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(255, 255, 255));
                        text.FontWeight = FontWeights.Medium;
                    }
                    else
                    {
                        border.Background = Brushes.Transparent;
                        text.Foreground = TryFindResource("TextSecondaryBrush") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(150, 150, 150));
                        text.FontWeight = FontWeights.Normal;
                    }
                }

                // 切换 wide/slim 类型
                if (clickedBorder == BorderSteve)
                {
                    _isWide = true;
                    LoadSkinsAsync();
                }
                else if (clickedBorder == BorderAlex)
                {
                    _isWide = false;
                    LoadSkinsAsync();
                }
                else if (clickedBorder == BorderUpload)
                {
                    // 打开上传弹窗
                    OpenUploadWindow();
                }
            }
        }

        private void OpenUploadWindow()
        {
            var uploadWindow = new UploadSkinWindow();
            uploadWindow.Owner = _parentWindow;

            if (uploadWindow.ShowDialog() == true)
            {
                // 上传成功，立即使用新皮肤
                var newSkinPath = uploadWindow.ResultSkinPath!;
                var newIsWide = uploadWindow.ResultIsWide;

                // 保存配置并刷新物品栏模型
                SetCurrentSkin(newSkinPath, newIsWide);

                // 切换到新上传的皮肤类型并刷新列表
                _isWide = newIsWide;
                LoadSkinsAsync();
                UpdateOptionButtonState(newIsWide);
            }
        }

        private void UpdateOptionButtonState(bool isWide)
        {
            // 根据类型更新选中状态
            var selectedBorder = isWide ? BorderSteve : BorderAlex;
            var selectedText = isWide ? TextSteve : TextAlex;

            for (int i = 0; i < _optionBorders.Length; i++)
            {
                var border = _optionBorders[i];
                var text = _optionTexts[i];

                if (border == selectedBorder)
                {
                    border.Background = TryFindResource("AccentBrush") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(0, 120, 215));
                    text.Foreground = TryFindResource("TextPrimaryBrush") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(255, 255, 255));
                    text.FontWeight = FontWeights.Medium;
                }
                else
                {
                    border.Background = Brushes.Transparent;
                    text.Foreground = TryFindResource("TextSecondaryBrush") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(150, 150, 150));
                    text.FontWeight = FontWeights.Normal;
                }
            }
        }

        private void SkinItemControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is SkinItemControl control && control.DataContext is SkinItem skinItem)
            {
                var uvPath = skinItem.IsWide ? WideUvPath : SlimUvPath;
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var fullUvPath = Path.Combine(basePath, uvPath);

                control.LoadSkin(skinItem.Path, fullUvPath, skinItem.IsWide);
                control.IsCustomSkin = skinItem.IsCustom;
                control.SkinName = skinItem.Name;

                // 检查是否是当前皮肤（根据配置）
                var currentSkinFullPath = Path.GetFullPath(Path.Combine(basePath, _settings.Player.Skin));
                var isCurrentSkin = Path.GetFullPath(skinItem.Path) == currentSkinFullPath;
                control.IsCurrentSkin = isCurrentSkin;

                if (isCurrentSkin && _selectedSkinControl == null)
                {
                    _selectedSkinControl = control;
                    control.IsSelected = true;
                    SkinPreview.LoadSkin(skinItem.Path, skinItem.IsWide);
                }

                // 订阅右键菜单事件
                control.RequestSetCurrent += SkinItem_RequestSetCurrent;
                control.RequestRename += SkinItem_RequestRename;
                control.RequestRemove += SkinItem_RequestRemove;

                // 计数并检查是否全部加载完成
                _loadedCount++;
                if (_loadedCount >= _totalSkinCount)
                {
                    SkinGrid.Visibility = Visibility.Visible;
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void SkinItemControl_Selected(object sender, EventArgs e)
        {
            if (sender is SkinItemControl control)
            {
                // 取消之前的选中
                if (_selectedSkinControl != null)
                {
                    _selectedSkinControl.IsSelected = false;
                }

                // 设置新的选中
                _selectedSkinControl = control;
                control.IsSelected = true;

                // 更新右侧预览
                if (control.DataContext is SkinItem skinItem)
                {
                    SkinPreview.LoadSkin(skinItem.Path, skinItem.IsWide);

                    // 设置当前皮肤并保存配置
                    SetCurrentSkin(skinItem.Path, skinItem.IsWide);
                }
            }
        }

        /// <summary>
        /// 设置当前皮肤并保存配置，同时刷新物品栏窗口的玩家模型
        /// </summary>
        private void SetCurrentSkin(string skinPath, bool isWide)
        {
            // 转换为相对路径（相对于程序目录）
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var relativePath = skinPath.StartsWith(basePath)
                ? skinPath.Substring(basePath.Length).TrimStart('\\', '/')
                : skinPath;

            var skinType = isWide ? "wide" : "slim";

            // 更新设置并保存
            _settings.Player.Skin = relativePath;
            _settings.Player.SkinType = skinType;

            // 保存配置文件
            if (System.Windows.Application.Current is App app)
            {
                app.SaveSettings();
                // 刷新物品栏窗口的玩家模型
                app.LoadPlayerSkin(skinPath, isWide);
            }

            // 更新所有皮肤项的 IsCurrentSkin 状态
            UpdateCurrentSkinStates(skinPath);
        }

        private void UpdateCurrentSkinStates(string currentSkinPath)
        {
            var normalizedCurrent = Path.GetFullPath(currentSkinPath);
            foreach (var item in _skinItems)
            {
                // 找到对应的 SkinItemControl 并更新状态
                var container = SkinGrid.ItemContainerGenerator.ContainerFromItem(item);
                if (container is FrameworkElement element)
                {
                    var control = FindSkinItemControl(element);
                    if (control != null)
                    {
                        control.IsCurrentSkin = Path.GetFullPath(control.SkinPath!) == normalizedCurrent;
                    }
                }
            }
        }

        private SkinItemControl? FindSkinItemControl(DependencyObject parent)
        {
            if (parent is SkinItemControl control)
                return control;

            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                var result = FindSkinItemControl(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private void SkinItem_RequestSetCurrent(object? sender, EventArgs e)
        {
            if (sender is SkinItemControl control && control.DataContext is SkinItem skinItem)
            {
                SetCurrentSkin(skinItem.Path, skinItem.IsWide);

                // 同时更新选中状态
                if (_selectedSkinControl != null)
                    _selectedSkinControl.IsSelected = false;
                _selectedSkinControl = control;
                control.IsSelected = true;
                SkinPreview.LoadSkin(skinItem.Path, skinItem.IsWide);
            }
        }

        private void SkinItem_RequestRename(object? sender, EventArgs e)
        {
            if (sender is SkinItemControl control && control.DataContext is SkinItem skinItem)
            {
                var renameWindow = new RenameSkinWindow(skinItem.Name);
                renameWindow.Owner = _parentWindow;
                renameWindow.ShowDialog();

                if (!renameWindow.IsConfirmed)
                    return;

                var newName = renameWindow.NewName!;
                if (newName == skinItem.Name)
                    return;

                // 检查重名
                var dir = Path.GetDirectoryName(skinItem.Path)!;
                var newPath = Path.Combine(dir, newName + ".png");
                if (File.Exists(newPath))
                {
                    System.Windows.MessageBox.Show(
                        $"已存在同名皮肤 \"{newName}\"",
                        "重命名",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Warning);
                    return;
                }

                // 重命名文件
                File.Move(skinItem.Path, newPath);

                // 如果是当前皮肤，更新配置
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var currentSkinFullPath = Path.GetFullPath(Path.Combine(basePath, _settings.Player.Skin));
                if (Path.GetFullPath(skinItem.Path) == currentSkinFullPath)
                {
                    var relativePath = newPath.StartsWith(basePath)
                        ? newPath.Substring(basePath.Length).TrimStart('\\', '/')
                        : newPath;
                    _settings.Player.Skin = relativePath;
                    if (System.Windows.Application.Current is App app)
                        app.SaveSettings();
                }

                // 刷新列表
                LoadSkinsAsync();
            }
        }

        private void SkinItem_RequestRemove(object? sender, EventArgs e)
        {
            if (sender is SkinItemControl control && control.DataContext is SkinItem skinItem)
            {
                var confirmWindow = new RemoveSkinConfirmWindow(skinItem.Name);
                confirmWindow.Owner = _parentWindow;
                confirmWindow.ShowDialog();

                if (!confirmWindow.IsConfirmed)
                    return;

                // 删除文件
                try
                {
                    File.Delete(skinItem.Path);
                }
                catch
                {
                    System.Windows.MessageBox.Show("删除文件失败", "移除皮肤",
                        System.Windows.MessageBoxButton.OK,
                        System.Windows.MessageBoxImage.Error);
                    return;
                }

                // 如果删除的是当前皮肤，回退到默认皮肤
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var currentSkinFullPath = Path.GetFullPath(Path.Combine(basePath, _settings.Player.Skin));
                if (Path.GetFullPath(skinItem.Path) == currentSkinFullPath)
                {
                    var defaultSkinPath = Path.Combine(basePath, "assets/minecraft/textures/entity/player/wide/steve.png");
                    SetCurrentSkin(defaultSkinPath, true);
                }

                // 刷新列表
                LoadSkinsAsync();
            }
        }
    }
}