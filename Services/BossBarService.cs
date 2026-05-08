using System;
using CraftSharp.Models;

namespace CraftSharp.Services
{
    /// <summary>
    /// BOSS血条窗口管理服务
    /// </summary>
    public class BossBarService
    {
        private static BossBarService? _instance;
        public static BossBarService Instance => _instance ??= new BossBarService();

        private Windows.BossBarWindow? _bossBarWindow;
        private AppSettings? _appSettings;

        /// <summary>
        /// BOSS血条可见性变化事件
        /// </summary>
        public event Action<bool>? VisibilityChanged;

        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize(Windows.BossBarWindow bossBarWindow, AppSettings settings)
        {
            _bossBarWindow = bossBarWindow;
            _appSettings = settings;
        }

        /// <summary>
        /// 设置BOSS血条可见性
        /// </summary>
        public void SetVisible(bool visible)
        {
            if (_bossBarWindow == null) return;

            _bossBarWindow.SetVisibility(visible);
            VisibilityChanged?.Invoke(visible);
        }

        /// <summary>
        /// 获取BOSS血条可见性
        /// </summary>
        public bool IsVisible()
        {
            return _bossBarWindow?.Visibility == System.Windows.Visibility.Visible;
        }

        /// <summary>
        /// 设置缩放比例
        /// </summary>
        public void SetScaleFactor(double scaleFactor)
        {
            if (_bossBarWindow == null) return;
            _bossBarWindow.SetScaleFactor(scaleFactor);
        }
    }
}