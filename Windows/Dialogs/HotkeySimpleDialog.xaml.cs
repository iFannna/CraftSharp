using System.Windows;

namespace CraftSharp.Windows.Dialogs
{
    public partial class HotkeySimpleDialog : Wpf.Ui.Controls.FluentWindow
    {
        public bool IsConfirmed { get; private set; }

        public HotkeySimpleDialog(string hotkey)
        {
            InitializeComponent();

            var message = Application.Current.TryFindResource("HotkeySimpleMessage") as string ?? "";
            MessageText.Text = string.Format(message, hotkey);
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
