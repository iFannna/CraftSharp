using CraftSharp.Models;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
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
        /// 拖动事件（实时通知父容器）
        /// </summary>
        public event EventHandler<BossBarDragEventArgs>? Dragging;

        /// <summary>
        /// 拖动完成事件
        /// </summary>
        public event EventHandler<BossBarDropEventArgs>? Dropped;

        private bool _isDragging = false;
        private double _dragStartY = 0;
        private double _visualOffset = 0;

        public BossBarItemControl(BossBarSettings settings)
        {
            InitializeComponent();
            Settings = settings;
            DataContext = settings;

            InitializeUI();
        }

        private void InitializeUI()
        {
            UpdateEnableButtonText();
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
            var iconPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.GetBossBarPath(Settings.IconType, "progress"));
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

            if (!string.IsNullOrEmpty(Settings.NotchType))
            {
                var notchPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.GetNotchPath(Settings.NotchType, "progress"));
                if (File.Exists(notchPath))
                {
                    var bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(notchPath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();
                    bitmap.Freeze();
                    NotchPreviewImage.Source = bitmap;
                }
                else
                {
                    NotchPreviewImage.Source = null;
                }
            }
            else
            {
                NotchPreviewImage.Source = null;
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

        // ==================== 拖动系统 ====================

        private Storyboard? _currentStoryboard;

        /// <summary>
        /// 开始拖动
        /// </summary>
        public void StartDrag(double startY)
        {
            _isDragging = true;
            _dragStartY = startY;
            _visualOffset = 0;

            // 取消之前的动画
            CancelCurrentAnimation();

            // 拖动状态：缩小 + 半透明（直接设置，不用动画）
            ScaleTransform.ScaleX = 0.95;
            ScaleTransform.ScaleY = 0.95;
            RootBorder.Opacity = 0.6;
            TranslateTransform.Y = 0;
        }

        /// <summary>
        /// 拖动过程中更新位置
        /// </summary>
        public void UpdateDragPosition(double currentY)
        {
            if (!_isDragging) return;

            _visualOffset = currentY - _dragStartY;
            TranslateTransform.Y = _visualOffset;

            // 实时通知父容器
            Dragging?.Invoke(this, new BossBarDragEventArgs(this, _visualOffset));
        }

        /// <summary>
        /// 拖动完成
        /// </summary>
        public void EndDrag()
        {
            if (!_isDragging) return;

            _isDragging = false;

            // 通知父容器完成拖动
            Dropped?.Invoke(this, new BossBarDropEventArgs(this));
        }

        /// <summary>
        /// 定格在当前位置（拖拽完成时调用）
        /// </summary>
        public void FinalizePosition()
        {
            // 取消动画
            CancelCurrentAnimation();

            // 直接恢复正常状态
            ScaleTransform.ScaleX = 1;
            ScaleTransform.ScaleY = 1;
            RootBorder.Opacity = 1;
            TranslateTransform.Y = 0;
        }

        /// <summary>
        /// 取消当前动画
        /// </summary>
        private void CancelCurrentAnimation()
        {
            if (_currentStoryboard != null)
            {
                _currentStoryboard.Stop(this);
                _currentStoryboard = null;
            }

            // 清除所有动画
            ScaleTransform.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            ScaleTransform.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            RootBorder.BeginAnimation(Border.OpacityProperty, null);
            TranslateTransform.BeginAnimation(TranslateTransform.YProperty, null);
        }

        /// <summary>
        /// 设置位移偏移（其他项让位时调用）
        /// </summary>
        public void SetShiftOffset(double offset)
        {
            if (_isDragging) return;

            TranslateTransform.Y = offset;
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

        // 拖动手柄点击事件
        private void OnDragHandleMouseDown(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;

            var container = Parent as System.Windows.Controls.Panel;
            if (container == null) return;

            _dragStartY = e.GetPosition(container).Y;
            StartDrag(_dragStartY);

            // 添加全局鼠标事件
            var window = System.Windows.Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewMouseMove += OnWindowMouseMove;
                window.PreviewMouseLeftButtonUp += OnWindowMouseUp;
            }

            Mouse.Capture(this);
        }

        private void OnWindowMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (!_isDragging) return;

            var container = Parent as System.Windows.Controls.Panel;
            if (container == null) return;

            var currentY = e.GetPosition(container).Y;
            UpdateDragPosition(currentY);
        }

        private void OnWindowMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (!_isDragging) return;

            var window = System.Windows.Window.GetWindow(this);
            if (window != null)
            {
                window.PreviewMouseMove -= OnWindowMouseMove;
                window.PreviewMouseLeftButtonUp -= OnWindowMouseUp;
            }

            Mouse.Capture(null);
            EndDrag();
        }
    }

    /// <summary>
    /// 拖动事件参数（实时）
    /// </summary>
    public class BossBarDragEventArgs : EventArgs
    {
        public BossBarItemControl DraggedItem { get; }
        public double VisualOffset { get; }

        public BossBarDragEventArgs(BossBarItemControl item, double offset)
        {
            DraggedItem = item;
            VisualOffset = offset;
        }
    }

    /// <summary>
    /// 拖动完成事件参数
    /// </summary>
    public class BossBarDropEventArgs : EventArgs
    {
        public BossBarItemControl DraggedItem { get; }

        public BossBarDropEventArgs(BossBarItemControl item)
        {
            DraggedItem = item;
        }
    }
}