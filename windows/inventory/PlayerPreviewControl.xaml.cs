using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using HelixToolkit.SharpDX;
using HelixToolkit.Wpf.SharpDX;
using CraftSharp.Models;
using CraftSharp.Helpers;

namespace CraftSharp.Windows.Inventory
{
    /// <summary>
    /// 玩家3D模型预览控件，支持鼠标跟踪旋转
    /// </summary>
    public partial class PlayerPreviewControl : UserControl
    {
        private GroupModel3D? _bodyGroup;  // 身体组（不含头部）
        private GroupModel3D? _headGroup;  // 头部组
        private float _bodyYaw = 0;   // 身体Y轴旋转角度
        private float _bodyPitch = 0; // 身体X轴旋转角度
        private float _headPitchOffset = 0; // 头部相对身体的额外X轴旋转

        // 旋转角度限制
        private const float BodyYawMax = 45f;    // 身体Y轴最大角度
        private const float BodyPitchMax = 10f;  // 整个模型X轴最大角度（±10°）
        private const float HeadPitchMax = 10f;  // 头部相对身体X轴最大额外角度

        // 模型尺寸参数
        // 原始UV：模型从y=-4到y=28，高度32单位
        // 布局要求：y=0在底部6px处，y=32在顶部6px处
        // 需要平移模型：向上偏移4单位（让y=-4变成y=0）
        private const float ModelHeight = 32f;        // 模型高度
        private const float ModelBottomY = 0f;       // 布局后模型底部（原y=-4）
        private const float ModelTopY = 32f;         // 布局后模型顶部（原y=28）
        private const float ModelCenterY = 16f;      // 模型中心 (32/2)
        private const float OriginalNeckY = 20f;     // 原始脖子位置
        private const float NeckY = 24f;             // 布局后脖子位置（20+4）

        // 预览区域布局参数（像素）
        private const float PreviewHeight = 70f;     // 预览区域高度
        private const float BottomMargin = 6f;       // 底部边距
        private const float TopMargin = 6f;          // 顶部边距
        private const float DisplayHeight = 58f;     // 模型显示高度 (70-12)

        // 缩放比例：58像素对应32单位模型
        private const float Scale = DisplayHeight / ModelHeight; // ≈ 1.81

        // 预览区域中心和尺寸缓存
        private Point _previewCenter;
        private Point _neckScreenPosition;  // 头部底部（脖子）在屏幕上的位置，作为鼠标跟随基准点
        private double _previewWidth;
        private double _previewHeight;

        // 预览区域在屏幕中的位置（用于计算鼠标相对位置）
        private Point _previewScreenPosition;

        // 头部底部（脖子）在模型坐标中的位置（偏移后）
        // 原始脖子位置 y=20，模型偏移 -22，所以脖子在 y=-2
        private const float NeckModelY = -2f;

        public PlayerPreviewControl()
        {
            InitializeComponent();
            SetupEffectsManager();
            SetupCamera();
            LoadPlayerModel();

            // 每帧更新旋转
            CompositionTarget.Rendering += OnRendering;
            Unloaded += OnUnloaded;
        }

        private void SetupEffectsManager()
        {
            // 共享设备：避免与皮肤页/预览窗口各自独立建设备
            Viewport.EffectsManager = SharedEffectsManager.Instance;
            Viewport.BackgroundColor = Color.FromArgb(0, 0, 0, 0);
        }

        private void SetupCamera()
        {
            // 相机位置：在模型高度中间（y=16），Z轴负坐标
            // LookDirection 水平向前，让模型上下居中显示（各6px边距）
            Viewport.Camera = new HelixToolkit.Wpf.SharpDX.OrthographicCamera
            {
                Position = new Point3D(0, ModelCenterY, -50),
                LookDirection = new Vector3D(0, 0, 50),
                UpDirection = new Vector3D(0, 1, 0),
                FarPlaneDistance = 500,
                NearPlaneDistance = 0.1,
                Width = 28
            };
        }

        private void LoadPlayerModel()
        {
            var skinPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.DefaultSkinWide);
            var uvJsonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.PlayerUvWide);

            if (!File.Exists(skinPath) || !File.Exists(uvJsonPath))
                return;

            var (bodyGroup, headGroup) = PlayerModelBuilder.CreatePlayerModelWithSeparateHead(skinPath, uvJsonPath);

            // 头部组已经嵌套在身体组中，只需添加身体组
            DisposeModelChildren();
            BodyModelGroup.Children.Add(bodyGroup);

            // 模型平移：将模型向上偏移4单位
            // 原始UV模型范围：y=-4（脚底）到 y=28（头顶）
            // 布局要求：y=0在预览区域底部6px处，y=32在顶部6px处
            // 所以需要将模型向上偏移4单位（让y=-4变成y=0）
            var offsetTransform = new TranslateTransform3D(0, 4, 0);
            BodyModelGroup.Transform = offsetTransform;

            _bodyGroup = bodyGroup;
            _headGroup = headGroup;
        }

        /// <summary>
        /// 加载指定的皮肤文件
        /// </summary>
        public void LoadSkin(string skinPath, bool isWide)
        {
            var uvJsonPath = isWide
                ? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.PlayerUvWide)
                : Path.Combine(AppDomain.CurrentDomain.BaseDirectory, AssetPaths.PlayerUvSlim);

            if (!File.Exists(skinPath) || !File.Exists(uvJsonPath))
                return;

            var (bodyGroup, headGroup) = PlayerModelBuilder.CreatePlayerModelWithSeparateHead(skinPath, uvJsonPath);

            DisposeModelChildren();
            BodyModelGroup.Children.Add(bodyGroup);

            var offsetTransform = new TranslateTransform3D(0, 4, 0);
            BodyModelGroup.Transform = offsetTransform;

            _bodyGroup = bodyGroup;
            _headGroup = headGroup;
        }

        /// <summary>
        /// 刷新玩家模型（重新加载皮肤）
        /// </summary>
        public void RefreshModel()
        {
            LoadPlayerModel();
        }

        /// <summary>
        /// 更新预览区域的位置信息（由父窗口调用）
        /// </summary>
        public void UpdatePreviewPosition(Point screenPosition, double width, double height)
        {
            _previewScreenPosition = screenPosition;
            _previewWidth = width;
            _previewHeight = height;
            _previewCenter = new Point(screenPosition.X + width / 2, screenPosition.Y + height / 2);

            // 计算头部底部（脖子）在屏幕上的位置，作为鼠标跟随基准点
            // 相机看向 y=-10，视野中心在预览区域中心
            // 脖子在 y=-2（模型偏移后），比视野中心向上偏移8单位
            // 需要将这个模型偏移转换为屏幕坐标
            CalculateNeckScreenPosition();
        }

        /// <summary>
        /// 计算头部底部（脖子）在屏幕上的位置
        /// </summary>
        private void CalculateNeckScreenPosition()
        {
            // 布局参数：
            // - 预览区域高度 70px
            // - 底部边距 6px，顶部边距 6px
            // - 模型显示高度 58px（对应32单位）
            // - 模型底部 y=0 在屏幕上 = 预览区域底部 + 6px
            // - 模型顶部 y=32 在屏幕上 = 预览区域顶部 - 6px
            // - 脖子 y=24（偏移后）

            // 计算脖子在屏幕上的位置：
            // y=0 → screenY = previewBottom + 6
            // y=32 → screenY = previewTop + 6 (屏幕坐标向下为正)
            // y=24 → screenY = previewBottom + 6 + (24/32) * 58

            double previewBottom = _previewScreenPosition.Y + _previewHeight;
            double neckScreenY = previewBottom - BottomMargin + (NeckY / ModelHeight) * DisplayHeight;

            _neckScreenPosition = new Point(_previewCenter.X, neckScreenY);
        }

        private void DisposeModelChildren()
        {
            foreach (var child in BodyModelGroup.Children.ToList())
            {
                // 必须先脱离场景再 Dispose：附着状态下释放会跳过 detach 链，
                // RenderHost 节点列表残留冻结死节点（表现为残影/后续模型不渲染）
                BodyModelGroup.Children.Remove(child);
                HelixDispose.Tree(child);
            }
            _bodyGroup = null;
            _headGroup = null;
        }

        private void OnRendering(object? sender, EventArgs e)
        {
            if (_bodyGroup == null || _headGroup == null || !IsVisible)
                return;

            // 获取鼠标在屏幕上的位置
            var cursorPos = Win32Helper.GetCursorPosition();
            var monitorInfo = Win32Helper.GetMonitorInfoFromCursor();

            // 使用头部底部（脖子）位置作为鼠标跟随基准点
            double offsetX = cursorPos.X - _neckScreenPosition.X;
            double offsetY = cursorPos.Y - _neckScreenPosition.Y;

            // 计算相对于脖子位置的角度比例
            double maxOffsetX = Math.Max(
                _neckScreenPosition.X - monitorInfo.rcMonitor.Left,
                monitorInfo.rcMonitor.Right - _neckScreenPosition.X
            );
            double maxOffsetY = Math.Max(
                _neckScreenPosition.Y - monitorInfo.rcMonitor.Top,
                monitorInfo.rcMonitor.Bottom - _neckScreenPosition.Y
            );

            // 计算比例（-1 到 1）
            double ratioX = Math.Clamp(offsetX / maxOffsetX, -1, 1);
            double ratioY = Math.Clamp(offsetY / maxOffsetY, -1, 1);

            // 计算旋转角度
            // Y轴旋转（左右）：身体旋转，头部嵌套其中自动跟随
            _bodyYaw = (float)(ratioX * BodyYawMax);

            // X轴旋转（上下）：鼠标向上时头部向上仰
            _bodyPitch = (float)(-ratioY * BodyPitchMax);
            _headPitchOffset = (float)(-ratioY * HeadPitchMax); // 头部额外旋转

            // 身体组旋转：整体Y轴 + X轴旋转（头部嵌套其中自动跟随）
            ApplyRotation(_bodyGroup, _bodyYaw, _bodyPitch);

            // 头部组额外旋转：只应用额外的X轴偏移，绕脖子旋转
            ApplyHeadExtraPitch(_headPitchOffset);
        }

        /// <summary>
        /// 应用头部额外的X轴旋转（绕脖子）
        /// </summary>
        private void ApplyHeadExtraPitch(float extraPitch)
        {
            if (_headGroup == null) return;

            // 头部额外X轴旋转：绕脖子位置旋转
            // 脖子位置 y=24（原始20 + 偏移4）
            var pitchRotation = new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(1, 0, 0), extraPitch),
                new Point3D(0, NeckY, 0)
            );

            _headGroup.Transform = pitchRotation;
        }

        private void ApplyRotation(GroupModel3D group, float yaw, float pitch)
        {
            // 创建旋转变换（Y轴 + X轴）
            var yawRotation = new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(0, 1, 0), yaw)
            );
            var pitchRotation = new RotateTransform3D(
                new AxisAngleRotation3D(new Vector3D(1, 0, 0), pitch)
            );

            // 组合旋转
            var transformGroup = new Transform3DGroup();
            transformGroup.Children.Add(yawRotation);
            transformGroup.Children.Add(pitchRotation);

            group.Transform = transformGroup;
        }

        private void OnUnloaded(object sender, RoutedEventArgs e)
        {
            CompositionTarget.Rendering -= OnRendering;
            // 释放本控件视口；EffectsManager 全应用共享不能动
            Viewport.Dispose();
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            // 更新预览区域尺寸
            _previewWidth = ActualWidth;
            _previewHeight = ActualHeight;
        }
    }
}