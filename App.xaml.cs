using System.Windows;
using System.Windows.Input;
using System.Drawing;
using CraftSharp.Windows;

namespace CraftSharp
{
    public partial class App : System.Windows.Application
    {
        private HotbarWindow? _hotbarWindow;
        private InventoryWindow? _inventoryWindow;
        private SettingsWindow? _settingsWindow;
        private System.Windows.Forms.NotifyIcon? _notifyIcon;

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // 创建系统托盘
            CreateNotifyIcon();

            // 创建设置窗口（主窗口）
            _settingsWindow = new SettingsWindow();
            _settingsWindow.Show();

            // 设置窗口关闭时最小化到托盘
            _settingsWindow.Closing += (s, e) =>
            {
                if (_notifyIcon != null && _notifyIcon.Visible)
                {
                    e.Cancel = true;
                    _settingsWindow.Hide();
                    _notifyIcon.ShowBalloonTip(2000, "Craft#", "程序已最小化到系统托盘", System.Windows.Forms.ToolTipIcon.Info);
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
        }

        /// <summary>
        /// 创建系统托盘图标
        /// </summary>
        private void CreateNotifyIcon()
        {
            // 加载自定义图标
            Icon? appIcon = null;
            var iconPath = System.IO.Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "Assets", "icon.ico");

            if (System.IO.File.Exists(iconPath))
            {
                appIcon = new Icon(iconPath);
            }

            _notifyIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = appIcon ?? SystemIcons.Application,
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

            // 右键菜单
            var contextMenu = new System.Windows.Forms.ContextMenuStrip();
            contextMenu.Items.Add("显示主窗口", null, (s, e) => { _settingsWindow?.Show(); _settingsWindow?.Activate(); });
            contextMenu.Items.Add("显示快捷栏", null, (s, e) => _hotbarWindow?.Show());
            contextMenu.Items.Add("隐藏快捷栏", null, (s, e) => _hotbarWindow?.Hide());
            contextMenu.Items.Add("打开背包", null, (s, e) => _inventoryWindow?.Show());
            contextMenu.Items.Add("-");
            contextMenu.Items.Add("退出", null, (s, e) => Shutdown());

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
    }
}