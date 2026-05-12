using System.Windows;

namespace CraftSharp.Windows.Dialogs
{
    /// <summary>
    /// 格子文件丢失确认弹窗
    /// </summary>
    public partial class SlotMissingConfirmWindow : Wpf.Ui.Controls.FluentWindow
    {
        /// <summary>
        /// 用户是否确认移除
        /// </summary>
        public bool IsConfirmed { get; private set; } = false;

        /// <summary>
        /// 设置显示的文件路径
        /// </summary>
        public void SetFilePath(string filePath)
        {
            FilePathText.Text = filePath;
        }

        public SlotMissingConfirmWindow()
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