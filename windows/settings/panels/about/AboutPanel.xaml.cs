using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using CraftSharp.Helpers;
using CraftSharp.Services.Update;

namespace CraftSharp.Windows.Settings.Panels.About
{
    public partial class AboutPanel : global::System.Windows.Controls.UserControl
    {
        public AboutPanel()
        {
            InitializeComponent();
            Loaded += (_, _) => VersionText.Text = $"v{UpdateService.CurrentVersion}";
        }

        private void GitHubLink_Click(object sender, RoutedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://github.com/iFannna/CraftSharp", UseShellExecute = true }); }
            catch { }
        }

        private void AuthorLink_Click(object sender, RoutedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://github.com/iFannna", UseShellExecute = true }); }
            catch { }
        }

        private void Terms_Click(object sender, RoutedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://github.com/iFannna/CraftSharp/blob/main/TERMS.md", UseShellExecute = true }); }
            catch { }
        }

        private void Privacy_Click(object sender, RoutedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://github.com/iFannna/CraftSharp/blob/main/PRIVACY.md", UseShellExecute = true }); }
            catch { }
        }

        private async void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            var btn = (Wpf.Ui.Controls.Button)sender;
            btn.IsEnabled = false;
            btn.Content = Application.Current.FindResource("AboutCheckingUpdate") ?? "...";

            var release = await UpdateService.Instance.CheckForUpdateAsync();

            btn.IsEnabled = true;
            btn.Content = Application.Current.FindResource("AboutCheckUpdate") ?? "检查更新";

            var owner = Window.GetWindow(this);
            if (release != null)
            {
                var dialog = new Dialogs.UpdateDialog(release.TagName.TrimStart('v', 'V'), release.Body);
                dialog.Owner = owner;
                dialog.ShowDialogQuiet();
            }
            else
            {
                var message = (string)Application.Current.FindResource("AboutCheckUpdateMessage") ?? "当前已是最新版本";
                var title = (string)Application.Current.FindResource("AboutCheckUpdateTitle") ?? "检查更新";
                var dialog = new Dialogs.MessageDialog(title, message);
                dialog.Owner = owner;
                dialog.ShowDialogQuiet();
            }
        }

        private void Feedback_Click(object sender, RoutedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://github.com/iFannna/CraftSharp/issues", UseShellExecute = true }); }
            catch { }
        }
    }
}
