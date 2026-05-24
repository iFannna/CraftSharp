using CraftSharp.Models;
using System.Windows.Controls;

namespace CraftSharp.Windows.Settings.Panels.Inventory
{
    /// <summary>
    /// 物品栏设置面板
    /// </summary>
    public partial class InventoryPanel : System.Windows.Controls.UserControl
    {
        private AppSettings? _settings;

        // 保存所有卡片的引用
        private System.Collections.Generic.List<InventoryAccordionItem> _cards = new();

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
            // 物品栏（物品栏相关）
            var card1 = new InventoryAccordionItem(_settings, "InventoryTitle");
            _cards.Add(card1);
            PlayerInventoryContainer.Children.Add(card1);
            // 文本提示框（物品栏相关）
            var tooltipCard = new InventoryAccordionItem(_settings, "TooltipTitle");
            _cards.Add(tooltipCard);
            PlayerInventoryContainer.Children.Add(tooltipCard);
            // 生存物品栏（玩家物品栏）
            var card2 = new InventoryAccordionItem(_settings, "SurvivalInventoryTitle");
            _cards.Add(card2);
            PlayerInventoryExtraContainer.Children.Add(card2);
            // 创造物品栏（玩家物品栏）
            var card3 = new InventoryAccordionItem(_settings, "CreativeInventoryTitle");
            _cards.Add(card3);
            PlayerInventoryExtraContainer.Children.Add(card3);
        }

        private void AddContainerCards()
        {
            // 箱子
            var card4 = new InventoryAccordionItem(_settings, "ChestTitle");
            _cards.Add(card4);
            ContainerInventoryContainer.Children.Add(card4);
            // 大箱子
            var card5 = new InventoryAccordionItem(_settings, "LargeChestTitle");
            _cards.Add(card5);
            ContainerInventoryContainer.Children.Add(card5);
        }

        private void AddFunctionalBlockCards()
        {
            // 工作台
            var card6 = new InventoryAccordionItem(_settings, "CraftingTableTitle");
            _cards.Add(card6);
            FunctionalBlockContainer.Children.Add(card6);
            // 附魔台
            var card7 = new InventoryAccordionItem(_settings, "EnchantmentTableTitle");
            _cards.Add(card7);
            FunctionalBlockContainer.Children.Add(card7);
            // 信标
            var card8 = new InventoryAccordionItem(_settings, "BeaconTitle");
            _cards.Add(card8);
            FunctionalBlockContainer.Children.Add(card8);
            // 铁砧
            var card9 = new InventoryAccordionItem(_settings, "AnvilTitle");
            _cards.Add(card9);
            FunctionalBlockContainer.Children.Add(card9);
            // 砂轮
            var card10 = new InventoryAccordionItem(_settings, "GrindstoneTitle");
            _cards.Add(card10);
            FunctionalBlockContainer.Children.Add(card10);
            // 制图台
            var card11 = new InventoryAccordionItem(_settings, "CartographyTableTitle");
            _cards.Add(card11);
            FunctionalBlockContainer.Children.Add(card11);
            // 切石机
            var card12 = new InventoryAccordionItem(_settings, "StonecutterTitle");
            _cards.Add(card12);
            FunctionalBlockContainer.Children.Add(card12);
            // 织布机
            var card13 = new InventoryAccordionItem(_settings, "LoomTitle");
            _cards.Add(card13);
            FunctionalBlockContainer.Children.Add(card13);
            // 锻造台
            var card14 = new InventoryAccordionItem(_settings, "SmithingTableTitle");
            _cards.Add(card14);
            FunctionalBlockContainer.Children.Add(card14);
        }

        private void AddCreatureInventoryCards()
        {
            // 村民
            var card15 = new InventoryAccordionItem(_settings, "VillagerTitle");
            _cards.Add(card15);
            CreatureInventoryContainer.Children.Add(card15);
            // 马
            var card16 = new InventoryAccordionItem(_settings, "HorseTitle");
            _cards.Add(card16);
            CreatureInventoryContainer.Children.Add(card16);
            // 驴
            var card17 = new InventoryAccordionItem(_settings, "DonkeyTitle");
            _cards.Add(card17);
            CreatureInventoryContainer.Children.Add(card17);
            // 骆驼
            var card18 = new InventoryAccordionItem(_settings, "CamelTitle");
            _cards.Add(card18);
            CreatureInventoryContainer.Children.Add(card18);
            // 羊驼
            var card19 = new InventoryAccordionItem(_settings, "LlamaTitle");
            _cards.Add(card19);
            CreatureInventoryContainer.Children.Add(card19);
            // 鹦鹉螺
            var card20 = new InventoryAccordionItem(_settings, "NautilusTitle");
            _cards.Add(card20);
            CreatureInventoryContainer.Children.Add(card20);
        }

        /// <summary>
        /// 刷新所有卡片状态（响应记住卡片状态开关变化）
        /// </summary>
        public void RefreshCardStates(bool rememberEnabled)
        {
            foreach (var card in _cards)
            {
                string key = card.TitleResourceKey;
                if (rememberEnabled)
                {
                    // 开关开启：保存当前状态到配置
                    if (_settings != null)
                    {
                        _settings.System.CardExpandedStates[key] = card.IsExpanded;
                    }
                }
                else
                {
                    // 开关关闭：恢复默认状态（所有卡片折叠）
                    card.SetExpanded(false, animate: false);
                }
            }

            // 开关开启后保存配置
            if (rememberEnabled && _settings != null)
            {
                if (System.Windows.Application.Current is App app)
                {
                    app.SaveSettings();
                }
            }
        }

        private void RestoreDefaultsBtn_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            if (_settings == null) return;
            _settings.Inventory = new InventorySettings();

            _cards.Clear();
            PlayerInventoryContainer.Children.Clear();
            PlayerInventoryExtraContainer.Children.Clear();
            ContainerInventoryContainer.Children.Clear();
            FunctionalBlockContainer.Children.Clear();
            CreatureInventoryContainer.Children.Clear();
            AddPlayerInventoryCards();
            AddContainerCards();
            AddFunctionalBlockCards();
            AddCreatureInventoryCards();

            if (System.Windows.Application.Current is App app)
            {
                app.SaveSettings();
            }
        }
    }
}