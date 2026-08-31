using System.Windows.Media;

namespace CraftSharp.Services.Core
{
    /// <summary>
    /// 字体管理服务
    /// </summary>
    public class FontService
    {
        private static FontService? _instance;
        public static FontService Instance => _instance ??= new FontService();

        /// <summary>
        /// 当前字体标识符
        /// </summary>
        public string CurrentFontTag { get; private set; } = "YaHei";

        /// <summary>
        /// 当前字体大小
        /// </summary>
        public double CurrentFontSize { get; private set; } = 14;

        /// <summary>
        /// 字体变化事件
        /// </summary>
        public event Action? FontChanged;

        /// <summary>
        /// 设置字体（使用标识符）
        /// </summary>
        public void SetFont(string fontTag)
        {
            fontTag = fontTag.ToLowerInvariant();
            if (fontTag == CurrentFontTag) return;

            CurrentFontTag = fontTag;
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

            System.Windows.Media.FontFamily fontFamily = GetFontFamilyByTag(CurrentFontTag);

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
        /// 根据标识符获取字体
        /// </summary>
        private System.Windows.Media.FontFamily GetFontFamilyByTag(string tag)
        {
            return tag.ToLowerInvariant() switch
            {
                "pixel" => new System.Windows.Media.FontFamily(new Uri("pack://application:,,,/"), "/assets/fonts/zpix.ttf#Zpix"),
                "unifont" => new System.Windows.Media.FontFamily(new Uri("pack://application:,,,/"), "/assets/fonts/unifont-16.0.04.ttf#Unifont"),
                "songti" => new System.Windows.Media.FontFamily("SimSun"),
                "heiti" => new System.Windows.Media.FontFamily("SimHei"),
                "kaiti" => new System.Windows.Media.FontFamily("KaiTi"),
                _ => new System.Windows.Media.FontFamily("Microsoft YaHei")
            };
        }

        /// <summary>
        /// 应用字体大小（同时派生各档位字号，跟随基础字号缩放）
        /// </summary>
        private void ApplyFontSize()
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            var sizes = new (string Key, double Size)[]
            {
                ("GlobalFontSize", CurrentFontSize),
                ("GlobalFontSizeTitleXL", Math.Max(8, CurrentFontSize + 10)),
                ("GlobalFontSizeTitle", Math.Max(8, CurrentFontSize + 6)),
                ("GlobalFontSizeSection", Math.Max(8, CurrentFontSize + 2)),
                ("GlobalFontSizeSmall", Math.Max(8, CurrentFontSize - 1)),
                ("GlobalFontSizeCaption", Math.Max(8, CurrentFontSize - 2)),
                ("GlobalFontSizeTiny", Math.Max(8, CurrentFontSize - 3)),
            };

            foreach (var (key, size) in sizes)
            {
                if (app.Resources.Contains(key))
                {
                    app.Resources[key] = size;
                }
                else
                {
                    app.Resources.Add(key, size);
                }
            }
        }

        /// <summary>
        /// 初始化字体（使用标识符）
        /// </summary>
        public void Initialize(string savedFontTag, double savedFontSize = 14)
        {
            CurrentFontTag = ConvertToTag(savedFontTag).ToLowerInvariant();
            CurrentFontSize = savedFontSize;
            ApplyFont();
            ApplyFontSize();
        }

        /// <summary>
        /// 将旧版本的中文名称转换为标识符
        /// </summary>
        private string ConvertToTag(string oldName)
        {
            return oldName switch
            {
                "像素字体" => "pixel",
                "统一字体" => "unifont",
                "宋体" => "songti",
                "黑体" => "heiti",
                "楷体" => "kaiti",
                _ => oldName // 如果已经是标识符，直接返回
            };
        }
    }
}
