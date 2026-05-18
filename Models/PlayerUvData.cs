using Newtonsoft.Json;

namespace CraftSharp.Models
{
    public class PlayerUvData
    {
        [JsonProperty("textureSize")]
        public TextureSize? TextureSize { get; set; }

        [JsonProperty("parts")]
        public Parts? Parts { get; set; }
    }

    public class TextureSize
    {
        [JsonProperty("width")]
        public int Width { get; set; }

        [JsonProperty("height")]
        public int Height { get; set; }
    }

    public class Parts
    {
        [JsonProperty("head")]
        public PartData? Head { get; set; }

        [JsonProperty("body")]
        public PartData? Body { get; set; }

        [JsonProperty("rightArm")]
        public PartData? RightArm { get; set; }

        [JsonProperty("leftArm")]
        public PartData? LeftArm { get; set; }

        [JsonProperty("rightLeg")]
        public PartData? RightLeg { get; set; }

        [JsonProperty("leftLeg")]
        public PartData? LeftLeg { get; set; }

        // 外层覆盖物
        [JsonProperty("outerHead")]
        public PartData? OuterHead { get; set; }

        [JsonProperty("outerBody")]
        public PartData? OuterBody { get; set; }

        [JsonProperty("outerRightArm")]
        public PartData? OuterRightArm { get; set; }

        [JsonProperty("outerLeftArm")]
        public PartData? OuterLeftArm { get; set; }

        [JsonProperty("outerRightLeg")]
        public PartData? OuterRightLeg { get; set; }

        [JsonProperty("outerLeftLeg")]
        public PartData? OuterLeftLeg { get; set; }
    }

    public class PartData
    {
        [JsonProperty("size")]
        public PartSize? Size { get; set; }

        [JsonProperty("position")]
        public PartPosition? Position { get; set; }

        [JsonProperty("uv")]
        public PartUv? Uv { get; set; }
    }

    public class PartSize
    {
        [JsonProperty("width")]
        public float Width { get; set; }

        [JsonProperty("height")]
        public float Height { get; set; }

        [JsonProperty("depth")]
        public float Depth { get; set; }
    }

    public class PartPosition
    {
        [JsonProperty("x")]
        public float X { get; set; }

        [JsonProperty("y")]
        public float Y { get; set; }

        [JsonProperty("z")]
        public float Z { get; set; }
    }

    public class PartUv
    {
        [JsonProperty("top")]
        public UvFace? Top { get; set; }

        [JsonProperty("bottom")]
        public UvFace? Bottom { get; set; }

        [JsonProperty("right")]
        public UvFace? Right { get; set; }

        [JsonProperty("front")]
        public UvFace? Front { get; set; }

        [JsonProperty("left")]
        public UvFace? Left { get; set; }

        [JsonProperty("back")]
        public UvFace? Back { get; set; }
    }

    public class UvFace
    {
        [JsonProperty("x")]
        public int X { get; set; }

        [JsonProperty("y")]
        public int Y { get; set; }

        [JsonProperty("w")]
        public int W { get; set; }

        [JsonProperty("h")]
        public int H { get; set; }
    }
}