namespace CraftSharp.Models
{
    public class SkinItem
    {
        public required string Name { get; set; }
        public required string Path { get; set; }
        public bool IsWide { get; set; }
        public bool IsCustom { get; set; }
    }
}