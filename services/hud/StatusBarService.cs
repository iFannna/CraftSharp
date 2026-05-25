using System;
using CraftSharp.Models;
using CraftSharp.Windows.StatusBar;

namespace CraftSharp.Services.Hud
{
    /// <summary>
    /// 状态栏窗口管理服务
    /// </summary>
    public class StatusBarService
    {
        private static StatusBarService? _instance;
        public static StatusBarService Instance => _instance ??= new StatusBarService();

        private StatusBarWindow? _statusBarWindow;
        private AppSettings? _appSettings;

        /// <summary>
        /// 状态栏可见性变化事件
        /// </summary>
        public event Action<bool>? VisibilityChanged;

        /// <summary>
        /// 状态栏锁定状态变化事件
        /// </summary>
        public event Action<bool>? LockStateChanged;

        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize(StatusBarWindow statusBarWindow, AppSettings settings)
        {
            _statusBarWindow = statusBarWindow;
            _appSettings = settings;
            _statusBarWindow.SetAppSettings(settings);
        }

        /// <summary>
        /// 初始化服务（无参数版本，兼容旧调用）
        /// </summary>
        public void Initialize(StatusBarWindow statusBarWindow)
        {
            _statusBarWindow = statusBarWindow;
        }

        /// <summary>
        /// 设置状态栏可见性
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_statusBarWindow == null) return;

            if (visible)
                _statusBarWindow.Show();
            else
                _statusBarWindow.Hide();

            VisibilityChanged?.Invoke(visible);
        }

        public void Toggle()
        {
            SetVisible(!IsVisible());
        }

        /// <summary>
        /// 获取状态栏可见性
        /// </summary>
        public bool IsVisible()
        {
            return _statusBarWindow?.IsVisible ?? false;
        }

        /// <summary>
        /// 设置状态栏锁定状态
        /// </summary>
        public void SetLocked(bool locked)
        {
            if (_statusBarWindow == null) return;

            _statusBarWindow.SetLocked(locked);
            LockStateChanged?.Invoke(locked);
        }

        /// <summary>
        /// 恢复状态栏位置
        /// </summary>
        public void RestorePosition(double x, double y)
        {
            if (_statusBarWindow == null) return;
            _statusBarWindow.Left = x;
            _statusBarWindow.Top = y;
        }

        /// <summary>
        /// 定位状态栏到屏幕底部水平居中（贴着任务栏上方）
        /// </summary>
        public void PositionToScreenBottomCenter()
        {
            if (_statusBarWindow == null) return;
            _statusBarWindow.PositionToScreenBottomCenter();
        }

        /// <summary>
        /// 设置副手槽启用状态
        /// </summary>
        public void SetOffhandConfig(bool leftEnabled, bool rightEnabled)
        {
            if (_statusBarWindow == null) return;
            _statusBarWindow.SetOffhandConfig(leftEnabled, rightEnabled);
        }

        /// <summary>
        /// 设置快捷栏可见性（包括格子、副手槽）
        /// </summary>
        public void SetHotbarVisible(bool visible)
        {
            if (_statusBarWindow == null) return;
            _statusBarWindow.SetHotbarVisible(visible);
        }

        /// <summary>
        /// 设置快捷栏点击模式（"single"单击/"double"双击）
        /// </summary>
        public void SetHotbarClickMode(string mode)
        {
            if (_statusBarWindow == null) return;
            _statusBarWindow.SetClickMode(mode);
        }

        /// <summary>
        /// 设置快捷栏悬浮效果（hover显示selection框）
        /// </summary>
        public void SetHotbarHoverEffect(bool enabled)
        {
            if (_statusBarWindow == null) return;
            _statusBarWindow.SetHoverEffectEnabled(enabled);
        }

        /// <summary>
        /// 设置经验条可见性
        /// </summary>
        public void SetExpBarVisible(bool visible)
        {
            if (_statusBarWindow == null) return;
            _statusBarWindow.SetExpBarVisible(visible);
        }

        /// <summary>
        /// 设置生命值可见性
        /// </summary>
        public void SetHealthVisible(bool visible)
        {
            if (_statusBarWindow == null) return;
            _statusBarWindow.SetHealthVisible(visible);
        }

        /// <summary>
        /// 设置饥饿值可见性
        /// </summary>
        public void SetFoodVisible(bool visible)
        {
            if (_statusBarWindow == null) return;
            _statusBarWindow.SetFoodVisible(visible);
        }

        /// <summary>
        /// 设置空气值可见性
        /// </summary>
        public void SetAirVisible(bool visible)
        {
            if (_statusBarWindow == null) return;
            _statusBarWindow.SetAirVisible(visible);
        }

        /// <summary>
        /// 设置护甲值可见性
        /// </summary>
        public void SetArmorVisible(bool visible)
        {
            if (_statusBarWindow == null) return;
            _statusBarWindow.SetArmorVisible(visible);
        }

        /// <summary>
        /// 设置伤害吸收值可见性
        /// </summary>
        public void SetAbsorbingVisible(bool visible)
        {
            if (_statusBarWindow == null) return;
            _statusBarWindow.SetAbsorbingVisible(visible);
        }

        /// <summary>
        /// 刷新指定HUD元素的显示
        /// </summary>
        public void RefreshHudElement(string id)
        {
            if (_statusBarWindow == null) return;
            _statusBarWindow.RefreshHudElement(id);
        }

        /// <summary>
        /// 刷新快捷栏图标（用于设置切换后重新加载）
        /// </summary>
        public void RefreshHotbarIcons()
        {
            if (_statusBarWindow == null) return;
            _statusBarWindow.RefreshHotbarIcons();
        }

        /// <summary>
        /// 刷新文件名显示颜色
        /// </summary>
        public void RefreshFileNameColor()
        {
            if (_statusBarWindow == null) return;
            _statusBarWindow.RefreshFileNameColor();
        }
    }
}
