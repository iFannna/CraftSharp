using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace CraftSharp.Helpers
{
    /// <summary>
    /// 键盘交互无障碍（全局类处理器）：
    /// - ComboBox 聚焦时 Enter/空格 展开下拉列表（WPF 原生仅 F4/Alt+下方向键，读屏用户无从得知选项）
    /// - ToggleButton 聚焦时 Enter 触发切换（原生仅空格；RadioButton 除外，避免破坏单选语义）
    /// - 仿真 Win32 键盘提示状态：WPF 里鼠标点击也赋键盘焦点，且 IsKeyboardFocused 触发器无从区分来源，
    ///   导致纯鼠标操作残留键盘焦点视觉。此处跟踪最近输入设备，仅当焦点经由键盘获得时置 ShowFocusVisual，
    ///   样式触发器据此绘制高亮（弹窗关闭后的焦点还原同样按当时的输入来源判定）
    /// </summary>
    public static class KeyboardAccessibility
    {
        /// <summary>最近一次输入是否来自键盘；仅键盘流程显示键盘焦点视觉。</summary>
        internal static bool LastInputWasKeyboard { get; private set; }

        public static void Attach()
        {
            EventManager.RegisterClassHandler(typeof(System.Windows.Controls.ComboBox),
                UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnComboBoxPreviewKeyDown));
            EventManager.RegisterClassHandler(typeof(ToggleButton),
                UIElement.PreviewKeyDownEvent, new KeyEventHandler(OnToggleButtonPreviewKeyDown));

            InputManager.Current.PreProcessInput += OnPreProcessInput;
            EventManager.RegisterClassHandler(typeof(UIElement), Keyboard.GotKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler(OnGotKeyboardFocus));
            EventManager.RegisterClassHandler(typeof(UIElement), Keyboard.LostKeyboardFocusEvent,
                new KeyboardFocusChangedEventHandler(OnLostKeyboardFocus));
        }

        private static void OnComboBoxPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (sender is not System.Windows.Controls.ComboBox comboBox) return;
            if (comboBox.IsEditable || comboBox.IsDropDownOpen) return;

            if (e.Key is Key.Enter or Key.Space)
            {
                comboBox.IsDropDownOpen = true;
                e.Handled = true;
            }
        }

        private static void OnToggleButtonPreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter) return;
            if (sender is not ToggleButton toggle || toggle is RadioButton) return;
            if (!toggle.IsKeyboardFocused) return;

            // 复刻原生空格的完整序列：先翻转 IsChecked（触发箭头等模板触发器），再路由 Click；
            // e.Handled 阻止后续原生处理，保证只触发一次
            toggle.IsChecked = !(toggle.IsChecked == true);
            toggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, toggle));
            e.Handled = true;
        }

        private static void OnPreProcessInput(object sender, PreProcessInputEventArgs e)
        {
            // 只看按键与鼠标按下/抬起，忽略鼠标移动：Tab 后移动鼠标不应熄灭已显示的焦点高亮
            switch (e.StagingItem?.Input)
            {
                case KeyEventArgs:
                    LastInputWasKeyboard = true;
                    break;
                case MouseButtonEventArgs:
                    LastInputWasKeyboard = false;
                    break;
            }
        }

        private static void OnGotKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.OldFocus is DependencyObject oldFocus)
                SetShowFocusVisual(oldFocus, false);
            if (e.NewFocus is DependencyObject newFocus)
                SetShowFocusVisual(newFocus, LastInputWasKeyboard);
        }

        private static void OnLostKeyboardFocus(object sender, KeyboardFocusChangedEventArgs e)
        {
            if (e.OldFocus is DependencyObject oldFocus)
                SetShowFocusVisual(oldFocus, false);
        }

        public static readonly DependencyProperty ShowFocusVisualProperty = DependencyProperty.RegisterAttached(
            "ShowFocusVisual", typeof(bool), typeof(KeyboardAccessibility), new PropertyMetadata(false));

        public static bool GetShowFocusVisual(DependencyObject obj)
            => (bool)obj.GetValue(ShowFocusVisualProperty);

        public static void SetShowFocusVisual(DependencyObject obj, bool value)
            => obj.SetValue(ShowFocusVisualProperty, value);
    }
}
