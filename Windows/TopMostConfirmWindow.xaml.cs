using System.Windows;

namespace CraftSharp.Windows
{
    /// <summary>
    /// 窗口置顶确认弹窗
    /// </summary>
    public partial class TopMostConfirmWindow : Wpf.Ui.Controls.FluentWindow
    {
        /// <summary>
        /// 用户是否确认
        /// </summary>
        public bool IsConfirmed { get; private set; } = false;

        public TopMostConfirmWindow()
        {
            InitializeComponent();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            Close();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            Close();
        }
    }
}