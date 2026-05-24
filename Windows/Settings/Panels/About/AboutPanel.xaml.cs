using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CraftSharp.Windows.Settings.Panels.About
{
    public partial class AboutPanel : global::System.Windows.Controls.UserControl
    {
        public AboutPanel()
        {
            InitializeComponent();
        }

        private void GitHubLink_Click(object sender, MouseButtonEventArgs e)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://github.com", UseShellExecute = true }); }
            catch { }
        }

        private void CheckUpdate_Click(object sender, RoutedEventArgs e)
        {
            var message = (string)Application.Current.TryFindResource("AboutCheckUpdateMessage") ?? "当前已是最新版本";
            var title = (string)Application.Current.TryFindResource("AboutCheckUpdateTitle") ?? "检查更新";
            global::System.Windows.MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Feedback_Click(object sender, RoutedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://github.com/issues", UseShellExecute = true }); }
            catch { }
        }
    }
}