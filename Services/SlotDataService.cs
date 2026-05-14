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
        /// 获取格子项（根据 SharedData 配置决定数据来源）
        /// </summary>
        /// <param name="slotId">格子ID</param>
        /// <param name="stylePath">当前样式文件名（如 inventory.png）</param>
        /// <param name="sharedData">是否共享数据</param>
        public SlotItem GetSlot(string slotId, string stylePath, bool sharedData)
        {
            if (sharedData)
            {
                // 共享数据：使用原有的 _slots
                return _slots.TryGetValue(slotId, out var item) ? item : new SlotItem();
            }
            else
            {
                // 独立数据：使用 StyleSlots
                if (!string.IsNullOrEmpty(stylePath))
                {
                    var styleSlots = GetStyleSlots(stylePath);
                    return styleSlots.TryGetValue(slotId, out var item) ? item : new SlotItem();
                }
                return new SlotItem();
            }
        }

        /// <summary>
        /// 设置格子项（根据 SharedData 配置决定数据存储位置）
        /// </summary>
        public void SetSlot(string slotId, SlotItem item, string stylePath, bool sharedData)
        {
            if (sharedData)
            {
                // 共享数据：存储到 _slots
                _slots[slotId] = item;
                SaveData();
            }
            else
            {
                // 独立数据：存储到 StyleSlots
                if (!string.IsNullOrEmpty(stylePath))
                {
                    var styleSlots = GetStyleSlots(stylePath);
                    styleSlots[slotId] = item;
                    SaveStyleSlotsData();
                }
            }
        }

        /// <summary>
        /// 清空格子（根据 SharedData 配置）
        /// </summary>
        public void ClearSlot(string slotId, string stylePath, bool sharedData)
        {
            if (sharedData)
            {
                _slots.Remove(slotId);
                SaveData();
            }
            else
            {
                if (!string.IsNullOrEmpty(stylePath))
                {
                    var styleSlots = GetStyleSlots(stylePath);
                    styleSlots.Remove(slotId);
                    SaveStyleSlotsData();
                }
            }
        }

        /// <summary>
        /// 获取指定样式的格子数据字典（如果不存在则创建）
        /// </summary>
        private Dictionary<string, SlotItem> GetStyleSlots(string stylePath)
        {
            var appSettings = GetAppSettings();
            if (appSettings?.StyleSlots == null) return new Dictionary<string, SlotItem>();

            if (!appSettings.StyleSlots.TryGetValue(stylePath, out var slots))
            {
                slots = new Dictionary<string, SlotItem>();
                appSettings.StyleSlots[stylePath] = slots;
            }
            return slots;
        }

        /// <summary>
        /// 保存 StyleSlots 数据到 settings.json
        /// </summary>
        private void SaveStyleSlotsData()
        {
            if (App.Current is App app)
            {
                app.SaveSettings();
            }
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