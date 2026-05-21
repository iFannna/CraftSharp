using System;
using System.Windows.Threading;
using CraftSharp.Models;
using CraftSharp.Services.Core;
using CraftSharp.Services.Hud;
using CraftSharp.Services.Slot;
using CraftSharp.Services.Resource;

namespace CraftSharp.Services.Slot
{
    /// <summary>
    /// 格子拖动服务
    /// 负责长按检测、拖动状态管理、格子内容交换
    /// </summary>
    public class SlotDragService
    {
        // ===== 长按拖动相关常量 =====
        /// <summary>
        /// 长按判定时间（150ms）
        /// </summary>
        public const int LongPressDurationMs = 150;

        /// <summary>
        /// 拖动触发移动阈值（10像素）
        /// </summary>
        public const double DragMoveThreshold = 10;

        private readonly SlotDataService _slotDataService;
        private DispatcherTimer? _longPressTimer;

        // ===== 拖动状态 =====
        private int _longPressSlotIndex = -1;
        private bool _isDragReady = false;
        private int _dragSourceSlotIndex = -1;
        private int _dragTargetSlotIndex = -1;
        private bool _isDragging = false;
        private int _lastHoverSlotIndex = -1;

        /// <summary>
        /// 拖动开始事件
        /// 参数：sourceSlotIndex, sourceItem
        /// </summary>
        public event EventHandler<DragStartedEventArgs>? DragStarted;

        /// <summary>
        /// 拖动结束事件
        /// 参数：sourceSlotIndex, targetSlotIndex
        /// </summary>
        public event EventHandler<DragEndedEventArgs>? DragEnded;

        /// <summary>
        /// 格子交换完成事件
        /// 参数：sourceSlotIndex, targetSlotIndex
        /// </summary>
        public event EventHandler<SwapCompletedEventArgs>? SwapCompleted;

        /// <summary>
        /// 拖动过程中hover变化事件
        /// 参数：currentHoverSlotIndex, lastHoverSlotIndex
        /// </summary>
        public event EventHandler<HoverChangedEventArgs>? HoverChanged;

        /// <summary>
        /// 当前是否正在拖动中
        /// </summary>
        public bool IsDragging => _isDragging;

        /// <summary>
        /// 定时器是否正在运行（用于判断是否是快速点击）
        /// </summary>
        public bool IsTimerRunning => _longPressTimer != null && _longPressTimer.IsEnabled;

        /// <summary>
        /// 拖动源格子索引
        /// </summary>
        public int DragSourceSlotIndex => _dragSourceSlotIndex;

        /// <summary>
        /// 拖动目标格子索引
        /// </summary>
        public int DragTargetSlotIndex => _dragTargetSlotIndex;

        public SlotDragService(SlotDataService slotDataService)
        {
            _slotDataService = slotDataService;
        }

        #region 事件参数类

        public class DragStartedEventArgs : EventArgs
        {
            public int SourceSlotIndex { get; }
            public SlotItem SourceItem { get; }

            public DragStartedEventArgs(int sourceSlotIndex, SlotItem sourceItem)
            {
                SourceSlotIndex = sourceSlotIndex;
                SourceItem = sourceItem;
            }
        }

        public class DragEndedEventArgs : EventArgs
        {
            public int SourceSlotIndex { get; }
            public int TargetSlotIndex { get; }
            public bool ShouldSwap { get; }
            public bool ShouldRestore { get; }
            public bool HasSwap { get; }

            public DragEndedEventArgs(int sourceSlotIndex, int targetSlotIndex)
            {
                SourceSlotIndex = sourceSlotIndex;
                TargetSlotIndex = targetSlotIndex;
                ShouldSwap = sourceSlotIndex != targetSlotIndex && targetSlotIndex >= 0;
                ShouldRestore = !ShouldSwap && sourceSlotIndex >= 0;
                HasSwap = ShouldSwap;
            }
        }

        public class SwapCompletedEventArgs : EventArgs
        {
            public int SourceSlotIndex { get; }
            public int TargetSlotIndex { get; }
            public SlotItem SourceItem { get; }
            public SlotItem TargetItem { get; }

            public SwapCompletedEventArgs(int sourceSlotIndex, int targetSlotIndex, SlotItem sourceItem, SlotItem targetItem)
            {
                SourceSlotIndex = sourceSlotIndex;
                TargetSlotIndex = targetSlotIndex;
                SourceItem = sourceItem;
                TargetItem = targetItem;
            }
        }

        public class HoverChangedEventArgs : EventArgs
        {
            public int CurrentHoverSlotIndex { get; }
            public int LastHoverSlotIndex { get; }

            public HoverChangedEventArgs(int currentHoverSlotIndex, int lastHoverSlotIndex)
            {
                CurrentHoverSlotIndex = currentHoverSlotIndex;
                LastHoverSlotIndex = lastHoverSlotIndex;
            }
        }

        #endregion

        /// <summary>
        /// 启动长按检测
        /// </summary>
        public void StartLongPressDetection(int slotIndex)
        {
            _longPressSlotIndex = slotIndex;
            _isDragReady = false;

            if (_longPressTimer == null)
            {
                _longPressTimer = new DispatcherTimer();
                _longPressTimer.Interval = TimeSpan.FromMilliseconds(LongPressDurationMs);
                _longPressTimer.Tick += OnLongPressTimerTick;
            }
            _longPressTimer.Start();
        }

        /// <summary>
        /// 取消长按检测
        /// </summary>
        public void CancelLongPress()
        {
            _longPressTimer?.Stop();
            _longPressSlotIndex = -1;
            _isDragReady = false;
        }

        /// <summary>
        /// 处理鼠标移动
        /// 返回：是否应取消长按（移动超过阈值）
        /// </summary>
        public bool HandleMouseMove(double distance)
        {
            // 定时器等待中：如果移动超过阈值，取消定时器（视为点击）
            if (_longPressTimer != null && _longPressTimer.IsEnabled)
            {
                if (distance > DragMoveThreshold)
                {
                    CancelLongPress();
                    return true; // 取消长按
                }
            }

            // 定时器已触发（_isDragReady）：如果移动超过阈值，启动拖动
            if (_isDragReady && distance > DragMoveThreshold)
            {
                StartDrag();
            }

            return false;
        }

        /// <summary>
        /// 长按定时器触发
        /// </summary>
        private void OnLongPressTimerTick(object? sender, EventArgs e)
        {
            _longPressTimer?.Stop();

            if (_longPressSlotIndex < 0) return;

            // 标记拖动待触发（等待移动阈值）
            _isDragReady = true;
        }

        /// <summary>
        /// 更新拖动过程中的目标格子hover
        /// </summary>
        public void UpdateDragTarget(int targetSlotIndex)
        {
            if (!_isDragging) return;

            _dragTargetSlotIndex = targetSlotIndex;

            // 触发hover变化事件
            if (targetSlotIndex != _lastHoverSlotIndex)
            {
                HoverChanged?.Invoke(this, new HoverChangedEventArgs(targetSlotIndex, _lastHoverSlotIndex));
                _lastHoverSlotIndex = targetSlotIndex;
            }
        }

        /// <summary>
        /// 结束拖动
        /// </summary>
        public void EndDrag()
        {
            if (!_isDragging) return;

            _isDragging = false;

            // 触发拖动结束事件
            var args = new DragEndedEventArgs(_dragSourceSlotIndex, _dragTargetSlotIndex);
            DragEnded?.Invoke(this, args);

            // 如果需要交换
            if (args.ShouldSwap)
            {
                SwapSlots(_dragSourceSlotIndex, _dragTargetSlotIndex);
            }

            // 清理状态
            _dragSourceSlotIndex = -1;
            _dragTargetSlotIndex = -1;
            _lastHoverSlotIndex = -1;
        }

        /// <summary>
        /// 是否处于长按等待状态（定时器触发后等待移动阈值）
        /// </summary>
        public bool IsDragReady => _isDragReady;

        /// <summary>
        /// 获取当前长按等待的格子索引
        /// </summary>
        public int LongPressSlotIndex => _longPressSlotIndex;

        /// <summary>
        /// 获取格子ID（需要由外部提供映射）
        /// 这里使用默认的快捷栏ID映射，外部可以覆盖
        /// </summary>
        protected virtual string GetSlotId(int index)
        {
            // 默认快捷栏ID映射
            var slotIds = new[] { "hotbar_left_offhand", "hotbar_right_offhand", "hotbar_0", "hotbar_1", "hotbar_2", "hotbar_3", "hotbar_4", "hotbar_5", "hotbar_6", "hotbar_7", "hotbar_8" };
            if (index >= 0 && index < slotIds.Length)
            {
                return slotIds[index];
            }
            return $"slot_{index}";
        }

        /// <summary>
        /// 设置格子ID映射函数（用于自定义格子类型）
        /// </summary>
        public Func<int, string>? SlotIdMapper { get; set; }

        /// <summary>
        /// 当前物品栏样式（用于独立数据模式）
        /// </summary>
        public string? CurrentStyle { get; set; }

        /// <summary>
        /// 是否共享数据（true=共享数据，false=独立数据）
        /// </summary>
        public bool SharedData { get; set; } = true;

        /// <summary>
        /// 使用映射函数获取格子ID
        /// </summary>
        public string GetMappedSlotId(int index)
        {
            if (SlotIdMapper != null)
            {
                return SlotIdMapper(index);
            }
            return GetSlotId(index);
        }

        /// <summary>
        /// 启动拖动
        /// </summary>
        private void StartDrag()
        {
            _isDragReady = false;
            _dragSourceSlotIndex = _longPressSlotIndex;
            _isDragging = true;

            // 获取源格子内容（根据 SharedData 配置）
            var sourceItem = _slotDataService.GetSlot(GetSlotId(_dragSourceSlotIndex), CurrentStyle ?? "inventory.png", SharedData);

            // 触发拖动开始事件
            DragStarted?.Invoke(this, new DragStartedEventArgs(_dragSourceSlotIndex, sourceItem));
        }

        /// <summary>
        /// 交换两个格子内容
        /// </summary>
        private void SwapSlots(int sourceIndex, int targetIndex)
        {
            var sourceSlotId = GetMappedSlotId(sourceIndex);
            var targetSlotId = GetMappedSlotId(targetIndex);

            var sourceItem = _slotDataService.GetSlot(sourceSlotId, CurrentStyle ?? "inventory.png", SharedData);
            var targetItem = _slotDataService.GetSlot(targetSlotId, CurrentStyle ?? "inventory.png", SharedData);

            // 交换数据存储（根据 SharedData 配置）
            _slotDataService.SetSlot(sourceSlotId, targetItem, CurrentStyle ?? "inventory.png", SharedData);
            _slotDataService.SetSlot(targetSlotId, sourceItem, CurrentStyle ?? "inventory.png", SharedData);

            // 触发交换完成事件
            SwapCompleted?.Invoke(this, new SwapCompletedEventArgs(sourceIndex, targetIndex, sourceItem, targetItem));
        }
    }
}
