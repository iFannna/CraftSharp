using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CraftSharp.Windows.StatusBar;
using CraftSharp.Windows.BossBar;
using CraftSharp.Windows.Crosshair;
using CraftSharp.Windows.Settings;
using CraftSharp.Windows.Inventory;
using CraftSharp.Services;
using Newtonsoft.Json;
using Hardcodet.Wpf.TaskbarNotification;

namespace CraftSharp
{
    public partial class App : System.Windows.Application
    {
        private StatusBarWindow? _statusBarWindow;
        private CrosshairWindow? _crosshairWindow;
        private InventoryWindow? _inventoryWindow;
        private BossBarWindow? _bossBarWindow;
        private SettingsWindow? _settingsWindow;
        private TaskbarIcon? _taskbarIcon;
        private System.Windows.Controls.ContextMenu? _trayContextMenu;
        private System.Windows.Controls.MenuItem? _showMainItem;
        private System.Windows.Controls.MenuItem? _showStatusBarItem;
        private System.Windows.Controls.MenuItem? _hideStatusBarItem;
        private System.Windows.Controls.MenuItem? _openInventoryItem;
        private System.Windows.Controls.MenuItem? _closeInventoryItem;
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
            LocalizationService.Instance.Initialize(_appSettings?.System.Language ?? "简体中文");

            // 初始化主题
            ThemeService.Instance.Initialize(_appSettings?.Appearance.Theme ?? "跟随系统");

            // 初始化字体（使用标识符）
            FontService.Instance.Initialize(_appSettings?.Appearance.Font ?? "yahei", _appSettings?.Appearance.FontSize ?? 14);

            // 创建设置窗口（主窗口）
            _settingsWindow = new SettingsWindow(_appSettings!);

            // 如果开启"记住位置"，在创建窗口前设置跳过默认定位
            if (_appSettings?.StatusBar.RememberPosition ?? false)
            {
                StatusBarWindow.ShouldSkipDefaultPositioning = true;
            }

            // 创建快捷栏窗口
            _statusBarWindow = new StatusBarWindow();

            // 初始化状态栏服务
            StatusBarService.Instance.Initialize(_statusBarWindow, _appSettings!);

            // 监听状态栏位置变化（即时保存到配置文件）
            _statusBarWindow.PositionChanged += (s, e) =>
            {
                if (_appSettings?.StatusBar.RememberPosition ?? false)
                {
                    _appSettings.StatusBar.PositionX = _statusBarWindow.Left;
                    _appSettings.StatusBar.PositionY = _statusBarWindow.Top;
                    SaveSettings();
                }
            };

            // 初始化副手槽配置
            StatusBarService.Instance.SetOffhandConfig(
                _appSettings?.Hotbar.LeftOffhand ?? false,
                _appSettings?.Hotbar.RightOffhand ?? false);

            // 初始化快捷栏可见性
            StatusBarService.Instance.SetHotbarVisible(_appSettings?.Hotbar.Visible ?? true);

            // 初始化HUD元素可见性
            InitializeHudElementsVisibility();

            // 状态栏位置定位：在窗口 Loaded 后定位（此时尺寸已计算好）
            _statusBarWindow.Loaded += (s, e) =>
            {
                if (_appSettings?.StatusBar.RememberPosition ?? false)
                {
                    // 记住位置开启 → 使用保存的位置
                    StatusBarService.Instance.RestorePosition(
                        _appSettings.StatusBar.PositionX,
                        _appSettings.StatusBar.PositionY);
                }
                else
                {
                    // 记住位置关闭 → 定位到屏幕底部水平居中
                    StatusBarService.Instance.PositionToScreenBottomCenter();
                }
            };

            // 根据设置决定是否显示状态栏
            if (_appSettings?.StatusBar.Visible ?? true)
                _statusBarWindow.Show();
            else
                _statusBarWindow.Hide();

            // 创建准星窗口
            _crosshairWindow = new CrosshairWindow();

            // 初始化准星服务
            CrosshairService.Instance.Initialize(_crosshairWindow, _appSettings!);

            // 准星窗口始终居中于屏幕，不需要记住位置

            // 初始化准星HUD元素可见性
            InitializeCrosshairElementsVisibility();

            // 创建BOSS血条窗口
            _bossBarWindow = new BossBarWindow(_appSettings!);

            // 初始化BOSS血条服务
            BossBarService.Instance.Initialize(_bossBarWindow, _appSettings!);

            // 默认显示BOSS血条窗口（如果有启用项）
            if (_appSettings?.BossBars?.Any(b => b.IsEnabled) ?? false)
                _bossBarWindow.Show();
            else
                _bossBarWindow.Hide();

            // 创建背包窗口（隐藏，按E键打开）
            _inventoryWindow = new InventoryWindow(_appSettings!);
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

                    // 清理重复的HudElements（只保留每个ID的第一个）
                    if (_appSettings?.HudElements != null && _appSettings.HudElements.Count > 0)
                    {
                        var uniqueElements = _appSettings.HudElements
                            .GroupBy(h => h.Id)
                            .Select(g => g.First())
                            .ToList();

                        _appSettings.HudElements.Clear();
                        foreach (var element in uniqueElements)
                        {
                            _appSettings.HudElements.Add(element);
                        }
                    }

                    // 确保所有HUD元素都存在（不存在则添加默认配置）
                    EnsureAllHudElementsExist();
                }
                catch { _appSettings = new Models.AppSettings(); EnsureAllHudElementsExist(); }
            }
            else
            {
                _appSettings = new Models.AppSettings();
                // 首次运行时添加默认HUD元素配置
                EnsureAllHudElementsExist();
            }
        }

        /// <summary>
        /// 确保所有HUD元素都存在（不存在则添加默认配置）
        /// 默认配置：
        /// - 经验条：默认开启自定义数值，当前值0
        /// - 生命值、饥饿值、伤害吸收值、护甲值：默认开启自定义数值，当前值20最大值20
        /// - 所有组件：数据映射默认为电池电量
        /// </summary>
        private void EnsureAllHudElementsExist()
        {
            if (_appSettings == null) return;

            var defaultConfigs = new Dictionary<string, Models.HudElementSettings>
            {
                { "expbar", new Models.HudElementSettings
                    {
                        Id = "expbar",
                        IsVisible = true,
                        CustomValueEnabled = true,
                        CustomCurrentValue = 0,
                        DataMappingType = "电池电量",
                    }
                },
                { "health", new Models.HudElementSettings
                    {
                        Id = "health",
                        IsVisible = true,
                        CustomValueEnabled = true,
                        CustomCurrentValue = 20,
                        CustomMaxValue = 20,
                        DataMappingType = "电池电量",
                    }
                },
                { "food", new Models.HudElementSettings
                    {
                        Id = "food",
                        IsVisible = true,
                        CustomValueEnabled = true,
                        CustomCurrentValue = 20,
                        CustomMaxValue = 20,
                        DataMappingType = "电池电量",
                    }
                },
                { "air", new Models.HudElementSettings
                    {
                        Id = "air",
                        IsVisible = false,
                        CustomValueEnabled = true,
                        CustomCurrentValue = 20,
                        CustomMaxValue = 20,
                        DataMappingType = "电池电量",
                    }
                },
                { "armor", new Models.HudElementSettings
                    {
                        Id = "armor",
                        IsVisible = false,
                        CustomValueEnabled = true,
                        CustomCurrentValue = 20,
                        CustomMaxValue = 20,
                        DataMappingType = "电池电量",
                    }
                },
                { "absorbing", new Models.HudElementSettings
                    {
                        Id = "absorbing",
                        IsVisible = false,
                        CustomValueEnabled = true,
                        CustomCurrentValue = 20,
                        CustomMaxValue = 20,
                        DataMappingType = "电池电量",
                    }
                },
                { "crosshair", new Models.HudElementSettings
                    {
                        Id = "crosshair",
                        IsVisible = false, // 默认不显示
                        TopMost = false,
                    }
                },
                { "attackindicator", new Models.HudElementSettings
                    {
                        Id = "attackindicator",
                        IsVisible = false, // 默认不显示
                        CustomValueEnabled = true,
                        CustomCurrentValue = 99,
                        DataMappingType = "电池电量",
                    }
                },
            };

            foreach (var kvp in defaultConfigs)
            {
                if (!_appSettings.HudElements.Any(h => h.Id == kvp.Key))
                {
                    _appSettings.HudElements.Add(kvp.Value);
                }
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
                _appSettings?.Appearance.AppIconPath ?? "minecraft/textures/block/block/glass.png",
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

            // 打开物品栏
            _openInventoryItem = new System.Windows.Controls.MenuItem
            {
                Header = TryFindResource("TrayOpenInventory") as string ?? "打开物品栏",
                Style = (Style)FindResource("WpfUiMenuItemStyle")
            };
            _openInventoryItem.Click += (s, e) =>
            {
                _trayContextMenu.IsOpen = false;
                _inventoryWindow?.Show();
            };
            _trayContextMenu.Items.Add(_openInventoryItem);

            // 关闭物品栏
            _closeInventoryItem = new System.Windows.Controls.MenuItem
            {
                Header = TryFindResource("TrayCloseInventory") as string ?? "关闭物品栏",
                Style = (Style)FindResource("WpfUiMenuItemStyle")
            };
            _closeInventoryItem.Click += (s, e) =>
            {
                _trayContextMenu.IsOpen = false;
                _inventoryWindow?.Hide();
            };
            _trayContextMenu.Items.Add(_closeInventoryItem);

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
                _openInventoryItem.Header = TryFindResource("TrayOpenInventory") as string ?? "打开物品栏";
            if (_closeInventoryItem != null)
                _closeInventoryItem.Header = TryFindResource("TrayCloseInventory") as string ?? "关闭物品栏";
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
            // E键 - 切换背包显示（仅当显示物品栏开启时）
            if (e.Key == Key.E && (_appSettings?.Inventory.Visible ?? true))
            {
                _inventoryWindow?.Toggle();
                e.Handled = true;
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 如果启用了记住位置，保存状态栏位置
            if (_appSettings?.StatusBar.RememberPosition ?? false)
            {
                _appSettings.StatusBar.PositionX = _statusBarWindow?.Left ?? 0;
                _appSettings.StatusBar.PositionY = _statusBarWindow?.Top ?? 0;
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
        /// 设置物品栏点击模式（即时生效）
        /// </summary>
        public void SetInventoryClickMode(string mode)
        {
            if (_inventoryWindow != null)
            {
                _inventoryWindow.SetClickMode(mode);
            }
        }

        /// <summary>
        /// 获取 AppSettings 实例（供其他组件调用）
        /// </summary>
        public Models.AppSettings? GetAppSettings()
        {
            return _appSettings;
        }

        /// <summary>
        /// 初始化HUD元素可见性（根据配置文件）
        /// </summary>
        private void InitializeHudElementsVisibility()
        {
            if (_appSettings == null) return;

            var visibilityMap = new Dictionary<string, Action<bool>>
            {
                { "expbar", StatusBarService.Instance.SetExpBarVisible },
                { "health", StatusBarService.Instance.SetHealthVisible },
                { "food", StatusBarService.Instance.SetFoodVisible },
                { "air", StatusBarService.Instance.SetAirVisible },
                { "armor", StatusBarService.Instance.SetArmorVisible },
                { "absorbing", StatusBarService.Instance.SetAbsorbingVisible },
            };

            foreach (var kvp in visibilityMap)
            {
                var settings = _appSettings.HudElements.FirstOrDefault(h => h.Id == kvp.Key);
                kvp.Value(settings?.IsVisible ?? true);
            }
        }

        /// <summary>
        /// 初始化准星HUD元素可见性（根据配置文件）
        /// </summary>
        private void InitializeCrosshairElementsVisibility()
        {
            if (_appSettings == null) return;

            // 准星
            var crosshairSettings = _appSettings.HudElements.FirstOrDefault(h => h.Id == "crosshair");
            CrosshairService.Instance.SetCrosshairVisible(crosshairSettings?.IsVisible ?? false);
            CrosshairService.Instance.SetTopMost(crosshairSettings?.TopMost ?? false);

            // 攻击指示器
            var attackIndicatorSettings = _appSettings.HudElements.FirstOrDefault(h => h.Id == "attackindicator");
            CrosshairService.Instance.SetAttackIndicatorVisible(attackIndicatorSettings?.IsVisible ?? false);
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
            Resources.Add("HoverBackgroundBrush", new SolidColorBrush(System.Windows.Media.Color.FromRgb(0x3D, 0x3D, 0x3D)));
        }
    }
}