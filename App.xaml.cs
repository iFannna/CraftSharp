using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CraftSharp.Windows;
using CraftSharp.Services;
using Newtonsoft.Json;
using Hardcodet.Wpf.TaskbarNotification;

namespace CraftSharp
{
    public partial class App : System.Windows.Application
    {
        private StatusBarWindow? _statusBarWindow;
        private InventoryWindow? _inventoryWindow;
        private SettingsWindow? _settingsWindow;
        private TaskbarIcon? _taskbarIcon;
        private System.Windows.Controls.ContextMenu? _trayContextMenu;
        private System.Windows.Controls.MenuItem? _showMainItem;
        private System.Windows.Controls.MenuItem? _showStatusBarItem;
        private System.Windows.Controls.MenuItem? _hideStatusBarItem;
        private System.Windows.Controls.MenuItem? _openInventoryItem;
        private System.Windows.Controls.MenuItem? _exitItem;
        private string _settingsPath = "";
        private Models.AppSettings? _appSettings;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 初始化自定义颜色画刷（必须在窗口创建之前）
            InitializeBrushes();

            // 加载设置
            _settingsPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "settings.json");
            LoadSettings();

            // 初始化语言
            LocalizationService.Instance.Initialize(_appSettings?.Language ?? "简体中文");

            // 初始化主题
            ThemeService.Instance.Initialize(_appSettings?.Theme ?? "跟随系统");

            // 初始化字体
            FontService.Instance.Initialize(_appSettings?.Font ?? "微软雅黑");

            // 创建设置窗口（主窗口）
            _settingsWindow = new SettingsWindow(_appSettings!);

            // 如果开启"记住位置"，在创建窗口前设置跳过默认定位
            if (_appSettings?.StatusBarRememberPosition ?? false)
            {
                CraftSharp.Windows.StatusBarWindow.ShouldSkipDefaultPositioning = true;
            }

            // 创建快捷栏窗口
            _statusBarWindow = new StatusBarWindow();

            // 初始化状态栏服务
            StatusBarService.Instance.Initialize(_statusBarWindow);

            // 监听状态栏位置变化（即时保存到配置文件）
            _statusBarWindow.PositionChanged += (s, e) =>
            {
                if (_appSettings?.StatusBarRememberPosition ?? false)
                {
                    _appSettings.StatusBarPositionX = _statusBarWindow.Left;
                    _appSettings.StatusBarPositionY = _statusBarWindow.Top;
                    SaveSettings();
                }
            };

            // 初始化副手槽配置
            StatusBarService.Instance.SetOffhandConfig(
                _appSettings?.HotbarLeftOffhand ?? false,
                _appSettings?.HotbarRightOffhand ?? false);

            // 初始化快捷栏可见性
            StatusBarService.Instance.SetHotbarVisible(_appSettings?.HotbarVisible ?? true);

            // 状态栏位置定位：在窗口 Loaded 后定位（此时尺寸已计算好）
            _statusBarWindow.Loaded += (s, e) =>
            {
                if (_appSettings?.StatusBarRememberPosition ?? false)
                {
                    // 记住位置开启 → 使用保存的位置
                    StatusBarService.Instance.RestorePosition(
                        _appSettings.StatusBarPositionX,
                        _appSettings.StatusBarPositionY);
                }
                else
                {
                    // 记住位置关闭 → 定位到屏幕底部水平居中
                    StatusBarService.Instance.PositionToScreenBottomCenter();
                }
            };

            // 根据设置决定是否显示状态栏
            if (_appSettings?.StatusBarVisible ?? true)
                _statusBarWindow.Show();
            else
                _statusBarWindow.Hide();

            // 创建背包窗口（隐藏，按E键打开）
            _inventoryWindow = new InventoryWindow();
            _inventoryWindow.Hide();

            // 创建系统托盘图标（使用纯 WPF 实现）
            CreateTaskbarIcon();

            // 监听语言切换事件
            LocalizationService.Instance.LanguageChanged += UpdateTrayMenuTexts;

            _settingsWindow.Show();

            // 设置窗口关闭时最小化到托盘
            _settingsWindow.Closing += (s, e) =>
            {
                if (_taskbarIcon != null)
                {
                    e.Cancel = true;
                    _settingsWindow.Hide();
                }
            };

            // 注册全局快捷键
            RegisterHotkeys();
        }

        /// <summary>
        /// 加载设置
        /// </summary>
        private void LoadSettings()
        {
            if (System.IO.File.Exists(_settingsPath))
            {
                try
                {
                    var json = System.IO.File.ReadAllText(_settingsPath);
                    _appSettings = JsonConvert.DeserializeObject<Models.AppSettings>(json);
                }
                catch { _appSettings = new Models.AppSettings(); }
            }
            else
            {
                _appSettings = new Models.AppSettings();
            }
        }

        /// <summary>
        /// 创建系统托盘图标（使用纯 WPF 实现）
        /// </summary>
        private void CreateTaskbarIcon()
        {
            _taskbarIcon = new TaskbarIcon
            {
                IconSource = new System.Drawing.Icon(SystemIcons.Application, 16, 16).ToImageSource(),
                ToolTipText = "Craft#"
            };

            // 双击显示设置窗口
            _taskbarIcon.TrayMouseDoubleClick += (s, e) =>
            {
                _settingsWindow?.Show();
                _settingsWindow?.Activate();
            };

            // 创建 Fluent Design 风格的 ContextMenu
            var contextMenu = CreateTrayContextMenu();
            _taskbarIcon.ContextMenu = contextMenu;

            // 初始化图标服务（动态加载图标）
            IconService.Instance.InitializeForTaskbarIcon(
                _appSettings?.AppIconPath ?? "minecraft/textures/block/block/glass.png",
                _taskbarIcon,
                _settingsWindow);
        }

        /// <summary>
        /// 创建托盘右键菜单（Fluent Design 风格）
        /// </summary>
        private System.Windows.Controls.ContextMenu CreateTrayContextMenu()
        {
            _trayContextMenu = new System.Windows.Controls.ContextMenu
            {
                Style = (Style)FindResource("WpfUiContextMenuStyle"),
                Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint
            };

            // 显示主窗口
            _showMainItem = new System.Windows.Controls.MenuItem
            {
                Header = TryFindResource("TrayShowMain") as string ?? "显示主窗口",
                Style = (Style)FindResource("WpfUiMenuItemStyle")
            };
            _showMainItem.Click += (s, e) =>
            {
                _trayContextMenu.IsOpen = false;
                _settingsWindow?.Show();
                _settingsWindow?.Activate();
            };
            _trayContextMenu.Items.Add(_showMainItem);

            // 显示状态栏
            _showStatusBarItem = new System.Windows.Controls.MenuItem
            {
                Header = TryFindResource("TrayShowStatusBar") as string ?? "显示状态栏",
                Style = (Style)FindResource("WpfUiMenuItemStyle")
            };
            _showStatusBarItem.Click += (s, e) =>
            {
                _trayContextMenu.IsOpen = false;
                _statusBarWindow?.Show();
            };
            _trayContextMenu.Items.Add(_showStatusBarItem);

            // 隐藏状态栏
            _hideStatusBarItem = new System.Windows.Controls.MenuItem
            {
                Header = TryFindResource("TrayHideStatusBar") as string ?? "隐藏状态栏",
                Style = (Style)FindResource("WpfUiMenuItemStyle")
            };
            _hideStatusBarItem.Click += (s, e) =>
            {
                _trayContextMenu.IsOpen = false;
                _statusBarWindow?.Hide();
            };
            _trayContextMenu.Items.Add(_hideStatusBarItem);

            // 打开背包
            _openInventoryItem = new System.Windows.Controls.MenuItem
            {
                Header = TryFindResource("TrayOpenInventory") as string ?? "打开背包",
                Style = (Style)FindResource("WpfUiMenuItemStyle")
            };
            _openInventoryItem.Click += (s, e) =>
            {
                _trayContextMenu.IsOpen = false;
                _inventoryWindow?.Show();
            };
            _trayContextMenu.Items.Add(_openInventoryItem);

            // 退出
            _exitItem = new System.Windows.Controls.MenuItem
            {
                Header = TryFindResource("TrayExit") as string ?? "退出",
                Style = (Style)FindResource("WpfUiMenuItemStyle")
            };
            _exitItem.Click += (s, e) =>
            {
                _trayContextMenu.IsOpen = false;
                Shutdown();
            };
            _trayContextMenu.Items.Add(_exitItem);

            return _trayContextMenu;
        }

        /// <summary>
        /// 更新托盘菜单文本（语言切换时调用）
        /// </summary>
        private void UpdateTrayMenuTexts()
        {
            if (_showMainItem != null)
                _showMainItem.Header = TryFindResource("TrayShowMain") as string ?? "显示主窗口";
            if (_showStatusBarItem != null)
                _showStatusBarItem.Header = TryFindResource("TrayShowStatusBar") as string ?? "显示状态栏";
            if (_hideStatusBarItem != null)
                _hideStatusBarItem.Header = TryFindResource("TrayHideStatusBar") as string ?? "隐藏状态栏";
            if (_openInventoryItem != null)
                _openInventoryItem.Header = TryFindResource("TrayOpenInventory") as string ?? "打开背包";
            if (_exitItem != null)
                _exitItem.Header = TryFindResource("TrayExit") as string ?? "退出";
        }

        /// <summary>
        /// 注册全局快捷键
        /// </summary>
        private void RegisterHotkeys()
        {
            // E键 - 打开/关闭背包
            // 使用 Keyboard.AddKeyDownHandler 实现全局快捷键
            AddKeyDownHandler();
        }

        /// <summary>
        /// 添加键盘事件处理
        /// </summary>
        private void AddKeyDownHandler()
        {
            // 在主窗口上监听键盘事件
            EventManager.RegisterClassHandler(typeof(Window),
                Keyboard.KeyDownEvent,
                new System.Windows.Input.KeyEventHandler(GlobalKeyDown));
        }

        /// <summary>
        /// 全局键盘按下事件
        /// </summary>
        private void GlobalKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // E键 - 切换背包显示
            if (e.Key == Key.E)
            {
                _inventoryWindow?.Toggle();
                e.Handled = true;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 如果启用了记住位置，保存状态栏位置
            if (_appSettings?.StatusBarRememberPosition ?? false)
            {
                _appSettings.StatusBarPositionX = _statusBarWindow?.Left ?? 0;
                _appSettings.StatusBarPositionY = _statusBarWindow?.Top ?? 0;
            }

            // 退出时始终保存所有设置到文件
            SaveSettings();

            _taskbarIcon?.Dispose();
            base.OnExit(e);
        }

        /// <summary>
        /// 保存设置到文件（公开方法，供其他组件调用）
        /// </summary>
        public void SaveSettings()
        {
            if (_appSettings == null) return;

            try
            {
                var json = JsonConvert.SerializeObject(_appSettings, Formatting.Indented);
                System.IO.File.WriteAllText(_settingsPath, json);
            }
            catch { }
        }

        /// <summary>
        /// 初始化自定义颜色画刷
        /// </summary>
        private void InitializeBrushes()
        {
            Resources.Add("ApplicationBackgroundBrush", new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x20, 0x20, 0x20)));
            Resources.Add("CardBackgroundBrush", new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2D, 0x2D, 0x2D)));
            Resources.Add("AccentBrush", new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x00, 0x78, 0xD4)));
            Resources.Add("TextPrimaryBrush", new SolidColorBrush(System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF)));
            Resources.Add("TextSecondaryBrush", new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x99, 0x99, 0x99)));
            Resources.Add("TextTertiaryBrush", new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x66, 0x66, 0x66)));
            Resources.Add("DividerBrush", new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x40, 0x40, 0x40)));
            Resources.Add("HoverBackgroundBrush", new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x2A, 0x2A, 0x2A)));
        }
    }
}