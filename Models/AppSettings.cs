using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CraftSharp.Models
{
    /// <summary>
    /// 应用设置配置模型（分层结构）
    /// </summary>
    public class AppSettings
    {
        public AppSettings()
        {
            // 不在构造函数中初始化默认元素，避免 JSON 反序列化时产生重复
            // 默认元素由 App.xaml.cs 的 EnsureAllHudElementsExist 方法添加
            HudElements = new ObservableCollection<HudElementSettings>();
            BossBars = new ObservableCollection<BossBarSettings>();
        }

        /// <summary>
        /// 系统设置
        /// </summary>
        public SystemSettings System { get; set; } = new SystemSettings();

        /// <summary>
        /// 外观设置
        /// </summary>
        public AppearanceSettings Appearance { get; set; } = new AppearanceSettings();

        /// <summary>
        /// 物品栏设置
        /// </summary>
        public InventorySettings Inventory { get; set; } = new InventorySettings();

        /// <summary>
        /// 状态栏设置
        /// </summary>
        public StatusBarSettings StatusBar { get; set; } = new StatusBarSettings();

        /// <summary>
        /// 快捷栏设置
        /// </summary>
        public HotbarSettings Hotbar { get; set; } = new HotbarSettings();

        /// <summary>
        /// HUD 元素配置列表
        /// </summary>
        public ObservableCollection<HudElementSettings> HudElements { get; set; }

        /// <summary>
        /// BOSS 血条配置列表
        /// </summary>
        public ObservableCollection<BossBarSettings> BossBars { get; set; }

        /// <summary>
        /// 快捷键设置
        /// </summary>
        public HotkeySettings Hotkeys { get; set; } = new HotkeySettings();

        /// <summary>
        /// 槽位数据
        /// </summary>
        public Dictionary<string, SlotItem> Slots { get; set; } = new Dictionary<string, SlotItem>();

        /// <summary>
        /// 样式独立格子数据（key: 样式文件名如 "inventory.png", value: 格子数据字典）
        /// 用于 SharedData=false 时每个样式独立存储格子数据
        /// </summary>
        public Dictionary<string, Dictionary<string, SlotItem>> StyleSlots { get; set; } = new Dictionary<string, Dictionary<string, SlotItem>>();

        /// <summary>
        /// 应用版本
        /// </summary>
        public string Version { get; set; } = "1.0.0";
    }

    /// <summary>
    /// 系统设置
    /// </summary>
    public class SystemSettings
    {
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
        public bool RememberWindowPosition { get; set; } = true;

        /// <summary>
        /// 记住设置窗口大小
        /// </summary>
        public bool RememberWindowSize { get; set; } = true;

        /// <summary>
        /// 设置窗口 X 坐标
        /// </summary>
        public double WindowPositionX { get; set; } = 0;

        /// <summary>
        /// 设置窗口 Y 坐标
        /// </summary>
        public double WindowPositionY { get; set; } = 0;

        /// <summary>
        /// 设置窗口宽度
        /// </summary>
        public double WindowWidth { get; set; } = 1080;

        /// <summary>
        /// 设置窗口高度
        /// </summary>
        public double WindowHeight { get; set; } = 720;

        /// <summary>
        /// 记住卡片状态
        /// </summary>
        public bool RememberCardStates { get; set; } = true;

        /// <summary>
        /// 卡片展开状态字典（Key: titleResourceKey, Value: 是否展开）
        /// </summary>
        public Dictionary<string, bool> CardExpandedStates { get; set; } = new Dictionary<string, bool>();

        /// <summary>
        /// 记住导航菜单选项
        /// </summary>
        public bool RememberNavSelection { get; set; } = true;

        /// <summary>
        /// 上次选中的导航菜单标签
        /// </summary>
        public string LastSelectedNav { get; set; } = "system";
    }

    /// <summary>
    /// 外观设置
    /// </summary>
    public class AppearanceSettings
    {
        /// <summary>
        /// 主题风格
        /// </summary>
        public string Theme { get; set; } = "跟随系统";

        /// <summary>
        /// 字体（使用标识符：yahei/pixel/unifont/songti/heiti/kaiti）
        /// </summary>
        public string Font { get; set; } = "yahei";

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
    }

    /// <summary>
    /// 物品栏设置
    /// </summary>
    public class InventorySettings
    {
        /// <summary>
        /// 显示物品栏窗口
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// 锁定物品栏窗口位置
        /// </summary>
        public bool Locked { get; set; } = true;

        /// <summary>
        /// 记住物品栏窗口位置
        /// </summary>
        public bool RememberPosition { get; set; } = true;

        /// <summary>
        /// 物品栏窗口 X 坐标
        /// </summary>
        public double PositionX { get; set; } = 0;

        /// <summary>
        /// 物品栏窗口 Y 坐标
        /// </summary>
        public double PositionY { get; set; } = 0;

        /// <summary>
        /// 灰色蒙版（打开物品栏时显示全屏灰色遮罩）
        /// </summary>
        public bool GrayOverlay { get; set; } = true;

        /// <summary>
        /// 灰色蒙版不透明度（0-100，默认75）
        /// </summary>
        public int GrayOverlayOpacity { get; set; } = 75;

        /// <summary>
        /// 隐藏状态栏（打开物品栏时隐藏状态栏窗口）
        /// </summary>
        public bool HideStatusBar { get; set; } = false;

        /// <summary>
        /// 点击模式（single/double，默认单击）
        /// </summary>
        public string ClickMode { get; set; } = "single";

        /// <summary>
        /// 物品栏样式路径（默认 inventory.png）
        /// </summary>
        public string StylePath { get; set; } = "inventory.png";

        /// <summary>
        /// 共享数据开关（开启时所有样式共用格子数据，关闭时每个样式独立存储）
        /// </summary>
        public bool SharedData { get; set; } = true;

        /// <summary>
        /// 悬浮效果（hover 显示 50% 白色蒙版）
        /// </summary>
        public bool HoverEffect { get; set; } = true;
    }

    /// <summary>
    /// 状态栏设置
    /// </summary>
    public class StatusBarSettings
    {
        /// <summary>
        /// 显示状态栏
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// 锁定状态栏位置
        /// </summary>
        public bool Locked { get; set; } = false;

        /// <summary>
        /// 记住状态栏位置
        /// </summary>
        public bool RememberPosition { get; set; } = false;

        /// <summary>
        /// 状态栏窗口 X 坐标
        /// </summary>
        public double PositionX { get; set; } = 0;

        /// <summary>
        /// 状态栏窗口 Y 坐标
        /// </summary>
        public double PositionY { get; set; } = 0;
    }

    /// <summary>
    /// 快捷栏设置
    /// </summary>
    public class HotbarSettings
    {
        /// <summary>
        /// 左副手槽
        /// </summary>
        public bool LeftOffhand { get; set; } = false;

        /// <summary>
        /// 右副手槽
        /// </summary>
        public bool RightOffhand { get; set; } = false;

        /// <summary>
        /// 显示快捷栏（包括格子、副手槽）
        /// </summary>
        public bool Visible { get; set; } = true;

        /// <summary>
        /// 点击模式（single/double，默认双击）
        /// </summary>
        public string ClickMode { get; set; } = "double";

        /// <summary>
        /// 悬浮效果（hover显示selection框）
        /// </summary>
        public bool HoverEffect { get; set; } = true;

        /// <summary>
        /// 显示目标程序图标（仅对快捷方式生效）
        /// </summary>
        public bool ShowTargetIcon { get; set; } = false;

        /// <summary>
        /// 文件名显示颜色（十六进制格式，如 "#FFFFFF"，或 "auto" 表示自动模式）
        /// </summary>
        public string FileNameColor { get; set; } = "#FFFFFF";

        /// <summary>
        /// 用户自定义的文件名颜色（可选，null表示无自定义）
        /// </summary>
        public string? CustomFileNameColor { get; set; } = null;

        /// <summary>
        /// 自动模式使用的预设颜色列表
        /// </summary>
        public static readonly string[] AutoColors = new string[]
        {
            "#FCFCFC", // 白色
            "#A8A8A8", // 灰色
            "#FCFC54", // 黄色
            "#5454FC", // 蓝色
            "#FC54FC", // 粶色
            "#A800A8", // 紫色
            "#FC5454", // 红色
            "#54FCFC", // 青色
            "#00A800"  // 绿色
        };

        /// <summary>
        /// 根据文件名计算自动颜色（相同文件名返回相同颜色）
        /// </summary>
        public static string GetAutoColorForFileName(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
                return AutoColors[0];

            int hash = Math.Abs(fileName.GetHashCode());
            int index = hash % AutoColors.Length;
            return AutoColors[index];
        }
    }

    /// <summary>
    /// 快捷键设置
    /// </summary>
    public class HotkeySettings
    {
        /// <summary>
        /// 打开背包快捷键
        /// </summary>
        public string Inventory { get; set; } = "E";

        /// <summary>
        /// 打开设置快捷键
        /// </summary>
        public string Settings { get; set; } = "Ctrl+S";

        /// <summary>
        /// 显示/隐藏快捷栏快捷键
        /// </summary>
        public string HotbarToggle { get; set; } = "";
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
        /// 图标样式（如"full"、"hardcore_full"、"food_full"等）
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
    public class BossBarSettings : INotifyPropertyChanged
    {
        private string _id = "";
        private string _name = "新BOSS";
        private string _iconType = "blue";
        private string _notchType = "";
        private bool _isEnabled = true;
        private bool _dataMappingEnabled = true;
        private string _dataMappingType = "电池电量";
        private bool _customValueEnabled = false;
        private int _customCurrentValue = 100;
        private int _customMaxValue = 100;

        public event PropertyChangedEventHandler? PropertyChanged;

        protected void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// 唯一标识
        /// </summary>
        public string Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged(nameof(Id));
                }
            }
        }

        /// <summary>
        /// 名称
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged(nameof(Name));
                }
            }
        }

        /// <summary>
        /// 图标类型（颜色样式）
        /// </summary>
        public string IconType
        {
            get => _iconType;
            set
            {
                if (_iconType != value)
                {
                    _iconType = value;
                    OnPropertyChanged(nameof(IconType));
                }
            }
        }

        /// <summary>
        /// 分段样式（Notches等级，空表示无）
        /// </summary>
        public string NotchType
        {
            get => _notchType;
            set
            {
                if (_notchType != value)
                {
                    _notchType = value;
                    OnPropertyChanged(nameof(NotchType));
                }
            }
        }

        /// <summary>
        /// 是否启用
        /// </summary>
        public bool IsEnabled
        {
            get => _isEnabled;
            set
            {
                if (_isEnabled != value)
                {
                    _isEnabled = value;
                    OnPropertyChanged(nameof(IsEnabled));
                }
            }
        }

        /// <summary>
        /// 数据映射开启
        /// </summary>
        public bool DataMappingEnabled
        {
            get => _dataMappingEnabled;
            set
            {
                if (_dataMappingEnabled != value)
                {
                    _dataMappingEnabled = value;
                    OnPropertyChanged(nameof(DataMappingEnabled));
                }
            }
        }

        /// <summary>
        /// 数据映射类型
        /// </summary>
        public string DataMappingType
        {
            get => _dataMappingType;
            set
            {
                if (_dataMappingType != value)
                {
                    _dataMappingType = value;
                    OnPropertyChanged(nameof(DataMappingType));
                }
            }
        }

        /// <summary>
        /// 自定义数值开启
        /// </summary>
        public bool CustomValueEnabled
        {
            get => _customValueEnabled;
            set
            {
                if (_customValueEnabled != value)
                {
                    _customValueEnabled = value;
                    OnPropertyChanged(nameof(CustomValueEnabled));
                }
            }
        }

        /// <summary>
        /// 自定义当前值
        /// </summary>
        public int CustomCurrentValue
        {
            get => _customCurrentValue;
            set
            {
                if (_customCurrentValue != value)
                {
                    _customCurrentValue = value;
                    OnPropertyChanged(nameof(CustomCurrentValue));
                }
            }
        }

        /// <summary>
        /// 自定义最大值
        /// </summary>
        public int CustomMaxValue
        {
            get => _customMaxValue;
            set
            {
                if (_customMaxValue != value)
                {
                    _customMaxValue = value;
                    OnPropertyChanged(nameof(CustomMaxValue));
                }
            }
        }
    }
}