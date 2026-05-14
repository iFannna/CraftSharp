using System;
using System.Collections.Generic;
using System.IO;
using CraftSharp.Models;

namespace CraftSharp.Services
{
    /// <summary>
    /// 格子文件有效性检测服务（App 级单例）
    /// 负责检测文件是否存在，管理丢失路径集合
    /// </summary>
    public class SlotFileValidator
    {
        private static SlotFileValidator? _instance;
        public static SlotFileValidator Instance => _instance ??= new SlotFileValidator();

        /// <summary>
        /// 文件丢失的路径集合
        /// </summary>
        private readonly HashSet<string> _missingFilePaths = new();

        /// <summary>
        /// 文件丢失事件
        /// </summary>
        public event EventHandler<string>? FileMissing;

        /// <summary>
        /// 文件恢复事件
        /// </summary>
        public event EventHandler<string>? FileRecovered;

        /// <summary>
        /// 获取所有丢失的路径
        /// </summary>
        public IReadOnlyCollection<string> MissingFilePaths => _missingFilePaths;

        /// <summary>
        /// 清空丢失路径记录
        /// </summary>
        public void ClearMissingPaths()
        {
            _missingFilePaths.Clear();
        }

        /// <summary>
        /// 检查文件路径是否有效（文件或目录存在）
        /// 对于快捷方式：检查快捷方式文件本身存在，不检查目标是否存在
        /// </summary>
        public bool IsFilePathValid(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            // 检查普通文件或目录是否存在
            if (File.Exists(filePath) || Directory.Exists(filePath))
                return true;

            // 如果是快捷方式（.lnk 文件），检查快捷方式文件本身是否存在
            if (filePath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                return File.Exists(filePath);
            }

            return false;
        }

        /// <summary>
        /// 标记文件为丢失状态
        /// </summary>
        public void MarkMissing(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            if (_missingFilePaths.Add(filePath))
            {
                FileMissing?.Invoke(this, filePath);
            }
        }

        /// <summary>
        /// 取消文件丢失标记
        /// </summary>
        public void UnmarkMissing(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return;

            if (_missingFilePaths.Remove(filePath))
            {
                FileRecovered?.Invoke(this, filePath);
            }
        }

        /// <summary>
        /// 检查路径是否在丢失集合中
        /// </summary>
        public bool IsMissing(string filePath)
        {
            return !string.IsNullOrEmpty(filePath) && _missingFilePaths.Contains(filePath);
        }

        /// <summary>
        /// 检查并标记：如果文件无效则标记为丢失
        /// 返回文件是否有效
        /// </summary>
        public bool ValidateAndMark(string filePath)
        {
            if (string.IsNullOrEmpty(filePath))
                return false;

            if (IsFilePathValid(filePath))
            {
                UnmarkMissing(filePath);
                return true;
            }
            else
            {
                MarkMissing(filePath);
                return false;
            }
        }

        /// <summary>
        /// 全量检查所有格子文件路径
        /// 遍历 AppSettings.Slots，检查每个路径是否有效
        /// 更新丢失状态并触发相应事件
        /// </summary>
        public void ValidateAllSlots(AppSettings? settings)
        {
            if (settings?.Slots == null) return;

            // 收集所有非空格子路径
            var allPaths = new HashSet<string>();
            foreach (var kvp in settings.Slots)
            {
                if (!kvp.Value.IsEmpty && !string.IsNullOrEmpty(kvp.Value.FilePath))
                {
                    allPaths.Add(kvp.Value.FilePath);
                }
            }

            // 检查每个路径
            foreach (var path in allPaths)
            {
                ValidateAndMark(path);
            }
        }

        /// <summary>
        /// 获取使用指定路径的所有格子 SlotId
        /// </summary>
        public List<string> GetSlotsByPath(AppSettings? settings, string filePath)
        {
            var result = new List<string>();
            if (settings?.Slots == null || string.IsNullOrEmpty(filePath)) return result;

            foreach (var kvp in settings.Slots)
            {
                if (!kvp.Value.IsEmpty && kvp.Value.FilePath == filePath)
                {
                    result.Add(kvp.Key);
                }
            }
            return result;
        }

        /// <summary>
        /// 清除所有使用指定路径的格子数据
        /// </summary>
        public void ClearAllSlotsByPath(AppSettings? settings, string filePath)
        {
            if (settings?.Slots == null || string.IsNullOrEmpty(filePath)) return;

            var slotsToRemove = GetSlotsByPath(settings, filePath);
            foreach (var slotId in slotsToRemove)
            {
                settings.Slots.Remove(slotId);
            }

            // 清除丢失标记
            UnmarkMissing(filePath);
        }
    }
}