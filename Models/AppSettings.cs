using System.Collections.ObjectModel;

namespace CraftSharp.Models
{
    /// <summary>
    /// 应用设置配置模型
    /// </summary>
    public class AppSettings
    {
        public AppSettings()
        {
            // 不在构造函数中初始化默认元素，避免 JSON 反序列化时产生重复
            // 默认元素由 App.xaml.cs 的 EnsureAllHudElementsExist 方法添加
            HudElements = new ObservableCollection<HudElementSettings>();
        }

        // ==================== 系统设置 ====================
        /// <summary>
        /// 界面语言
        /// </summary>
        public string Language { get; set; } = "简体中文";

        /// <summary>
        /// 开机自启动
        /// </summary>
        public bool AutoStart { get; set; } = true;

        /// <summary>
        /// 记住设置窗口位置
        /// </summary>
        public bool SettingsWindowRememberPosition { get; set; } = true;

        /// <summary>
        /// 记住设置窗口大小
        /// </summary>
        public bool SettingsWindowRememberSize { get; set; } = true;

        /// <summary>
        /// 设置窗口 X 坐标
        /// </summary>
        public double SettingsWindowPositionX { get; set; } = 0;

        /// <summary>
        /// 设置窗口 Y 坐标
        /// </summary>
        public double SettingsWindowPositionY { get; set; } = 0;

        /// <summary>
        /// 设置窗口宽度
        /// </summary>
        public double SettingsWindowWidth { get; set; } = 1080;

        /// <summary>
        /// 设置窗口高度
        /// </summary>
        public double SettingsWindowHeight { get; set; } = 720;

        // ==================== 外观设置 ====================
        /// <summary>
        /// 主题风格
        /// </summary>
        public string Theme { get; set; } = "跟随系统";

        /// <summary>
        /// 字体
        /// </summary>
        public string Font { get; set; } = "微软雅黑";

        /// <summary>
        /// 字体大小
        /// </summary>
        public int FontSize { get; set; } = 14;

        /// <summary>
        /// 图标样式
        /// </summary>
        public string IconStyle { get; set; } = "默认";

        /// <summary>
        /// 应用图标路径（相对于Assets目录）
        /// </summary>
        public string AppIconPath { get; set; } = "minecraft/textures/block/block/debug.png";

        // ==================== HUD 设置 ====================
        /// <summary>
        /// 显示状态栏
        /// </summary>
        public bool StatusBarVisible { get; set; } = true;

        /// <summary>
        /// 锁定状态栏位置
        /// </summary>
        public bool StatusBarLocked { get; set; } = false;

        /// <summary>
        /// 记住状态栏位置
        /// </summary>
        public bool StatusBarRememberPosition { get; set; } = false;

        /// <summary>
        /// 状态栏窗口 X 坐标
        /// </summary>
        public double StatusBarPositionX { get; set; } = 0;

        /// <summary>
        /// 状态栏窗口 Y 坐标
        /// </summary>
        public double StatusBarPositionY { get; set; } = 0;

        /// <summary>
        /// 左副手槽
        /// </summary>
        public bool HotbarLeftOffhand { get; set; } = false;

        /// <summary>
        /// 右副手槽
        /// </summary>
        public bool HotbarRightOffhand { get; set; } = false;

        /// <summary>
        /// 显示快捷栏（包括格子、副手槽）
        /// </summary>
        public bool HotbarVisible { get; set; } = true;

        /// <summary>
        /// HUD 元素配置列表
        /// </summary>
        public ObservableCollection<HudElementSettings> HudElements { get; set; }

        // ==================== 快捷键设置 ====================
        /// <summary>
        /// 打开背包快捷键
        /// </summary>
        public string InventoryHotkey { get; set; } = "E";

        /// <summary>
        /// 打开设置快捷键
        /// </summary>
        public string SettingsHotkey { get; set; } = "Ctrl+S";

        /// <summary>
        /// 显示/隐藏快捷栏快捷键
        /// </summary>
        public string HotbarToggleHotkey { get; set; } = "";

        // ==================== 关于 ====================
        /// <summary>
        /// 应用版本
        /// </summary>
        public string Version { get; set; } = "1.0.0";
    }

    /// <summary>
    /// HUD 单个元素配置
    /// </summary>
    public class HudElementSettings
    {
        /// <summary>
        /// 元素标识
        /// </summary>
        public string Id { get; set; } = "";
        

        /// <summary>
        /// 显示元素
        /// </summary>
        public bool IsVisible { get; set; } = true;

        /// <summary>
        /// 图标样式（如"full"、"hardcore_full"、"food_full"、"food_full_hunger"等）
        /// </summary>
        public string IconStyle { get; set; } = "";


        /// <summary>
        /// 锁定位置（仅快捷栏）
        /// </summary>
        public bool IsLocked { get; set; } = false;

        /// <summary>
        /// 左副手槽（仅快捷栏）
        /// </summary>
        public bool LeftOffhand { get; set; } = false;

        /// <summary>
        /// 右副手槽（仅快捷栏）
        /// </summary>
        public bool RightOffhand { get; set; } = false;

        /// <summary>
        /// 恢复动画（仅生命值）
        /// </summary>
        public bool RegenAnimation { get; set; } = false;

        /// <summary>
        /// 数据映射开启
        /// </summary>
        public bool DataMappingEnabled { get; set; } = false;

        /// <summary>
        /// 数据映射类型
        /// </summary>
        public string DataMappingType { get; set; } = "电池电量";

        /// <summary>
        /// 自定义数值开启
        /// </summary>
        public bool CustomValueEnabled { get; set; } = false;

        /// <summary>
        /// 自定义当前值
        /// </summary>
        public int CustomCurrentValue { get; set; } = 20;

        /// <summary>
        /// 自定义最大值（上限20，代表10个完整图标）
        /// </summary>
        public int CustomMaxValue { get; set; } = 20;

        /// <summary>
        /// 自定义饱和度（仅饥饿值，默认0，最大20）
        /// </summary>
        public int CustomSaturationValue { get; set; } = 0;

        /// <summary>
        /// 窗口置顶（仅准星）
        /// </summary>
        public bool TopMost { get; set; } = false;
    }

    /// <summary>
    /// BOSS 血条配置
    /// </summary>
    public class BossBarSettings
    {
        /// <summary>
        /// 唯一标识
        /// </summary>
        public string Id { get; set; } = "";

        /// <summary>
        /// 名称
        /// </summary>
        public string Name { get; set; } = "新BOSS";

        /// <summary>
        /// 图标类型
        /// </summary>
        public string IconType { get; set; } = "dragon";

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled { get; set; } = true;

        /// <summary>
        /// 数据映射开启
        /// </summary>
        public bool DataMappingEnabled { get; set; } = true;

        /// <summary>
        /// 数据映射类型
        /// </summary>
        public string DataMappingType { get; set; } = "电池电量";

        /// <summary>
        /// 自定义数值开启
        /// </summary>
        public bool CustomValueEnabled { get; set; } = false;

        /// <summary>
        /// 自定义当前值
        /// </summary>
        public int CustomCurrentValue { get; set; } = 100;

        /// <summary>
        /// 自定义最大值
        /// </summary>
        public int CustomMaxValue { get; set; } = 100;
    }
}