using CraftSharp.Helpers;
using CraftSharp.Services.Core;
using CraftSharp.Services.Hud;
using CraftSharp.Services.Slot;
using CraftSharp.Services.Resource;
using System.Windows;
using System.Windows.Controls;

namespace CraftSharp.Windows.Settings.Panels.Hud
{
    /// <summary>
    /// HudAccordionItem 标准HUD元素配置（生命值、饥饿值、空气值、经验条、伤害吸收、护甲）
    /// </summary>
    public partial class HudAccordionItem
    {
        private void AddHealthContent()
        {
            var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "Health");
            string iconStyle = settings?.IconStyle ?? "";
            string iconPath = AssetPaths.GetHeartPathWithFallback(iconStyle, "full");
            AddStandardHudElement("Health", StatusBarService.Instance.SetHealthVisible, hasRegenAnimation: true, iconPath: iconPath);
        }

        private void AddFoodContent()
        {
            var setVisibleAction = GetSetVisibleAction("Food");
            var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "Food");
            string iconStyle = settings?.IconStyle ?? "";
            string iconPath = AssetPaths.GetFoodPath(iconStyle, "full");
            AddStandardHudElement("Food", setVisibleAction, hasSaturation: true, iconPath: iconPath);
        }

        private void AddAirContent()
        {
            var setVisibleAction = GetSetVisibleAction("Air");
            AddStandardHudElement("Air", setVisibleAction, hasRegenAnimation: true, hasAirAnimation: true, iconPath: AssetPaths.Air);
        }

        private void AddExpBarContent()
        {
            var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "ExpBar");
            string iconStyle = settings?.IconStyle ?? "";
            string iconPath = GetExpBarIconPath(iconStyle);
            AddStandardHudElement("ExpBar", StatusBarService.Instance.SetExpBarVisible, hasMaxValue: false, iconPath: iconPath);
        }

        private void AddAbsorbingContent()
        {
            var setVisibleAction = GetSetVisibleAction("Absorbing");
            var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "Absorbing");
            string iconStyle = settings?.IconStyle ?? "";
            string iconPath = GetAbsorbingIconPath(iconStyle);
            AddStandardHudElement("Absorbing", setVisibleAction, maxValueLimit: 1024, iconPath: iconPath);
        }

        private void AddArmorContent()
        {
            var setVisibleAction = GetSetVisibleAction("Armor");
            AddStandardHudElement("Armor", setVisibleAction, iconPath: AssetPaths.ArmorFull);
        }

        private void AddStandardHudElement(string id, Action<bool>? setVisibleAction, bool hasRegenAnimation = false, bool hasAirAnimation = false, bool hasMaxValue = true, int maxValueLimit = 20, bool hasSaturation = false, string iconPath = "")
        {
            EnsureHudElementExists(id);
            var settings = _settings.HudElements.FirstOrDefault(h => h.Id == id);
            bool isVisible = settings?.IsVisible ?? true;
            bool regenAnim = settings?.RegenAnimation ?? false;
            bool dataMappingEnabled = settings?.DataMappingEnabled ?? false;
            string dataMappingType = settings?.DataMappingType ?? "BatteryLevel";
            bool customValueEnabled = settings?.CustomValueEnabled ?? false;
            int customCurrentValue = settings?.CustomCurrentValue ?? 10;
            int customMaxValue = settings?.CustomMaxValue ?? 20;
            int customSaturationValue = settings?.CustomSaturationValue ?? 0;

            if (!string.IsNullOrEmpty(iconPath))
            {
                AddIconPreviewRow("HudOptionElementIcon", iconPath);
            }

            var showToggle = AddToggleRow("HudOptionShowElement", "HudOptionShowElementDesc", isVisible);
            showToggle.Checked += (s, e) =>
            {
                EnsureHudElementExists(id);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                if (elem != null) elem.IsVisible = true;
                setVisibleAction?.Invoke(true);
                SaveSettings();
            };
            showToggle.Unchecked += (s, e) =>
            {
                EnsureHudElementExists(id);
                var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                if (elem != null) elem.IsVisible = false;
                setVisibleAction?.Invoke(false);
                SaveSettings();
            };

            if (hasRegenAnimation)
            {
                string animLabelKey = hasAirAnimation ? "HudOptionAirAnim" : "HudOptionRegenAnim";
                string animDescKey = hasAirAnimation ? "HudOptionAirAnimDesc" : "HudOptionRegenAnimDesc";
                var regenToggle = AddToggleRow(animLabelKey, animDescKey, regenAnim);
                regenToggle.Checked += (s, e) =>
                {
                    EnsureHudElementExists(id);
                    var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                    if (elem != null) elem.RegenAnimation = true;
                    SaveSettings();
                };
                regenToggle.Unchecked += (s, e) =>
                {
                    EnsureHudElementExists(id);
                    var elem = _settings.HudElements.FirstOrDefault(h => h.Id == id);
                    if (elem != null) elem.RegenAnimation = false;
                    SaveSettings();
                };
            }

            AddDataMappingSection(id, dataMappingEnabled, dataMappingType);

            AddCustomValueSection(id, customValueEnabled, customCurrentValue, customMaxValue, hasMaxValue, maxValueLimit, hasSaturation, customSaturationValue);
        }
    }
}