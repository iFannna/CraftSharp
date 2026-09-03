# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CraftSharp 是一个 Minecraft HUD 模拟器，在 Windows 桌面上显示 Minecraft 风格的状态栏、物品栏、准星、BOSS 血条和皮肤预览，并提供静态/动态桌面壁纸（多显示器每屏独立设置与跨屏拼接）。基于 .NET 8 + WPF，仅支持 Windows。

## Build & Run

```bash
dotnet run                                          # 运行项目
dotnet publish -c Release -r win-x64 --self-contained  # 发布独立可执行文件
```

无测试基础设施，无 CI/CD 配置。安装包双工具链：WiX（`build/wix/build-wix.cmd`，中英双语言 MSI）与 Inno Setup（`build/innosetup/installer.iss`，setup.exe），产物输出至 `installer/`。

## Architecture

**Service-based Singleton + MVVM**

- 所有服务为手动单例（`Instance` 属性 + `Initialize()` 方法），在 `App.OnStartup()` 中统一创建和初始化
- 无依赖注入框架，服务之间直接持有窗口引用
- MVVM 使用 CommunityToolkit.Mvvm 的 `ObservableObject` 和 `[ObservableProperty]` 源生成器
- 大型窗口使用 C# partial class 拆分（如 `StatusBarWindow.Hearts.cs`、`StatusBarWindow.Hotbar.cs`）

**核心层：**
- `App.xaml.cs` — 应用入口（~975 行），负责所有初始化：颜色画笔、配置加载、服务创建、窗口创建、系统托盘、全局热键、单实例保护、窗口级类处理器（缩放填充色/图标补投/键盘无障碍）
- `models/AppSettings.cs` — 分层配置模型（~570 行，10+ 嵌套类），序列化为 `config/settings.json`；壁纸配置在 `models/WallpaperModels.cs`
- `interfaces/ISlot.cs` — 格子系统统一接口，快捷栏和物品栏共用

**服务层 (`services/`)：**
- `hud/` — HUD 元素管理（StatusBar/Crosshair/BossBar/SkinPreview）
- `slot/` — 格子系统（拖拽/剪贴板/右键菜单/图标/文件校验/数据持久化）
- `core/` — 基础服务（国际化/主题/字体/缩放/数据映射/全局热键）
- `resource/` — 图标提取（Win32 P/Invoke）、应用图标自定义（按任务栏槽位渲染 HICON）、图像处理（Magick.NET）
- `update/` — GitHub Releases 更新检查
- `wallpaper/` — 壁纸下载（Refit HTTP 客户端）、静态壁纸应用（IDesktopWallpaper，位置直写注册表）、动态壁纸（进程内 libmpv + 遮挡/显示变更看门狗自愈）、多屏布局（MonitorLayoutService/DisplayInfoService）、跨屏拼接（SpanCropService）

**窗口层 (`windows/`)：**
- `statusbar/` — 主 HUD 窗口，partial class 拆分为 Hearts/Food/Armor/Air/Absorbing/ExpBar/Hotbar
- `inventory/` — 物品栏窗口 + 灰色蒙版 + 文件提示 + 3D 皮肤预览控件
- `crosshair/` — 准星窗口
- `bossbar/` — BOSS 血条窗口 + 编辑窗口
- `skin/` — 皮肤上传/重命名/删除确认对话框
- `skinpreview/` — 皮肤预览窗口
- `settings/` — 设置窗口 + 各配置面板（general/appearance/hud/inventory/hotkey/skin/wallpaper/about）
- `wallpaper/` — 动态壁纸窗口（纯 Win32 CreateWindowEx 挂 mpv --wid，非 WPF Window）
- `dialogs/` — 13 个通用对话框（颜色选择、图标选择、快捷键冲突、显示器设置等）

**Win32 互操作与其余助手 (`helpers/`)：**
- `Win32Helper.cs` — P/Invoke（电池电量、全局热键、桌面图标切换等）
- `NativeFileDropHelper.cs` — COM 文件拖放处理
- `DesktopWindowHelper.cs` — 桌面覆盖窗口
- `DpiScope.cs` — 线程级 PerMonitorV2 上下文（显示器枚举/Win32 矩形读取必须包，见下方窗口定位规则）
- `MpvNative.cs` — 进程内 libmpv 绑定（动态壁纸）
- `KeyboardAccessibility.cs` / `TitleBarAutomation.cs` — 键盘无障碍（焦点视觉按输入来源门控、标题栏按钮可聚焦可朗读）
- `DirectionalFocusHelper.cs` — ItemsControl 方向键导航（按可视位置移焦点，图标网格等用）
- `ModalDialogHelper.cs` — 模态弹窗期间拦截对被禁用属主窗口的点击
- `WindowFillBrushHelper.cs` — 窗口缩放填充色钉死为背景色，消除暗色主题放大闪白
- `SharedEffectsManager.cs` — 全应用共享单一 D3D11 EffectsManager（每视口各建一份会各持独立 D3D11 设备且 COM 资源无终结器，控件重建即永久泄漏）
- `AssetPaths.cs` — HUD 图标纹理路径映射；`ColorPickerHelper.cs` — 颜色选择器辅助

**窗口定位规则（多显示器/DPI，实测总结）：**
- 进程为系统级 DPI 感知（无 app.manifest）。WPF `Left/Top`（DIP）→ HWND 设备坐标 = DIP × 系统缩放（= 主屏 DPI/96，与窗口所在屏无关）
- 摆在缩放 ≠ 系统缩放的屏上时，DWM 以**显示器原点**锚定做视觉缩放（×该屏DPI/系统DPI）。物理坐标直写、`PointToScreen` 校正、Show 后迭代逼近全部不可靠：PointToScreen 只报 WPF 记账坐标对此全盲，迭代会因跨屏重锚定发散，窗口关闭后的延迟回调会崩（无 PresentationSource）
- 主屏或缩放=系统缩放的屏：`SystemParameters.WorkArea` DIP 直接四则运算赋 `Left/Top` 即可（`StatusBarWindow.PositionWindowToBottom` 即此惯例，勿做任何比例换算）
- 跨非系统缩放屏：用 `DisplayInfoService.GetAppSpaceBounds` 矩形反解。矩形契约（实测）：原点是物理值，尺寸 = 物理 × (sysScale/monScale)。像素级公式（`DisplaySettingsWindow.Identify_Click` 现行实现）：`Left = (monRect.Left + (物理目标x − monRect.Left) × sysScale/monScale) / sysScale`，Top 同理锚定 monRect.Top
- `DisplayInfoService.GetDisplays` 及一切 Win32 窗口矩形读取/比较（GetWindowRect 等）必须包 `DpiScope.EnterPerMonitorV2()`，否则非系统缩放屏的 DPI/矩形被虚拟化错报（如 150% 报成 100%）
- 纯 Win32 窗口（DynamicWallpaperWindow）在 PMv2 上下文内物理坐标直接摆，无上述换算
- WPF-UI 隐式 Window 样式带 `MinWidth=460/MinHeight=320`：纯 `new Window`（非 FluentWindow）要小尺寸必须本地 `MinWidth=0/MinHeight=0`，否则经属性 coercion 连 `Width` 读回值都是钳后的 460×320

## Key Patterns

**添加新 HUD 元素：**
1. `App.xaml.cs` → `EnsureAllHudElementsExist()` 添加默认配置
2. 对应窗口添加渲染逻辑和可见性控制
3. 对应服务添加 `SetXxxVisible()` 方法

**添加新服务：**
1. `services/` 下创建单例类
2. `App.xaml.cs` → `OnStartup()` 中调用 `Initialize()`

**添加新设置项：**
1. `models/AppSettings.cs` 添加 Settings 子类
2. 设置面板 UI 中绑定

**添加可交互控件（无障碍惯例）：**
1. 用真控件承载（Button/ToggleButton 等），不做伪按钮
2. 键盘焦点视觉用 `KeyboardAccessibility.ShowFocusVisualProperty` 模板触发器（仅键盘输入获得焦点时显示），不新增原版没有的 hover

## Resources

- Minecraft 纹理：`assets/minecraft/textures/`（gui/sprites/hud、gui/container、entity/player、block、item）
- 语言资源：`assets/resources/Strings.zh-CN.xaml`、`Strings.en-US.xaml`
- 字体：`assets/fonts/`（unifont-16.0.04.ttf、zpix.ttf），FontService 中以 `YaHei`/`Pixel`/`UniFont` 标识
- 配置持久化：`config/settings.json`（运行时生成，含格子数据）
- 物品栏样式：`assets/minecraft/textures/gui/container/*.png`

## Dependencies

- CommunityToolkit.Mvvm 8.3.2 — MVVM 基础（ObservableProperty 源生成器）
- WPF-UI 3.0.5 — Fluent Design 控件和主题（注意：其隐式 Window 样式钳制纯 Window 最小尺寸，见窗口定位规则）
- HelixToolkit.Wpf.SharpDX 3.1.2 — 3D 玩家模型渲染
- LibreHardwareMonitorLib 0.9.4 — 硬件监控数据
- Magick.NET-Q8-AnyCPU — 图像处理
- Refit 10.1.6 — REST API 客户端（壁纸服务）
- Hardcodet.NotifyIcon.Wpf — 系统托盘图标
- libmpv-2.dll — 动态壁纸本地播放（native DLL，随应用分发，非 NuGet 包）
