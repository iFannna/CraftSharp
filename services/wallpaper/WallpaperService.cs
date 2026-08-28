using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using CraftSharp.Helpers;
using CraftSharp.Models;
using CraftSharp.Windows.Wallpaper;

namespace CraftSharp.Services.Wallpaper;

/// <summary>
/// 壁纸编排层：下载/裁切 + 布局快照 diff + 全量重排。
/// 核心模型：WorkerW 全虚拟桌面只有一个，任何系统壁纸 API 调用都会销毁重建它，
/// 因此静态路径有任何变化时必须走全量重排（停所有动态→设所有静态→等 WorkerW→重建动态）；
/// 仅动态视频变化时走 IPC 热切换快路径。
/// </summary>
public class WallpaperService
{
    private static readonly HttpClient _http = new();

    public static WallpaperService Instance { get; } = new();

    private WallpaperService() { }

    private sealed record MonitorStaticPlan(string DevicePath, string ImagePath);
    private sealed record DynamicPlan(string Key, string VideoPath, Win32Helper.RECT Bounds);
    private sealed record LayoutSnapshot(string Mode, List<MonitorStaticPlan> Statics, List<DynamicPlan> Dynamics);

    private readonly SemaphoreSlim _layoutGate = new(1, 1);
    private LayoutSnapshot? _currentSnapshot;
    private int _applyRunning;
    private volatile bool _relayoutQueued;

    private static App? App => System.Windows.Application.Current as App;
    private static WallpaperSettings? Settings => App?.GetAppSettings()?.Wallpaper;

    private static string WallpaperDir => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "assets", "wallpaper");

    #region 布局编排

    /// <summary>
    /// 应用当前配置的壁纸布局（唯一公共入口，fire-and-forget 安全，重入自动合并）
    /// </summary>
    public async Task ApplyLayoutAsync()
    {
        // 编排必须运行在 UI 线程：COM 单元正确性 + 窗口创建线程正确性
        var dispatcher = App?.Dispatcher;
        if (dispatcher != null && !dispatcher.CheckAccess())
        {
            dispatcher.BeginInvoke(() => _ = ApplyLayoutAsync());
            return;
        }

        if (Interlocked.Exchange(ref _applyRunning, 1) == 1)
        {
            _relayoutQueued = true;
            return;
        }

        try
        {
            await ApplyLayoutCoreAsync();
        }
        finally
        {
            var replay = _relayoutQueued;
            _relayoutQueued = false;
            _applyRunning = 0;
            if (replay)
            {
                dispatcher?.BeginInvoke(() => _ = ApplyLayoutAsync());
            }
        }
    }

    private async Task ApplyLayoutCoreAsync()
    {
        var settings = Settings;
        if (settings == null) return;

        var snapshot = await BuildSnapshotAsync(settings);
        if (snapshot == null) return;

        if (_currentSnapshot != null
            && _currentSnapshot.Mode == snapshot.Mode
            && _currentSnapshot.Statics.SequenceEqual(snapshot.Statics)
            && _currentSnapshot.Dynamics.SequenceEqual(snapshot.Dynamics))
        {
            Debug.WriteLine("[Wallpaper] Layout unchanged, skip");
            return;
        }

        var sw = Stopwatch.StartNew();

        // 快路径：静态全部不变且模式相同，仅动态内容差异 → IPC 热切换
        if (_currentSnapshot != null
            && snapshot.Mode == _currentSnapshot.Mode
            && snapshot.Statics.SequenceEqual(_currentSnapshot.Statics)
            && await ApplyDynamicOnlyAsync(snapshot, _currentSnapshot))
        {
            _currentSnapshot = snapshot;
            Debug.WriteLine($"[Wallpaper] Dynamic-only apply done in {sw.ElapsedMilliseconds}ms");
            return;
        }

        await ApplyFullRelayoutAsync(snapshot);
        _currentSnapshot = snapshot;
        Debug.WriteLine($"[Wallpaper] Full relayout done in {sw.ElapsedMilliseconds}ms (statics={snapshot.Statics.Count}, dynamics={snapshot.Dynamics.Count})");
    }

    /// <summary>
    /// 构建期望布局快照。所有下载/裁切都在 teardown 之前完成，期间桌面保持现状。
    /// </summary>
    private async Task<LayoutSnapshot?> BuildSnapshotAsync(WallpaperSettings settings)
    {
        var monitors = MonitorLayoutService.Instance.GetMonitors();
        if (monitors.Count == 0) return null;

        var statics = new List<MonitorStaticPlan>();
        var dynamics = new List<DynamicPlan>();
        var settingsDirty = false;

        if (settings.Mode == "span" && settings.Span is { } span)
        {
            var fingerprint = MonitorLayoutService.Instance.GetTopologyFingerprint(monitors);

            if (span.Type == "static" && File.Exists(span.LocalFilePath))
            {
                var crops = await SpanCropService.Instance.ProduceCropsAsync(
                    span.LocalFilePath, span.WallpaperId, monitors, fingerprint);
                statics.AddRange(crops.Select(c => new MonitorStaticPlan(c.Monitor.DevicePath, c.CropPath)));
            }
            else if (span.Type == "dynamic" && File.Exists(span.LocalFilePath))
            {
                var previewSource = span.PreviewPath;
                if (IsStalePreviewFallback(previewSource, span.WallpaperId) && !string.IsNullOrEmpty(span.WallpaperId))
                {
                    var backfilled = await EnsureFallbackImageAsync(span.WallpaperId);
                    if (backfilled != null)
                    {
                        span.PreviewPath = backfilled;
                        previewSource = backfilled;
                        settingsDirty = true;
                    }
                }
                if (File.Exists(previewSource))
                {
                    var crops = await SpanCropService.Instance.ProduceCropsAsync(
                        previewSource, span.WallpaperId, monitors, fingerprint);
                    statics.AddRange(crops.Select(c => new MonitorStaticPlan(c.Monitor.DevicePath, c.CropPath)));
                }
                dynamics.Add(new DynamicPlan(
                    DynamicWallpaperService.SpanKey,
                    span.LocalFilePath,
                    MonitorLayoutService.Instance.GetVirtualScreenBounds(monitors)));
            }
        }
        else
        {
            foreach (var monitor in monitors)
            {
                if (!settings.Monitors.TryGetValue(monitor.DevicePath, out var entry))
                    continue;

                if (entry.Type == "static" && File.Exists(entry.LocalFilePath))
                {
                    statics.Add(new MonitorStaticPlan(monitor.DevicePath, entry.LocalFilePath));
                }
                else if (entry.Type == "dynamic" && File.Exists(entry.LocalFilePath))
                {
                    var fallback = entry.PreviewPath;
                    if (IsStalePreviewFallback(fallback, entry.WallpaperId) && !string.IsNullOrEmpty(entry.WallpaperId))
                    {
                        var backfilled = await EnsureFallbackImageAsync(entry.WallpaperId);
                        if (backfilled != null)
                        {
                            entry.PreviewPath = backfilled;
                            fallback = backfilled;
                            settingsDirty = true;
                        }
                    }
                    if (File.Exists(fallback))
                        statics.Add(new MonitorStaticPlan(monitor.DevicePath, fallback));
                    dynamics.Add(new DynamicPlan(monitor.DevicePath, entry.LocalFilePath, monitor.Bounds));
                }
            }
        }

        if (settingsDirty)
            App?.SaveSettings();

        return new LayoutSnapshot(
            settings.Mode,
            statics.OrderBy(s => s.DevicePath, StringComparer.Ordinal).ToList(),
            dynamics.OrderBy(d => d.Key, StringComparer.Ordinal).ToList());
    }

    /// <summary>
    /// 动态快路径：静态不变时逐 key IPC 热切换/新建窗口。无法快路径时返回 false。
    /// </summary>
    private static async Task<bool> ApplyDynamicOnlyAsync(LayoutSnapshot target, LayoutSnapshot current)
    {
        var oldByKey = current.Dynamics.ToDictionary(d => d.Key);
        var newByKey = target.Dynamics.ToDictionary(d => d.Key);

        // 消失的 key：直接停对应窗口，不涉及 WorkerW
        foreach (var gone in oldByKey.Keys.Where(k => !newByKey.ContainsKey(k)).ToList())
            DynamicWallpaperService.Instance.Stop(gone);

        foreach (var plan in target.Dynamics)
        {
            if (!oldByKey.TryGetValue(plan.Key, out var old))
            {
                await DynamicWallpaperService.Instance.StartPlaybackAsync(plan.Key, plan.VideoPath, plan.Bounds);
                await Task.Delay(200); // 多实例错峰启动
                continue;
            }

            if (!SameBounds(old.Bounds, plan.Bounds))
                return false; // 目标矩形变了，必须重建窗口

            if (old.VideoPath == plan.VideoPath)
                continue;

            if (!await DynamicWallpaperService.Instance.SwitchVideoAsync(plan.Key, plan.VideoPath))
                return false; // IPC 不可用，回退全量重排
        }
        return true;
    }

    private static bool SameBounds(Win32Helper.RECT a, Win32Helper.RECT b) =>
        a.Left == b.Left && a.Top == b.Top && a.Right == b.Right && a.Bottom == b.Bottom;

    /// <summary>
    /// 全量重排：停所有动态 → 设所有静态 → 等 WorkerW 重建 → 重建动态窗口
    /// </summary>
    private static async Task ApplyFullRelayoutAsync(LayoutSnapshot snapshot)
    {
        DynamicWallpaperService.Instance.StopAllPlayback();

        var oldWorkerW = DynamicWallpaperWindow.FindDesktopWorkerW(nudge: false);

        if (snapshot.Statics.Count > 0)
        {
            if (DesktopWallpaperService.Instance.IsComAvailable)
            {
                foreach (var s in snapshot.Statics)
                    DesktopWallpaperService.Instance.SetWallpaperForMonitor(s.DevicePath, s.ImagePath);
            }
            else
            {
                // 降级：全局单图（第一张）
                DesktopWallpaperService.Instance.SetWallpaper(snapshot.Statics[0].ImagePath);
            }

            await WaitForWorkerWRebuiltAsync(oldWorkerW);
        }
        else if (DynamicWallpaperWindow.FindDesktopWorkerW(nudge: false) == IntPtr.Zero)
        {
            // 无静态路径需要设置且 WorkerW 不存在（如首次动态壁纸），建立一次
            NudgeWorkerW();
        }

        foreach (var d in snapshot.Dynamics)
        {
            _ = DynamicWallpaperService.Instance.StartPlaybackAsync(d.Key, d.VideoPath, d.Bounds)
                .ContinueWith(t => Debug.WriteLine($"[Wallpaper] StartPlayback faulted for {d.Key}: {t.Exception!.GetBaseException().Message}"),
                    TaskContinuationOptions.OnlyOnFaulted);
            await Task.Delay(200); // 多实例错峰启动
        }
    }

    /// <summary>
    /// 等待 WorkerW 因壁纸变更而销毁重建完成（替换 500ms 硬延迟）。
    /// 0x052C 同步广播先行，再轮询验证句柄变化；超时 best-effort 继续。
    /// </summary>
    private static async Task<bool> WaitForWorkerWRebuiltAsync(IntPtr oldWorkerW, int timeoutMs = 2000)
    {
        var progman = Win32Helper.FindWindow("Progman", null);
        if (progman == IntPtr.Zero) return false;

        // 主动触发一次重建链（同步消息，返回时 WorkerW 链已建立）
        Win32Helper.SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero,
            Win32Helper.SMTO_NORMAL, 3000, out _);

        var sw = Stopwatch.StartNew();
        IntPtr prev = IntPtr.Zero;
        var stableCount = 0;

        while (sw.ElapsedMilliseconds < timeoutMs)
        {
            var current = DynamicWallpaperWindow.FindDesktopWorkerW(nudge: false);
            if (current != IntPtr.Zero && current != oldWorkerW)
            {
                // 壁纸变更可能引发多波延迟重建，句柄变化后再采一样确认稳定，
                // 避免窗口建在随即又被销毁的 WorkerW 上
                await Task.Delay(200);
                if (DynamicWallpaperWindow.FindDesktopWorkerW(nudge: false) == current)
                    return true;
                continue;
            }

            // 句柄值被复用的罕见情形：连续多次采样不变视为稳定
            if (current == prev) stableCount++;
            else { prev = current; stableCount = 1; }
            if (current != IntPtr.Zero && stableCount >= 4)
                return true;

            await Task.Delay(200);
        }

        Debug.WriteLine("[Wallpaper] WorkerW rebuild wait timed out, continuing best-effort");
        return false;
    }

    private static void NudgeWorkerW()
    {
        var progman = Win32Helper.FindWindow("Progman", null);
        if (progman == IntPtr.Zero) return;
        Win32Helper.SendMessageTimeout(progman, 0x052C, IntPtr.Zero, IntPtr.Zero,
            Win32Helper.SMTO_NORMAL, 3000, out _);
    }

    #endregion

    #region UI 入口

    /// <summary>
    /// 为指定显示器应用壁纸（独立模式）
    /// </summary>
    public async Task ApplyToMonitorAsync(WallpaperItem wallpaper, string monitorDevicePath)
    {
        var settings = Settings;
        if (settings == null) return;

        var monitors = MonitorLayoutService.Instance.GetMonitors();
        if (monitors.All(m => m.DevicePath != monitorDevicePath)) return;

        Directory.CreateDirectory(WallpaperDir);

        if (wallpaper.Type == "dynamic")
        {
            if (string.IsNullOrEmpty(wallpaper.PreviewVideoUrl)) return;
            var video = await EnsureVideoDownloadedAsync(wallpaper);
            var fallback = await EnsureImageDownloadedAsync(wallpaper);
            settings.Monitors[monitorDevicePath] = new MonitorWallpaperEntry
            {
                MonitorId = monitorDevicePath,
                WallpaperId = wallpaper.Id,
                Type = "dynamic",
                LocalFilePath = video,
                PreviewPath = fallback
            };
        }
        else
        {
            var image = await EnsureImageDownloadedAsync(wallpaper);
            settings.Monitors[monitorDevicePath] = new MonitorWallpaperEntry
            {
                MonitorId = monitorDevicePath,
                WallpaperId = wallpaper.Id,
                Type = "static",
                LocalFilePath = image,
                PreviewPath = ""
            };
        }

        settings.Mode = "independent";
        App?.SaveSettings();
        await ApplyLayoutAsync();
    }

    /// <summary>
    /// 应用跨屏拼接壁纸（所有显示器）
    /// </summary>
    public async Task ApplySpanAsync(WallpaperItem wallpaper)
    {
        var settings = Settings;
        if (settings == null) return;

        Directory.CreateDirectory(WallpaperDir);

        if (wallpaper.Type == "dynamic")
        {
            if (string.IsNullOrEmpty(wallpaper.PreviewVideoUrl)) return;
            var video = await EnsureVideoDownloadedAsync(wallpaper);
            var fallback = await EnsureImageDownloadedAsync(wallpaper);
            settings.Span = new SpanWallpaperEntry
            {
                WallpaperId = wallpaper.Id,
                Type = "dynamic",
                LocalFilePath = video,
                PreviewPath = fallback
            };
        }
        else
        {
            var image = await EnsureImageDownloadedAsync(wallpaper);
            settings.Span = new SpanWallpaperEntry
            {
                WallpaperId = wallpaper.Id,
                Type = "static",
                LocalFilePath = image,
                PreviewPath = image
            };
        }

        settings.Mode = "span";
        App?.SaveSettings();
        await ApplyLayoutAsync();
    }

    /// <summary>
    /// 旧单屏配置迁移到多显示器模型（幂等可重入）。
    /// 旧配置 = 所有在场显示器同图，保持旧可见行为。
    /// </summary>
    public static void MigrateLegacySettings(WallpaperSettings settings, List<MonitorInfo> monitors)
    {
        if (settings.Monitors.Count > 0) return;
        if (settings.CurrentType != "static" && settings.CurrentType != "dynamic") return;

        foreach (var monitor in monitors)
        {
            settings.Monitors[monitor.DevicePath] = new MonitorWallpaperEntry
            {
                MonitorId = monitor.DevicePath,
                WallpaperId = settings.CurrentWallpaperId,
                Type = settings.CurrentType,
                LocalFilePath = settings.LocalFilePath,
                PreviewPath = "" // BuildSnapshot 按需补拉
            };
        }
    }

    #endregion

    #region 下载与缓存

    private async Task<string> EnsureImageDownloadedAsync(WallpaperItem wallpaper)
    {
        var url = await GetOriginalUrlAsync(wallpaper);
        var path = BuildImagePath(url, wallpaper.Id);
        if (!File.Exists(path))
            await File.WriteAllBytesAsync(path, await DownloadWithRetryAsync(url));
        return path;
    }

    private async Task<string> EnsureVideoDownloadedAsync(WallpaperItem wallpaper)
    {
        var path = Path.Combine(WallpaperDir, $"{wallpaper.Id}.mp4");
        if (!File.Exists(path))
            await File.WriteAllBytesAsync(path, await DownloadWithRetryAsync(wallpaper.PreviewVideoUrl!));
        return path;
    }

    /// <summary>
    /// 旧回退图是站点带水印的 _preview 预览，视为过期需重新取原图
    /// </summary>
    private static bool IsStalePreviewFallback(string? path, string wallpaperId) =>
        string.IsNullOrEmpty(path)
        || !File.Exists(path)
        || Path.GetFileName(path).StartsWith($"{wallpaperId}_preview.", StringComparison.Ordinal);

    /// <summary>
    /// 按壁纸 Id 取原图作为动态壁纸回退静态图（与静态壁纸同源，无水印）。
    /// 离线失败返回 null，不阻塞。
    /// </summary>
    private static async Task<string?> EnsureFallbackImageAsync(string wallpaperId)
    {
        try
        {
            var detail = await McBlockApiClient.Instance.GetWallpaperDetailAsync(wallpaperId);
            var url = detail.OriginalUrl ?? detail.PreviewUrl;
            if (string.IsNullOrEmpty(url)) return null;

            var path = BuildImagePath(url, wallpaperId);
            if (!File.Exists(path))
                await File.WriteAllBytesAsync(path, await DownloadWithRetryAsync(url));
            return path;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"[Wallpaper] Fallback backfill failed for {wallpaperId}: {ex.Message}");
            return null;
        }
    }

    private static async Task<byte[]> DownloadWithRetryAsync(string url, int maxRetries = 3)
    {
        for (var i = 0; i < maxRetries; i++)
        {
            try
            {
                return await _http.GetByteArrayAsync(url);
            }
            catch (HttpRequestException) when (i < maxRetries - 1) { }
        }
        return await _http.GetByteArrayAsync(url);
    }

    public async Task<string> GetOriginalUrlAsync(WallpaperItem wallpaper)
    {
        try
        {
            var detail = await McBlockApiClient.Instance.GetWallpaperDetailAsync(wallpaper.Id);
            return detail.OriginalUrl ?? wallpaper.PreviewUrl;
        }
        catch
        {
            return wallpaper.PreviewUrl;
        }
    }

    public async Task<byte[]> DownloadBytesAsync(string url)
    {
        return await DownloadWithRetryAsync(url);
    }

    public async Task DownloadFileAsync(string url, string localPath, Action<string>? onError = null)
    {
        try
        {
            if (!Directory.Exists(WallpaperDir))
                Directory.CreateDirectory(WallpaperDir);

            if (!File.Exists(localPath))
            {
                var bytes = await DownloadWithRetryAsync(url);
                await File.WriteAllBytesAsync(localPath, bytes);
            }
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex.Message);
        }
    }

    public string GetWallpaperFilePath(WallpaperItem wallpaper)
    {
        return BuildImagePath(wallpaper.PreviewUrl, wallpaper.Id);
    }

    public static string BuildImagePath(string url, string wallpaperId)
    {
        var ext = url.EndsWith(".jpg") || url.EndsWith(".jpeg") ? "jpg" : "webp";
        return Path.Combine(WallpaperDir, $"{wallpaperId}.{ext}");
    }

    #endregion
}
