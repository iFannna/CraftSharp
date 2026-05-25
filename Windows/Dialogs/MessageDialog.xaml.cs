using System.Windows;

namespace CraftSharp.Windows.Dialogs;

public partial class MessageDialog : Wpf.Ui.Controls.FluentWindow
{
    public MessageDialog(string title, string message)
    {
        InitializeComponent();
        Title = title;
        MessageText.Text = message;
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
