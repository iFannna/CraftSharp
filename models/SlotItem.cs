namespace CraftSharp.Models
{
    /// <summary>
    /// 格子项数据模型
    /// </summary>
    public class SlotItem
    {
        /// <summary>
        /// 文件/快捷方式的完整路径
        /// </summary>
        public string FilePath { get; set; } = string.Empty;

        /// <summary>
        /// 显示名称（可选，用于识别）
        /// </summary>
        public string DisplayName { get; set; } = string.Empty;

        /// <summary>
        /// 是否为空格子
        /// </summary>
        public bool IsEmpty => string.IsNullOrEmpty(FilePath);
    }
}