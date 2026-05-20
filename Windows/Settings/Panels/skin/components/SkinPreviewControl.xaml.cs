using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using CraftSharp.Models;

namespace CraftSharp.Windows.Settings.Panels.Skin.Components
{
    public partial class SkinPreviewControl : System.Windows.Controls.UserControl
    {
        private static readonly string WideUvPath = "assets/minecraft/textures/entity/player/uv/wide.json";
        private static readonly string SlimUvPath = "assets/minecraft/textures/entity/player/uv/slim.json";

        public SkinPreviewControl()
        {
            InitializeComponent();
            SetupEffectsManager();
        }

        private void SetupEffectsManager()
        {
            Viewport.EffectsManager = new DefaultEffectsManager();
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

        public void LoadSkin(string skinPath, bool isWide)
        {
            try
            {
                ModelGroup.Children.Clear();

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
            ModelGroup.Children.Clear();
        }
    }
}