# CraftSharp

Minecraft HUD 模拟器 - 在 Windows 桌面显示 Minecraft 风格的状态栏、物品栏、准星等界面元素。

## 功能特性

- **状态栏 (StatusBar)**：快捷栏格子、经验条、生命值、饥饿值、护甲值、氧气值、伤害吸收值
- **物品栏 (Inventory)**：按 E 键打开背包界面，支持自定义样式和格子数量
- **准星 (Crosshair)**：屏幕中心准星 + 攻击指示器
- **BOSS 血条 (BossBar)**：可自定义名称、图标、分段的 BOSS 血条
- **皮肤预览**：3D 玩家模型渲染，支持上传自定义皮肤
- **系统托盘**：右键菜单控制窗口显示/隐藏
- **多语言支持**：简体中文 / English
- **数据映射**：HUD 数值可映射到硬件监控数据（电池电量、CPU 使用率等）

## 技术栈

- .NET 8 + WPF
- CommunityToolkit.Mvvm (MVVM 框架)
- WPF-UI (Fluent Design 风格)
- HelixToolkit.Wpf.SharpDX (3D 渲染)
- LibreHardwareMonitorLib (硬件监控)
- Magick.NET (图像处理)
- Hardcodet.NotifyIcon.Wpf (系统托盘)

## 项目结构

\\\
CraftSharp/
├── App.xaml.cs           # 应用入口，初始化所有窗口和服务
├── models/               # 数据模型
│   ├── AppSettings.cs    # 配置模型（分层结构）
│   ├── SlotItem.cs       # 格子项数据
│   └── SkinItem.cs       # 皮肤项数据
├── interfaces/           # 接口定义
│   └── ISlot.cs          # 格子接口（快捷栏/背包通用）
├── services/             # 服务层（单例）
│   ├── hud/                    # HUD 元素管理
│   │   ├── StatusBarService.cs
│   │   ├── BossBarService.cs
│   │   ├── CrosshairService.cs
│   │   └── SkinPreviewService.cs
│   ├── slot/                   # 格子系统
│   │   ├── SlotDataService.cs
│   │   ├── SlotDragService.cs
│   │   ├── SlotContextMenuService.cs
│   │   ├── SlotClipboardService.cs
│   │   ├── SlotFileValidator.cs
│   │   └── SlotIconService.cs
│   ├── core/                   # 核心/通用服务
│   │   ├── LocalizationService.cs
│   │   ├── ThemeService.cs
│   │   ├── FontService.cs
│   │   ├── ScaleService.cs
│   │   └── DataMappingService.cs
│   └── resource/               # 资源处理
│   │   ├── IconService.cs
│   │   ├── IconExtractor.cs
│   │   └── ImageService.cs
├── windows/              # 界面窗口
│   ├── statusbar/        # 状态栏窗口
│   ├── inventory/        # 物品栏窗口
│   ├── crosshair/        # 准星窗口
│   ├── bossbar/          # BOSS 血条窗口
│   ├── settings/         # 设置窗口 + 各配置面板
│   └── dialogs/          # 对话框（颜色选择、图标选择等）
├── helpers/              # 工具类
├── assets/               # 资源文件
│   ├── minecraft/        # Minecraft 纹理资源
│   ├── resources/        # 语言资源文件
│   └── fonts/            # 字体文件
└── config/               # 运行时配置（settings.json）
\\\

## 核心模块

### 1. 状态栏 (StatusBar)

文件：[windows/statusbar/StatusBarWindow.xaml.cs](/windows/statusbar/StatusBarWindow.xaml.cs)

功能：
- 快捷栏 9 格 + 左/右副手槽（可配置）
- HUD 元素：经验条、生命值、饥饿值、护甲值、氧气值、伤害吸收值
- 每个元素支持：显示/隐藏、自定义数值、数据映射
- 状态栏窗口可拖拽、锁定位置、记住位置

关键服务：[services/hud/StatusBarService.cs](/services/StatusBarService.cs)

### 2. 物品栏 (Inventory)

文件：[windows/inventory/InventoryWindow.xaml.cs](/windows/inventory/InventoryWindow.xaml.cs)

功能：
- 按 E 键全局快捷键打开/关闭
- 支持多种样式（inventory.png 等），样式文件存放在 assets 目录
- 格子数量由样式配置文件（data/gui/*.json）决定
- 支持拖拽文件/快捷方式到格子、右键菜单操作
- 可选灰色蒙版（打开时全屏遮罩）

关键配置：
- \InventorySettings.SharedData\：共享数据开关（所有样式共用格子数据 vs 每个样式独立）
- \InventorySettings.ClickMode\：单击/双击打开文件

### 3. 格子系统 (Slot)

接口：[interfaces/ISlot.cs](/interfaces/ISlot.cs)

数据模型：[models/SlotItem.cs](/models/SlotItem.cs)

格子统一接口，支持快捷栏和背包格子：
- \ISlot.SlotId\：格子唯一标识（如 \hotbar_0\, \inventory_10\）
- \ISlot.SetItem()/GetItem()\：设置/获取格子项
- \ISlot.Clear()\：清空格子

关键服务：
- [services/slot/SlotDataService.cs](/services/SlotDataService.cs)：格子数据存储（持久化到 settings.json）
- [services/slot/SlotDragService.cs](/services/SlotDragService.cs)：格子拖拽逻辑
- [services/slot/SlotContextMenuService.cs](/services/SlotContextMenuService.cs)：格子右键菜单

### 4. 配置系统 (AppSettings)

文件：[models/AppSettings.cs](/models/AppSettings.cs)

分层结构：
\\\
AppSettings
├── SystemSettings        # 系统设置（语言、自启动等）
├── AppearanceSettings    # 外观设置（主题、字体、图标）
├── InventorySettings     # 物品栏设置
├── StatusBarSettings     # 状态栏设置
├── HotbarSettings        # 快捷栏设置
├── HudElements           # HUD 元素配置列表
├── BossBars              # BOSS 血条配置列表
├── PlayerSettings        # 玩家皮肤设置
├── HotkeySettings        # 快捷键设置
├── Slots                 # 格子数据（字典）
├── StyleSlots            # 样式独立格子数据
└── Version               # 应用版本
\\\

配置持久化：[App.xaml.cs](/App.xaml.cs) 的 \SaveSettings()/LoadSettings()\ 方法，存储为 \config/settings.json\。

### 5. 数据映射 (DataMapping)

服务：[services/core/DataMappingService.cs](/services/DataMappingService.cs)

将 HUD 数值映射到硬件监控数据：
- \BatteryLevel\：电池电量百分比
- \CpuUsage\：CPU 使用率
- \MemoryUsage\：内存使用率
- \GpuUsage\：GPU 使用率

每个 HUD 元素可通过 \HudElementSettings.DataMappingType\ 配置映射类型。

## 架构设计

### 服务层

采用单例模式，所有服务在 [App.xaml.cs](/App.xaml.cs) 的 \OnStartup\ 中初始化：

\\\csharp
StatusBarService.Instance.Initialize(statusBarWindow, appSettings);
LocalizationService.Instance.Initialize(appSettings.System.Language);
ThemeService.Instance.Initialize(appSettings.Appearance.Theme);
\\\

### 窗口管理

- 主窗口：[SettingsWindow](/windows/settings/SettingsWindow.xaml.cs)（设置面板，关闭时最小化到托盘）
- HUD 窗口：StatusBarWindow、CrosshairWindow、BossBarWindow（始终显示或按配置隐藏）
- 工具窗口：InventoryWindow（按 E 键切换显示）

### MVVM 模式

使用 CommunityToolkit.Mvvm 的 \ObservableObject\ 和 \ObservableProperty\：

\\\csharp
public partial class BossBarSettings : ObservableObject
{
    [ObservableProperty]
    private string _name = "";
}
\\\

## 开发指南

### 运行项目

\\\ash
dotnet run
\\\

### 发布项目

\\\ash
dotnet publish -c Release -r win-x64 --self-contained
\\\

使用 Inno Setup 打包安装程序：[installer.iss](/installer.iss)

### 添加新 HUD 元素

1. 在 [models/AppSettings.cs](/models/AppSettings.cs) 无需添加新类（使用 \HudElementSettings\ 通用配置）
2. 在 [App.xaml.cs](/App.xaml.cs) 的 \EnsureAllHudElementsExist()\ 添加默认配置
3. 在对应窗口（如 StatusBarWindow）添加渲染逻辑和可见性控制方法
4. 在对应服务（如 StatusBarService）添加 \SetXxxVisible()\ 方法

### 添加新窗口

1. 在 \windows/<模块名>/\ 创建 XAML + XAML.cs
2. 在 [App.xaml.cs](/App.xaml.cs) 的 \OnStartup\ 中创建并初始化
3. 如需持久化配置，在 [models/AppSettings.cs](/models/AppSettings.cs) 添加对应 Settings 类

### 添加新服务

1. 在 \services/\ 创建服务类（单例模式）
2. 在 [App.xaml.cs](/App.xaml.cs) 的 \OnStartup\ 中调用 \Initialize()\

## 资源文件

### Minecraft 纹理

存放路径：\ssets/minecraft/textures/\

结构：
\\\
textures/
├── gui/sprites/hud/    # HUD 元素纹理（ hearts, air, armor, food 等）
├── gui/container/      # 容器界面纹理（ inventory.png 等）
├── entity/player/      # 玩家皮肤纹理
├── block/              # 方块纹理（用于图标）
├── item/               # 物品纹理（用于图标）
\\\

### 语言资源

存放路径：\ssets/resources/Strings.<语言代码>.xaml\

支持语言：
- \Strings.zh-CN.xaml\ - 简体中文
- \Strings.en-US.xaml\ - English

### 字体资源

存放路径：\ssets/fonts/\

可用字体标识符（在 FontService 中配置）：
- \YaHei\ - Microsoft YaHei
- \Pixel\ - Minecraft Pixel Font
- \UniFont\ - Unicode Font

## 配置文件

### settings.json

路径：\config/settings.json\（运行时生成）

包含所有应用配置、格子数据、HUD 元素配置等。

### 样式配置

路径：\data/gui/<样式名>.json\

定义物品栏样式的格子数量、位置、大小等。

## 许可证

MIT License
