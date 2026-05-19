using CraftSharp.Models;
using CraftSharp.Windows.BossBar;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CraftSharp.Windows.Settings.Panels.Hud
{
    /// <summary>
    /// HudAccordionItem BOSS血条配置
    /// </summary>
    public partial class HudAccordionItem
    {
        private StackPanel? _bossBarItemsContainer;

        private void AddBossBarContent()
        {
            _bossBarItemsContainer = new StackPanel
            {
                Orientation = System.Windows.Controls.Orientation.Vertical
            };
            ContentPanel.Children.Add(_bossBarItemsContainer);

            var addIcon = new System.Windows.Shapes.Path
            {
                Width = 16,
                Height = 16,
                Stretch = Stretch.Uniform,
                HorizontalAlignment = System.Windows.HorizontalAlignment.Center,
                VerticalAlignment = System.Windows.VerticalAlignment.Center
            };
            addIcon.SetResourceReference(System.Windows.Shapes.Path.FillProperty, "TextPrimaryBrush");
            addIcon.Data = Geometry.Parse("M7 0 L7 7 L0 7 L0 9 L7 9 L7 16 L9 16 L9 9 L16 9 L16 7 L9 7 L9 0 Z");

            var addButton = new System.Windows.Controls.Button
            {
                Content = addIcon,
                Height = 40,
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = System.Windows.HorizontalAlignment.Stretch
            };
            addButton.Click += AddBossBarButton_Click;
            ContentPanel.Children.Add(addButton);

            foreach (var bossBar in _settings.BossBars)
            {
                var itemControl = new BossBarItemControl(bossBar);
                itemControl.EditRequested += BossBarItem_EditRequested;
                itemControl.DeleteRequested += BossBarItem_DeleteRequested;
                itemControl.EnableChanged += BossBarItem_EnableChanged;
                itemControl.Dragging += BossBarItem_Dragging;
                itemControl.Dropped += BossBarItem_Dropped;
                _bossBarItemsContainer.Children.Add(itemControl);
            }

            _settings.BossBars.CollectionChanged += BossBars_CollectionChanged;
        }

        private void AddBossBarButton_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new BossBarEditWindow(null);
            editWindow.Owner = System.Windows.Window.GetWindow(this);
            editWindow.ShowDialog();
            if (editWindow.Result != null)
            {
                _settings.BossBars.Add(editWindow.Result);
                SaveSettings();
            }
        }

        private void BossBarItem_EditRequested(object? sender, BossBarSettings settings)
        {
            var editWindow = new BossBarEditWindow(settings);
            editWindow.Owner = System.Windows.Window.GetWindow(this);
            editWindow.ShowDialog();
            if (editWindow.Result != null)
            {
                var existing = _settings.BossBars.FirstOrDefault(b => b.Id == settings.Id);
                if (existing != null)
                {
                    existing.Name = editWindow.Result.Name;
                    existing.IconType = editWindow.Result.IconType;
                    existing.NotchType = editWindow.Result.NotchType;
                    existing.DataMappingEnabled = editWindow.Result.DataMappingEnabled;
                    existing.DataMappingType = editWindow.Result.DataMappingType;
                    existing.CustomValueEnabled = editWindow.Result.CustomValueEnabled;
                    existing.CustomCurrentValue = editWindow.Result.CustomCurrentValue;
                    SaveSettings();

                    if (sender is BossBarItemControl control)
                    {
                        control.UpdateSettings(existing);
                    }
                }
            }
        }

        private void BossBarItem_DeleteRequested(object? sender, BossBarSettings settings)
        {
            _settings.BossBars.Remove(settings);
            SaveSettings();
        }

        private void BossBarItem_EnableChanged(object? sender, BossBarSettings settings)
        {
            SaveSettings();
        }

        private int _draggedOriginalIndex = -1;
        private double _itemHeight = 0;

        private void BossBarItem_Dragging(object? sender, BossBarDragEventArgs e)
        {
            if (_bossBarItemsContainer == null) return;

            var draggedItem = e.DraggedItem;
            int currentIndex = _bossBarItemsContainer.Children.IndexOf(draggedItem);

            if (_draggedOriginalIndex < 0)
            {
                _draggedOriginalIndex = currentIndex;
                _itemHeight = draggedItem.ActualHeight;
            }

            double draggedVisualY = currentIndex * (_itemHeight + 8) + e.VisualOffset;

            int targetIndex = (int)Math.Round(draggedVisualY / (_itemHeight + 8));
            targetIndex = Math.Clamp(targetIndex, 0, _bossBarItemsContainer.Children.Count - 1);

            for (int i = 0; i < _bossBarItemsContainer.Children.Count; i++)
            {
                if (_bossBarItemsContainer.Children[i] is BossBarItemControl item && item != draggedItem)
                {
                    double offset = 0;

                    if (targetIndex > _draggedOriginalIndex)
                    {
                        if (i > _draggedOriginalIndex && i <= targetIndex)
                        {
                            offset = -(_itemHeight + 8);
                        }
                    }
                    else if (targetIndex < _draggedOriginalIndex)
                    {
                        if (i >= targetIndex && i < _draggedOriginalIndex)
                        {
                            offset = (_itemHeight + 8);
                        }
                    }

                    item.SetShiftOffset(offset);
                }
            }
        }

        private void BossBarItem_Dropped(object? sender, BossBarDropEventArgs e)
        {
            if (_bossBarItemsContainer == null) return;

            var draggedItem = e.DraggedItem;
            int currentIndex = _bossBarItemsContainer.Children.IndexOf(draggedItem);

            double currentOffset = draggedItem.TranslateTransform.Y;
            int targetIndex = currentIndex + (int)Math.Round(currentOffset / (_itemHeight + 8));
            targetIndex = Math.Clamp(targetIndex, 0, _bossBarItemsContainer.Children.Count - 1);

            if (currentIndex != targetIndex)
            {
                _settings.BossBars.Move(currentIndex, targetIndex);
                SaveSettings();
            }

            for (int i = 0; i < _bossBarItemsContainer.Children.Count; i++)
            {
                if (_bossBarItemsContainer.Children[i] is BossBarItemControl item)
                {
                    item.FinalizePosition();
                }
            }

            _bossBarItemsContainer.Children.Remove(draggedItem);
            _bossBarItemsContainer.Children.Insert(targetIndex, draggedItem);

            _draggedOriginalIndex = -1;
            _itemHeight = 0;
        }

        private void BossBars_CollectionChanged(object? sender, System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (_bossBarItemsContainer == null) return;

            if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Add && e.NewItems != null)
            {
                foreach (BossBarSettings newItem in e.NewItems)
                {
                    var itemControl = new BossBarItemControl(newItem);
                    itemControl.EditRequested += BossBarItem_EditRequested;
                    itemControl.DeleteRequested += BossBarItem_DeleteRequested;
                    itemControl.EnableChanged += BossBarItem_EnableChanged;
                    itemControl.Dragging += BossBarItem_Dragging;
                    itemControl.Dropped += BossBarItem_Dropped;
                    _bossBarItemsContainer.Children.Add(itemControl);
                }
                RefreshContentHeight();
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Remove && e.OldItems != null)
            {
                foreach (BossBarSettings oldItem in e.OldItems)
                {
                    foreach (var child in _bossBarItemsContainer.Children)
                    {
                        if (child is BossBarItemControl control && control.Settings.Id == oldItem.Id)
                        {
                            _bossBarItemsContainer.Children.Remove(control);
                            break;
                        }
                    }
                }
                RefreshContentHeight();
            }
            else if (e.Action == System.Collections.Specialized.NotifyCollectionChangedAction.Reset)
            {
                _bossBarItemsContainer.Children.Clear();
                RefreshContentHeight();
            }
        }
    }
}