using CraftSharp.Models;

namespace CraftSharp.Interfaces
{
    /// <summary>
    /// 格子接口 - 定义格子通用行为
    /// 快捷栏格子、背包格子等都可实现此接口
    /// </summary>
    public interface ISlot
    {
        /// <summary>
        /// 格子唯一标识符
        /// </summary>
        string SlotId { get; }

        /// <summary>
        /// 格子索引（在所属格子组中的位置）
        /// </summary>
        int Index { get; }

        /// <summary>
        /// 格子是否为空
        /// </summary>
        bool IsEmpty { get; }

        /// <summary>
        /// 格子当前项的文件路径
        /// </summary>
        string FilePath { get; }

        /// <summary>
        /// 获取格子当前项
        /// </summary>
        SlotItem GetItem();

        /// <summary>
        /// 设置格子项
        /// </summary>
        void SetItem(SlotItem item);

        /// <summary>
        /// 清空格子
        /// </summary>
        void Clear();

        /// <summary>
        /// 格子所属的格子组类型
        /// </summary>
        SlotGroupType GroupType { get; }
    }

    /// <summary>
    /// 格子组类型
    /// </summary>
    public enum SlotGroupType
    {
        /// <summary>
        /// 快捷栏格子（主快捷栏 + 副手槽）
        /// </summary>
        Hotbar,

        /// <summary>
        /// 背包格子
        /// </summary>
        Inventory,

        /// <summary>
        /// 自定义格子组
        /// </summary>
        Custom
    }

    /// <summary>
    /// 格子组接口 - 定义格子组行为
    /// </summary>
    public interface ISlotGroup
    {
        /// <summary>
        /// 格子组唯一标识符
        /// </summary>
        string GroupId { get; }

        /// <summary>
        /// 格子组类型
        /// </summary>
        SlotGroupType GroupType { get; }

        /// <summary>
        /// 格子数量
        /// </summary>
        int SlotCount { get; }

        /// <summary>
        /// 获取指定索引的格子
        /// </summary>
        ISlot GetSlot(int index);

        /// <summary>
        /// 获取所有格子
        /// </summary>
        IReadOnlyList<ISlot> GetAllSlots();

        /// <summary>
        /// 清空所有格子
        /// </summary>
        void ClearAll();
    }
}