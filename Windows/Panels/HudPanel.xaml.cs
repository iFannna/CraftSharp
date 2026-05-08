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
            HudStatusBarContainer.Children.Clear();
            HudCrosshairContainer.Children.Clear();
            HudBossBarContainer.Children.Clear();
            InitializeHudAccordion();
        }

        private void InitializeHudAccordion()
        {
            // 状态栏元素
            var statusBarElements = new[]
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

            foreach (var (id, resourceKey) in statusBarElements)
            {
                var name = System.Windows.Application.Current.TryFindResource(resourceKey) as string ?? id;
                var item = new HudAccordionItem(_settings, id, name);
                HudStatusBarContainer.Children.Add(item);
            }

            // 准星元素
            var crosshairElements = new[]
            {
                ("crosshair", "HudElementCrosshair"),
                ("attackindicator", "HudElementAttackIndicator"),
            };

            foreach (var (id, resourceKey) in crosshairElements)
            {
                var name = System.Windows.Application.Current.TryFindResource(resourceKey) as string ?? id;
                var item = new HudAccordionItem(_settings, id, name);
                HudCrosshairContainer.Children.Add(item);
            }

            // BOSS血条元素
            var bossBarElements = new[]
            {
                ("bossbar", "HudElementBossBar"),
            };

            foreach (var (id, resourceKey) in bossBarElements)
            {
                var name = System.Windows.Application.Current.TryFindResource(resourceKey) as string ?? id;
                var item = new HudAccordionItem(_settings, id, name);
                HudBossBarContainer.Children.Add(item);
            }
        }
    }
}