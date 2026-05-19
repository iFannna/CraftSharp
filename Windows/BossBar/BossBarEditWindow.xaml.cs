using CraftSharp.Helpers;
using CraftSharp.Models;
using CraftSharp.Services;
using CraftSharp.Windows.Dialogs;
using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Imaging;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.BossBar
{
    /// <summary>
    /// BOSS血条编辑弹窗
    /// </summary>
    public partial class BossBarEditWindow : FluentWindow
    {
        /// <summary>
        /// 编辑后的BOSS血条配置
        /// </summary>
        public BossBarSettings? Result { get; private set; }

        private readonly BossBarSettings? _originalSettings;
        private readonly bool _isNew;
        private string _iconType = "blue";
        private string _notchType = "";

        // 原生拖放目标（支持 Windows 拖拽缩略图）
        private IDisposable? _nativeDropTarget;

        public BossBarEditWindow(BossBarSettings? settings = null)
        {
            InitializeComponent();
            _originalSettings = settings;
            _isNew = settings == null;

            // 设置窗口图标
            IconService.Instance.ApplyWindowIcon(this);

            // 注册原生拖放（仅显示缩略图，不接受文件）
            SourceInitialized += (_, _) =>
            {
                try
                {
                    _nativeDropTarget = NativeDropHelper.RegisterForThumbnail(this);
                }
                catch (Exception)
                {
                    _nativeDropTarget?.Dispose();
                    _nativeDropTarget = null;
                }
            };

            // 窗口关闭时释放资源
            Closed += (_, _) =>
            {
                _nativeDropTarget?.Dispose();
                _nativeDropTarget = null;
            };

            // 初始化UI
            InitializeUI();

            // 绑定事件
            DataMappingToggle.Click += DataMappingToggle_Click;
            CustomValueToggle.Click += CustomValueToggle_Click;

            // 限制数值输入框只能输入数字
            CurrentValueTextBox.PreviewTextInput += (_, e) =>
            {
                e.Handled = !e.Text.All(c => char.IsDigit(c));
            };
            System.Windows.DataObject.AddPastingHandler(CurrentValueTextBox, (_, e) =>
            {
                if (e.DataObject.GetDataPresent(typeof(string)))
                {
                    var text = (string)e.DataObject.GetData(typeof(string));
                    if (!text.All(c => char.IsDigit(c)))
                        e.CancelCommand();
                }
                else
                    e.CancelCommand();
            });
        }

        private void InitializeUI()
        {
            if (_originalSettings != null)
            {
                // 编辑模式：加载现有配置
                _iconType = _originalSettings.IconType;
                _notchType = _originalSettings.NotchType;
                NameTextBox.Text = _originalSettings.Name;
                DataMappingToggle.IsChecked = _originalSettings.DataMappingEnabled;
                CustomValueToggle.IsChecked = _originalSettings.CustomValueEnabled;
                CurrentValueTextBox.Text = _originalSettings.CustomCurrentValue.ToString();

                // 设置数据映射类型下拉框
                SetDataMappingComboBox(_originalSettings.DataMappingType);
            }
            else
            {
                // 新建模式：默认配置（自定义数值100，数据映射关闭）
                _iconType = "blue";
                _notchType = "";
                NameTextBox.Text = null!;
                DataMappingToggle.IsChecked = false;
                CustomValueToggle.IsChecked = true;
                CurrentValueTextBox.Text = "100";
                SetDataMappingComboBox("BatteryLevel");
            }

            // 更新可见性
            UpdateDataMappingVisibility();
            UpdateCustomValueVisibility();

            // 加载图标预览
            LoadIconPreview();
            LoadNotchPreview();
        }

        private void SetDataMappingComboBox(string dataMappingType)
        {
            foreach (ComboBoxItem item in DataMappingComboBox.Items)
            {
                if (item.Tag as string == dataMappingType)
                {
                    DataMappingComboBox.SelectedItem = item;
                    break;
                }
            }
        }

        private void LoadIconPreview()
        {
            var bitmap = ImageService.Instance.LoadBitmapImage(AssetPaths.GetBossBarPath(_iconType, "progress"));
            IconPreviewImage.Source = bitmap;
            // 同时更新等级预览区域的底层元素图标
            NotchIconPreviewImage.Source = bitmap;
        }

        private void LoadNotchPreview()
        {
            if (string.IsNullOrEmpty(_notchType))
            {
                // 无分段样式，只显示元素图标
                NotchNotchPreviewImage.Source = null;
                return;
            }

            var bitmap = ImageService.Instance.LoadBitmapImage(AssetPaths.GetNotchPath(_notchType, "progress"));
            NotchNotchPreviewImage.Source = bitmap;
        }

        private void DataMappingToggle_Click(object sender, RoutedEventArgs e)
        {
            // 开启数据映射时，自动关闭自定义数值
            if (DataMappingToggle.IsChecked == true)
            {
                CustomValueToggle.IsChecked = false;
            }
            UpdateDataMappingVisibility();
            UpdateCustomValueVisibility();
        }

        private void CustomValueToggle_Click(object sender, RoutedEventArgs e)
        {
            // 开启自定义数值时，自动关闭数据映射
            if (CustomValueToggle.IsChecked == true)
            {
                DataMappingToggle.IsChecked = false;
            }
            UpdateDataMappingVisibility();
            UpdateCustomValueVisibility();
        }

        private void UpdateDataMappingVisibility()
        {
            DataMappingComboBox.Visibility = DataMappingToggle.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        private void UpdateCustomValueVisibility()
        {
            CustomValueGrid.Visibility = CustomValueToggle.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
        }

        
        private void IconPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var picker = new HudIconPickerWindow("boss_bar");
            picker.Owner = this;
            if (picker.ShowDialog() == true && picker.SelectedIconStyle != null)
            {
                _iconType = picker.SelectedIconStyle;
                LoadIconPreview();
            }
        }

        private void NotchPreview_Click(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            var picker = new HudIconPickerWindow("boss_bar_notch");
            picker.Owner = this;
            if (picker.ShowDialog() == true && picker.SelectedIconStyle != null)
            {
                _notchType = picker.SelectedIconStyle;
                LoadNotchPreview();
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Result = null;
            Close();
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            // 获取数据映射类型
            string dataMappingType = "BatteryLevel";
            if (DataMappingComboBox.SelectedItem is ComboBoxItem item)
            {
                dataMappingType = item.Tag as string ?? "BatteryLevel";
            }

            // 验证并获取自定义数值
            int customCurrentValue = 100;
            if (CustomValueToggle.IsChecked == true)
            {
                if (!int.TryParse(CurrentValueTextBox.Text, out customCurrentValue))
                {
                    customCurrentValue = 100;
                }
                customCurrentValue = Math.Clamp(customCurrentValue, 0, 100);
                CurrentValueTextBox.Text = customCurrentValue.ToString();
            }

            if (_isNew)
            {
                // 新建
                Result = new BossBarSettings
                {
                    Id = System.Guid.NewGuid().ToString(),
                    Name = NameTextBox.Text,
                    IconType = _iconType,
                    NotchType = _notchType,
                    IsEnabled = true,
                    DataMappingEnabled = DataMappingToggle.IsChecked == true,
                    DataMappingType = dataMappingType,
                    CustomValueEnabled = CustomValueToggle.IsChecked == true,
                    CustomCurrentValue = customCurrentValue,
                    CustomMaxValue = 100
                };
            }
            else
            {
                // 编辑
                Result = new BossBarSettings
                {
                    Id = _originalSettings!.Id,
                    Name = NameTextBox.Text,
                    IconType = _iconType,
                    NotchType = _notchType,
                    IsEnabled = _originalSettings.IsEnabled,
                    DataMappingEnabled = DataMappingToggle.IsChecked == true,
                    DataMappingType = dataMappingType,
                    CustomValueEnabled = CustomValueToggle.IsChecked == true,
                    CustomCurrentValue = customCurrentValue,
                    CustomMaxValue = 100
                };
            }

            Close();
        }
    }
}