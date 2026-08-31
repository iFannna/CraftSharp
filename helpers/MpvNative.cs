using System;
using System.IO;
using System.Runtime.InteropServices;

namespace CraftSharp.Helpers;

/// <summary>
/// libmpv（libmpv-2.dll）进程内播放的 P/Invoke 绑定。
/// DLL 从应用 tools/ 目录预加载（不在系统搜索路径中），需随发布部署。
/// 字符串参数必须 LPUTF8Str 编组：mpv 只接受 UTF-8，Ansi 会在中文路径下坏路径。
/// </summary>
internal static class MpvNative
{
    private const string LibName = "libmpv-2.dll";
    private static bool _loadAttempted;
    private static bool _loaded;

    public const int MpvEventShutdown = 1;
    public const int MpvEventLogMessageId = 2;
    public const int MpvEventPlaybackRestart = 21;

    [StructLayout(LayoutKind.Sequential)]
    internal struct MpvEvent
    {
        public int EventId;
        public int Error;
        public ulong ReplyUserdata;
        public IntPtr Data;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct MpvEventLogMessage
    {
        public IntPtr Prefix;
        public IntPtr Level;
        public IntPtr Text;
        public int LogLevel;
    }

    /// <summary>
    /// 从 tools/ 显式加载 libmpv-2.dll（发布目录与开发目录两个候选位置）。
    /// 已加载过的模块后续 DllImport 按名字直接命中，无需再设搜索路径。
    /// </summary>
    public static bool EnsureLoaded()
    {
        if (_loadAttempted) return _loaded;
        _loadAttempted = true;

        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var candidates = new[]
        {
            Path.Combine(baseDir, "tools", LibName),
            Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "tools", LibName))
        };
        foreach (var path in candidates)
        {
            if (!File.Exists(path)) continue;
            try
            {
                NativeLibrary.Load(path);
                _loaded = true;
                return true;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[Wallpaper] Load {path} failed: {ex.Message}");
            }
        }
        return false;
    }

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_request_log_messages(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string minLevel);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_create();

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_initialize(IntPtr ctx);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_option_string(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_command(IntPtr ctx, IntPtr[] args);

    /// <summary>
    /// argv 式命令调用：参数以原始字符串传递，不过 mpv 控制台语法解析器，
    /// 路径中的反斜杠/引号无需转义。
    /// </summary>
    public static int Command(IntPtr ctx, params string[] args)
    {
        var ptrs = new IntPtr[args.Length + 1]; // 末位 NULL 终止
        try
        {
            for (var i = 0; i < args.Length; i++)
            {
                var bytes = System.Text.Encoding.UTF8.GetBytes(args[i]);
                var p = Marshal.AllocHGlobal(bytes.Length + 1);
                Marshal.Copy(bytes, 0, p, bytes.Length);
                Marshal.WriteByte(p, bytes.Length, 0);
                ptrs[i] = p;
            }
            return mpv_command(ctx, ptrs);
        }
        finally
        {
            foreach (var p in ptrs)
                if (p != IntPtr.Zero) Marshal.FreeHGlobal(p);
        }
    }

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern int mpv_set_property_string(IntPtr ctx,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string name,
        [MarshalAs(UnmanagedType.LPUTF8Str)] string value);

    /// <summary>timeout 单位为秒（double），负值永久阻塞；声明成 int 会走错寄存器导致自旋</summary>
    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern IntPtr mpv_wait_event(IntPtr ctx, double timeoutSeconds);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_wakeup(IntPtr ctx);

    [DllImport(LibName, CallingConvention = CallingConvention.Cdecl)]
    public static extern void mpv_terminate_destroy(IntPtr ctx);
}
