using CraftSharp.Models;
using CraftSharp.Services;
using CraftSharp.Helpers;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Settings.Panels.Hud
{
    /// <summary>
    /// HudAccordionItem 核心逻辑 - 构造函数、动画、设置管理
    /// </summary>
    public partial class HudAccordionItem : System.Windows.Controls.UserControl
    {
        private AppSettings _settings;
        private string _hudId;
        private bool _isExpanded = false;
        private bool _isAnimating = false;

        // UI element references for mutual exclusion and visibility control
        private ToggleSwitch? _mappingToggle;
        private System.Windows.Controls.ComboBox? _mappingComboBox;
        private ToggleSwitch? _customToggle;
        private StackPanel? _valueContainer;
        private System.Windows.Controls.TextBox? _currentValueTextBox;
        private System.Windows.Controls.TextBox? _maxValueTextBox;
        private System.Windows.Controls.TextBlock? _maxValueDisplay;
        private System.Windows.Controls.TextBox? _saturationTextBox;
        private System.Windows.Controls.TextBlock? _saturationLimitDisplay;

        /// <summary>
        /// 展开状态变化事件
        /// </summary>
        public event EventHandler<(string Key, bool IsExpanded)>? ExpandedChanged;

        /// <summary>
        /// HUD 元素 ID
        /// </summary>
        public string HudId => _hudId;

        /// <summary>
        /// 当前是否展开
        /// </summary>
        public bool IsExpanded => _isExpanded;

        public HudAccordionItem(AppSettings settings, string id, string name)
        {
            InitializeComponent();
            _settings = settings;
            _hudId = id;

            TitleText.Text = name;

            // 读取保存的展开状态（如果启用了记住卡片状态）
            // 使用 HudElement_xxx 作为 Key（因为 HudAccordionItem 使用 hudId）
            string stateKey = $"HudElement_{id}";
            if (_settings.System.RememberCardStates)
            {
                if (_settings.System.CardExpandedStates.TryGetValue(stateKey, out bool savedExpanded))
                {
                    _isExpanded = savedExpanded;
                }
                else
                {
                    _isExpanded = false; // HUD 卡片默认折叠
                }
            }
            else
            {
                _isExpanded = false; // HUD 卡片默认折叠
            }

            AddHudContent(id);

            LocalizationService.Instance.LanguageChanged += OnLanguageChanged;

            // 窗口加载后设置初始展开状态
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // 根据初始展开状态设置 UI（不执行动画）
            if (_isExpanded)
            {
                ContentBorder.Height = double.NaN;
                ArrowRotate.Angle = 0;
            }
            else
            {
                ContentBorder.Height = 0;
                ArrowRotate.Angle = -90;
            }
        }

        private void OnLanguageChanged()
        {
            ContentPanel.Children.Clear();
            AddHudContent(_hudId);
        }

        private void Header_Click(object sender, RoutedEventArgs e)
        {
            if (_isAnimating) return;

            _isExpanded = !_isExpanded;

            if (_isExpanded)
                AnimateExpand();
            else
                AnimateCollapse();

            var arrowAnimation = new DoubleAnimation
            {
                To = _isExpanded ? 0 : -90,
                Duration = TimeSpan.FromMilliseconds(_isExpanded ? 200 : 150),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
            };
            ArrowRotate.BeginAnimation(RotateTransform.AngleProperty, arrowAnimation);

            // 如果启用了记住卡片状态，保存到配置
            string stateKey = $"HudElement_{_hudId}";
            if (_settings.System.RememberCardStates)
            {
                _settings.System.CardExpandedStates[stateKey] = _isExpanded;
                SaveSettings();
            }

            // 触发展开状态变化事件
            ExpandedChanged?.Invoke(this, (stateKey, _isExpanded));
        }

        private void AnimateExpand()
        {
            _isAnimating = true;

            ContentPanel.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            double targetHeight = ContentPanel.DesiredSize.Height + 32;

            var animation = new DoubleAnimation
            {
                From = 0,
                To = targetHeight,
                Duration = TimeSpan.FromMilliseconds(200),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            animation.Completed += (s, e) =>
            {
                ContentBorder.Height = double.NaN;
                _isAnimating = false;
            };

            ContentBorder.Height = 0;
            ContentBorder.BeginAnimation(FrameworkElement.HeightProperty, animation);
        }

        private void AnimateCollapse()
        {
            _isAnimating = true;

            double currentHeight = ContentBorder.ActualHeight;

            var animation = new DoubleAnimation
            {
                From = currentHeight,
                To = 0,
                Duration = TimeSpan.FromMilliseconds(150),
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn }
            };

            animation.Completed += (s, e) =>
            {
                ContentBorder.Height = 0;
                _isAnimating = false;
            };

            ContentBorder.BeginAnimation(FrameworkElement.HeightProperty, animation);
        }

        private void EnsureHudElementExists(string id)
        {
            if (_settings.HudElements.Any(h => h.Id == id)) return;

            var newElement = new HudElementSettings { Id = id };
            _settings.HudElements.Add(newElement);
        }

        private string GetResourceString(string key)
        {
            return System.Windows.Application.Current.TryFindResource(key) as string ?? key;
        }

        private void SaveSettings()
        {
            if (System.Windows.Application.Current is App app)
            {
                app.SaveSettings();
            }
        }

        private void RefreshHudElement(string id)
        {
            if (id == "crosshair" || id == "attackindicator")
            {
                CrosshairService.Instance.RefreshHudElement(id);
            }
            else
            {
                StatusBarService.Instance.RefreshHudElement(id);
            }
        }

        private void RefreshContentHeight()
        {
            if (!_isExpanded || _isAnimating) return;

            ContentPanel.Measure(new System.Windows.Size(double.PositiveInfinity, double.PositiveInfinity));
            double newHeight = ContentPanel.DesiredSize.Height + 32;
            double currentHeight = ContentBorder.ActualHeight;

            if (Math.Abs(newHeight - currentHeight) > 5)
            {
                _isAnimating = true;
                var animation = new DoubleAnimation
                {
                    From = currentHeight,
                    To = newHeight,
                    Duration = TimeSpan.FromMilliseconds(150),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                animation.Completed += (s, e) =>
                {
                    ContentBorder.Height = double.NaN;
                    _isAnimating = false;
                };
                ContentBorder.BeginAnimation(FrameworkElement.HeightProperty, animation);
            }
            else
            {
                ContentBorder.BeginAnimation(FrameworkElement.HeightProperty, null);
                ContentBorder.Height = double.NaN;
            }
        }

        private Action<bool>? GetSetVisibleAction(string id)
        {
            return id switch
            {
                "expbar" => StatusBarService.Instance.SetExpBarVisible,
                "food" => StatusBarService.Instance.SetFoodVisible,
                "air" => StatusBarService.Instance.SetAirVisible,
                "armor" => StatusBarService.Instance.SetArmorVisible,
                "absorbing" => StatusBarService.Instance.SetAbsorbingVisible,
                _ => null
            };
        }

        /// <summary>
        /// 设置展开状态（用于外部控制，不触发保存）
        /// </summary>
        public void SetExpanded(bool expanded, bool animate = true)
        {
            if (_isAnimating) return;

            _isExpanded = expanded;

            if (animate)
            {
                if (_isExpanded)
                    AnimateExpand();
                else
                    AnimateCollapse();

                var arrowAnimation = new DoubleAnimation
                {
                    To = _isExpanded ? 0 : -90,
                    Duration = TimeSpan.FromMilliseconds(_isExpanded ? 200 : 150),
                    EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut }
                };
                ArrowRotate.BeginAnimation(RotateTransform.AngleProperty, arrowAnimation);
            }
            else
            {
                // 不执行动画，直接设置状态
                if (_isExpanded)
                {
                    ContentBorder.Height = double.NaN;
                    ArrowRotate.Angle = 0;
                }
                else
                {
                    ContentBorder.Height = 0;
                    ArrowRotate.Angle = -90;
                }
            }
        }
    }
}