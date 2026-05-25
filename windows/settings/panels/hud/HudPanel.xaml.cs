using CraftSharp.Models;
using CraftSharp.Services.Core;
using CraftSharp.Services.Hud;
using CraftSharp.Services.Slot;
using CraftSharp.Services.Resource;
using CraftSharp.Windows.Dialogs;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;

namespace CraftSharp.Windows.Settings.Panels.Hud
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
                ("StatusBar", "HudElementStatusBar"),
                ("Hotbar", "HudElementHotbar"),
                ("ExpBar", "HudElementExpbar"),
                ("Health", "HudElementHealth"),
                ("Food", "HudElementFood"),
                ("Air", "HudElementAir"),
                ("Absorbing", "HudElementAbsorbing"),
                ("Armor", "HudElementArmor"),
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
                ("Crosshair", "HudElementCrosshair"),
                ("AttackIndicator", "HudElementAttackIndicator"),
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
                ("BossBar", "HudElementBossBar"),
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
                    // 开关开启：保存当前状态到配置
                    _settings.System.CardExpandedStates[stateKey] = card.IsExpanded;
                }
                else
                {
                    // 开关关闭：恢复默认状态（折叠）
                    card.SetExpanded(false, animate: false);
                }
            }

            // 开关开启后保存配置
            if (rememberEnabled)
            {
                if (System.Windows.Application.Current is App app)
                {
                    app.SaveSettings();
                }
            }
        }

        private void RestoreDefaultsBtn_Click(object sender, RoutedEventArgs e)
        {
            var title = (string)Application.Current.TryFindResource("RestoreDefaultsConfirmTitle") ?? "";
            var message = (string)Application.Current.TryFindResource("RestoreDefaultsConfirmMessage") ?? "";
            var dialog = new ConfirmDialog(title, message) { Owner = Window.GetWindow(this) };
            dialog.ShowDialog();
            if (!dialog.IsConfirmed) return;

            _settings.HudElements.Clear();
            _settings.Hotbar = new HotbarSettings();
            _settings.StatusBar = new StatusBarSettings();

            _cards.Clear();
            HudStatusBarContainer.Children.Clear();
            HudCrosshairContainer.Children.Clear();
            HudBossBarContainer.Children.Clear();
            InitializeHudAccordion();

            if (Application.Current is App app)
            {
                app.SaveSettings();
            }
        }
    }
}