using System;
using System.Linq;
using HelixToolkit.SharpDX;

namespace CraftSharp.Helpers;

/// <summary>
/// 全应用共享的单一 D3D11 EffectsManager。
/// 每个视口各建 DefaultEffectsManager 会各持一份独立 D3D11 设备与驱动堆，
/// 且 SharpDX 的 COM 资源没有终结器，控件重建时不显式 Dispose 即永久泄漏
/// （皮肤页宽窄切换/搜索过滤每批重建 9 个卡片控件，实测约 160MB/批）。
/// 共享后设备只此一份；各视口仍保留自己的渲染目标，外观不变。
/// </summary>
public static class SharedEffectsManager
{
    private static DefaultEffectsManager? _instance;

    public static DefaultEffectsManager Instance => _instance ??= new DefaultEffectsManager();

    /// <summary>应用退出时统一回收设备（须在所有窗口关闭后调用）。</summary>
    public static void Shutdown()
    {
        _instance?.Dispose();
        _instance = null;
    }
}

/// <summary>
/// HelixToolkit 场景元素的显式释放：GroupModel3D/MeshGeometryModel3D 等 IDisposable
/// 的原生资源无终结器，从场景移除后必须逐个 Dispose，否则随重建累积泄漏。
/// </summary>
internal static class HelixDispose
{
    public static void Tree(HelixToolkit.Wpf.SharpDX.Element3D element)
    {
        if (element is HelixToolkit.Wpf.SharpDX.GroupModel3D group)
        {
            foreach (var child in group.Children.ToList())
                Tree(child);
        }
        (element as IDisposable)?.Dispose();
    }
}
