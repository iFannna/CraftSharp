using CraftSharp.Services.Core;
using CraftSharp.Services.Hud;
using CraftSharp.Services.Slot;
using CraftSharp.Services.Resource;
using System.Windows;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Settings.Panels.Hud
{
    /// <summary>
    /// HudAccordionItem 状态栏配置
    /// </summary>
    public partial class HudAccordionItem
    {
        private ToggleSwitch? _statusBarShowToggle;
        private bool _statusBarSubscribed;
        private bool _syncingStatusBarToggle;

        private void AddStatusBarContent()
        {
            var showToggle = AddToggleRow("HudOptionShowStatusBar", "HudOptionShowStatusBarDesc", _settings.StatusBar.Visible);
            _statusBarShowToggle = showToggle;
            // 语言切换会重建内容，事件只订阅一次，处理器始终引用最新开关实例
            if (!_statusBarSubscribed)
            {
                StatusBarService.Instance.VisibilityChanged += OnStatusBarVisibilityChanged;
                _statusBarSubscribed = true;
            }
            showToggle.Checked += (s, e) => { if (_syncingStatusBarToggle) return; StatusBarService.Instance.SetVisible(true); };
            showToggle.Unchecked += (s, e) => { if (_syncingStatusBarToggle) return; StatusBarService.Instance.SetVisible(false); };

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

        /// <summary>
        /// 显隐变化事件同步开关 UI（托盘/热键等面板外路径触发）
        /// </summary>
        private void OnStatusBarVisibilityChanged(bool visible)
        {
            var toggle = _statusBarShowToggle;
            if (toggle == null) return;
            _syncingStatusBarToggle = true;
            toggle.IsChecked = visible;
            _syncingStatusBarToggle = false;
        }
    }
}