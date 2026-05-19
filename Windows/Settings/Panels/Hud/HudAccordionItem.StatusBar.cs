using CraftSharp.Services;
using System.Windows;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Settings.Panels.Hud
{
    /// <summary>
    /// HudAccordionItem 状态栏配置
    /// </summary>
    public partial class HudAccordionItem
    {
        private void AddStatusBarContent()
        {
            var showToggle = AddToggleRow("HudOptionShowStatusBar", "HudOptionShowStatusBarDesc", _settings.StatusBar.Visible);
            showToggle.Checked += (s, e) => { _settings.StatusBar.Visible = true; StatusBarService.Instance.SetVisible(true); SaveSettings(); };
            showToggle.Unchecked += (s, e) => { _settings.StatusBar.Visible = false; StatusBarService.Instance.SetVisible(false); SaveSettings(); };

            var lockToggle = AddToggleRow("HudOptionLockPosition", "HudOptionLockPositionDesc", _settings.StatusBar.Locked);
            lockToggle.Checked += (s, e) => { _settings.StatusBar.Locked = true; StatusBarService.Instance.SetLocked(true); SaveSettings(); };
            lockToggle.Unchecked += (s, e) => { _settings.StatusBar.Locked = false; StatusBarService.Instance.SetLocked(false); SaveSettings(); };

            var rememberToggle = AddToggleRow("HudOptionRememberPosition", "HudOptionRememberPositionDesc", _settings.StatusBar.RememberPosition);
            rememberToggle.Checked += (s, e) => { _settings.StatusBar.RememberPosition = true; SaveSettings(); };
            rememberToggle.Unchecked += (s, e) =>
            {
                _settings.StatusBar.RememberPosition = false;
                // 关闭开关时重置位置为默认值（下次启动居中）
                _settings.StatusBar.PositionX = 0;
                _settings.StatusBar.PositionY = 0;
                SaveSettings();
            };
        }
    }
}