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
                ("statusbar", "HudElementStatusBar"),
                ("hotbar", "HudElementHotbar"),
                ("expbar", "HudElementExpbar"),
                ("health", "HudElementHealth"),
                ("food", "HudElementFood"),
                ("air", "HudElementAir"),
                ("absorbing", "HudElementAbsorbing"),
                ("armor", "HudElementArmor"),
            };

            foreach (var (id, resourceKey) in hudElements)
            {
                var name = System.Windows.Application.Current.TryFindResource(resourceKey) as string ?? id;
                var item = new HudAccordionItem(_settings, id, name);
                HudAccordionContainer.Children.Add(item);
            }
        }
    }
}