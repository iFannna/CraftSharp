using CraftSharp.Helpers;
using CraftSharp.Services;
using CraftSharp.Windows.Dialogs;
using System.Windows;
using Wpf.Ui.Controls;

namespace CraftSharp.Windows.Settings.Panels
{
    /// <summary>
    /// HudAccordionItem 准星和攻击指示器配置
    /// </summary>
    public partial class HudAccordionItem
    {
        private void AddCrosshairContent()
        {
            EnsureHudElementExists("crosshair");
            var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "crosshair");
            bool isVisible = settings?.IsVisible ?? false;
            bool topMost = settings?.TopMost ?? false;

            AddIconPreviewRow("HudOptionElementIcon", AssetPaths.Crosshair);

            var showToggle = AddToggleRow("HudOptionShowElement", "HudOptionShowElementDesc", isVisible);
            showToggle.Checked += (s, e) =>
            {
                EnsureHudElementExists("crosshair");
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == "crosshair");
                if (elem != null) elem.IsVisible = true;
                CrosshairService.Instance.SetCrosshairVisible(true);
                SaveSettings();
            };
            showToggle.Unchecked += (s, e) =>
            {
                EnsureHudElementExists("crosshair");
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == "crosshair");
                if (elem != null) elem.IsVisible = false;
                CrosshairService.Instance.SetCrosshairVisible(false);
                SaveSettings();
            };

            var topMostToggle = AddToggleRow("HudOptionTopMost", "HudOptionTopMostDesc", topMost);

            topMostToggle.Click += (s, e) =>
            {
                bool newState = topMostToggle.IsChecked ?? false;

                if (newState)
                {
                    var confirmWindow = new TopMostConfirmWindow();
                    confirmWindow.Owner = System.Windows.Window.GetWindow(this);
                    confirmWindow.ShowDialog();

                    if (confirmWindow.IsConfirmed)
                    {
                        EnsureHudElementExists("crosshair");
                        var elem = _settings.HudElements.FirstOrDefault(h => h.Id == "crosshair");
                        if (elem != null) elem.TopMost = true;
                        CrosshairService.Instance.SetTopMost(true);
                        SaveSettings();
                    }
                    else
                    {
                        topMostToggle.IsChecked = false;
                    }
                }
                else
                {
                    EnsureHudElementExists("crosshair");
                    var elem = _settings.HudElements.FirstOrDefault(h => h.Id == "crosshair");
                    if (elem != null) elem.TopMost = false;
                    CrosshairService.Instance.SetTopMost(false);
                    SaveSettings();
                }
            };
        }

        private void AddAttackIndicatorContent()
        {
            EnsureHudElementExists("attackindicator");
            var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "attackindicator");
            bool isVisible = settings?.IsVisible ?? false;
            bool dataMappingEnabled = settings?.DataMappingEnabled ?? false;
            string dataMappingType = settings?.DataMappingType ?? "电池电量";
            bool customValueEnabled = settings?.CustomValueEnabled ?? true;
            int customCurrentValue = settings?.CustomCurrentValue ?? 100;

            AddIconPreviewRow("HudOptionElementIcon", AssetPaths.CrosshairAttackIndicatorFull);

            var showToggle = AddToggleRow("HudOptionShowElement", "HudOptionShowElementDesc", isVisible);
            showToggle.Checked += (s, e) =>
            {
                EnsureHudElementExists("attackindicator");
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == "attackindicator");
                if (elem != null) elem.IsVisible = true;
                CrosshairService.Instance.SetAttackIndicatorVisible(true);
                SaveSettings();
            };
            showToggle.Unchecked += (s, e) =>
            {
                EnsureHudElementExists("attackindicator");
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == "attackindicator");
                if (elem != null) elem.IsVisible = false;
                CrosshairService.Instance.SetAttackIndicatorVisible(false);
                SaveSettings();
            };

            AddDataMappingSection("attackindicator", dataMappingEnabled, dataMappingType);

            AddCustomValueSection("attackindicator", customValueEnabled, customCurrentValue, 100, hasMaxValue: false, maxValueLimit: 100);
        }
    }
}