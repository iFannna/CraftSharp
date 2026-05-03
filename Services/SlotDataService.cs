using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using CraftSharp.Models;

namespace CraftSharp.Services
{
    /// <summary>
    /// 格子数据存储服务
    /// </summary>
    public class SlotDataService
    {
        private static readonly string DataFilePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory, "slot_data.json");

        private readonly Dictionary<string, SlotItem> _slots = new();

        public SlotDataService()
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
        /// 加载数据
        /// </summary>
        private void LoadData()
        {
            if (!File.Exists(DataFilePath))
                return;

            try
            {
                var json = File.ReadAllText(DataFilePath);
                var data = JsonConvert.DeserializeObject<Dictionary<string, SlotItem>>(json);
                if (data != null)
                {
                    foreach (var kvp in data)
                    {
                        _slots[kvp.Key] = kvp.Value;
                    }
                }
            }
            catch
            {
                // 加载失败时使用空数据
            }
        }

        /// <summary>
        /// 保存数据
        /// </summary>
        private void SaveData()
        {
            try
            {
                var json = JsonConvert.SerializeObject(_slots, Formatting.Indented);
                File.WriteAllText(DataFilePath, json);
            }
            catch
            {
                // 保存失败时忽略
            }
        }
    }
}