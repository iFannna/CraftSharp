using CraftSharp.Services;
using System.Windows;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Settings.Panels
{
    /// <summary>
    /// HudAccordionItem 状态栏配置
    /// </summary>
    public partial class HudAccordionItem
    {
        private void AddStatusBarContent()
        {
            var showToggle = AddToggleRow("HudOptionShowStatusBar", "HudOptionShowStatusBarDesc", _settings.StatusBarVisible);
            showToggle.Checked += (s, e) => { _settings.StatusBarVisible = true; StatusBarService.Instance.SetVisible(true); SaveSettings(); };
            showToggle.Unchecked += (s, e) => { _settings.StatusBarVisible = false; StatusBarService.Instance.SetVisible(false); SaveSettings(); };

            var lockToggle = AddToggleRow("HudOptionLockPosition", "HudOptionLockPositionDesc", _settings.StatusBarLocked);
            lockToggle.Checked += (s, e) => { _settings.StatusBarLocked = true; StatusBarService.Instance.SetLocked(true); SaveSettings(); };
            lockToggle.Unchecked += (s, e) => { _settings.StatusBarLocked = false; StatusBarService.Instance.SetLocked(false); SaveSettings(); };

            var rememberToggle = AddToggleRow("HudOptionRememberPosition", "HudOptionRememberPositionDesc", _settings.StatusBarRememberPosition);
            rememberToggle.Checked += (s, e) => { _settings.StatusBarRememberPosition = true; SaveSettings(); };
            rememberToggle.Unchecked += (s, e) => { _settings.StatusBarRememberPosition = false; SaveSettings(); };
        }
    }
}