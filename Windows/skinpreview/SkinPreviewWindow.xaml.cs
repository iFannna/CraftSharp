using System;
using System.IO;
using System.Windows;
using System.Windows.Input;
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
            LoadDefaultSkin();
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
                _lastMousePosition = e.GetPosition(this);
                e.Handled = true;
            }
        }

        private void Grid_PreviewMouseMove(object sender, System.Windows.Input.MouseEventArgs e)
        {
            if (_isDragging && e.LeftButton == MouseButtonState.Pressed)
            {
                var currentPosition = e.GetPosition(this);
                var delta = currentPosition - _lastMousePosition;
                Viewport.CameraRotationMode = CameraRotationMode.Turntable;
                Viewport.AddRotateForce((float)delta.X, (float)delta.Y);
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