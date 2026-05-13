using CraftSharp.Models;
using CraftSharp.Services;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CraftSharp.Windows.Settings.Panels
{
    public partial class HudPanel : System.Windows.Controls.UserControl
    {
        private AppSettings _settings;

        // 保存所有卡片的引用
        private List<HudAccordionItem> _cards = new();

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
            _cards.Clear();
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
                _cards.Add(item);
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
                _cards.Add(item);
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
                _cards.Add(item);
                HudBossBarContainer.Children.Add(item);
            }
        }

        /// <summary>
        /// 刷新所有卡片状态（响应记住卡片状态开关变化）
        /// </summary>
        public void RefreshCardStates(bool rememberEnabled)
        {
            foreach (var card in _cards)
            {
                // 获取卡片 Key（HudElement_xxx）
                string stateKey = $"HudElement_{card.HudId}";
                if (rememberEnabled)
                {
                    // 开关开启：从配置读取状态并应用
                    if (_settings.CardExpandedStates.TryGetValue(stateKey, out bool savedExpanded))
                    {
                        card.SetExpanded(savedExpanded, animate: false);
                    }
                    else
                    {
                        // 配置中没有此卡片状态，使用默认值（折叠）
                        card.SetExpanded(false, animate: false);
                    }
                }
                else
                {
                    // 开关关闭：恢复默认状态（折叠）
                    card.SetExpanded(false, animate: false);
                }
            }
        }
    }
}