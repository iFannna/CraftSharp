using System;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using CraftSharp.Models;
using CraftSharp.Helpers;

namespace CraftSharp.Windows.Settings.Panels.Skin.Components
{
    public partial class SkinItemControl : System.Windows.Controls.UserControl
    {
        private bool _isSelected = false;
        private static readonly SolidColorBrush SelectedBorderBrush = new SolidColorBrush(Color.FromRgb(0, 120, 215));
        private static readonly SolidColorBrush NormalBorderBrush = new SolidColorBrush(Color.FromArgb(0, 0, 0, 0));

        public event EventHandler? Selected;
        public event EventHandler? RequestSetCurrent;
        public event EventHandler? RequestRename;
        public event EventHandler? RequestRemove;

        public string? SkinPath { get; private set; }
        public string? SkinName { get; set; }
        public bool IsCustomSkin { get; set; }

        private bool _isCurrentSkin;

        public bool IsCurrentSkin
        {
            get => _isCurrentSkin;
            set
            {
                _isCurrentSkin = value;
                MenuSetCurrent.Visibility = value ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        public SkinItemControl()
        {
            InitializeComponent();
            SetupEffectsManager();
        }

        private void SetupEffectsManager()
        {
            // 共享设备：每卡片各建 DefaultEffectsManager = 各一个 D3D11 设备，
            // 网格重建时旧设备无人 Dispose 即永久泄漏
            Viewport.EffectsManager = SharedEffectsManager.Instance;
            Viewport.BackgroundColor = Color.FromArgb(0, 0, 0, 0);

            // 使用正交相机：没有透视变形，两个模型大小相等
            Viewport.Camera = new HelixToolkit.Wpf.SharpDX.OrthographicCamera
            {
                Position = new Point3D(0, 30, -35),
                LookDirection = new Vector3D(0, -18, 35),
                UpDirection = new Vector3D(0, 1, 0),
                FarPlaneDistance = 500,
                NearPlaneDistance = 0.1,
                Width = 45  // 减小宽度，模型更大
            };
        }

        public void LoadSkin(string skinPath, string uvJsonPath, bool isWide)
        {
            SkinPath = skinPath;

            try
            {
                DisposeModelChildren();

                // 正面模型：放在左侧（屏幕左边，X轴负方向）
                var frontModel = PlayerModelBuilder.CreatePlayerModel(skinPath, uvJsonPath);
                var frontRotate = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), -45));
                var frontTranslate = new TranslateTransform3D(-9, 0, 0);
                var frontTransformGroup = new Transform3DGroup();
                frontTransformGroup.Children.Add(frontRotate);
                frontTransformGroup.Children.Add(frontTranslate);

                var frontContainer = new GroupModel3D();
                frontContainer.Transform = frontTransformGroup;
                frontContainer.Children.Add(frontModel);
                ModelGroup.Children.Add(frontContainer);

                // 背面模型：放在右侧（屏幕右边，X轴正方向）
                var backModel = PlayerModelBuilder.CreatePlayerModel(skinPath, uvJsonPath);
                var backRotate = new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 1, 0), 135));
                var backTranslate = new TranslateTransform3D(9, 0, 0);
                var backTransformGroup = new Transform3DGroup();
                backTransformGroup.Children.Add(backRotate);
                backTransformGroup.Children.Add(backTranslate);

                var backContainer = new GroupModel3D();
                backContainer.Transform = backTransformGroup;
                backContainer.Children.Add(backModel);
                ModelGroup.Children.Add(backContainer);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load skin: {ex.Message}");
            }
        }

        /// <summary>
        /// 控件即将被网格重建丢弃时释放全部原生渲染资源。
        /// SharpDX COM 资源无终结器，不显式 Dispose 则永不回收；幂等可重复调用。
        /// RenderHost 必须单独 Dispose：Viewport.Dispose 不会终止其渲染线程，
        /// 泄漏表现为每控件残留 2 个僵尸线程 + ~7MB 驱动内存。
        /// </summary>
        public void ReleaseGraphics()
        {
            DisposeModelChildren();
            (Viewport.RenderHost as IDisposable)?.Dispose();
            Viewport.Dispose();
        }

        private void DisposeModelChildren()
        {
            foreach (var child in ModelGroup.Children.ToList())
            {
                // 必须先脱离场景再 Dispose：附着状态下释放会跳过 detach 链，
                // RenderHost 节点列表残留冻结死节点（表现为残影/后续模型不渲染）
                ModelGroup.Children.Remove(child);
                HelixDispose.Tree(child);
            }
        }

        public bool IsSelected
        {
            get => _isSelected;
            set
            {
                _isSelected = value;
                UpdateBorderAppearance();
            }
        }

        private void UpdateBorderAppearance()
        {
            ItemBorder.BorderBrush = _isSelected ? SelectedBorderBrush : NormalBorderBrush;
        }

        private void ItemBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            Selected?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }

        private void ItemBorder_MouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            Selected?.Invoke(this, EventArgs.Empty);
            e.Handled = true;
        }

        private void ItemBorder_ContextMenuOpening(object sender, System.Windows.Controls.ContextMenuEventArgs e)
        {
            MenuRename.Visibility = IsCustomSkin ? Visibility.Visible : Visibility.Collapsed;
            MenuRemove.Visibility = IsCustomSkin ? Visibility.Visible : Visibility.Collapsed;
            MenuSetCurrent.Visibility = IsCurrentSkin ? Visibility.Collapsed : Visibility.Visible;

            if (MenuSetCurrent.Visibility == Visibility.Collapsed &&
                MenuRename.Visibility == Visibility.Collapsed &&
                MenuRemove.Visibility == Visibility.Collapsed)
            {
                ItemBorder.ContextMenu = null;
            }
            else
            {
                ItemBorder.ContextMenu = ItemContextMenu;
            }
        }

        private void MenuSetCurrent_Click(object sender, RoutedEventArgs e)
        {
            RequestSetCurrent?.Invoke(this, EventArgs.Empty);
        }

        private void MenuRename_Click(object sender, RoutedEventArgs e)
        {
            RequestRename?.Invoke(this, EventArgs.Empty);
        }

        private void MenuRemove_Click(object sender, RoutedEventArgs e)
        {
            RequestRemove?.Invoke(this, EventArgs.Empty);
        }
    }
}