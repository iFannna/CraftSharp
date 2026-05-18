using CraftSharp.Windows.SkinPreview;

namespace CraftSharp.Services
{
    public class SkinPreviewService
    {
        private static SkinPreviewService? _instance;
        public static SkinPreviewService Instance => _instance ??= new SkinPreviewService();

        private SkinPreviewWindow? _window;

        private SkinPreviewService() { }

        public void Initialize(SkinPreviewWindow window)
        {
            _window = window;
        }

        public void Show()
        {
            _window?.Show();
            _window?.Activate();
        }

        public void Hide()
        {
            _window?.Hide();
        }

        public void LoadSkin(string skinPath)
        {
            _window?.LoadSkin(skinPath);
        }

        public bool IsVisible => _window?.IsVisible ?? false;
    }
}