using System;
using System.Collections.Generic;
using System.IO;
using HelixToolkit.Wpf.SharpDX;
using Newtonsoft.Json;
using SharpDX;
using SharpDX.Direct3D11;

namespace CraftSharp.Models
{
    public static class PlayerModelBuilder
    {
        // UV 配置缓存：支持多路径缓存
        private static readonly Dictionary<string, PlayerUvData> _uvDataCache = new();
        private static readonly object _uvLock = new();

        // 材质缓存：避免重复创建相同皮肤的材质
        private static readonly Dictionary<string, Material> _materialCache = new();
        private static readonly object _materialLock = new();

        /// <summary>
        /// 加载 UV 配置数据（支持多路径缓存）
        /// </summary>
        public static PlayerUvData LoadUvData(string jsonPath)
        {
            lock (_uvLock)
            {
                if (_uvDataCache.TryGetValue(jsonPath, out var cached))
                    return cached;

                if (!File.Exists(jsonPath))
                    throw new FileNotFoundException($"UV configuration file not found: {jsonPath}");

                var json = File.ReadAllText(jsonPath);
                var data = JsonConvert.DeserializeObject<PlayerUvData>(json);
                if (data == null)
                    throw new InvalidOperationException($"Failed to parse UV configuration: {jsonPath}");

                _uvDataCache[jsonPath] = data;
                return data;
            }
        }

        /// <summary>
        /// 清除指定路径的 UV 配置缓存
        /// </summary>
        public static void ClearUvCache(string jsonPath)
        {
            lock (_uvLock)
            {
                _uvDataCache.Remove(jsonPath);
            }
        }

        /// <summary>
        /// 清除所有 UV 配置缓存
        /// </summary>
        public static void ClearAllUvCache()
        {
            lock (_uvLock)
            {
                _uvDataCache.Clear();
            }
        }

        /// <summary>
        /// 创建或获取皮肤材质（带缓存）
        /// </summary>
        public static Material CreateSkinMaterial(string skinPath)
        {
            lock (_materialLock)
            {
                if (_materialCache.TryGetValue(skinPath, out var cached))
                    return cached;

                var textureModel = new TextureModel(skinPath);

                var material = new PhongMaterial
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

                _materialCache[skinPath] = material;
                return material;
            }
        }

        /// <summary>
        /// 创建外层覆盖物材质（支持 Alpha 混合）
        /// </summary>
        public static Material CreateOuterLayerMaterial(string skinPath)
        {
            lock (_materialLock)
            {
                var cacheKey = skinPath + "_outer";
                if (_materialCache.TryGetValue(cacheKey, out var cached))
                    return cached;

                var textureModel = new TextureModel(skinPath);

                var material = new PhongMaterial
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

                _materialCache[cacheKey] = material;
                return material;
            }
        }

        /// <summary>
        /// 清除指定皮肤的材质缓存
        /// </summary>
        public static void ClearMaterialCache(string skinPath)
        {
            lock (_materialLock)
            {
                _materialCache.Remove(skinPath);
            }
        }

        /// <summary>
        /// 清除所有材质缓存
        /// </summary>
        public static void ClearAllMaterialCache()
        {
            lock (_materialLock)
            {
                _materialCache.Clear();
            }
        }

        /// <summary>
        /// 判断面是否需要镜像UV映射
        /// </summary>
        /// <param name="face">面类型</param>
        /// <returns>(mirroredU, mirroredV) - U坐标镜像和V坐标镜像</returns>
        private static (bool mirroredU, bool mirroredV) NeedsMirroredUv(FaceType face)
        {
            // Top 面：上下颠倒 + 镜像翻转
            // Bottom 面：镜像翻转

            return face switch
            {
                FaceType.Top => (true, true),     // U镜像 + V镜像
                FaceType.Bottom => (true, false), // U镜像，V不镜像
                _ => (false, false)
            };
        }

        /// <summary>
        /// 创建单个面的网格，UV 坐标指向纹理中的实际像素位置
        /// </summary>
        public static MeshGeometry3D CreateFaceMesh(
            Vector3 center, float width, float height, float depth,
            FaceType face, UvFace uvData, int textureWidth = 64, int textureHeight = 64)
        {
            var hw = width / 2f;
            var hh = height / 2f;
            var hd = depth / 2f;

            // 计算顶点位置
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
                _ => throw new ArgumentException($"Invalid face type: {face}")
            };

            var normal = face switch
            {
                FaceType.Front => new Vector3(0, 0, 1),
                FaceType.Back => new Vector3(0, 0, -1),
                FaceType.Top => new Vector3(0, 1, 0),
                FaceType.Bottom => new Vector3(0, -1, 0),
                FaceType.Right => new Vector3(1, 0, 0),
                FaceType.Left => new Vector3(-1, 0, 0),
                _ => throw new ArgumentException($"Invalid face type: {face}")
            };

            // UV 坐标归一化到纹理像素位置
            // 纹理坐标系：原点在左上角，X 向右，Y 向下
            var u0 = uvData.X / (float)textureWidth;
            var u1 = (uvData.X + uvData.W) / (float)textureWidth;
            var v0 = uvData.Y / (float)textureHeight;
            var v1 = (uvData.Y + uvData.H) / (float)textureHeight;

            // 根据镜像设置选择UV坐标
            var (mirroredU, mirroredV) = NeedsMirroredUv(face);
            var uvCoords = new Vector2Collection
            {
                new Vector2(mirroredU ? u1 : u0, mirroredV ? v0 : v1),  // p0
                new Vector2(mirroredU ? u0 : u1, mirroredV ? v0 : v1),  // p1
                new Vector2(mirroredU ? u0 : u1, mirroredV ? v1 : v0),  // p2
                new Vector2(mirroredU ? u1 : u0, mirroredV ? v1 : v0)   // p3
            };

            return new MeshGeometry3D
            {
                Positions = new Vector3Collection { p0, p1, p2, p3 },
                Normals = new Vector3Collection { normal, normal, normal, normal },
                TextureCoordinates = uvCoords,
                Indices = new IntCollection { 0, 1, 2, 0, 2, 3 }
            };
        }

        /// <summary>
        /// 创建单个部位的所有面网格，合并为一个 MeshGeometry3D
        /// </summary>
        public static MeshGeometry3D CreatePartMesh(
            Vector3 center, float width, float height, float depth,
            PartUv uv, int textureWidth = 64, int textureHeight = 64)
        {
            var positions = new Vector3Collection();
            var normals = new Vector3Collection();
            var textureCoords = new Vector2Collection();
            var indices = new IntCollection();

            var faces = new[]
            {
                (FaceType.Front, uv.Back),    // Front 面使用 Back 的 UV
                (FaceType.Back, uv.Front),    // Back 面使用 Front 的 UV
                (FaceType.Top, uv.Top),
                (FaceType.Bottom, uv.Bottom),
                (FaceType.Right, uv.Right),
                (FaceType.Left, uv.Left)
            };

            foreach (var (faceType, uvFace) in faces)
            {
                if (uvFace == null)
                    continue;

                var faceMesh = CreateFaceMesh(center, width, height, depth, faceType, uvFace, textureWidth, textureHeight);

                var baseIndex = positions.Count;

                // 添加顶点数据
                positions.AddRange(faceMesh.Positions);
                normals.AddRange(faceMesh.Normals);
                textureCoords.AddRange(faceMesh.TextureCoordinates);

                // 添加索引数据，偏移到当前顶点位置
                foreach (var idx in faceMesh.Indices)
                {
                    indices.Add(baseIndex + idx);
                }
            }

            return new MeshGeometry3D
            {
                Positions = positions,
                Normals = normals,
                TextureCoordinates = textureCoords,
                Indices = indices
            };
        }

        /// <summary>
        /// 创建单个部位的模型（使用单一材质）
        /// </summary>
        public static MeshGeometryModel3D CreatePartModel(Material material, PartData partData, int textureWidth = 64, int textureHeight = 64)
        {
            if (partData.Size == null || partData.Position == null || partData.Uv == null)
                return new MeshGeometryModel3D();

            // 直接使用 JSON 配置中的位置，不做任何翻转
            var center = new Vector3(
                partData.Position.X,
                partData.Position.Y,
                partData.Position.Z
            );

            var mesh = CreatePartMesh(
                center,
                partData.Size.Width,
                partData.Size.Height,
                partData.Size.Depth,
                partData.Uv,
                textureWidth,
                textureHeight
            );

            return new MeshGeometryModel3D
            {
                Geometry = mesh,
                Material = material
            };
        }

        /// <summary>
        /// 创建完整的玩家模型（使用单一材质）
        /// </summary>
        public static GroupModel3D CreatePlayerModel(string skinPath, string uvJsonPath)
        {
            var uvData = LoadUvData(uvJsonPath);
            var playerGroup = new GroupModel3D();

            if (uvData.Parts == null || uvData.TextureSize == null)
                return playerGroup;

            // 创建或获取单一材质，所有部位共享
            var material = CreateSkinMaterial(skinPath);
            var outerMaterial = CreateOuterLayerMaterial(skinPath);
            var texWidth = uvData.TextureSize.Width;
            var texHeight = uvData.TextureSize.Height;

            // 内层部位
            var innerParts = new List<(string Name, PartData? Data)>
            {
                ("Head", uvData.Parts.Head),
                ("Body", uvData.Parts.Body),
                ("RightArm", uvData.Parts.RightArm),
                ("LeftArm", uvData.Parts.LeftArm),
                ("RightLeg", uvData.Parts.RightLeg),
                ("LeftLeg", uvData.Parts.LeftLeg)
            };

            // 外层覆盖物部位
            var outerParts = new List<(string Name, PartData? Data)>
            {
                ("OuterHead", uvData.Parts.OuterHead),
                ("OuterBody", uvData.Parts.OuterBody),
                ("OuterRightArm", uvData.Parts.OuterRightArm),
                ("OuterLeftArm", uvData.Parts.OuterLeftArm),
                ("OuterRightLeg", uvData.Parts.OuterRightLeg),
                ("OuterLeftLeg", uvData.Parts.OuterLeftLeg)
            };

            // 先添加内层部位
            foreach (var (_, data) in innerParts)
            {
                if (data != null)
                {
                    var partModel = CreatePartModel(material, data, texWidth, texHeight);
                    playerGroup.Children.Add(partModel);
                }
            }

            // 后添加外层覆盖物（渲染顺序在内层之后）
            foreach (var (_, data) in outerParts)
            {
                if (data != null)
                {
                    var partModel = CreatePartModel(outerMaterial, data, texWidth, texHeight);
                    playerGroup.Children.Add(partModel);
                }
            }

            return playerGroup;
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