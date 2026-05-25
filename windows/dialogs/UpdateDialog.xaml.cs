using System.Windows;
using CraftSharp.Services.Update;

namespace CraftSharp.Windows.Dialogs;

public partial class UpdateDialog : Wpf.Ui.Controls.FluentWindow
{
    public UpdateDialog(string version, string changelog)
    {
        InitializeComponent();
        Title = "Craft#";
        NewVersionText.Text = $"v{version}";
        ChangelogText.Text = string.IsNullOrWhiteSpace(changelog)
            ? Application.Current.FindResource("UpdateDialogNoChangelog") as string ?? ""
            : changelog;
    }

    private void DownloadButton_Click(object sender, RoutedEventArgs e)
    {
        UpdateService.Instance.OpenReleasePage();
        Close();
    }

    private void SkipButton_Click(object sender, RoutedEventArgs e)
    {
        Close();
    }
}
