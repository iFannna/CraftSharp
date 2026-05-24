using System.Windows;

namespace CraftSharp.Windows.Dialogs
{
    public partial class HotkeyConflictDialog : Wpf.Ui.Controls.FluentWindow
    {
        public bool IsConfirmed { get; private set; }

        public HotkeyConflictDialog(string hotkey, string conflictFuncName)
        {
            InitializeComponent();

            var warning = Application.Current.TryFindResource("HotkeyConflictWarning") as string ?? "";
            MessageText.Text = string.Format(warning, hotkey, conflictFuncName);
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            Close();
        }
    }
}
