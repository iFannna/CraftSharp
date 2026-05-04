using System;

namespace CraftSharp.Services
{
    /// <summary>
    /// 状态栏窗口管理服务
    /// </summary>
    public class StatusBarService
    {
        private static StatusBarService? _instance;
        public static StatusBarService Instance => _instance ??= new StatusBarService();

        private Windows.StatusBarWindow? _statusBarWindow;

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
        public void Initialize(Windows.StatusBarWindow statusBarWindow)
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
        /// 保存状态栏位置
        /// </summary>
        public void SavePosition()
        {
            if (_statusBarWindow == null) return;
            // TODO: 实现保存位置逻辑
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
        /// 设置副手槽启用状态
        /// </summary>
        public void SetOffhandConfig(bool leftEnabled, bool rightEnabled)
        {
            if (_statusBarWindow == null) return;
            _statusBarWindow.SetOffhandConfig(leftEnabled, rightEnabled);
        }
    }
}