using System.Windows;

namespace CraftSharp.Windows.Dialogs
{
    public partial class HotkeyConflictDialog : Wpf.Ui.Controls.FluentWindow
    {
        public HotkeyConflictDialog(string hotkey)
        {
            InitializeComponent();
            var message = string.Format(
                Application.Current.TryFindResource("HotkeyConflictWarning") as string ?? "",
                hotkey);
            MessageText.Text = message;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
