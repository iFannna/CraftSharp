using CraftSharp.Services;
using System.Windows;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Settings.Panels
{
    /// <summary>
    /// HudAccordionItem 快捷栏配置
    /// </summary>
    public partial class HudAccordionItem
    {
        private void AddHotbarContent()
        {
            AddClickModeComboBox();

            var hotbarToggle = AddToggleRow("HudOptionShowHotbar", "HudOptionShowHotbarDesc", _settings.Hotbar.Visible);
            hotbarToggle.Checked += (s, e) => { _settings.Hotbar.Visible = true; StatusBarService.Instance.SetHotbarVisible(true); SaveSettings(); };
            hotbarToggle.Unchecked += (s, e) => { _settings.Hotbar.Visible = false; StatusBarService.Instance.SetHotbarVisible(false); SaveSettings(); };

            var hoverToggle = AddToggleRow("HudOptionHoverEffect", "HudOptionHoverEffectDesc", _settings.Hotbar.HoverEffect);
            hoverToggle.Checked += (s, e) => { _settings.Hotbar.HoverEffect = true; StatusBarService.Instance.SetHotbarHoverEffect(true); SaveSettings(); };
            hoverToggle.Unchecked += (s, e) => { _settings.Hotbar.HoverEffect = false; StatusBarService.Instance.SetHotbarHoverEffect(false); SaveSettings(); };

            var leftOffhandToggle = AddToggleRow("HudOptionLeftOffhand", "HudOptionLeftOffhandDesc", _settings.Hotbar.LeftOffhand);
            leftOffhandToggle.Checked += (s, e) => { _settings.Hotbar.LeftOffhand = true; StatusBarService.Instance.SetOffhandConfig(true, _settings.Hotbar.RightOffhand); SaveSettings(); };
            leftOffhandToggle.Unchecked += (s, e) => { _settings.Hotbar.LeftOffhand = false; StatusBarService.Instance.SetOffhandConfig(false, _settings.Hotbar.RightOffhand); SaveSettings(); };

            var rightOffhandToggle = AddToggleRow("HudOptionRightOffhand", "HudOptionRightOffhandDesc", _settings.Hotbar.RightOffhand);
            rightOffhandToggle.Checked += (s, e) => { _settings.Hotbar.RightOffhand = true; StatusBarService.Instance.SetOffhandConfig(_settings.Hotbar.LeftOffhand, true); SaveSettings(); };
            rightOffhandToggle.Unchecked += (s, e) => { _settings.Hotbar.RightOffhand = false; StatusBarService.Instance.SetOffhandConfig(_settings.Hotbar.LeftOffhand, false); SaveSettings(); };

            var showTargetIconToggle = AddToggleRow("HudOptionShowTargetIcon", "HudOptionShowTargetIconDesc", _settings.Hotbar.ShowTargetIcon);
            showTargetIconToggle.Checked += (s, e) => {
                _settings.Hotbar.ShowTargetIcon = true;
                StatusBarService.Instance.RefreshHotbarIcons();
                if (System.Windows.Application.Current is App app) app.GetInventoryWindow()?.RefreshIcons();
                SaveSettings();
            };
            showTargetIconToggle.Unchecked += (s, e) => {
                _settings.Hotbar.ShowTargetIcon = false;
                StatusBarService.Instance.RefreshHotbarIcons();
                if (System.Windows.Application.Current is App app) app.GetInventoryWindow()?.RefreshIcons();
                SaveSettings();
            };
        }
    }
}