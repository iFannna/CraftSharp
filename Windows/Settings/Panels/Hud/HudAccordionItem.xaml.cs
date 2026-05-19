using CraftSharp.Models;

namespace CraftSharp.Windows.Settings.Panels.Hud
{
    /// <summary>
    /// HudAccordionItem 主入口 - AddHudContent 分发逻辑
    /// </summary>
    public partial class HudAccordionItem
    {
        private void AddHudContent(string id)
        {
            _mappingToggle = null;
            _mappingComboBox = null;
            _customToggle = null;
            _valueContainer = null;
            _currentValueTextBox = null;
            _maxValueTextBox = null;
            _maxValueDisplay = null;
            _saturationTextBox = null;
            _saturationLimitDisplay = null;

            switch (id)
            {
                case "StatusBar":
                    AddStatusBarContent();
                    break;
                case "Hotbar":
                    AddHotbarContent();
                    break;
                case "Health":
                    AddHealthContent();
                    break;
                case "ExpBar":
                    AddExpBarContent();
                    break;
                case "Absorbing":
                    AddAbsorbingContent();
                    break;
                case "Air":
                    AddAirContent();
                    break;
                case "Food":
                    AddFoodContent();
                    break;
                case "Armor":
                    AddArmorContent();
                    break;
                case "Crosshair":
                    AddCrosshairContent();
                    break;
                case "AttackIndicator":
                    AddAttackIndicatorContent();
                    break;
                case "BossBar":
                    AddBossBarContent();
                    break;
            }
        }
    }
}