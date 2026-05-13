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

            var hotbarToggle = AddToggleRow("HudOptionShowHotbar", "HudOptionShowHotbarDesc", _settings.HotbarVisible);
            hotbarToggle.Checked += (s, e) => { _settings.HotbarVisible = true; StatusBarService.Instance.SetHotbarVisible(true); SaveSettings(); };
            hotbarToggle.Unchecked += (s, e) => { _settings.HotbarVisible = false; StatusBarService.Instance.SetHotbarVisible(false); SaveSettings(); };

            var hoverToggle = AddToggleRow("HudOptionHoverEffect", "HudOptionHoverEffectDesc", _settings.HotbarHoverEffect);
            hoverToggle.Checked += (s, e) => { _settings.HotbarHoverEffect = true; StatusBarService.Instance.SetHotbarHoverEffect(true); SaveSettings(); };
            hoverToggle.Unchecked += (s, e) => { _settings.HotbarHoverEffect = false; StatusBarService.Instance.SetHotbarHoverEffect(false); SaveSettings(); };

            var leftOffhandToggle = AddToggleRow("HudOptionLeftOffhand", "HudOptionLeftOffhandDesc", _settings.HotbarLeftOffhand);
            leftOffhandToggle.Checked += (s, e) => { _settings.HotbarLeftOffhand = true; StatusBarService.Instance.SetOffhandConfig(true, _settings.HotbarRightOffhand); SaveSettings(); };
            leftOffhandToggle.Unchecked += (s, e) => { _settings.HotbarLeftOffhand = false; StatusBarService.Instance.SetOffhandConfig(false, _settings.HotbarRightOffhand); SaveSettings(); };

            var rightOffhandToggle = AddToggleRow("HudOptionRightOffhand", "HudOptionRightOffhandDesc", _settings.HotbarRightOffhand);
            rightOffhandToggle.Checked += (s, e) => { _settings.HotbarRightOffhand = true; StatusBarService.Instance.SetOffhandConfig(_settings.HotbarLeftOffhand, true); SaveSettings(); };
            rightOffhandToggle.Unchecked += (s, e) => { _settings.HotbarRightOffhand = false; StatusBarService.Instance.SetOffhandConfig(_settings.HotbarLeftOffhand, false); SaveSettings(); };

            var showTargetIconToggle = AddToggleRow("HudOptionShowTargetIcon", "HudOptionShowTargetIconDesc", _settings.HotbarShowTargetIcon);
            showTargetIconToggle.Checked += (s, e) => { _settings.HotbarShowTargetIcon = true; StatusBarService.Instance.RefreshHotbarIcons(); SaveSettings(); };
            showTargetIconToggle.Unchecked += (s, e) => { _settings.HotbarShowTargetIcon = false; StatusBarService.Instance.RefreshHotbarIcons(); SaveSettings(); };
        }
    }
}