using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CraftSharp.Helpers;
using CraftSharp.Windows.StatusBar;
using CraftSharp.Windows.BossBar;
using CraftSharp.Windows.Crosshair;
using CraftSharp.Windows.Settings;
using CraftSharp.Windows.Inventory;
using CraftSharp.Windows.SkinPreview;
using CraftSharp.Services.Core;
using CraftSharp.Services.Hud;
using CraftSharp.Services.Slot;
using CraftSharp.Services.Resource;
using System.Text.Json;
using Hardcodet.Wpf.TaskbarNotification;

namespace CraftSharp
{
    public partial class App
    {
        private StatusBarWindow? _statusBarWindow;
        private CrosshairWindow? _crosshairWindow;
        private InventoryWindow? _inventoryWindow;
        private SkinPreviewWindow? _skinPreviewWindow;
        private BossBarWindow? _bossBarWindow;
        private SettingsWindow? _settingsWindow;
        private TaskbarIcon? _taskbarIcon;
        private System.Windows.Controls.ContextMenu? _trayContextMenu;
        private System.Windows.Controls.MenuItem? _settingsPanelItem;
        private System.Windows.Controls.MenuItem? _statusBarItem;
        private System.Windows.Controls.MenuItem? _inventoryItem;
        private System.Windows.Controls.MenuItem? _exitItem;
        private string _settingsPath = "";
        private Models.AppSettings? _appSettings;
        private Window? _hotkeyMessageWindow;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 初始化自定义颜色画刷（必须在窗口创建之前）
            InitializeBrushes();

            // 加载设置
            var configDir = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config");
            if (!System.IO.Directory.Exists(configDir))
            {
                System.IO.Directory.CreateDirectory(configDir);
            }
            _settingsPath = System.IO.Path.Combine(configDir, "settings.json");
            LoadSettings();

            // 初始化 SlotFileValidator 单例并执行全量检查
            SlotFileValidator.Instance.ValidateAllSlots(_appSettings);

            // 初始化语言
            LocalizationService.Instance.Initialize(_appSettings?.System.Language ?? "zh-CN");

            // 初始化主题
            ThemeService.Instance.Initialize(_appSettings?.Appearance.Theme ?? "System");

            // 初始化字体（使用标识符）
            FontService.Instance.Initialize(_appSettings?.Appearance.Font ?? "YaHei", _appSettings?.Appearance.FontSize ?? 14);

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
            _statusBarWindow.PositionChanged += (_, _) =>
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
            _statusBarWindow.Loaded += (_, _) =>
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
            if (_appSettings?.BossBars.Any(b => b.IsEnabled) ?? false)
                _bossBarWindow.Show();
            else
                _bossBarWindow.Hide();

            // 如果开启"记住位置"，在创建物品栏窗口前设置跳过默认定位
            if (_appSettings?.Inventory.RememberPosition ?? false)
            {
                InventoryWindow.ShouldSkipDefaultPositioning = true;
            }

            // 创建背包窗口（隐藏，按E键打开）
            _inventoryWindow = new InventoryWindow(_appSettings!);
            _inventoryWindow.Hide();

            // 从配置加载玩家皮肤
            LoadPlayerSkinFromSettings();

            // 监听物品栏位置变化（即时保存到配置文件）
            _inventoryWindow.PositionChanged += (_, _) =>
            {
                if (_appSettings?.Inventory.RememberPosition ?? false)
                {
                    _appSettings.Inventory.PositionX = _inventoryWindow.Left;
                    _appSettings.Inventory.PositionY = _inventoryWindow.Top;
                    SaveSettings();
                }
            };

            // 物品栏位置定位：在窗口第一次显示时定位
            _inventoryWindow.Loaded += (_, _) =>
            {
                if (_appSettings?.Inventory.RememberPosition ?? false)
                {
                    // 记住位置开启 → 检查保存的位置是否有效
                    double savedX = _appSettings.Inventory.PositionX;
                    double savedY = _appSettings.Inventory.PositionY;

                    var screenWidth = SystemParameters.PrimaryScreenWidth;
                    var screenHeight = SystemParameters.PrimaryScreenHeight;

                    // 如果位置是默认值（0,0）或超出屏幕右下边缘太多，则居中显示
                    // 允许负坐标（窗口可部分超出屏幕左/上边缘）
                    if (savedX == 0 && savedY == 0 ||
                        savedX > screenWidth - 100 ||
                        savedY > screenHeight - 100)
                    {
                        // 位置无效 → 定位到屏幕居中
                        _inventoryWindow.Left = (screenWidth - _inventoryWindow.Width) / 2;
                        _inventoryWindow.Top = (screenHeight - _inventoryWindow.Height) / 2;
                    }
                    else
                    {
                        // 位置有效 → 使用保存的位置
                        _inventoryWindow.Left = savedX;
                        _inventoryWindow.Top = savedY;
                    }
                }
                else
                {
                    // 记住位置关闭 → 定位到屏幕居中
                    var screenWidth = SystemParameters.PrimaryScreenWidth;
                    var screenHeight = SystemParameters.PrimaryScreenHeight;
                    _inventoryWindow.Left = (screenWidth - _inventoryWindow.Width) / 2;
                    _inventoryWindow.Top = (screenHeight - _inventoryWindow.Height) / 2;
                }
            };

            // 创建系统托盘图标（使用纯 WPF 实现）
            CreateTaskbarIcon();

            // 监听语言切换事件
            LocalizationService.Instance.LanguageChanged += UpdateTrayMenuTexts;

            if (_appSettings.System.DefaultOpenPanel)
                _settingsWindow.Show();

            // 创建皮肤预览窗口（暂时不显示）
            _skinPreviewWindow = new SkinPreviewWindow();
            SkinPreviewService.Instance.Initialize(_skinPreviewWindow);
            // _skinPreviewWindow.Show();

            // 设置窗口关闭时最小化到托盘
            _settingsWindow.Closing += (_, cancelEventArgs) =>
            {
                if (_taskbarIcon == null) return;
                cancelEventArgs.Cancel = true;
                _settingsWindow.Hide();
            };

            // 注册全局快捷键（Win32 RegisterHotKey）
            InitializeGlobalHotkeys();
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
                    _appSettings = JsonSerializer.Deserialize<Models.AppSettings>(json);

                    // 清理重复的HudElements（只保留每个ID的第一个）
                    if (_appSettings?.HudElements is { Count: > 0 })
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
        /// - 所有组件：数据映射默认为 BatteryLevel
        /// </summary>
        private void EnsureAllHudElementsExist()
        {
            if (_appSettings == null) return;

            var defaultConfigs = new Dictionary<string, Models.HudElementSettings>
            {
                { "ExpBar", new Models.HudElementSettings
                    {
                        Id = "ExpBar",
                        IsVisible = true,
                        CustomValueEnabled = true,
                        CustomCurrentValue = 0,
                        DataMappingType = "BatteryLevel",
                    }
                },
                { "Health", new Models.HudElementSettings
                    {
                        Id = "Health",
                        IsVisible = true,
                        CustomValueEnabled = true,
                        CustomCurrentValue = 20,
                        CustomMaxValue = 20,
                        DataMappingType = "BatteryLevel",
                    }
                },
                { "Food", new Models.HudElementSettings
                    {
                        Id = "Food",
                        IsVisible = true,
                        CustomValueEnabled = true,
                        CustomCurrentValue = 20,
                        CustomMaxValue = 20,
                        DataMappingType = "BatteryLevel",
                    }
                },
                { "Air", new Models.HudElementSettings
                    {
                        Id = "Air",
                        IsVisible = false,
                        CustomValueEnabled = true,
                        CustomCurrentValue = 20,
                        CustomMaxValue = 20,
                        DataMappingType = "BatteryLevel",
                    }
                },
                { "Armor", new Models.HudElementSettings
                    {
                        Id = "Armor",
                        IsVisible = false,
                        CustomValueEnabled = true,
                        CustomCurrentValue = 20,
                        CustomMaxValue = 20,
                        DataMappingType = "BatteryLevel",
                    }
                },
                { "Absorbing", new Models.HudElementSettings
                    {
                        Id = "Absorbing",
                        IsVisible = false,
                        CustomValueEnabled = true,
                        CustomCurrentValue = 20,
                        CustomMaxValue = 20,
                        DataMappingType = "BatteryLevel",
                    }
                },
                { "Crosshair", new Models.HudElementSettings
                    {
                        Id = "Crosshair",
                        IsVisible = false, // 默认不显示
                        TopMost = false,
                    }
                },
                { "AttackIndicator", new Models.HudElementSettings
                    {
                        Id = "AttackIndicator",
                        IsVisible = false, // 默认不显示
                        CustomValueEnabled = true,
                        CustomCurrentValue = 99,
                        DataMappingType = "BatteryLevel",
                    }
                },
            };

            foreach (var kvp in defaultConfigs)
            {
                if (_appSettings.HudElements.All(h => h.Id != kvp.Key))
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
            // 使用项目内置图标文件
            var iconPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "CraftSharp.ico");
            var icon = new System.Drawing.Icon(iconPath);
            _taskbarIcon = new TaskbarIcon
            {
                IconSource = icon.ToImageSource(),
                ToolTipText = "Craft#"
            };

            // 双击显示设置窗口
            _taskbarIcon.TrayMouseDoubleClick += (_, _) =>
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
                _settingsWindow!);
        }

        /// <summary>
        /// 创建托盘右键菜单（Fluent Design 风格）
        /// </summary>
        private System.Windows.Controls.ContextMenu CreateTrayContextMenu()
        {
            _trayContextMenu = new System.Windows.Controls.ContextMenu
            {
                Style = (Style)FindResource("WpfUiContextMenuStyle")!,
                Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint
            };

            // 设置面板
            _settingsPanelItem = new System.Windows.Controls.MenuItem
            {
                Style = (Style)FindResource("WpfUiMenuItemStyle")!
            };
            _settingsPanelItem.Click += (_, _) =>
            {
                _trayContextMenu!.IsOpen = false;
                if (_settingsWindow?.IsVisible == true)
                    _settingsWindow.Hide();
                else
                {
                    _settingsWindow?.Show();
                    _settingsWindow?.Activate();
                }
            };
            _trayContextMenu!.Items.Add(_settingsPanelItem);

            // 状态栏
            _statusBarItem = new System.Windows.Controls.MenuItem
            {
                Style = (Style)FindResource("WpfUiMenuItemStyle")!
            };
            _statusBarItem.Click += (_, _) =>
            {
                _trayContextMenu!.IsOpen = false;
                if (_statusBarWindow?.IsVisible == true)
                    _statusBarWindow.Hide();
                else
                    _statusBarWindow?.Show();
            };
            _trayContextMenu!.Items.Add(_statusBarItem);

            // 物品栏
            _inventoryItem = new System.Windows.Controls.MenuItem
            {
                Style = (Style)FindResource("WpfUiMenuItemStyle")!
            };
            _inventoryItem.Click += (_, _) =>
            {
                _trayContextMenu!.IsOpen = false;
                if (_inventoryWindow?.IsVisible == true)
                    _inventoryWindow.Hide();
                else
                    _inventoryWindow?.Show();
            };
            _trayContextMenu!.Items.Add(_inventoryItem);

            // 退出
            _exitItem = new System.Windows.Controls.MenuItem
            {
                Header = TryFindResource("TrayExit") as string ?? "退出",
                Style = (Style)FindResource("WpfUiMenuItemStyle")!
            };
            _exitItem.Click += (_, _) =>
            {
                _trayContextMenu!.IsOpen = false;
                Shutdown();
            };
            _trayContextMenu!.Items.Add(_exitItem);

            // 每次打开菜单时刷新文字
            _trayContextMenu.Opened += (_, _) => UpdateTrayMenuTexts();

            return _trayContextMenu;
        }

        /// <summary>
        /// 根据窗口可见状态更新托盘菜单文本
        /// </summary>
        private void UpdateTrayMenuTexts()
        {
            // 设置面板
            if (_settingsPanelItem != null)
            {
                var showKey = "TrayShowSettingsPanel";
                var hideKey = "TrayHideSettingsPanel";
                _settingsPanelItem.Header = _settingsWindow?.IsVisible == true
                    ? (TryFindResource(hideKey) as string ?? "隐藏设置面板")
                    : (TryFindResource(showKey) as string ?? "显示设置面板");
            }

            // 状态栏
            if (_statusBarItem != null)
            {
                var showKey = "TrayShowStatusBar";
                var hideKey = "TrayHideStatusBar";
                _statusBarItem.Header = _statusBarWindow?.IsVisible == true
                    ? (TryFindResource(hideKey) as string ?? "隐藏状态栏")
                    : (TryFindResource(showKey) as string ?? "显示状态栏");
            }

            // 物品栏
            if (_inventoryItem != null)
            {
                var showKey = "TrayShowInventory";
                var hideKey = "TrayHideInventory";
                _inventoryItem.Header = _inventoryWindow?.IsVisible == true
                    ? (TryFindResource(hideKey) as string ?? "隐藏物品栏")
                    : (TryFindResource(showKey) as string ?? "显示物品栏");
            }

            if (_exitItem != null)
                _exitItem.Header = TryFindResource("TrayExit") as string ?? "退出";
        }

        private void InitializeGlobalHotkeys()
        {
            // ESC 保持 WPF 级别处理（仅关闭物品栏）
            EventManager.RegisterClassHandler(typeof(Window),
                Keyboard.KeyDownEvent,
                new KeyEventHandler((sender, e) =>
                {
                    if (e.Key == Key.Escape && _inventoryWindow?.IsVisible == true)
                    {
                        _inventoryWindow.HideInventory();
                        e.Handled = true;
                    }
                }));

            // Win32 全局快捷键（使用独立的隐藏窗口接收 WM_HOTKEY）
            _hotkeyMessageWindow = new Window
            {
                Width = 0, Height = 0,
                ShowInTaskbar = false,
                WindowStyle = WindowStyle.None,
                AllowsTransparency = true,
                Opacity = 0
            };
            _hotkeyMessageWindow.ShowInTaskbar = false;
            _hotkeyMessageWindow.SourceInitialized += (_, _) =>
            {
                var hwnd = new System.Windows.Interop.WindowInteropHelper(_hotkeyMessageWindow!).Handle;
                int exStyle = Win32Helper.GetWindowLong(hwnd, Win32Helper.GWL_EXSTYLE);
                Win32Helper.SetWindowLong(hwnd, Win32Helper.GWL_EXSTYLE, exStyle | Win32Helper.WS_EX_TOOLWINDOW);

                HotkeyService.Instance.HookWindow(_hotkeyMessageWindow);

                HotkeyService.Instance.RegisterHotkey("DesktopIcons",
                    _appSettings?.Hotkeys.DesktopIcons ?? "Ctrl+Alt+D", ToggleDesktopIcons);

                HotkeyService.Instance.RegisterHotkey("Inventory",
                    _appSettings?.Hotkeys.Inventory ?? "Ctrl+Alt+E", () =>
                    {
                        if (_appSettings?.Inventory.Visible ?? true)
                            _inventoryWindow?.Toggle();
                    });

                HotkeyService.Instance.RegisterHotkey("Settings",
                    _appSettings?.Hotkeys.Settings ?? "Ctrl+Alt+S", () =>
                    {
                        if (_settingsWindow?.IsVisible == true)
                            _settingsWindow.Hide();
                        else
                        {
                            _settingsWindow?.Show();
                            _settingsWindow?.Activate();
                        }
                    });

                HotkeyService.Instance.RegisterHotkey("StatusBar",
                    _appSettings?.Hotkeys.StatusBar ?? "Ctrl+Alt+H", () =>
                    {
                        StatusBarService.Instance.Toggle();
                    });

                HotkeyService.Instance.RegisterHotkey("Crosshair",
                    _appSettings?.Hotkeys.Crosshair ?? "Ctrl+Alt+C", () =>
                    {
                        CrosshairService.Instance.Toggle();
                    });
            };
            _hotkeyMessageWindow.Show();
        }

        private bool _desktopIconsVisible = true;

        private void ToggleDesktopIcons()
        {
            var shellWnd = Win32Helper.FindWindow("Progman", null);
            if (shellWnd == IntPtr.Zero) return;

            var defView = Win32Helper.FindWindowEx(shellWnd, IntPtr.Zero, "SHELLDLL_DefView", null);
            if (defView == IntPtr.Zero) return;

            var listWnd = Win32Helper.FindWindowEx(defView, IntPtr.Zero, "SysListView32", null);
            if (listWnd == IntPtr.Zero) return;

            _desktopIconsVisible = !_desktopIconsVisible;
            Win32Helper.ShowWindow(listWnd, _desktopIconsVisible ? Win32Helper.SW_SHOW : 0);
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

            HotkeyService.Instance.Dispose();
            _hotkeyMessageWindow?.Close();
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
                var json = JsonSerializer.Serialize(_appSettings, new JsonSerializerOptions
                {
                    WriteIndented = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                System.IO.File.WriteAllText(_settingsPath, json);
            }
            catch
            {
                // ignored
            }
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
        /// 刷新物品栏样式（即时生效）
        /// </summary>
        public void RefreshInventoryStyle(string stylePath)
        {
            if (_inventoryWindow != null)
            {
                _inventoryWindow.RefreshStyle(stylePath);
            }
        }

        /// <summary>
        /// 执行全量格子文件路径检查（用于格子相关操作前）
        /// </summary>
        public void ValidateAllSlots()
        {
            SlotFileValidator.Instance.ValidateAllSlots(_appSettings);
        }

        /// <summary>
        /// 获取 AppSettings 实例（供其他组件调用）
        /// </summary>
        public Models.AppSettings? GetAppSettings()
        {
            return _appSettings;
        }

        /// <summary>
        /// 获取物品栏窗口实例（供其他组件调用）
        /// </summary>
        public InventoryWindow? GetInventoryWindow()
        {
            return _inventoryWindow;
        }

        /// <summary>
        /// 刷新物品栏窗口的玩家模型（上传新皮肤后调用）
        /// </summary>
        public void RefreshInventoryPlayerModel()
        {
            if (_inventoryWindow != null)
            {
                _inventoryWindow.RefreshPlayerModel();
            }
        }

        /// <summary>
        /// 加载指定的皮肤到物品栏窗口的玩家模型
        /// </summary>
        public void LoadPlayerSkin(string skinPath, bool isWide)
        {
            if (_inventoryWindow != null)
            {
                _inventoryWindow.LoadPlayerSkin(skinPath, isWide);
            }
        }

        /// <summary>
        /// 设置当前皮肤并保存配置
        /// </summary>
        public void SetPlayerSkin(string skinPath, string skinType)
        {
            if (_appSettings != null)
            {
                _appSettings.Player.Skin = skinPath;
                _appSettings.Player.SkinType = skinType;
                SaveSettings();
            }
        }

        /// <summary>
        /// 从配置加载玩家皮肤
        /// </summary>
        private void LoadPlayerSkinFromSettings()
        {
            if (_appSettings == null || _inventoryWindow == null) return;

            var skinPath = _appSettings.Player.Skin;
            var skinType = _appSettings.Player.SkinType;
            var isWide = skinType == "wide";

            // 转换为绝对路径
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var fullPath = System.IO.Path.Combine(basePath, skinPath);

            if (System.IO.File.Exists(fullPath))
            {
                _inventoryWindow.LoadPlayerSkin(fullPath, isWide);
            }
            else
            {
                // 如果配置中的皮肤不存在，加载默认皮肤
                var defaultSkinPath = System.IO.Path.Combine(basePath, "assets/minecraft/textures/entity/player/wide/steve.png");
                if (System.IO.File.Exists(defaultSkinPath))
                {
                    _inventoryWindow.LoadPlayerSkin(defaultSkinPath, true);
                    // 重置配置为默认值
                    _appSettings.Player.Skin = "assets/minecraft/textures/entity/player/wide/steve.png";
                    _appSettings.Player.SkinType = "wide";
                    SaveSettings();
                }
            }
        }

        /// <summary>
        /// 初始化HUD元素可见性（根据配置文件）
        /// </summary>
        private void InitializeHudElementsVisibility()
        {
            if (_appSettings == null) return;

            var visibilityMap = new Dictionary<string, Action<bool>>
            {
                { "ExpBar", StatusBarService.Instance.SetExpBarVisible },
                { "Health", StatusBarService.Instance.SetHealthVisible },
                { "Food", StatusBarService.Instance.SetFoodVisible },
                { "Air", StatusBarService.Instance.SetAirVisible },
                { "Armor", StatusBarService.Instance.SetArmorVisible },
                { "Absorbing", StatusBarService.Instance.SetAbsorbingVisible },
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
            var crosshairSettings = _appSettings.HudElements.FirstOrDefault(h => h.Id == "Crosshair");
            CrosshairService.Instance.SetCrosshairVisible(crosshairSettings?.IsVisible ?? false);
            CrosshairService.Instance.SetTopMost(crosshairSettings?.TopMost ?? false);

            // 攻击指示器
            var attackIndicatorSettings = _appSettings.HudElements.FirstOrDefault(h => h.Id == "AttackIndicator");
            CrosshairService.Instance.SetAttackIndicatorVisible(attackIndicatorSettings?.IsVisible ?? false);
        }

        /// <summary>
        /// 初始化自定义颜色画刷
        /// </summary>
        private void InitializeBrushes()
        {
            Resources.Add("ApplicationBackgroundBrush", new SolidColorBrush(Color.FromRgb(0x20, 0x20, 0x20)));
            Resources.Add("CardBackgroundBrush", new SolidColorBrush(Color.FromRgb(0x2D, 0x2D, 0x2D)));
            Resources.Add("AccentBrush", new SolidColorBrush(Color.FromRgb(0x00, 0x78, 0xD4)));
            Resources.Add("TextPrimaryBrush", new SolidColorBrush(Color.FromRgb(0xFF, 0xFF, 0xFF)));
            Resources.Add("TextSecondaryBrush", new SolidColorBrush(Color.FromRgb(0x99, 0x99, 0x99)));
            Resources.Add("TextTertiaryBrush", new SolidColorBrush(Color.FromRgb(0x66, 0x66, 0x66)));
            Resources.Add("DividerBrush", new SolidColorBrush(Color.FromRgb(0x40, 0x40, 0x40)));
            Resources.Add("HoverBackgroundBrush", new SolidColorBrush(Color.FromRgb(0x3D, 0x3D, 0x3D)));
        }
    }
}
