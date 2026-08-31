using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using CraftSharp.Helpers;
using CraftSharp.Models;
using CraftSharp.Services.Hud;
using CraftSharp.Windows.Dialogs;

namespace CraftSharp.Services.Slot
{
    /// <summary>
    /// 格子右键菜单服务 - 创建和管理 ContextMenu
    /// </summary>
    public class SlotContextMenuService
    {
        private static SlotContextMenuService? _instance;
        public static SlotContextMenuService Instance => _instance ??= new SlotContextMenuService();

        private ContextMenu? _currentMenu;
        private Action? _pendingAction; // 待执行的操作（菜单关闭后执行）

        // 可执行文件扩展名列表
        private static readonly string[] ExecutableExtensions = { ".exe", ".bat", ".cmd", ".msi", ".ps1", ".vbs" };

        /// <summary>
        /// 创建格子右键菜单
        /// </summary>
        /// <param name="slotId">格子ID</param>
        /// <param name="item">格子项数据</param>
        /// <param name="isMissing">文件是否丢失</param>
        /// <param name="currentStyle">当前样式（如 inventory.png）</param>
        /// <param name="sharedData">是否共享数据</param>
        /// <param name="refreshCallback">刷新UI回调</param>
        public ContextMenu CreateSlotContextMenu(
            string slotId,
            SlotItem item,
            bool isMissing,
            string currentStyle,
            bool sharedData,
            Action refreshCallback)
        {
            var menu = new ContextMenu
            {
                Style = (Style)System.Windows.Application.Current.FindResource("WpfUiContextMenuStyle")
            };

            bool isEmpty = item.IsEmpty;

            // 清理上次遗留的待执行操作
            _pendingAction = null;

            // 菜单关闭后执行待执行的操作
            menu.Closed += (s, e) =>
            {
                if (_pendingAction != null)
                {
                    // 使用 ContextIdle 优先级，等待所有 UI 操作完成后再执行
                    var action = _pendingAction;
                    _pendingAction = null;
                    System.Windows.Application.Current.Dispatcher.InvokeAsync(action, System.Windows.Threading.DispatcherPriority.ContextIdle);
                }
            };

            if (!isEmpty)
            {
                // 有文件格子菜单
                AddMenuItem(menu, "ContextMenuOpen", () => OpenFile(item.FilePath, isMissing), isMissing);
                AddMenuItem(menu, "ContextMenuOpenLocation", () => OpenFileLocation(item.FilePath, isMissing), isMissing);

                // 仅对可执行文件显示"以管理员身份运行"
                bool isExecutable = IsExecutableFile(item.FilePath);
                if (isExecutable)
                {
                    AddMenuItem(menu, "ContextMenuRunAsAdmin", () => RunAsAdmin(item.FilePath, isMissing), isMissing);
                }

                AddSeparator(menu);
                AddMenuItem(menu, "ContextMenuCopyPath", () => CopyPathToClipboardAsync(item.FilePath));
                AddMenuItem(menu, "ContextMenuCut", () => CutSlotAsync(slotId, item));
                AddMenuItem(menu, "ContextMenuCopy", () => CopySlotAsync(slotId, item));
                AddMenuItem(menu, "ContextMenuPaste", () => PasteToSlot(slotId, currentStyle, sharedData, refreshCallback),
                    !SlotClipboardService.Instance.HasClipboardContent && !SlotClipboardService.Instance.HasWindowsClipboardFile());
                AddSeparator(menu);
                AddMenuItem(menu, "ContextMenuDelete", () => DeleteSlot(slotId, currentStyle, sharedData, refreshCallback));
                AddMenuItem(menu, "ContextMenuRename", () => RenameFile(slotId, item, currentStyle, sharedData, refreshCallback), isMissing);
            }
            else
            {
                // 空格子菜单 - 仅显示粘贴
                AddMenuItem(menu, "ContextMenuPaste", () => PasteToSlot(slotId, currentStyle, sharedData, refreshCallback),
                    !SlotClipboardService.Instance.HasClipboardContent && !SlotClipboardService.Instance.HasWindowsClipboardFile());
            }

            _currentMenu = menu;
            return menu;
        }

        /// <summary>
        /// 判断是否为可执行文件
        /// </summary>
        private bool IsExecutableFile(string filePath)
        {
            if (string.IsNullOrEmpty(filePath)) return false;
            var ext = Path.GetExtension(filePath).ToLowerInvariant();
            return Array.IndexOf(ExecutableExtensions, ext) >= 0;
        }

        /// <summary>
        /// 添加菜单项
        /// </summary>
        private void AddMenuItem(ContextMenu menu, string resourceKey, Action action, bool isDisabled = false)
        {
            var header = System.Windows.Application.Current.TryFindResource(resourceKey) as string ?? resourceKey;
            var item = new MenuItem
            {
                Header = header,
                Style = (Style)System.Windows.Application.Current.FindResource("WpfUiMenuItemStyle"),
                IsEnabled = !isDisabled
            };
            item.Click += (s, e) =>
            {
                // 记录待执行操作，菜单关闭后执行
                _pendingAction = action;
                // 关闭菜单（触发 Closed 事件执行操作）
                if (_currentMenu != null)
                {
                    _currentMenu.IsOpen = false;
                }
            };
            menu.Items.Add(item);
        }

        /// <summary>
        /// 添加分隔符
        /// </summary>
        private void AddSeparator(ContextMenu menu)
        {
            var separator = new Separator
            {
                Style = (Style)System.Windows.Application.Current.FindResource("WpfUiMenuSeparatorStyle")
            };
            menu.Items.Add(separator);
        }

        // ===== 菜单操作实现 =====

        /// <summary>
        /// 打开文件
        /// </summary>
        private void OpenFile(string filePath, bool isMissing)
        {
            if (isMissing)
            {
                ShowMissingFileDialog(filePath);
                return;
            }
            TryExecuteFile(filePath);
        }

        /// <summary>
        /// 打开文件所在位置
        /// </summary>
        private void OpenFileLocation(string filePath, bool isMissing)
        {
            if (isMissing)
            {
                ShowMissingFileDialog(filePath);
                return;
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select, \"{filePath}\"",
                UseShellExecute = true
            });
        }

        /// <summary>
        /// 以管理员身份运行（仅对可执行文件）
        /// </summary>
        private void RunAsAdmin(string filePath, bool isMissing)
        {
            if (isMissing)
            {
                ShowMissingFileDialog(filePath);
                return;
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = filePath,
                Verb = "runas",
                UseShellExecute = true
            });
        }

        /// <summary>
        /// 复制文件路径到剪贴板（STA 后台线程，不阻塞 UI）
        /// </summary>
        private void CopyPathToClipboardAsync(string filePath)
        {
            var thread = new Thread(() =>
            {
                try
                {
                    System.Windows.Clipboard.SetText(filePath);
                }
                catch { }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.IsBackground = true;
            thread.Start();
        }

        /// <summary>
        /// 剪切格子（内部状态立即更新，Windows 剪贴板异步写入）
        /// </summary>
        private void CutSlotAsync(string slotId, SlotItem item)
        {
            SlotClipboardService.Instance.Cut(slotId, item.FilePath, item.DisplayName);
        }

        /// <summary>
        /// 复制格子（内部状态立即更新，Windows 剪贴板异步写入）
        /// </summary>
        private void CopySlotAsync(string slotId, SlotItem item)
        {
            SlotClipboardService.Instance.Copy(slotId, item.FilePath, item.DisplayName);
        }

        /// <summary>
        /// 粘贴到格子
        /// </summary>
        private void PasteToSlot(string slotId, string currentStyle, bool sharedData, Action refreshCallback)
        {
            // 首先检查内部剪贴板
            if (SlotClipboardService.Instance.HasClipboardContent)
            {
                SlotClipboardService.Instance.Paste(slotId, currentStyle, sharedData);
            }
            // 然后检查 Windows 剪贴板
            else if (SlotClipboardService.Instance.HasWindowsClipboardFile())
            {
                var filePath = SlotClipboardService.Instance.GetWindowsClipboardFilePath();
                if (filePath != null)
                {
                    SlotDataService.Instance.SetSlot(slotId, new SlotItem { FilePath = filePath }, currentStyle, sharedData);
                }
            }
            refreshCallback();
            // 通知其他窗口刷新
            StatusBarService.Instance.RefreshHotbarIcons();
            if (System.Windows.Application.Current is App app)
            {
                app.GetInventoryWindow()?.RefreshIcons();
            }
        }

        /// <summary>
        /// 删除格子内容
        /// </summary>
        private void DeleteSlot(string slotId, string currentStyle, bool sharedData, Action refreshCallback)
        {
            SlotDataService.Instance.ClearSlot(slotId, currentStyle, sharedData);
            refreshCallback();
            // 通知其他窗口刷新
            StatusBarService.Instance.RefreshHotbarIcons();
            if (System.Windows.Application.Current is App app)
            {
                app.GetInventoryWindow()?.RefreshIcons();
            }
        }

        /// <summary>
        /// 重命名文件（物理文件重命名）
        /// </summary>
        private void RenameFile(string slotId, SlotItem item, string currentStyle, bool sharedData, Action refreshCallback)
        {
            if (item.IsEmpty) return;

            string oldFilePath = item.FilePath;
            string oldFileName = Path.GetFileNameWithoutExtension(oldFilePath);
            string extension = Path.GetExtension(oldFilePath);
            string directory = Path.GetDirectoryName(oldFilePath) ?? "";

            // 弹出重命名对话框
            var renameWindow = new RenameSlotWindow(oldFileName, oldFilePath);
            renameWindow.Owner = GetActiveWindow();
            renameWindow.ShowDialogQuiet();

            if (!renameWindow.IsConfirmed) return;

            string newFileName = renameWindow.NewDisplayName.Trim();
            if (string.IsNullOrEmpty(newFileName) || newFileName == oldFileName)
            {
                // 名称未改变，不做操作
                return;
            }

            // 构建新文件路径
            string newFilePath = Path.Combine(directory, newFileName + extension);

            // 检查新文件名是否已存在
            if (File.Exists(newFilePath))
            {
                System.Windows.MessageBox.Show(
                    $"文件 \"{newFileName + extension}\" 已存在，请使用其他名称。",
                    "重命名失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
                return;
            }

            try
            {
                // 重命名物理文件
                File.Move(oldFilePath, newFilePath);

                // 更新格子数据（所有使用此路径的格子）
                SlotFileValidator.Instance.UpdateAllSlotsFilePath(
                    (System.Windows.Application.Current as App)?.GetAppSettings(),
                    oldFilePath,
                    newFilePath);

                // 刷新 UI
                refreshCallback();
                StatusBarService.Instance.RefreshHotbarIcons();
                if (System.Windows.Application.Current is App app)
                {
                    app.GetInventoryWindow()?.RefreshIcons();
                }
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"重命名失败：{ex.Message}",
                    "重命名失败",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }

        /// <summary>
        /// 显示文件丢失提示对话框
        /// </summary>
        private void ShowMissingFileDialog(string filePath)
        {
            var confirmWindow = new SlotMissingConfirmWindow(filePath);
            confirmWindow.Owner = GetActiveWindow();
            confirmWindow.ShowDialogQuiet();
        }

        /// <summary>
        /// 尝试执行文件
        /// </summary>
        private bool TryExecuteFile(string filePath)
        {
            try
            {
                if (File.Exists(filePath) || Directory.Exists(filePath))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = filePath,
                        UseShellExecute = true
                    });
                    return true;
                }
                return false;
            }
            catch { return false; }
        }

        /// <summary>
        /// 获取当前活动窗口
        /// </summary>
        private Window? GetActiveWindow()
        {
            return System.Windows.Application.Current.Windows.OfType<Window>()
                .FirstOrDefault(w => w.IsActive);
        }
    }
}
