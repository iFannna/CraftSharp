using System.Text.Json.Serialization;

namespace CraftSharp.Models
{
    public class PlayerUvData
    {
        [JsonPropertyName("textureSize")]
        public TextureSize? TextureSize { get; set; }

        [JsonPropertyName("parts")]
        public Parts? Parts { get; set; }
    }

    public class TextureSize
    {
        [JsonPropertyName("width")]
        public int Width { get; set; }

        [JsonPropertyName("height")]
        public int Height { get; set; }
    }

    public class Parts
    {
        [JsonPropertyName("head")]
        public PartData? Head { get; set; }

        [JsonPropertyName("body")]
        public PartData? Body { get; set; }

        [JsonPropertyName("rightArm")]
        public PartData? RightArm { get; set; }

        [JsonPropertyName("leftArm")]
        public PartData? LeftArm { get; set; }

        [JsonPropertyName("rightLeg")]
        public PartData? RightLeg { get; set; }

        [JsonPropertyName("leftLeg")]
        public PartData? LeftLeg { get; set; }

        // 外层覆盖物
        [JsonPropertyName("outerHead")]
        public PartData? OuterHead { get; set; }

        [JsonPropertyName("outerBody")]
        public PartData? OuterBody { get; set; }

        [JsonPropertyName("outerRightArm")]
        public PartData? OuterRightArm { get; set; }

        [JsonPropertyName("outerLeftArm")]
        public PartData? OuterLeftArm { get; set; }

        [JsonPropertyName("outerRightLeg")]
        public PartData? OuterRightLeg { get; set; }

        [JsonPropertyName("outerLeftLeg")]
        public PartData? OuterLeftLeg { get; set; }
    }

    public class PartData
    {
        [JsonPropertyName("size")]
        public PartSize? Size { get; set; }

        [JsonPropertyName("position")]
        public PartPosition? Position { get; set; }

        [JsonPropertyName("uv")]
        public PartUv? Uv { get; set; }
    }

    public class PartSize
    {
        [JsonPropertyName("width")]
        public float Width { get; set; }

        [JsonPropertyName("height")]
        public float Height { get; set; }

        [JsonPropertyName("depth")]
        public float Depth { get; set; }
    }

    public class PartPosition
    {
        [JsonPropertyName("x")]
        public float X { get; set; }

        [JsonPropertyName("y")]
        public float Y { get; set; }

        [JsonPropertyName("z")]
        public float Z { get; set; }
    }

    public class PartUv
    {
        [JsonPropertyName("top")]
        public UvFace? Top { get; set; }

        [JsonPropertyName("bottom")]
        public UvFace? Bottom { get; set; }

        [JsonPropertyName("right")]
        public UvFace? Right { get; set; }

        [JsonPropertyName("front")]
        public UvFace? Front { get; set; }

        [JsonPropertyName("left")]
        public UvFace? Left { get; set; }

        [JsonPropertyName("back")]
        public UvFace? Back { get; set; }
    }

    public class UvFace
    {
        [JsonPropertyName("x")]
        public int X { get; set; }

        [JsonPropertyName("y")]
        public int Y { get; set; }

        [JsonPropertyName("w")]
        public int W { get; set; }

        [JsonPropertyName("h")]
        public int H { get; set; }
    }
}