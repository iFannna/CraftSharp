using System;
using System.Collections.Generic;
using CraftSharp.Models;

namespace CraftSharp.Services
{
    /// <summary>
    /// 格子数据存储服务 - App 级单例，数据存储在 settings.json 中
    /// </summary>
    public class SlotDataService
    {
        private static SlotDataService? _instance;
        public static SlotDataService Instance => _instance ??= new SlotDataService();

        private readonly Dictionary<string, SlotItem> _slots = new();

        private SlotDataService()
        {
            LoadData();
        }

        /// <summary>
        /// 获取格子项
        /// </summary>
        public SlotItem GetSlot(string slotId)
        {
            return _slots.TryGetValue(slotId, out var item) ? item : new SlotItem();
        }

        /// <summary>
        /// 设置格子项
        /// </summary>
        public void SetSlot(string slotId, SlotItem item)
        {
            _slots[slotId] = item;
            SaveData();
        }

        /// <summary>
        /// 清空格子
        /// </summary>
        public void ClearSlot(string slotId)
        {
            _slots.Remove(slotId);
            SaveData();
        }

        /// <summary>
        /// 获取所有格子数据
        /// </summary>
        public Dictionary<string, SlotItem> GetAllSlots()
        {
            return new Dictionary<string, SlotItem>(_slots);
        }

        /// <summary>
        /// 从 AppSettings 重新加载数据（用于外部修改后同步）
        /// </summary>
        public void Reload()
        {
            _slots.Clear();
            LoadData();
        }

        /// <summary>
        /// 加载数据 - 从 AppSettings.Slots 加载
        /// </summary>
        private void LoadData()
        {
            var appSettings = GetAppSettings();
            if (appSettings?.Slots != null)
            {
                foreach (var kvp in appSettings.Slots)
                {
                    _slots[kvp.Key] = kvp.Value;
                }
            }
        }

        /// <summary>
        /// 保存数据 - 保存到 AppSettings.Slots 并触发 settings.json 保存
        /// </summary>
        private void SaveData()
        {
            var appSettings = GetAppSettings();
            if (appSettings != null)
            {
                appSettings.Slots = new Dictionary<string, SlotItem>(_slots);
                // 触发 App.xaml.cs 的 SaveSettings
                if (App.Current is App app)
                {
                    app.SaveSettings();
                }
            }
        }

        /// <summary>
        /// 获取 AppSettings 实例
        /// </summary>
        private AppSettings? GetAppSettings()
        {
            if (App.Current is App app)
            {
                return app.GetAppSettings();
            }
            return null;
        }
    }
}