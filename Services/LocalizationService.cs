using System.Windows;

namespace CraftSharp.Services
{
    /// <summary>
    /// 本地化服务，管理语言切换
    /// </summary>
    public class LocalizationService
    {
        private static LocalizationService? _instance;
        public static LocalizationService Instance => _instance ??= new LocalizationService();

        /// <summary>
        /// 当前语言代码
        /// </summary>
        public string CurrentLanguage { get; private set; } = "zh-CN";

        /// <summary>
        /// 语言变化事件
        /// </summary>
        public event Action? LanguageChanged;

        /// <summary>
        /// 切换语言（传入显示名称）
        /// </summary>
        public void SetLanguage(string displayName)
        {
            var languageCode = GetLanguageCode(displayName);
            if (languageCode == CurrentLanguage) return;

            CurrentLanguage = languageCode;
            ApplyLanguage(languageCode);
            LanguageChanged?.Invoke();
        }

        /// <summary>
        /// 应用语言资源
        /// </summary>
        private void ApplyLanguage(string languageCode)
        {
            var app = System.Windows.Application.Current;
            if (app == null) return;

            // 找到并移除当前语言资源字典
            var currentDict = app.Resources.MergedDictionaries
                .FirstOrDefault(d => d.Source?.OriginalString?.Contains("Strings.") == true);

            if (currentDict != null)
            {
                app.Resources.MergedDictionaries.Remove(currentDict);
            }

            // 加载新的语言资源字典
            var newDictUri = new Uri($"pack://application:,,,/assets/resources/Strings.{languageCode}.xaml");
            var newDict = new ResourceDictionary { Source = newDictUri };
            app.Resources.MergedDictionaries.Add(newDict);
        }

        /// <summary>
        /// 初始化语言（传入语言代码）
        /// </summary>
        public void Initialize(string savedLanguageCode)
        {
            CurrentLanguage = savedLanguageCode;
            ApplyLanguage(savedLanguageCode);
        }

        /// <summary>
        /// 获取语言显示名称
        /// </summary>
        public string GetLanguageDisplayName()
        {
            return CurrentLanguage == "zh-CN" ? "简体中文" : "English";
        }

        /// <summary>
        /// 从显示名称获取语言代码
        /// </summary>
        public string GetLanguageCode(string displayName)
        {
            return displayName == "English" ? "en-US" : "zh-CN";
        }
    }
}