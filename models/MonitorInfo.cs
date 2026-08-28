using CraftSharp.Helpers;

namespace CraftSharp.Models
{
    /// <summary>
    /// 显示器信息（物理像素坐标，虚拟桌面坐标系）
    /// </summary>
    /// <param name="DevicePath">IDesktopWallpaper 设备路径，硬件级稳定，作持久化 key</param>
    /// <param name="Bounds">物理像素矩形（虚拟桌面坐标系，可为负）</param>
    /// <param name="IsPrimary">是否主屏（矩形包含虚拟桌面原点 (0,0)）</param>
    /// <param name="Index">按 Bounds.Left 从左到右排序的序号（1 起，UI 展示用）</param>
    public record MonitorInfo(string DevicePath, Win32Helper.RECT Bounds, bool IsPrimary, int Index)
    {
        public int Width => Bounds.Right - Bounds.Left;
        public int Height => Bounds.Bottom - Bounds.Top;
    }
}
