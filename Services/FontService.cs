using System.Windows;
using System.Windows.Media;

namespace CraftSharp.Services
{
    /// <summary>
    /// 字体管理服务
    /// </summary>
    public class FontService
    {
        private static FontService? _instance;
        public static FontService Instance => _instance ??= new FontService();

        /// <summary>
        /// 当前字体设置
        /// </summary>
        public string CurrentFont { get; private set; } = "跟随系统";

        /// <summary>
        /// 字体变化事件
        /// </summary>
        public event Action? FontChanged;

        /// <summary>
        /// 设置字体
        /// </summary>
        public void SetFont(string displayName)
        {
            if (displayName == CurrentFont) return;

            CurrentFont = displayName;
            ApplyFont(displayName);
            FontChanged?.Invoke();
        }

        /// <summary>
        /// 应用字体
        /// </summary>
        private void ApplyFont(string displayName)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            System.Windows.Media.FontFamily fontFamily;

            // 像素字体使用嵌入资源
            if (displayName == "像素字体")
            {
                // 加载嵌入的像素字体
                fontFamily = new System.Windows.Media.FontFamily(new Uri("pack://application:,,,/"), "/Fonts/PressStart2P-Regular.ttf#Press Start 2P");
            }
            else
            {
                var fontFamilyName = GetSystemFontName(displayName);
                fontFamily = new System.Windows.Media.FontFamily(fontFamilyName);
            }

            if (app.Resources.Contains("GlobalFontFamily"))
            {
                app.Resources["GlobalFontFamily"] = fontFamily;
            }
            else
            {
                app.Resources.Add("GlobalFontFamily", fontFamily);
            }
        }

        /// <summary>
        /// 获取系统字体名称
        /// </summary>
        private string GetSystemFontName(string displayName)
        {
            return displayName switch
            {
                "微软雅黑" => "Microsoft YaHei",
                "宋体" => "SimSun",
                "黑体" => "SimHei",
                "楷体" => "KaiTi",
                _ => "Segoe UI"
            };
        }

        /// <summary>
        /// 初始化字体
        /// </summary>
        public void Initialize(string savedFont)
        {
            CurrentFont = savedFont;
            ApplyFont(savedFont);
        }
    }
}