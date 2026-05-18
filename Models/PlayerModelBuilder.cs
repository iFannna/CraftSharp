using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using HelixToolkit.Wpf.SharpDX;
using Newtonsoft.Json;
using SharpDX;
using SharpDX.Direct3D11;

namespace CraftSharp.Models
{
    public static class PlayerModelBuilder
    {
        private static PlayerUvData? _uvData;
        private static readonly object _lock = new();

        public static PlayerUvData LoadUvData(string jsonPath)
        {
            lock (_lock)
            {
                if (_uvData != null)
                    return _uvData;

                var json = File.ReadAllText(jsonPath);
                _uvData = JsonConvert.DeserializeObject<PlayerUvData>(json);
                return _uvData!;
            }
        }

        public static Material CreatePixelMaterial(string skinPath, int x, int y, int w, int h)
        {
            var bitmap = new BitmapImage();
            bitmap.BeginInit();
            bitmap.UriSource = new Uri(skinPath, UriKind.Absolute);
            bitmap.CacheOption = BitmapCacheOption.OnLoad;
            bitmap.EndInit();
            bitmap.Freeze();

            var croppedBitmap = new CroppedBitmap(bitmap, new Int32Rect(x, y, w, h));
            croppedBitmap.Freeze();

            var tempPath = Path.Combine(Path.GetTempPath(), $"skin_crop_{x}_{y}_{w}_{h}.png");
            using (var fileStream = new FileStream(tempPath, FileMode.Create))
            {
                var encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(croppedBitmap));
                encoder.Save(fileStream);
            }

            var textureModel = new TextureModel(tempPath);

            return new PhongMaterial
            {
                DiffuseMap = textureModel,
                DiffuseColor = new Color4(1, 1, 1, 1),
                SpecularColor = new Color4(0, 0, 0, 0),
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

        public static MeshGeometry3D CreateFaceMesh(Vector3 center, float width, float height, float depth, FaceType face)
        {
            var hw = width / 2f;
            var hh = height / 2f;
            var hd = depth / 2f;

            var (p0, p1, p2, p3) = face switch
            {
                FaceType.Front => (
                    new Vector3(center.X - hw, center.Y - hh, center.Z + hd),
                    new Vector3(center.X + hw, center.Y - hh, center.Z + hd),
                    new Vector3(center.X + hw, center.Y + hh, center.Z + hd),
                    new Vector3(center.X - hw, center.Y + hh, center.Z + hd)),
                FaceType.Back => (
                    new Vector3(center.X + hw, center.Y - hh, center.Z - hd),
                    new Vector3(center.X - hw, center.Y - hh, center.Z - hd),
                    new Vector3(center.X - hw, center.Y + hh, center.Z - hd),
                    new Vector3(center.X + hw, center.Y + hh, center.Z - hd)),
                FaceType.Top => (
                    new Vector3(center.X - hw, center.Y + hh, center.Z + hd),
                    new Vector3(center.X + hw, center.Y + hh, center.Z + hd),
                    new Vector3(center.X + hw, center.Y + hh, center.Z - hd),
                    new Vector3(center.X - hw, center.Y + hh, center.Z - hd)),
                FaceType.Bottom => (
                    new Vector3(center.X - hw, center.Y - hh, center.Z - hd),
                    new Vector3(center.X + hw, center.Y - hh, center.Z - hd),
                    new Vector3(center.X + hw, center.Y - hh, center.Z + hd),
                    new Vector3(center.X - hw, center.Y - hh, center.Z + hd)),
                FaceType.Right => (
                    new Vector3(center.X + hw, center.Y - hh, center.Z + hd),
                    new Vector3(center.X + hw, center.Y - hh, center.Z - hd),
                    new Vector3(center.X + hw, center.Y + hh, center.Z - hd),
                    new Vector3(center.X + hw, center.Y + hh, center.Z + hd)),
                FaceType.Left => (
                    new Vector3(center.X - hw, center.Y - hh, center.Z - hd),
                    new Vector3(center.X - hw, center.Y - hh, center.Z + hd),
                    new Vector3(center.X - hw, center.Y + hh, center.Z + hd),
                    new Vector3(center.X - hw, center.Y + hh, center.Z - hd)),
                _ => throw new ArgumentException("Invalid face type")
            };

            var normal = face switch
            {
                FaceType.Front => new Vector3(0, 0, 1),
                FaceType.Back => new Vector3(0, 0, -1),
                FaceType.Top => new Vector3(0, 1, 0),
                FaceType.Bottom => new Vector3(0, -1, 0),
                FaceType.Right => new Vector3(1, 0, 0),
                FaceType.Left => new Vector3(-1, 0, 0),
                _ => throw new ArgumentException("Invalid face type")
            };

            var uvCoords = face switch
            {
                FaceType.Right => new Vector2Collection
                {
                    new Vector2(1, 1),
                    new Vector2(0, 1),
                    new Vector2(0, 0),
                    new Vector2(1, 0)
                },
                _ => new Vector2Collection
                {
                    new Vector2(0, 1),
                    new Vector2(1, 1),
                    new Vector2(1, 0),
                    new Vector2(0, 0)
                }
            };

            var mesh = new MeshGeometry3D
            {
                Positions = new Vector3Collection { p0, p1, p2, p3 },
                Normals = new Vector3Collection { normal, normal, normal, normal },
                TextureCoordinates = uvCoords,
                Indices = new IntCollection { 0, 1, 2, 0, 2, 3 }
            };

            return mesh;
        }

        public static GroupModel3D CreatePartModel(string skinPath, PartData partData)
        {
            var group = new GroupModel3D();

            if (partData.Size == null || partData.Position == null || partData.Uv == null)
                return group;

            var center = new Vector3(
                partData.Position.X,
                partData.Position.Y,
                partData.Position.Z
            );

            var width = partData.Size.Width;
            var height = partData.Size.Height;
            var depth = partData.Size.Depth;

            var faces = new[]
            {
                (FaceType.Front, partData.Uv.Front),
                (FaceType.Back, partData.Uv.Back),
                (FaceType.Top, partData.Uv.Top),
                (FaceType.Bottom, partData.Uv.Bottom),
                (FaceType.Right, partData.Uv.Right),
                (FaceType.Left, partData.Uv.Left)
            };

            foreach (var (faceType, uvFace) in faces)
            {
                if (uvFace == null)
                    continue;

                var material = CreatePixelMaterial(skinPath, uvFace.X, uvFace.Y, uvFace.W, uvFace.H);
                var mesh = CreateFaceMesh(center, width, height, depth, faceType);

                group.Children.Add(new MeshGeometryModel3D
                {
                    Geometry = mesh,
                    Material = material
                });
            }

            return group;
        }

        public static GroupModel3D CreatePlayerModel(string skinPath, string uvJsonPath)
        {
            var uvData = LoadUvData(uvJsonPath);
            var playerGroup = new GroupModel3D();

            if (uvData.Parts == null)
                return playerGroup;

            var parts = new List<(string Name, PartData? Data)>
            {
                ("Head", uvData.Parts.Head),
                ("Body", uvData.Parts.Body),
                ("RightArm", uvData.Parts.RightArm),
                ("LeftArm", uvData.Parts.LeftArm),
                ("RightLeg", uvData.Parts.RightLeg),
                ("LeftLeg", uvData.Parts.LeftLeg)
            };

            foreach (var (_, data) in parts)
            {
                if (data != null)
                {
                    var partModel = CreatePartModel(skinPath, data);
                    playerGroup.Children.Add(partModel);
                }
            }

            return playerGroup;
        }

        public static MeshGeometry3D CreateHeadMesh()
        {
            var builder = new MeshBuilder();
            builder.AddBox(new Vector3(0, 24, 0), 8, 8, 8);
            return builder.ToMeshGeometry3D();
        }
    }

    public enum FaceType
    {
        Front,
        Back,
        Top,
        Bottom,
        Right,
        Left
    }
}