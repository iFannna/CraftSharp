using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using HelixToolkit.Wpf.SharpDX;
using CraftSharp.Models;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.SkinPreview
{
    public partial class SkinPreviewWindow : FluentWindow
    {
        private bool _isDragging;
        private System.Windows.Point _lastMousePosition;

        public SkinPreviewWindow()
        {
            InitializeComponent();
            Viewport.EffectsManager = new DefaultEffectsManager();
            SetupCamera();
            LoadDefaultSkin();
        }

        private void SetupCamera()
        {
            // 设置相机位置，高度略低于模型顶部，以便能看到底部
            Viewport.Camera = new HelixToolkit.Wpf.SharpDX.PerspectiveCamera
            {
                Position = new Point3D(50, 25, 50),  // 相机位置：右前方，高度25
                LookDirection = new Vector3D(-50, -10, -50),  // 观察方向：指向模型中心
                UpDirection = new Vector3D(0, 1, 0),
                FarPlaneDistance = 500,
                NearPlaneDistance = 0.1,
                FieldOfView = 45
            };
        }

        private void LoadDefaultSkin()
        {
            var skinPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "assets/minecraft/textures/entity/player/wide/steve.png");
            LoadSkin(skinPath);
        }

        public void LoadSkin(string skinPath)
        {
            if (!File.Exists(skinPath))
                return;

            var uvJsonPath = Path.Combine(
                AppDomain.CurrentDomain.BaseDirectory,
                "assets/minecraft/textures/entity/player/uv/player.json");

            var playerModel = PlayerModelBuilder.CreatePlayerModel(skinPath, uvJsonPath);

            PlayerModelGroup.Children.Clear();
            PlayerModelGroup.Children.Add(playerModel);
        }

        private void Grid_PreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                _isDragging = true;
                _lastMousePosition = e.GetPosition(Viewport);
                e.Handled = true;
            }
        }

        private void Grid_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
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

        private void Grid_PreviewMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left && _isDragging)
            {
                _isDragging = false;
                e.Handled = true;
            }
        }

        protected override void OnClosed(EventArgs e)
        {
            Viewport.EffectsManager?.Dispose();
            base.OnClosed(e);
        }
    }
}