using System.Windows;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Dialogs
{
    /// <summary>
    /// 重命名格子对话框
    /// </summary>
    public partial class RenameSlotWindow : FluentWindow
    {
        /// <summary>
        /// 是否确认
        /// </summary>
        public bool IsConfirmed { get; private set; } = false;

        /// <summary>
        /// 新的显示名称
        /// </summary>
        public string NewDisplayName { get; private set; } = "";

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="currentDisplayName">当前显示名称</param>
        /// <param name="filePath">文件路径</param>
        public RenameSlotWindow(string currentDisplayName, string filePath)
        {
            InitializeComponent();
            DisplayNameTextBox.Text = currentDisplayName;
            FilePathText.Text = filePath;

            // 聚焦到输入框
            Loaded += (_, _) => DisplayNameTextBox.Focus();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = false;
            Close();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            IsConfirmed = true;
            NewDisplayName = DisplayNameTextBox.Text ?? "";
            Close();
        }
    }
}