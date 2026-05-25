using System.Windows;

namespace CraftSharp.Windows.Skin
{
    public partial class RemoveSkinConfirmWindow : Wpf.Ui.Controls.FluentWindow
    {
        public bool IsConfirmed { get; private set; }

        public RemoveSkinConfirmWindow(string skinName)
        {
            InitializeComponent();
            SkinNameText.Text = skinName;
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
