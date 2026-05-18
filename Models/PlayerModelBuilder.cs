using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using HelixToolkit.Wpf.SharpDX;
using SharpDX.Direct3D11;

namespace CraftSharp.Models
{
    public static class PlayerModelBuilder
    {
        public static MeshGeometry3D CreateHeadMesh()
        {
            var builder = new MeshBuilder();
            builder.AddBox(new SharpDX.Vector3(0, 24, 0), 8, 8, 8);
            return builder.ToMeshGeometry3D();
        }

        public static Material CreatePixelMaterial(string skinPath, int x, int y, int w, int h)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new System.Uri(skinPath, System.UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            var croppedBitmap = new CroppedBitmap(bitmap, new Int32Rect(x, y, w, h));
            croppedBitmap.Freeze();

            // 保存裁剪后的图片到临时文件
            var tempPath = Path.Combine(Path.GetTempPath(), $"skin_crop_{x}_{y}.png");
            using (var fileStream = new FileStream(tempPath, FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));
                encoder.Save(fileStream);
            }

            var textureModel = new TextureModel(tempPath);

            // 使用PhongMaterial并设置Point采样实现像素清晰渲染
            return new PhongMaterial
            {
                DiffuseMap = textureModel,
                DiffuseColor = new SharpDX.Color4(1, 1, 1, 1),
                SpecularColor = new SharpDX.Color4(0, 0, 0, 0),
                SpecularShininess = 1,
                DiffuseMapSampler = new SamplerStateDescription
                {
                    Filter = Filter.MinMagMipPoint,
                    AddressU = TextureAddressMode.Clamp,
                    AddressV = TextureAddressMode.Clamp,
                    AddressW = TextureAddressMode.Clamp
                }
            };
        }
    }
}