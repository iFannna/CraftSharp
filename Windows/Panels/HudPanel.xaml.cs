using CraftSharp.Models;
using System.Windows.Controls;

namespace CraftSharp.Windows.Panels
{
    public partial class HudPanel : System.Windows.Controls.UserControl
    {
        private AppSettings _settings;

        public HudPanel(AppSettings settings)
        {
            InitializeComponent();
            _settings = settings;
            InitializeHudAccordion();
        }

        private void InitializeHudAccordion()
        {
            var hudElements = new[]
            {
                ("hotbar", "快捷栏", "#3b82f6"),
                ("health", "生命值", "#ef4444"),
                ("food", "饥饿值", "#eab308"),
                ("expbar", "经验条", "#22c55e"),
                ("air", "空气值", "#06b6d4"),
                ("armor", "护甲值", "#6b7280"),
                ("absorbing", "伤害吸收值", "#f97316"),
            };

            foreach (var (id, name, color) in hudElements)
            {
                var item = new HudAccordionItem(_settings, id, name, color);
                HudAccordionContainer.Children.Add(item);
            }
        }
    }
}