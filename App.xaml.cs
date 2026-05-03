using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using CraftSharp.Windows;
using CraftSharp.Services;
using Newtonsoft.Json;

namespace CraftSharp
{
    public partial class App : System.Windows.Application
    {
        private HotbarWindow? _hotbarWindow;
        private InventoryWindow? _inventoryWindow;
        private SettingsWindow? _settingsWindow;
        private System.Windows.Forms.NotifyIcon? _notifyIcon;
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
            _settingsWindow = new SettingsWindow();

            // 创建系统托盘图标
            CreateNotifyIcon();

            // 初始化图标服务
            IconService.Instance.Initialize(_appSettings?.AppIconPath ?? "minecraft/textures/block/block/glass.png", _notifyIcon!, _settingsWindow);

            _settingsWindow.Show();

            // 设置窗口关闭时最小化到托盘
            _settingsWindow.Closing += (s, e) =>
            {
                if (_notifyIcon != null && _notifyIcon.Visible)
                {
                    e.Cancel = true;
                    _settingsWindow.Hide();
                }
            };

            // 创建快捷栏窗口
            _hotbarWindow = new HotbarWindow();
            _hotbarWindow.Show();

            // 创建背包窗口（隐藏，按E键打开）
            _inventoryWindow = new InventoryWindow();
            _inventoryWindow.Hide();

            // 注册全局快捷键
            RegisterHotkeys();

            // 监听语言变化，更新托盘菜单
            LocalizationService.Instance.LanguageChanged += UpdateTrayMenu;
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
        /// 创建系统托盘图标
        /// </summary>
        private void CreateNotifyIcon()
        {
            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = SystemIcons.Application, // 默认图标，由IconService更新
                Text = "Craft#",
                Visible = true
            };

            // 双击显示设置窗口
            _notifyIcon.DoubleClick += (s, e) =>
            {
                if (_settingsWindow != null)
                {
                    _settingsWindow.Show();
                    _settingsWindow.Activate();
                }
            };

            // 创建右键菜单
            UpdateTrayMenu();
        }

        /// <summary>
        /// 更新托盘菜单（支持多语言）
        /// </summary>
        private void UpdateTrayMenu()
        {
            if (_notifyIcon == null) return;

            var contextMenu = new System.Windows.Forms.ContextMenuStrip();

            var showMainText = FindResource("TrayShowMain") as string ?? "显示主窗口";
            var showHotbarText = FindResource("TrayShowHotbar") as string ?? "显示快捷栏";
            var hideHotbarText = FindResource("TrayHideHotbar") as string ?? "隐藏快捷栏";
            var openInventoryText = FindResource("TrayOpenInventory") as string ?? "打开背包";
            var exitText = FindResource("TrayExit") as string ?? "退出";

            contextMenu.Items.Add(showMainText, null, (s, e) => { _settingsWindow?.Show(); _settingsWindow?.Activate(); });
            contextMenu.Items.Add(showHotbarText, null, (s, e) => _hotbarWindow?.Show());
            contextMenu.Items.Add(hideHotbarText, null, (s, e) => _hotbarWindow?.Hide());
            contextMenu.Items.Add(openInventoryText, null, (s, e) => _inventoryWindow?.Show());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add(exitText, null, (s, e) => Shutdown());

            _notifyIcon.ContextMenuStrip = contextMenu;
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
            _notifyIcon?.Dispose();
            base.OnExit(e);
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