using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using CraftSharp.Models;

namespace CraftSharp.Services
{
    /// <summary>
    /// 格子剪贴板服务 - 管理剪切/复制/粘贴操作
    /// </summary>
    public class SlotClipboardService
    {
        private static SlotClipboardService? _instance;
        public static SlotClipboardService Instance => _instance ??= new SlotClipboardService();

        // 剪贴板状态
        private string? _clipboardFilePath = null;
        private string? _clipboardDisplayName = null;
        private string? _sourceSlotId = null;
        private bool _isCut = false; // true=剪切, false=复制

        /// <summary>
        /// 当前剪贴板文件路径
        /// </summary>
        public string? ClipboardFilePath => _clipboardFilePath;

        /// <summary>
        /// 源格子ID（用于剪切操作）
        /// </summary>
        public string? SourceSlotId => _sourceSlotId;

        /// <summary>
        /// 是否为剪切操作（true=剪切, false=复制）
        /// </summary>
        public bool IsCut => _isCut;

        /// <summary>
        /// 是否有剪贴板内容
        /// </summary>
        public bool HasClipboardContent => !string.IsNullOrEmpty(_clipboardFilePath);

        /// <summary>
        /// 复制格子内容到剪贴板（后台 STA 线程写入 Windows 剪贴板）
        /// </summary>
        public void Copy(string slotId, string filePath, string? displayName = null)
        {
            // 立即更新内部状态（同步，无卡顿）
            _clipboardFilePath = filePath;
            _clipboardDisplayName = displayName;
            _sourceSlotId = slotId;
            _isCut = false;

            // 后台 STA 线程写入 Windows 剪贴板（避免 UI 卡顿）
            StartStaThread(() => TrySetWindowsClipboard(filePath));
        }

        /// <summary>
        /// 剪切格子内容到剪贴板（后台 STA 线程写入 Windows 剪贴板）
        /// </summary>
        public void Cut(string slotId, string filePath, string? displayName = null)
        {
            // 立即更新内部状态（同步，无卡顿）
            _clipboardFilePath = filePath;
            _clipboardDisplayName = displayName;
            _sourceSlotId = slotId;
            _isCut = true;

            // 后台 STA 线程写入 Windows 剪贴板（避免 UI 卡顿）
            StartStaThread(() => TrySetWindowsClipboard(filePath));
        }

        /// <summary>
        /// 启动 STA 后台线程执行操作（Clipboard 操作需要 STA 线程）
        /// </summary>
        private void StartStaThread(Action action)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch { }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// 粘贴剪贴板内容到目标格子
        /// </summary>
        /// <returns>粘贴的 SlotItem，如果剪贴板为空则返回 null</returns>
        public SlotItem? Paste(string targetSlotId, string currentStyle, bool sharedData)
        {
            if (!HasClipboardContent) return null;

            var pastedItem = new SlotItem
            {
                FilePath = _clipboardFilePath!,
                DisplayName = _clipboardDisplayName ?? ""
            };

            // 设置目标格子数据
            SlotDataService.Instance.SetSlot(targetSlotId, pastedItem, currentStyle, sharedData);

            // 如果是剪切操作，清除源格子
            if (_isCut && !string.IsNullOrEmpty(_sourceSlotId) && _sourceSlotId != targetSlotId)
            {
                SlotDataService.Instance.ClearSlot(_sourceSlotId, currentStyle, sharedData);
            }

            // 剪切操作粘贴后清空剪贴板，复制操作保持
            if (_isCut)
            {
                Clear();
            }

            return pastedItem;
        }

        /// <summary>
        /// 清空剪贴板
        /// </summary>
        public void Clear()
        {
            _clipboardFilePath = null;
            _clipboardDisplayName = null;
            _sourceSlotId = null;
            _isCut = false;
        }

        /// <summary>
        /// 检查 Windows 剪贴板是否有文件路径
        /// </summary>
        public bool HasWindowsClipboardFile()
        {
            try
            {
                // 需要在 UI 线程访问 Clipboard
                if (System.Windows.Application.Current.Dispatcher.CheckAccess())
                {
                    if (System.Windows.Clipboard.ContainsText())
                    {
                        var text = System.Windows.Clipboard.GetText();
                        return File.Exists(text) || Directory.Exists(text);
                    }
                }
                else
                {
                    // 后台线程调用时，通过 Dispatcher 同步执行
                    return System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (System.Windows.Clipboard.ContainsText())
                        {
                            var text = System.Windows.Clipboard.GetText();
                            return File.Exists(text) || Directory.Exists(text);
                        }
                        return false;
                    });
                }
            }
            catch { }
            return false;
        }

        /// <summary>
        /// 获取 Windows 剪贴板中的文件路径
        /// </summary>
        public string? GetWindowsClipboardFilePath()
        {
            try
            {
                // 需要在 UI 线程访问 Clipboard
                if (System.Windows.Application.Current.Dispatcher.CheckAccess())
                {
                    if (System.Windows.Clipboard.ContainsText())
                    {
                        var text = System.Windows.Clipboard.GetText();
                        if (File.Exists(text) || Directory.Exists(text))
                            return text;
                    }
                }
                else
                {
                    // 后台线程调用时，通过 Dispatcher 同步执行
                    return System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        if (System.Windows.Clipboard.ContainsText())
                        {
                            var text = System.Windows.Clipboard.GetText();
                            if (File.Exists(text) || Directory.Exists(text))
                                return text;
                        }
                        return null;
                    });
                }
            }
            catch { }
            return null;
        }

        /// <summary>
        /// 尝试写入 Windows 剪贴板（在 STA 后台线程中直接执行）
        /// </summary>
        private void TrySetWindowsClipboard(string text)
        {
            try
            {
                // 在 STA 线程中可以直接访问 Clipboard，不需要 Dispatcher
                System.Windows.Clipboard.SetText(text);
            }
            catch { }
        }
    }
}