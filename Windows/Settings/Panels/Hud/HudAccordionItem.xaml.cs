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
                case "statusbar":
                    AddStatusBarContent();
                    break;
                case "hotbar":
                    AddHotbarContent();
                    break;
                case "health":
                    AddHealthContent();
                    break;
                case "expbar":
                    AddExpBarContent();
                    break;
                case "absorbing":
                    AddAbsorbingContent();
                    break;
                case "air":
                    AddAirContent();
                    break;
                case "food":
                    AddFoodContent();
                    break;
                case "armor":
                    AddArmorContent();
                    break;
                case "crosshair":
                    AddCrosshairContent();
                    break;
                case "attackindicator":
                    AddAttackIndicatorContent();
                    break;
                case "bossbar":
                    AddBossBarContent();
                    break;
            }
        }
    }
}