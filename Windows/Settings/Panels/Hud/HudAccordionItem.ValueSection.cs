using System.Windows;
using System.Windows.Controls;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Settings.Panels.Hud
{
    /// <summary>
    /// HudAccordionItem 数据映射和自定义数值配置
    /// </summary>
    public partial class HudAccordionItem
    {
        private void AddDataMappingSection(string id, bool enabled, string mappingType)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            var titleLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("HudOptionDataMapping"),
                FontWeight = FontWeights.Medium
            };
            titleLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextPrimaryBrush");
            var descLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("HudOptionDataMappingDesc"),
                Margin = new Thickness(0, 4, 0, 0)
            };
            descLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextSecondaryBrush");
            left.Children.Add(titleLabel);
            left.Children.Add(descLabel);
            grid.Children.Add(left);

            _mappingToggle = new ToggleSwitch { IsChecked = enabled };
            grid.Children.Add(_mappingToggle);
            Grid.SetColumn(_mappingToggle, 1);

            ContentPanel.Children.Add(grid);

            _mappingComboBox = new System.Windows.Controls.ComboBox
            {
                Width = 160,
                Margin = new Thickness(0, 0, 0, 12),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Left,
                Visibility = enabled ? Visibility.Visible : Visibility.Collapsed
            };

            var types = new[] { "电池电量", "内存占用率", "CPU利用率", "GPU利用率" };
            var typeKeys = new[] { "DataMappingBattery", "DataMappingMemory", "DataMappingCpu", "DataMappingGpu" };

            for (int i = 0; i < types.Length; i++)
            {
                var item = new System.Windows.Controls.ComboBoxItem { Content = GetResourceString(typeKeys[i]), Tag = types[i] };
                _mappingComboBox.Items.Add(item);
                if (mappingType == types[i])
                    _mappingComboBox.SelectedIndex = i;
            }
            if (_mappingComboBox.SelectedIndex < 0)
                _mappingComboBox.SelectedIndex = 0;

            _mappingComboBox.SelectionChanged += (s, e) =>
            {
                if (_mappingComboBox.SelectedItem is System.Windows.Controls.ComboBoxItem item)
                {
                    EnsureHudElementExists(id);
                    var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                    if (elem != null)
                    {
                        elem.DataMappingType = item.Tag?.ToString() ?? "电池电量";
                        SaveSettings();
                    }
                }
            };

            ContentPanel.Children.Add(_mappingComboBox);

            _mappingToggle.Checked += (s, e) =>
            {
                EnsureHudElementExists(id);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                if (elem != null)
                {
                    elem.DataMappingEnabled = true;
                    elem.CustomValueEnabled = false;
                }
                _mappingComboBox.Visibility = Visibility.Visible;

                if (_customToggle != null)
                {
                    _customToggle.IsChecked = false;
                }
                if (_valueContainer != null)
                {
                    _valueContainer.Visibility = Visibility.Collapsed;
                }

                SaveSettings();
                RefreshHudElement(id);
                RefreshContentHeight();
            };
            _mappingToggle.Unchecked += (s, e) =>
            {
                EnsureHudElementExists(id);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                if (elem != null) elem.DataMappingEnabled = false;
                _mappingComboBox.Visibility = Visibility.Collapsed;
                SaveSettings();
                RefreshHudElement(id);
                RefreshContentHeight();
            };
        }

        private void AddCustomValueSection(string id, bool enabled, int currentValue, int maxValue, bool hasMaxValue = true, int maxValueLimit = 20, bool hasSaturation = false, int saturationValue = 0)
        {
            var grid = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var left = new StackPanel();
            var titleLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("HudOptionCustomValue"),
                FontWeight = FontWeights.Medium
            };
            titleLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextPrimaryBrush");
            var descLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("HudOptionCustomValueDesc"),
                Margin = new Thickness(0, 4, 0, 0)
            };
            descLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextSecondaryBrush");
            left.Children.Add(titleLabel);
            left.Children.Add(descLabel);
            grid.Children.Add(left);

            _customToggle = new ToggleSwitch { IsChecked = enabled };
            grid.Children.Add(_customToggle);
            Grid.SetColumn(_customToggle, 1);

            ContentPanel.Children.Add(grid);

            _valueContainer = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Vertical,
                Visibility = enabled ? Visibility.Visible : Visibility.Collapsed
            };

            AddCurrentValueRow(id, currentValue, maxValue, hasMaxValue);
            if (hasMaxValue)
            {
                AddMaxValueRow(id, maxValue, maxValueLimit);
            }
            if (hasSaturation)
            {
                AddSaturationRow(id, saturationValue, maxValue);
            }

            ContentPanel.Children.Add(_valueContainer);

            _customToggle.Checked += (s, e) =>
            {
                EnsureHudElementExists(id);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                if (elem != null)
                {
                    elem.CustomValueEnabled = true;
                    elem.DataMappingEnabled = false;
                }
                _valueContainer.Visibility = Visibility.Visible;

                if (_mappingToggle != null)
                {
                    _mappingToggle.IsChecked = false;
                }
                if (_mappingComboBox != null)
                {
                    _mappingComboBox.Visibility = Visibility.Collapsed;
                }

                SaveSettings();
                RefreshHudElement(id);
                RefreshContentHeight();
            };
            _customToggle.Unchecked += (s, e) =>
            {
                EnsureHudElementExists(id);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                if (elem != null) elem.CustomValueEnabled = false;
                _valueContainer.Visibility = Visibility.Collapsed;
                SaveSettings();
                RefreshHudElement(id);
                RefreshContentHeight();
            };
        }

        private void AddCurrentValueRow(string id, int currentValue, int maxValue, bool hasMaxValue)
        {
            var currentRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            currentRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            currentRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            currentRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            currentRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var currentLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("CustomCurrentValue") + ":",
                VerticalAlignment = VerticalAlignment.Center
            };
            currentLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextSecondaryBrush");
            currentRow.Children.Add(currentLabel);

            var inputContainer = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(8, 0, 0, 0)
            };

            _currentValueTextBox = new System.Windows.Controls.TextBox
            {
                Text = currentValue.ToString(),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            _currentValueTextBox.PreviewTextInput += (s, e) =>
            {
                e.Handled = !e.Text.All(c => char.IsDigit(c));
            };
            System.Windows.DataObject.AddPastingHandler(_currentValueTextBox, (s, e) =>
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
            inputContainer.Children.Add(_currentValueTextBox);

            _maxValueDisplay = new System.Windows.Controls.TextBlock
            {
                Text = "/" + (hasMaxValue ? maxValue : 100),
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            _maxValueDisplay.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextSecondaryBrush");
            inputContainer.Children.Add(_maxValueDisplay);

            currentRow.Children.Add(inputContainer);
            Grid.SetColumn(inputContainer, 1);

            _currentValueTextBox.LostFocus += (s, e) =>
            {
                EnsureHudElementExists(id);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                if (elem != null)
                {
                    int maxVal = hasMaxValue ? elem.CustomMaxValue : 100;
                    int val;
                    if (!int.TryParse(_currentValueTextBox.Text, out val) || _currentValueTextBox.Text.Length == 0)
                        val = maxVal;
                    if (val < 0) val = 0;
                    if (val > maxVal) val = maxVal;
                    elem.CustomCurrentValue = val;
                    _currentValueTextBox.Text = val.ToString();
                    SaveSettings();
                    RefreshHudElement(id);
                }
            };
            _valueContainer.Children.Add(currentRow);
        }

        private void AddMaxValueRow(string id, int maxValue, int maxValueLimit)
        {
            var maxRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            maxRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            maxRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            maxRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var maxLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("CustomMaxValue") + ":",
                VerticalAlignment = VerticalAlignment.Center
            };
            maxLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextSecondaryBrush");
            maxRow.Children.Add(maxLabel);

            var maxInputContainer = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(8, 0, 0, 0)
            };

            _maxValueTextBox = new System.Windows.Controls.TextBox
            {
                Text = maxValue.ToString(),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            _maxValueTextBox.PreviewTextInput += (s, e) =>
            {
                e.Handled = !e.Text.All(c => char.IsDigit(c));
            };
            System.Windows.DataObject.AddPastingHandler(_maxValueTextBox, (s, e) =>
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
            maxInputContainer.Children.Add(_maxValueTextBox);

            var maxValueLimitDisplay = new System.Windows.Controls.TextBlock
            {
                Text = "/" + maxValueLimit,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            maxValueLimitDisplay.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextSecondaryBrush");
            maxInputContainer.Children.Add(maxValueLimitDisplay);

            maxRow.Children.Add(maxInputContainer);
            Grid.SetColumn(maxInputContainer, 1);
            _valueContainer.Children.Add(maxRow);

            _maxValueTextBox.LostFocus += (s, e) =>
            {
                EnsureHudElementExists(id);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                if (elem != null)
                {
                    int val;
                    if (!int.TryParse(_maxValueTextBox.Text, out val) || _maxValueTextBox.Text.Length == 0)
                        val = maxValueLimit;
                    if (val > maxValueLimit) val = maxValueLimit;
                    val = Math.Max(2, (val / 2) * 2);
                    elem.CustomMaxValue = val;

                    if (elem.CustomCurrentValue > val)
                    {
                        elem.CustomCurrentValue = val;
                        if (_currentValueTextBox != null)
                        {
                            _currentValueTextBox.Text = val.ToString();
                        }
                    }

                    _maxValueTextBox.Text = val.ToString();

                    if (_maxValueDisplay != null)
                    {
                        _maxValueDisplay.Text = "/" + val;
                    }

                    if (_saturationLimitDisplay != null)
                    {
                        _saturationLimitDisplay.Text = "/" + val;
                    }
                    if (elem.CustomSaturationValue > val)
                    {
                        elem.CustomSaturationValue = val;
                        if (_saturationTextBox != null)
                        {
                            _saturationTextBox.Text = val.ToString();
                        }
                    }

                    SaveSettings();
                    RefreshHudElement(id);
                }
            };
        }

        private void AddSaturationRow(string id, int saturationValue, int maxValue)
        {
            var saturationRow = new Grid { Margin = new Thickness(0, 0, 0, 8) };
            saturationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            saturationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Auto) });
            saturationRow.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var saturationLabel = new System.Windows.Controls.TextBlock
            {
                Text = GetResourceString("CustomSaturationValue") + ":",
                VerticalAlignment = VerticalAlignment.Center
            };
            saturationLabel.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextSecondaryBrush");
            saturationRow.Children.Add(saturationLabel);

            var saturationInputContainer = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Horizontal,
                Margin = new Thickness(8, 0, 0, 0)
            };

            _saturationTextBox = new System.Windows.Controls.TextBox
            {
                Text = saturationValue.ToString(),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalContentAlignment = VerticalAlignment.Center
            };
            _saturationTextBox.PreviewTextInput += (s, e) =>
            {
                e.Handled = !e.Text.All(c => char.IsDigit(c));
            };
            System.Windows.DataObject.AddPastingHandler(_saturationTextBox, (s, e) =>
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
            saturationInputContainer.Children.Add(_saturationTextBox);

            _saturationLimitDisplay = new System.Windows.Controls.TextBlock
            {
                Text = "/" + maxValue,
                VerticalAlignment = VerticalAlignment.Center,
                Margin = new Thickness(4, 0, 0, 0)
            };
            _saturationLimitDisplay.SetResourceReference(System.Windows.Controls.TextBlock.ForegroundProperty, "TextSecondaryBrush");
            saturationInputContainer.Children.Add(_saturationLimitDisplay);

            saturationRow.Children.Add(saturationInputContainer);
            Grid.SetColumn(saturationInputContainer, 1);
            _valueContainer.Children.Add(saturationRow);

            _saturationTextBox.LostFocus += (s, e) =>
            {
                EnsureHudElementExists(id);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                if (elem != null)
                {
                    int maxVal = elem.CustomMaxValue;
                    int val;
                    if (!int.TryParse(_saturationTextBox.Text, out val) || _saturationTextBox.Text.Length == 0)
                        val = 0;
                    if (val < 0) val = 0;
                    if (val > maxVal) val = maxVal;
                    elem.CustomSaturationValue = val;
                    _saturationTextBox.Text = val.ToString();
                    SaveSettings();
                    RefreshHudElement(id);
                }
            };
        }
    }
}