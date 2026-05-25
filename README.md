# CraftSharp

Minecraft 风格的桌面管理及壁纸引擎应用，将 Minecraft 的经典 HUD 界面融入 Windows 桌面体验。

## 功能特性

- **状态栏**：快捷栏、经验条、生命值、饥饿值、护甲值、氧气值、伤害吸收值
- **物品栏**：按 E 键打开背包界面，支持自定义样式和格子数量
- **准星**：屏幕中心准星 + 攻击指示器
- **BOSS 血条**：可自定义名称、图标、分段的 BOSS 血条
- **皮肤预览**：3D 玩家模型渲染，支持上传自定义皮肤
- **壁纸设置**：在线浏览和设置 Minecraft 风格桌面壁纸
- **数据映射**：HUD 数值可映射到硬件监控数据（电池电量、CPU、内存、GPU 使用率）
- **系统托盘**：最小化到托盘，右键菜单控制窗口显示
- **多语言**：简体中文 / English
- **Fluent Design**：基于 WPF-UI 的现代化设置界面

## 技术栈

- .NET 8 + WPF
- CommunityToolkit.Mvvm
- WPF-UI 3.0
- HelixToolkit.Wpf.SharpDX（3D 渲染）
- LibreHardwareMonitorLib（硬件监控）
- Magick.NET（图像处理）

## 运行环境

- Windows 10/11
- 安装程序为自包含发布，无需安装 .NET Runtime

## 许可证

MIT License
