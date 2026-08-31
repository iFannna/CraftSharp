using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Microsoft.Win32;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Skin
{
    public partial class UploadSkinWindow : FluentWindow
    {
        private string? _selectedFilePath;
        private bool _isWide = true;

        private static readonly string WideSkinFolder = "assets/minecraft/textures/entity/player/wide";
        private static readonly string SlimSkinFolder = "assets/minecraft/textures/entity/player/slim";

        // 允许的字符：字母、数字、下划线、横杠、空格
        private static readonly Regex ValidNameRegex = new(@"^[a-zA-Z0-9_\-\s]+$", RegexOptions.Compiled);

        public string? ResultSkinName { get; private set; }
        public string? ResultSkinPath { get; private set; }
        public bool ResultIsWide { get; private set; }

        public UploadSkinWindow()
        {
            InitializeComponent();
            RadioSteve.Checked += RadioSteve_Checked;
            RadioAlex.Checked += RadioAlex_Checked;

            // 弹窗中模型显示更小
            SkinPreview.SetCameraWidth(35);
        }

        private void NameTextBox_TextChanged(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text;

            if (string.IsNullOrEmpty(name))
            {
                ShowValidationError("");
                return;
            }

            // 长度检查（32字节）
            if (name.Length > 32)
            {
                ShowValidationError(FindResource("UploadSkinNameTooLong") as string ?? "名称长度不能超过32个字符");
                return;
            }

            // 字符检查
            if (!ValidNameRegex.IsMatch(name))
            {
                ShowValidationError(FindResource("UploadSkinNameInvalidChars") as string ?? "名称只能包含字母、数字、下划线、横杠和空格");
                return;
            }

            // 验证通过
            HideValidationError();
        }

        private void ShowValidationError(string message)
        {
            ValidationErrorText.Text = message;
            ValidationErrorText.Visibility = string.IsNullOrEmpty(message) ? Visibility.Collapsed : Visibility.Visible;
        }

        private void HideValidationError()
        {
            ValidationErrorText.Visibility = Visibility.Collapsed;
        }

        private void SelectFileButton_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new OpenFileDialog
            {
                Title = FindResource("UploadSkinTitle") as string ?? "选择皮肤文件",
                Filter = FindResource("PngFileFilter") as string ?? "PNG 文件 (*.png)|*.png",
                CheckFileExists = true
            };

            if (dialog.ShowDialog(this) == true)
            {
                _selectedFilePath = dialog.FileName;
                FileNameText.Text = dialog.FileName;

                // 立即预览
                SkinPreview.LoadSkin(_selectedFilePath, _isWide);
            }
        }

        private void RadioSteve_Checked(object sender, RoutedEventArgs e)
        {
            _isWide = true;
            if (_selectedFilePath != null)
            {
                SkinPreview.LoadSkin(_selectedFilePath, _isWide);
            }
        }

        private void RadioAlex_Checked(object sender, RoutedEventArgs e)
        {
            _isWide = false;
            if (_selectedFilePath != null)
            {
                SkinPreview.LoadSkin(_selectedFilePath, _isWide);
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            var name = NameTextBox.Text.Trim();

            // 验证名称
            if (string.IsNullOrEmpty(name))
            {
                ShowValidationError(FindResource("UploadSkinNameRequired") as string ?? "请输入材质名称");
                return;
            }

            if (name.Length > 32 || !ValidNameRegex.IsMatch(name))
            {
                return; // 已在 TextChanged 中显示错误
            }

            // 验证文件
            if (_selectedFilePath == null)
            {
                System.Windows.MessageBox.Show(
                    FindResource("UploadSkinNoFile") as string ?? "请先选择一个皮肤文件",
                    FindResource("UploadSkinTitle") as string ?? "上传材质",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // 确定目标目录（上传到 skins 子目录）
            var basePath = AppDomain.CurrentDomain.BaseDirectory;
            var baseFolder = _isWide ? WideSkinFolder : SlimSkinFolder;
            var targetFolder = Path.Combine(baseFolder, "skins");
            var targetPath = Path.Combine(basePath, targetFolder, name + ".png");

            // 检查重名冲突
            if (File.Exists(targetPath))
            {
                var conflictMsg = FindResource("UploadSkinNameConflict") as string ?? "已存在同名皮肤 \"{0}\"，请使用其他名称";
                System.Windows.MessageBox.Show(
                    string.Format(conflictMsg, name),
                    FindResource("UploadSkinTitle") as string ?? "上传材质",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
                return;
            }

            // 复制文件
            try
            {
                // 确保目标目录存在
                Directory.CreateDirectory(Path.GetDirectoryName(targetPath)!);
                File.Copy(_selectedFilePath, targetPath);

                // 设置结果
                ResultSkinName = name;
                ResultSkinPath = targetPath;
                ResultIsWide = _isWide;

                DialogResult = true;
                Close();
            }
            catch (Exception ex)
            {
                var failedMsg = FindResource("UploadSkinCopyFailed") as string ?? "复制文件失败：{0}";
                System.Windows.MessageBox.Show(
                    string.Format(failedMsg, ex.Message),
                    FindResource("UploadSkinTitle") as string ?? "上传材质",
                    System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Error);
            }
        }
    }
}