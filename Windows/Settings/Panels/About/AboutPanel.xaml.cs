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
            global::System.Windows.MessageBox.Show("当前已是最新版本", "检查更新", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void Feedback_Click(object sender, RoutedEventArgs e)
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo { FileName = "https://github.com/issues", UseShellExecute = true }); }
            catch { }
        }
    }
}