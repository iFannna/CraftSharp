using System.Text.RegularExpressions;
using System.Windows;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Skin
{
    public partial class RenameSkinWindow : FluentWindow
    {
        private static readonly Regex ValidNameRegex = new(@"^[a-zA-Z0-9_\-\s]+$", RegexOptions.Compiled);

        public bool IsConfirmed { get; private set; }
        public string? NewName { get; private set; }

        public RenameSkinWindow(string currentName)
        {
            InitializeComponent();
            NameTextBox.Text = currentName;
            NameTextBox.SelectAll();
        }

        private void NameTextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text?.Trim() ?? "";

            if (string.IsNullOrEmpty(name))
            {
                ValidationErrorText.Text = TryFindResource("RenameSkinNameEmpty") as string ?? "名称不能为空";
                ValidationErrorText.Visibility = Visibility.Visible;
                ConfirmButton.IsEnabled = false;
            }
            else if (!ValidNameRegex.IsMatch(name))
            {
                ValidationErrorText.Text = TryFindResource("RenameSkinInvalidChars") as string ?? "名称只能包含字母、数字、下划线、横杠和空格";
                ValidationErrorText.Visibility = Visibility.Visible;
                ConfirmButton.IsEnabled = false;
            }
            else if (name.Length > 32)
            {
                ValidationErrorText.Text = TryFindResource("RenameSkinNameTooLong") as string ?? "名称长度不能超过32个字符";
                ValidationErrorText.Visibility = Visibility.Visible;
                ConfirmButton.IsEnabled = false;
            }
            else
            {
                ValidationErrorText.Visibility = Visibility.Collapsed;
                ConfirmButton.IsEnabled = true;
            }
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text?.Trim() ?? "";
            if (string.IsNullOrEmpty(name) || !ValidNameRegex.IsMatch(name) || name.Length > 32)
                return;

            NewName = name;
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
