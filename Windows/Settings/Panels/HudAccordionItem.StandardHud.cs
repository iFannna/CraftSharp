using CraftSharp.Helpers;
using CraftSharp.Services;
using System.Windows;
using System.Windows.Controls;

namespace CraftSharp.Windows.Settings.Panels
{
    /// <summary>
    /// HudAccordionItem 标准HUD元素配置（生命值、饥饿值、空气值、经验条、伤害吸收、护甲）
    /// </summary>
    public partial class HudAccordionItem
    {
        private void AddHealthContent()
        {
            var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "health");
            string iconStyle = settings?.IconStyle ?? "";
            string iconPath = AssetPaths.GetHeartPathWithFallback(iconStyle, "full");
            AddStandardHudElement("health", StatusBarService.Instance.SetHealthVisible, hasRegenAnimation: true, iconPath: iconPath);
        }

        private void AddFoodContent()
        {
            var setVisibleAction = GetSetVisibleAction("food");
            var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "food");
            string iconStyle = settings?.IconStyle ?? "";
            string iconPath = AssetPaths.GetFoodPath(iconStyle, "full");
            AddStandardHudElement("food", setVisibleAction, hasSaturation: true, iconPath: iconPath);
        }

        private void AddAirContent()
        {
            var setVisibleAction = GetSetVisibleAction("air");
            AddStandardHudElement("air", setVisibleAction, hasRegenAnimation: true, hasAirAnimation: true, iconPath: AssetPaths.Air);
        }

        private void AddExpBarContent()
        {
            var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "expbar");
            string iconStyle = settings?.IconStyle ?? "";
            string iconPath = GetExpBarIconPath(iconStyle);
            AddStandardHudElement("expbar", StatusBarService.Instance.SetExpBarVisible, hasMaxValue: false, iconPath: iconPath);
        }

        private void AddAbsorbingContent()
        {
            var setVisibleAction = GetSetVisibleAction("absorbing");
            var settings = _settings.HudElements.FirstOrDefault(h => h.Id == "absorbing");
            string iconStyle = settings?.IconStyle ?? "";
            string iconPath = GetAbsorbingIconPath(iconStyle);
            AddStandardHudElement("absorbing", setVisibleAction, maxValueLimit: 1024, iconPath: iconPath);
        }

        private void AddArmorContent()
        {
            var setVisibleAction = GetSetVisibleAction("armor");
            AddStandardHudElement("armor", setVisibleAction, iconPath: AssetPaths.ArmorFull);
        }

        private void AddStandardHudElement(string id, Action<bool>? setVisibleAction, bool hasRegenAnimation = false, bool hasAirAnimation = false, bool hasMaxValue = true, int maxValueLimit = 20, bool hasSaturation = false, string iconPath = "")
        {
            EnsureHudElementExists(id);
            var settings = _settings.HudElements.FirstOrDefault(h => h.Id == id);
            bool isVisible = settings?.IsVisible ?? true;
            bool regenAnim = settings?.RegenAnimation ?? false;
            bool dataMappingEnabled = settings?.DataMappingEnabled ?? false;
            string dataMappingType = settings?.DataMappingType ?? "电池电量";
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