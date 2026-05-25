# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

CraftSharp 是一个 Minecraft HUD 模拟器，在 Windows 桌面上显示 Minecraft 风格的状态栏、物品栏、准星、BOSS 血条和皮肤预览。基于 .NET 8 + WPF，仅支持 Windows。

## Build & Run

```bash
dotnet run                                          # 运行项目
dotnet publish -c Release -r win-x64 --self-contained  # 发布独立可执行文件
```

无测试基础设施，无 CI/CD 配置。安装包通过 Inno Setup (`installer.iss`) 打包。

## Architecture

**Service-based Singleton + MVVM**

- 所有服务为手动单例（`Instance` 属性 + `Initialize()` 方法），在 `App.OnStartup()` 中统一创建和初始化
- 无依赖注入框架，服务之间直接持有窗口引用
- MVVM 使用 CommunityToolkit.Mvvm 的 `ObservableObject` 和 `[ObservableProperty]` 源生成器
- 大型窗口使用 C# partial class 拆分（如 `StatusBarWindow.Hearts.cs`、`StatusBarWindow.Hotbar.cs`）

**核心层：**
- `App.xaml.cs` — 应用入口（~600 行），负责所有初始化：颜色画笔、配置加载、服务创建、窗口创建、系统托盘、全局热键
- `models/AppSettings.cs` — 分层配置模型（~560 行，15+ 嵌套类），序列化为 `config/settings.json`
- `interfaces/ISlot.cs` — 格子系统统一接口，快捷栏和物品栏共用

**服务层 (`services/`)：**
- `hud/` — HUD 元素管理（StatusBar/Crosshair/BossBar/SkinPreview）
- `slot/` — 格子系统（拖拽/剪贴板/右键菜单/图标/文件校验/数据持久化）
- `core/` — 基础服务（国际化/主题/字体/缩放/数据映射/全局热键）
- `resource/` — 图标提取（Win32 P/Invoke）和图像处理（Magick.NET）
- `update/` — GitHub Releases 更新检查
- `wallpaper/` — 壁纸下载（Refit HTTP 客户端）和桌面壁纸设置

**窗口层 (`windows/`)：**
- `statusbar/` — 主 HUD 窗口，partial class 拆分为 Hearts/Food/Armor/Air/Absorbing/ExpBar/Hotbar
- `inventory/` — 物品栏窗口 + 灰色蒙版 + 文件提示 + 3D 皮肤预览控件
- `settings/` — 设置窗口 + 各配置面板（general/appearance/hud/inventory/hotkey/skin/wallpaper/about）
- `dialogs/` — 13 个通用对话框（颜色选择、图标选择、快捷键冲突等）

**Win32 互操作 (`helpers/`)：**
- `Win32Helper.cs` — P/Invoke（电池电量、全局热键、桌面图标切换等）
- `NativeFileDropHelper.cs` — COM 文件拖放处理
- `DesktopWindowHelper.cs` — 桌面覆盖窗口

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

## Resources

- Minecraft 纹理：`assets/minecraft/textures/`（gui/sprites/hud、gui/container、entity/player、block、item）
- 语言资源：`assets/resources/Strings.zh-CN.xaml`、`Strings.en-US.xaml`
- 字体：`assets/fonts/`（unifont-16.0.04.ttf、zpix.ttf），FontService 中以 `YaHei`/`Pixel`/`UniFont` 标识
- 配置持久化：`config/settings.json`（运行时生成）
- 物品栏样式配置：`data/gui/<样式名>.json`

## Dependencies

- WPF-UI 3.0.5 — Fluent Design 控件和主题
- HelixToolkit.Wpf.SharpDX 3.1.2 — 3D 玩家模型渲染
- LibreHardwareMonitorLib 0.9.4 — 硬件监控数据
- Magick.NET-Q8-AnyCPU — 图像处理
- Refit 10.1.6 — REST API 客户端（壁纸服务）
- Hardcodet.NotifyIcon.Wpf — 系统托盘图标
