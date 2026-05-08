using CraftSharp.Models;
using CraftSharp.Windows;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;

namespace CraftSharp.Controls
{
    /// <summary>
    /// BOSS血条列表项控件
    /// </summary>
    public partial class BossBarItemControl : System.Windows.Controls.UserControl
    {
        /// <summary>
        /// 当前绑定的BOSS血条配置
        /// </summary>
        public BossBarSettings Settings { get; private set; }

        /// <summary>
        /// 编辑事件
        /// </summary>
        public event EventHandler<BossBarSettings>? EditRequested;

        /// <summary>
        /// 删除事件
        /// </summary>
        public event EventHandler<BossBarSettings>? DeleteRequested;

        /// <summary>
        /// 启用状态变化事件
        /// </summary>
        public event EventHandler<BossBarSettings>? EnableChanged;

        /// <summary>
        /// 拖动开始事件
        /// </summary>
        public event EventHandler<BossBarItemControl>? DragStarted;

        /// <summary>
        /// 拖动结束事件（用于排序）
        /// </summary>
        public event EventHandler<BossBarItemControl>? Dropped;

        public BossBarItemControl(BossBarSettings settings)
        {
            InitializeComponent();
            Settings = settings;
            DataContext = settings;

            // 初始化UI
            InitializeUI();
        }

        private void InitializeUI()
        {
            // 设置启用按钮文本
            UpdateEnableButtonText();

            // 加载图标预览
            LoadIconPreview();
        }

        private void UpdateEnableButtonText()
        {
            string enableText = System.Windows.Application.Current.TryFindResource("BossBarItemEnable") as string ?? "启用";
            string disableText = System.Windows.Application.Current.TryFindResource("BossBarItemDisable") as string ?? "禁用";
            EnableRun.Text = Settings.IsEnabled ? disableText : enableText;
        }

        private void LoadIconPreview()
        {
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.GetBossBarPath(Settings.IconType));
            if (File.Exists(iconPath))
            {
                var bitmap = new BitmapImage();
                bitmap.BeginInit();
                bitmap.UriSource = new Uri(iconPath, UriKind.Absolute);
                bitmap.CacheOption = BitmapCacheOption.OnLoad;
                bitmap.EndInit();
                bitmap.Freeze();
                IconPreviewImage.Source = bitmap;
            }
            else
            {
                IconPreviewImage.Source = null;
            }
        }

        private void EditLink_Click(object sender, RoutedEventArgs e)
        {
            EditRequested?.Invoke(this, Settings);
        }

        private void DeleteLink_Click(object sender, RoutedEventArgs e)
        {
            DeleteRequested?.Invoke(this, Settings);
        }

        private void EnableLink_Click(object sender, RoutedEventArgs e)
        {
            Settings.IsEnabled = !Settings.IsEnabled;
            UpdateEnableButtonText();
            EnableChanged?.Invoke(this, Settings);
        }

        // ==================== 拖动排序 ====================

        private void OnDragHandleClick(object sender, MouseButtonEventArgs e)
        {
            // 开始拖动
            DragStarted?.Invoke(this, this);
        }

        private void OnDragStart(object sender, MouseButtonEventArgs e)
        {
            // 只有拖动手柄区域才能触发拖动
            // 这里不实现，实际在 OnDragHandleClick 中触发
        }

        public void StartDrag()
        {
            System.Windows.DragDrop.DoDragDrop(this, this, System.Windows.DragDropEffects.Move);
        }

        private void OnDragOver(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(BossBarItemControl)))
            {
                e.Effects = System.Windows.DragDropEffects.Move;
            }
            else
            {
                e.Effects = System.Windows.DragDropEffects.None;
            }
            e.Handled = true;
        }

        private void OnDrop(object sender, System.Windows.DragEventArgs e)
        {
            if (e.Data.GetDataPresent(typeof(BossBarItemControl)))
            {
                var sourceControl = e.Data.GetData(typeof(BossBarItemControl)) as BossBarItemControl;
                if (sourceControl != null && sourceControl != this)
                {
                    Dropped?.Invoke(this, sourceControl);
                }
            }
            e.Handled = true;
        }

        /// <summary>
        /// 更新配置并刷新UI
        /// </summary>
        public void UpdateSettings(BossBarSettings newSettings)
        {
            Settings = newSettings;
            DataContext = newSettings;
            InitializeUI();
        }
    }
}