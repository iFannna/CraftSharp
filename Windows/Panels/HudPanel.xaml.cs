using CraftSharp.Models;
using CraftSharp.Services;
using System.Windows;
using System.Windows.Controls;

namespace CraftSharp.Windows.Panels
{
    public partial class HudPanel : System.Windows.Controls.UserControl
    {
        private AppSettings _settings;

        public HudPanel(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            InitializeHudAccordion();

            // 监听语言变化，重新构建内容
            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;
        }

        private void OnLanguageChanged()
        {
            // 清空并重新构建
            HudAccordionContainer.Children.Clear();
            InitializeHudAccordion();
        }

        private void InitializeHudAccordion()
        {
            var hudElements = new[]
            {
                ("hotbar", "HudElementHotbar", "#3b82f6"),
                ("health", "HudElementHealth", "#ef4444"),
                ("food", "HudElementFood", "#eab308"),
                ("expbar", "HudElementExpbar", "#22c55e"),
                ("air", "HudElementAir", "#06b6d4"),
                ("armor", "HudElementArmor", "#6b7280"),
                ("absorbing", "HudElementAbsorbing", "#f97316"),
            };

            foreach (var (id, resourceKey, color) in hudElements)
            {
                var name = System.Windows.Application.Current.TryFindResource(resourceKey) as string ?? id;
                var item = new HudAccordionItem(_settings, id, name, color);
                HudAccordionContainer.Children.Add(item);
            }
        }
    }
}