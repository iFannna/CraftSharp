using System.Windows;

namespace CraftSharp.Windows.Dialogs
{
    public partial class ConfirmDialog : Wpf.Ui.Controls.FluentWindow
    {
        public bool IsConfirmed { get; private set; }

        public ConfirmDialog(string title, string message)
        {
            InitializeComponent();
            Title = title;
            MessageText.Text = message;
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
