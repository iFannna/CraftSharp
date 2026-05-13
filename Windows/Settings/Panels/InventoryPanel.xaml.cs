using CraftSharp.Models;
using System.Windows.Controls;

namespace CraftSharp.Windows.Settings.Panels
{
    /// <summary>
    /// 物品栏设置面板
    /// </summary>
    public partial class InventoryPanel : System.Windows.Controls.UserControl
    {
        private AppSettings? _settings;

        public InventoryPanel(AppSettings? settings)
        {
            InitializeComponent();

            _settings = settings;

            // 添加玩家物品栏卡片
            AddPlayerInventoryCards();

            // 添加容器卡片
            AddContainerCards();

            // 添加功能方块卡片
            AddFunctionalBlockCards();

            // 添加生物物品栏卡片
            AddCreatureInventoryCards();
        }

        private void AddPlayerInventoryCards()
        {
            // 物品栏
            PlayerInventoryContainer.Children.Add(new InventoryAccordionItem(_settings, "InventoryTitle"));
            // 生存物品栏
            PlayerInventoryContainer.Children.Add(new InventoryAccordionItem(_settings, "SurvivalInventoryTitle"));
            // 创造物品栏
            PlayerInventoryContainer.Children.Add(new InventoryAccordionItem(_settings, "CreativeInventoryTitle"));
        }

        private void AddContainerCards()
        {
            // 箱子
            ContainerInventoryContainer.Children.Add(new InventoryAccordionItem(_settings, "ChestTitle"));
            // 大箱子
            ContainerInventoryContainer.Children.Add(new InventoryAccordionItem(_settings, "LargeChestTitle"));
        }

        private void AddFunctionalBlockCards()
        {
            // 工作台
            FunctionalBlockContainer.Children.Add(new InventoryAccordionItem(_settings, "CraftingTableTitle"));
            // 附魔台
            FunctionalBlockContainer.Children.Add(new InventoryAccordionItem(_settings, "EnchantmentTableTitle"));
            // 信标
            FunctionalBlockContainer.Children.Add(new InventoryAccordionItem(_settings, "BeaconTitle"));
            // 铁砧
            FunctionalBlockContainer.Children.Add(new InventoryAccordionItem(_settings, "AnvilTitle"));
            // 砂轮
            FunctionalBlockContainer.Children.Add(new InventoryAccordionItem(_settings, "GrindstoneTitle"));
            // 制图台
            FunctionalBlockContainer.Children.Add(new InventoryAccordionItem(_settings, "CartographyTableTitle"));
            // 切石机
            FunctionalBlockContainer.Children.Add(new InventoryAccordionItem(_settings, "StonecutterTitle"));
            // 织布机
            FunctionalBlockContainer.Children.Add(new InventoryAccordionItem(_settings, "LoomTitle"));
            // 锻造台
            FunctionalBlockContainer.Children.Add(new InventoryAccordionItem(_settings, "SmithingTableTitle"));
        }

        private void AddCreatureInventoryCards()
        {
            // 村民
            CreatureInventoryContainer.Children.Add(new InventoryAccordionItem(_settings, "VillagerTitle"));
            // 马
            CreatureInventoryContainer.Children.Add(new InventoryAccordionItem(_settings, "HorseTitle"));
            // 驴
            CreatureInventoryContainer.Children.Add(new InventoryAccordionItem(_settings, "DonkeyTitle"));
            // 骆驼
            CreatureInventoryContainer.Children.Add(new InventoryAccordionItem(_settings, "CamelTitle"));
            // 羊驼
            CreatureInventoryContainer.Children.Add(new InventoryAccordionItem(_settings, "LlamaTitle"));
            // 鹦鹉螺
            CreatureInventoryContainer.Children.Add(new InventoryAccordionItem(_settings, "NautilusTitle"));
        }
    }
}