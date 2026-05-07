using System;
using CraftSharp.Models;

namespace CraftSharp.Services
{
    /// <summary>
    /// 准星窗口管理服务
    /// </summary>
    public class CrosshairService
    {
        private static CrosshairService? _instance;
        public static CrosshairService Instance => _instance ??= new CrosshairService();

        private Windows.CrosshairWindow? _crosshairWindow;
        private AppSettings? _appSettings;

        /// <summary>
        /// 初始化服务
        /// </summary>
        public void Initialize(Windows.CrosshairWindow crosshairWindow, AppSettings settings)
        {
            _crosshairWindow = crosshairWindow;
            _appSettings = settings;
            _crosshairWindow.SetAppSettings(settings);
        }

        /// <summary>
        /// 设置准星可见性
        /// </summary>
        public void SetCrosshairVisible(bool visible)
        {
            if (_crosshairWindow == null) return;
            _crosshairWindow.SetCrosshairVisible(visible);
        }

        /// <summary>
        /// 设置攻击指示器可见性
        /// </summary>
        public void SetAttackIndicatorVisible(bool visible)
        {
            if (_crosshairWindow == null) return;
            _crosshairWindow.SetAttackIndicatorVisible(visible);
        }

        /// <summary>
        /// 设置窗口置顶
        /// </summary>
        public void SetTopMost(bool topMost)
        {
            if (_crosshairWindow == null) return;
            _crosshairWindow.SetTopMost(topMost);
        }

        /// <summary>
        /// 刷新攻击指示器显示
        /// </summary>
        public void RefreshAttackIndicator()
        {
            if (_crosshairWindow == null) return;
            _crosshairWindow.RefreshAttackIndicator();
        }

        /// <summary>
        /// 刷新指定HUD元素的显示
        /// </summary>
        public void RefreshHudElement(string id)
        {
            if (_crosshairWindow == null) return;

            switch (id)
            {
                case "crosshair":
                    // 准星没有进度，只需检查可见性
                    var crosshairSettings = _appSettings?.HudElements.FirstOrDefault(h => h.Id == "crosshair");
                    if (crosshairSettings != null)
                    {
                        SetCrosshairVisible(crosshairSettings.IsVisible);
                        SetTopMost(crosshairSettings.TopMost);
                    }
                    break;
                case "attackindicator":
                    RefreshAttackIndicator();
                    break;
            }
        }
    }
}