using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CraftSharp.Windows.Inventory
{
    /// <summary>
    /// 九宫格贴图控件，边框拉伸不变形
    /// </summary>
    public class NineSliceImage : FrameworkElement
    {
        private BitmapImage? _source;
        private double _borderSizeBase = 28; // 基础边框宽度（像素）
        private double _scaleFactor = 1.0; // 缩放因子

        // 九宫格裁剪图片缓存
        private CroppedBitmap?[] _croppedParts = new CroppedBitmap?[9];

        /// <summary>
        /// 图片源
        /// </summary>
        public BitmapImage? Source
        {
            get => _source;
            set
            {
                _source = value;
                UpdateCroppedParts();
                InvalidateVisual();
            }
        }

        /// <summary>
        /// 基础边框宽度（像素）
        /// </summary>
        public double BorderSizeBase
        {
            get => _borderSizeBase;
            set
            {
                _borderSizeBase = value;
                UpdateCroppedParts();
                InvalidateVisual();
            }
        }

        /// <summary>
        /// 缩放因子
        /// </summary>
        public double ScaleFactor
        {
            get => _scaleFactor;
            set
            {
                _scaleFactor = value;
                InvalidateVisual();
            }
        }

        public NineSliceImage()
        {
            // 设置像素缩放模式
            RenderOptions.SetBitmapScalingMode(this, BitmapScalingMode.NearestNeighbor);
        }

        /// <summary>
        /// 更新九宫格裁剪图片
        /// </summary>
        private void UpdateCroppedParts()
        {
            if (_source == null)
            {
                for (int i = 0; i < 9; i++) _croppedParts[i] = null;
                return;
            }

            double imgWidth = _source.PixelWidth;
            double imgHeight = _source.PixelHeight;
            double b = _borderSizeBase;

            // 九宫格索引：
            // 0: 左上角, 1: 上边, 2: 右上角
            // 3: 左边,   4: 中心, 5: 右边
            // 6: 左下角, 7: 下边, 8: 右下角

            _croppedParts[0] = new CroppedBitmap(_source, new Int32Rect(0, 0, (int)b, (int)b)); // 左上角
            _croppedParts[1] = new CroppedBitmap(_source, new Int32Rect((int)b, 0, (int)(imgWidth - 2 * b), (int)b)); // 上边
            _croppedParts[2] = new CroppedBitmap(_source, new Int32Rect((int)(imgWidth - b), 0, (int)b, (int)b)); // 右上角
            _croppedParts[3] = new CroppedBitmap(_source, new Int32Rect(0, (int)b, (int)b, (int)(imgHeight - 2 * b))); // 左边
            _croppedParts[4] = new CroppedBitmap(_source, new Int32Rect((int)b, (int)b, (int)(imgWidth - 2 * b), (int)(imgHeight - 2 * b))); // 中心
            _croppedParts[5] = new CroppedBitmap(_source, new Int32Rect((int)(imgWidth - b), (int)b, (int)b, (int)(imgHeight - 2 * b))); // 右边
            _croppedParts[6] = new CroppedBitmap(_source, new Int32Rect(0, (int)(imgHeight - b), (int)b, (int)b)); // 左下角
            _croppedParts[7] = new CroppedBitmap(_source, new Int32Rect((int)b, (int)(imgHeight - b), (int)(imgWidth - 2 * b), (int)b)); // 下边
            _croppedParts[8] = new CroppedBitmap(_source, new Int32Rect((int)(imgWidth - b), (int)(imgHeight - b), (int)b, (int)b)); // 右下角
        }

        protected override void OnRenderSizeChanged(SizeChangedInfo sizeInfo)
        {
            base.OnRenderSizeChanged(sizeInfo);
            InvalidateVisual();
        }

        protected override void OnRender(DrawingContext dc)
        {
            if (_source == null || ActualWidth <= 0 || ActualHeight <= 0) return;

            double targetWidth = ActualWidth;
            double targetHeight = ActualHeight;
            double tb = _borderSizeBase * _scaleFactor; // 目标边框宽度（缩放后）

            // 中心区域尺寸
            double centerWidth = targetWidth - 2 * tb;
            double centerHeight = targetHeight - 2 * tb;
            if (centerWidth < 0) centerWidth = 0;
            if (centerHeight < 0) centerHeight = 0;

            // 绘制九个区域（使用 NearestNeighbor 模式保持像素清晰）
            // 0: 左上角（不拉伸）
            dc.DrawImage(_croppedParts[0], new Rect(0, 0, tb, tb));

            // 1: 上边（水平拉伸）
            if (centerWidth > 0)
                dc.DrawImage(_croppedParts[1], new Rect(tb, 0, centerWidth, tb));

            // 2: 右上角（不拉伸）
            dc.DrawImage(_croppedParts[2], new Rect(targetWidth - tb, 0, tb, tb));

            // 3: 左边（垂直拉伸）
            if (centerHeight > 0)
                dc.DrawImage(_croppedParts[3], new Rect(0, tb, tb, centerHeight));

            // 4: 中心（双向拉伸）
            if (centerWidth > 0 && centerHeight > 0)
                dc.DrawImage(_croppedParts[4], new Rect(tb, tb, centerWidth, centerHeight));

            // 5: 右边（垂直拉伸）
            if (centerHeight > 0)
                dc.DrawImage(_croppedParts[5], new Rect(targetWidth - tb, tb, tb, centerHeight));

            // 6: 左下角（不拉伸）
            dc.DrawImage(_croppedParts[6], new Rect(0, targetHeight - tb, tb, tb));

            // 7: 下边（水平拉伸）
            if (centerWidth > 0)
                dc.DrawImage(_croppedParts[7], new Rect(tb, targetHeight - tb, centerWidth, tb));

            // 8: 右下角（不拉伸）
            dc.DrawImage(_croppedParts[8], new Rect(targetWidth - tb, targetHeight - tb, tb, tb));
        }
    }
}