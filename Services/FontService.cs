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
        public string CurrentFont { get; private set; } = "微软雅黑";

        /// <summary>
        /// 当前字体大小
        /// </summary>
        public double CurrentFontSize { get; private set; } = 14;

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
            ApplyFont();
            FontChanged?.Invoke();
        }

        /// <summary>
        /// 设置字体大小
        /// </summary>
        public void SetFontSize(double size)
        {
            if (size == CurrentFontSize) return;

            CurrentFontSize = size;
            ApplyFontSize();
            FontChanged?.Invoke();
        }

        /// <summary>
        /// 应用字体
        /// </summary>
        private void ApplyFont()
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            System.Windows.Media.FontFamily fontFamily;

            // 像素字体使用嵌入资源
            if (CurrentFont == "像素字体")
            {
                // 加载嵌入的像素字体 Zpix
                fontFamily = new System.Windows.Media.FontFamily(new Uri("pack://application:,,,/"), "/Fonts/zpix.ttf#Zpix");
            }
            else
            {
                var fontFamilyName = GetSystemFontName(CurrentFont);
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
        /// 应用字体大小
        /// </summary>
        private void ApplyFontSize()
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            if (app.Resources.Contains("GlobalFontSize"))
            {
                app.Resources["GlobalFontSize"] = CurrentFontSize;
            }
            else
            {
                app.Resources.Add("GlobalFontSize", CurrentFontSize);
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
                _ => "Microsoft YaHei"
            };
        }

        /// <summary>
        /// 初始化字体（字体名称和大小）
        /// </summary>
        public void Initialize(string savedFont, double savedFontSize = 14)
        {
            CurrentFont = savedFont;
            CurrentFontSize = savedFontSize;
            ApplyFont();
            ApplyFontSize();
        }
    }
}