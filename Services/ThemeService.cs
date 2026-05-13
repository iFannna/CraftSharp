using System.Windows.Media;
using Wpf.Ui.Appearance;
using Microsoft.Win32;

namespace CraftSharp.Services
{
    /// <summary>
    /// 主题管理服务
    /// </summary>
    public class ThemeService
    {
        private static ThemeService? _instance;
        public static ThemeService Instance => _instance ??= new ThemeService();

        /// <summary>
        /// 当前设置的主题模式（跟随系统/暗色/亮色）
        /// </summary>
        public string ThemeMode { get; private set; } = "跟随系统";

        /// <summary>
        /// 当前实际应用的主题
        /// </summary>
        public string CurrentTheme { get; private set; } = "暗色";

        /// <summary>
        /// 系统主题变化事件
        /// </summary>
        public event Action? SystemThemeChanged;

        /// <summary>
        /// 初始化主题服务，注册系统主题变化监听
        /// </summary>
        private ThemeService()
        {
            // 监听系统主题变化
            SystemEvents.UserPreferenceChanged += OnUserPreferenceChanged;
        }

        /// <summary>
        /// 系统偏好设置变化时触发
        /// </summary>
        private void OnUserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            if (e.Category == UserPreferenceCategory.General)
            {
                // 检查系统主题是否变化
                var systemTheme = IsSystemLightTheme() ? "亮色" : "暗色";
                if (systemTheme != CurrentTheme && ThemeMode == "跟随系统")
                {
                    CurrentTheme = systemTheme;
                    ApplyTheme(systemTheme);
                    SystemThemeChanged?.Invoke();
                }
            }
        }

        /// <summary>
        /// 设置主题模式
        /// </summary>
        public void SetThemeMode(string mode)
        {
            ThemeMode = mode;
            var actualTheme = GetActualTheme(mode);
            CurrentTheme = actualTheme;
            ApplyTheme(actualTheme);
        }

        /// <summary>
        /// 应用主题
        /// </summary>
        private void ApplyTheme(string theme)
        {
            // 使用 WPF UI 的 ApplicationThemeManager 切换主题
            var wpfTheme = theme == "亮色" ? ApplicationTheme.Light : ApplicationTheme.Dark;
            ApplicationThemeManager.Apply(wpfTheme);

            // 更新自定义颜色画笔
            UpdateCustomBrushes(theme);
        }

        /// <summary>
        /// 根据主题更新自定义颜色画笔
        /// </summary>
        private void UpdateCustomBrushes(string theme)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            var isLight = theme == "亮色";

            UpdateBrush(app, "ApplicationBackgroundBrush",
                isLight ? System.Windows.Media.Color.FromRgb(0xF3, 0xF3, 0xF3) : System.Windows.Media.Color.FromRgb(0x20, 0x20, 0x20));
            UpdateBrush(app, "CardBackgroundBrush",
                isLight ? System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF) : System.Windows.Media.Color.FromRgb(0x2D, 0x2D, 0x2D));
            UpdateBrush(app, "TextPrimaryBrush",
                isLight ? System.Windows.Media.Color.FromRgb(0x1A, 0x1A, 0x1A) : System.Windows.Media.Color.FromRgb(0xFF, 0xFF, 0xFF));
            UpdateBrush(app, "TextSecondaryBrush",
                isLight ? System.Windows.Media.Color.FromRgb(0x66, 0x66, 0x66) : System.Windows.Media.Color.FromRgb(0x99, 0x99, 0x99));
            UpdateBrush(app, "TextTertiaryBrush",
                isLight ? System.Windows.Media.Color.FromRgb(0x99, 0x99, 0x99) : System.Windows.Media.Color.FromRgb(0x66, 0x66, 0x66));
            UpdateBrush(app, "DividerBrush",
                isLight ? System.Windows.Media.Color.FromRgb(0xE0, 0xE0, 0xE0) : System.Windows.Media.Color.FromRgb(0x40, 0x40, 0x40));
            UpdateBrush(app, "HoverBackgroundBrush",
                isLight ? System.Windows.Media.Color.FromRgb(0xE8, 0xE8, 0xE8) : System.Windows.Media.Color.FromRgb(0x3D, 0x3D, 0x3D));
        }

        private void UpdateBrush(System.Windows.Application app, string key, System.Windows.Media.Color color)
        {
            if (app.Resources.Contains(key))
            {
                // 替换整个画刷（因为冻结的画刷无法修改颜色）
                app.Resources[key] = new SolidColorBrush(color);
            }
        }

        /// <summary>
        /// 根据设置值获取实际主题
        /// </summary>
        public string GetActualTheme(string settingValue)
        {
            if (settingValue == "跟随系统")
            {
                return IsSystemLightTheme() ? "亮色" : "暗色";
            }
            return settingValue;
        }

        /// <summary>
        /// 检查系统是否使用亮色主题
        /// </summary>
        private bool IsSystemLightTheme()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                var value = key?.GetValue("AppsUseLightTheme");
                return value != null && (int)value == 1;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 初始化主题
        /// </summary>
        public void Initialize(string savedTheme)
        {
            ThemeMode = savedTheme;
            var actualTheme = GetActualTheme(savedTheme);
            CurrentTheme = actualTheme;
            ApplyTheme(actualTheme);
        }
    }
}