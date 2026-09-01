using System.ComponentModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace CraftSharp.Helpers
{
    /// <summary>
    /// WPF-UI 标题栏的最小化/最大化/还原/关闭按钮只有图标没有文本，读屏无法朗读。
    /// 按模板部件名（PART_MinimizeButton/MaximizeButton/CloseButton/Restore...）分类补自动化名称，
    /// 覆盖当前及未来所有窗口。主题切换会重建模板按钮，因此随 Loaded/键盘焦点进入时重新应用。
    /// </summary>
    public static class TitleBarAutomation
    {
        private static readonly HashSet<TitleBar> _maximizeHooked = new();
        private static readonly HashSet<System.Windows.Controls.Button> _focusVisualHooked = new();

        /// <summary>FocusVisualStyle 无法在模板触发器里按条件绘制，改为每次获得键盘焦点时按输入来源切换。</summary>
        private static void HookFocusVisual(System.Windows.Controls.Button button)
        {
            if (!_focusVisualHooked.Add(button)) return;

            button.FocusVisualStyle = null;
            button.GotKeyboardFocus += (_, _) =>
                button.FocusVisualStyle = KeyboardAccessibility.LastInputWasKeyboard
                    ? Application.Current.TryFindResource("CaptionButtonFocusVisualStyle") as Style
                    : null;
            button.Unloaded += (_, _) => _focusVisualHooked.Remove(button);
        }

        public static void Attach()
        {
            EventManager.RegisterClassHandler(typeof(Window), FrameworkElement.LoadedEvent,
                new RoutedEventHandler((sender, _) => Apply(sender as Window)));
            EventManager.RegisterClassHandler(typeof(Window), UIElement.GotKeyboardFocusEvent,
                new RoutedEventHandler((sender, _) => Apply(sender as Window)));
        }

        internal static void Apply(Window? window)
        {
            if (window == null) return;

            foreach (var titleBar in FindDescendants<TitleBar>(window))
            {
                HookMaximizeState(titleBar, window);
                foreach (var button in FindDescendants<System.Windows.Controls.Button>(titleBar))
                {
                    string? resourceKey = null;
                    var name = button.Name;
                    if (name.Contains("Minimize")) resourceKey = "WindowButtonMinimize";
                    else if (name.Contains("Maximize")) resourceKey =
                        titleBar.IsMaximized ? "WindowButtonRestore" : "WindowButtonMaximize";
                    else if (name.Contains("Restore")) resourceKey = "WindowButtonRestore";
                    else if (name.Contains("Close")) resourceKey = "WindowButtonClose";
                    if (resourceKey == null) continue;

                    // WPF-UI 模板按钮默认不可键盘聚焦（TAB 不可达），重新启用；
                    // 虚线描边按输入来源门控：键盘流程才显示，鼠标点击/弹窗还原不残留
                    button.Focusable = true;
                    button.IsTabStop = true;
                    HookFocusVisual(button);
                    SetName(button, resourceKey);
                }
            }
        }

        /// <summary>模板只有一个 MaximizeButton（图标随窗口状态切换），最大化后名称须改为"还原"。</summary>
        private static void HookMaximizeState(TitleBar titleBar, Window window)
        {
            if (!_maximizeHooked.Add(titleBar)) return;

            var descriptor = DependencyPropertyDescriptor.FromProperty(
                TitleBar.IsMaximizedProperty, typeof(TitleBar));
            EventHandler handler = (_, _) => Apply(window);
            descriptor.AddValueChanged(titleBar, handler);
            titleBar.Unloaded += (_, _) =>
            {
                descriptor.RemoveValueChanged(titleBar, handler);
                _maximizeHooked.Remove(titleBar);
            };
        }

        private static void SetName(System.Windows.Controls.Button button, string resourceKey)
        {
            AutomationProperties.SetName(button,
                Application.Current.TryFindResource(resourceKey) as string ?? resourceKey);
        }

        private static IEnumerable<T> FindDescendants<T>(DependencyObject root) where T : DependencyObject
        {
            int count = VisualTreeHelper.GetChildrenCount(root);
            for (int i = 0; i < count; i++)
            {
                var child = VisualTreeHelper.GetChild(root, i);
                if (child is T matched) yield return matched;
                foreach (var descendant in FindDescendants<T>(child))
                    yield return descendant;
            }
        }
    }
}
