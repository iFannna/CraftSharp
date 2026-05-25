using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using CraftSharp.Models;
using CraftSharp.Helpers;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.SkinPreview
{
    public partial class SkinPreviewWindow : FluentWindow
    {
        private bool _isDragging;
        private Point _lastMousePosition;

        public SkinPreviewWindow()
        {
            InitializeComponent();
            SetupEffectsManager();
            SetupCamera();
            LoadDefaultSkin();
        }

        private void SetupEffectsManager()
        {
            Viewport.EffectsManager = new DefaultEffectsManager();
            Viewport.BackgroundColor = Color.FromArgb(0, 0, 0, 0);
        }

        private void SetupCamera()
        {
            Viewport.Camera = new HelixToolkit.Wpf.SharpDX.PerspectiveCamera
            {
                Position = new Point3D(-50, 25, -50),
                LookDirection = new Vector3D(50, -10, 50),
                UpDirection = new Vector3D(0, 1, 0),
                FarPlaneDistance = 500,
                NearPlaneDistance = 0.1,
                FieldOfView = 45
            };
        }

        private void LoadDefaultSkin()
        {
            var skinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.DefaultSkinWide);
            LoadSkin(skinPath);
        }

        public void LoadSkin(string skinPath)
        {
            if (!File.Exists(skinPath))
                return;

            var uvJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.PlayerUvWide);
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

        private void Grid_PreviewMouseMove(object sender, MouseEventArgs e)
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