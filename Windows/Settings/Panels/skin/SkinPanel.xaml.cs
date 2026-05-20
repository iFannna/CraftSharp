using CraftSharp.Models;
using CraftSharp.Windows.Settings.Panels.Skin.Components;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CraftSharp.Windows.Settings.Panels.Skin
{
    public partial class SkinPanel : global::System.Windows.Controls.UserControl
    {
        private AppSettings _settings;
        private global::System.Windows.Controls.Border[] _optionBorders;
        private global::System.Windows.Controls.TextBlock[] _optionTexts;
        private ObservableCollection<SkinItem> _skinItems;
        private SkinItemControl? _selectedSkinControl;

        // 当前选中的类型：wide 或 slim
        private bool _isWide = true;

        private static readonly string WideSkinFolder = "assets/minecraft/textures/entity/player/wide";
        private static readonly string SlimSkinFolder = "assets/minecraft/textures/entity/player/slim";
        private static readonly string WideUvPath = "assets/minecraft/textures/entity/player/uv/wide.json";
        private static readonly string SlimUvPath = "assets/minecraft/textures/entity/player/uv/slim.json";

        public SkinPanel(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            _optionBorders = new global::System.Windows.Controls.Border[] { BorderSteve, BorderAlex, BorderUpload };
            _optionTexts = new global::System.Windows.Controls.TextBlock[] { TextSteve, TextAlex, TextUpload };
            _skinItems = new ObservableCollection<SkinItem>();
            SkinGrid.ItemsSource = _skinItems;

            LoadSkins();
        }

        public void SetParentWindow(global::System.Windows.Window parent)
        {
            // 后续实现
        }

        private void LoadSkins()
        {
            _skinItems.Clear();

            var skinFolder = _isWide ? WideSkinFolder : SlimSkinFolder;
            var basePath = AppDomain.CurrentDomain.BaseDirectory;

            var fullPath = Path.Combine(basePath, skinFolder);
            if (Directory.Exists(fullPath))
            {
                var files = Directory.GetFiles(fullPath, "*.png");
                foreach (var file in files)
                {
                    var name = Path.GetFileNameWithoutExtension(file);
                    _skinItems.Add(new SkinItem
                    {
                        Name = name,
                        Path = file,
                        IsWide = _isWide,
                        IsCustom = false
                    });
                }
            }
        }

        private void OptionBorder_Click(object sender, global::System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is global::System.Windows.Controls.Border clickedBorder)
            {
                for (int i = 0; i < _optionBorders.Length; i++)
                {
                    var border = _optionBorders[i];
                    var text = _optionTexts[i];

                    if (border == clickedBorder)
                    {
                        border.Background = TryFindResource("AccentBrush") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(0, 120, 215));
                        text.Foreground = TryFindResource("TextPrimaryBrush") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(255, 255, 255));
                        text.FontWeight = FontWeights.Medium;
                    }
                    else
                    {
                        border.Background = Brushes.Transparent;
                        text.Foreground = TryFindResource("TextSecondaryBrush") as SolidColorBrush ?? new SolidColorBrush(Color.FromRgb(150, 150, 150));
                        text.FontWeight = FontWeights.Normal;
                    }
                }

                // 切换 wide/slim 类型
                if (clickedBorder == BorderSteve)
                {
                    _isWide = true;
                    LoadSkins();
                }
                else if (clickedBorder == BorderAlex)
                {
                    _isWide = false;
                    LoadSkins();
                }
            }
        }

        private void SkinItemControl_Loaded(object sender, RoutedEventArgs e)
        {
            if (sender is SkinItemControl control && control.DataContext is SkinItem skinItem)
            {
                var uvPath = skinItem.IsWide ? WideUvPath : SlimUvPath;
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var fullUvPath = Path.Combine(basePath, uvPath);

                control.LoadSkin(skinItem.Path, fullUvPath, skinItem.IsWide);
            }
        }

        private void SkinItemControl_Selected(object sender, EventArgs e)
        {
            if (sender is SkinItemControl control)
            {
                // 取消之前的选中
                if (_selectedSkinControl != null)
                {
                    _selectedSkinControl.IsSelected = false;
                }

                // 设置新的选中
                _selectedSkinControl = control;
                control.IsSelected = true;
            }
        }
    }
}