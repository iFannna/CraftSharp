using System;
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
    public partial class SkinPreviewControl : System.Windows.Controls.UserControl
    {
        private static readonly string WideUvPath = "assets/minecraft/textures/entity/player/uv/wide.json";
        private static readonly string SlimUvPath = "assets/minecraft/textures/entity/player/uv/slim.json";

        private bool _isDragging;
        private Point _lastMousePosition;

        public SkinPreviewControl()
        {
            InitializeComponent();
            SetupEffectsManager();
        }

        private void SetupEffectsManager()
        {
            Viewport.EffectsManager = SharedEffectsManager.Instance;
            Viewport.BackgroundColor = Color.FromArgb(0, 0, 0, 0);

            // 正交相机：无透视变形
            Viewport.Camera = new HelixToolkit.Wpf.SharpDX.OrthographicCamera
            {
                Position = new Point3D(0, 20, -40),
                LookDirection = new Vector3D(0, -10, 40),
                UpDirection = new Vector3D(0, 1, 0),
                FarPlaneDistance = 500,
                NearPlaneDistance = 0.1,
                Width = 30
            };
        }

        /// <summary>
        /// 设置相机宽度（调整模型显示大小）
        /// </summary>
        public void SetCameraWidth(double width)
        {
            if (Viewport.Camera is HelixToolkit.Wpf.SharpDX.OrthographicCamera camera)
            {
                camera.Width = width;
                // 调整相机位置让模型居中
                camera.Position = new Point3D(0, 12, -40);
                camera.LookDirection = new Vector3D(0, 0, 40);
            }
        }

        public void LoadSkin(string skinPath, bool isWide)
        {
            try
            {
                DisposeModelChildren();

                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var uvPath = isWide ? WideUvPath : SlimUvPath;
                var fullUvPath = System.IO.Path.Combine(basePath, uvPath);

                // 只显示正面模型，正方向（不旋转）
                var model = PlayerModelBuilder.CreatePlayerModel(skinPath, fullUvPath);
                ModelGroup.Children.Add(model);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load skin preview: {ex.Message}");
            }
        }

        public void Clear()
        {
            DisposeModelChildren();
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

        private void UserControl_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDragging = true;
                _lastMousePosition = e.GetPosition(Viewport);
                e.Handled = true;
            }
        }

        private void UserControl_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(Viewport);
                var delta = currentPosition - _lastMousePosition;
                Viewport.AddRotateForce((float)delta.X * 0.5f, (float)delta.Y * 0.5f);
                _lastMousePosition = currentPosition;
                e.Handled = true;
            }
        }

        private void UserControl_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && _isDragging)
            {
                _isDragging = false;
                e.Handled = true;
            }
        }
    }
}