#!/usr/bin/env python3
"""
图标分类配置生成器
根据图标文件名推断分类，一个图标可属于多个分类
"""

import os
import json
import re

# 分类规则定义
BLOCK_RULES = {
    "building_block": [
        r"_planks",
        r"bricks",
        r"_brick",
        r"stone_bricks",
        r"nether_bricks",
        r"quartz_block",
        r"prismarine",
        r"purpur",
        r"sandstone",
        r"mud_bricks",
        r"deepslate_",
        r"cobblestone",
        r"smooth_",
        r"cut_",
        r"chiseled",
        r"pillar",
        r"^raw_",
        r"_block$",
        r"polished_",
        r"tuff",
        r"resin",
    ],
    "colored_block": [
        r"stained_glass",
        r"glass$",
        r"glass_pane",
        r"_wool",
        r"_carpet",
        r"_terracotta",
        r"glazed_terracotta",
        r"_concrete",
        r"_dye",
        r"shulker_box",
    ],
    "functional_block": [
        r"_door",
        r"_trapdoor",
        r"_fence",
        r"_gate",
        r"chest",
        r"barrel",
        r"crafting_table",
        r"furnace",
        r"blast_furnace",
        r"smoker",
        r"anvil",
        r"grindstone",
        r"stonecutter",
        r"loom",
        r"composter",
        r"bell",
        r"campfire",
        r"soul_campfire",
        r"lantern",
        r"soul_lantern",
        r"torch",
        r"soul_torch",
        r"lamp",
        r"redstone_lamp",
        r"bed",
        r"bedrock",  # bedrock 不属于 functional，需要排除
        r"bookshelf",
        r"lectern",
        r"sign",
        r"hanging_sign",
        r"banner",
        r"armor_stand",
        r"enchanting",
        r"brewing",
        r"beacon",
        r"conduit",
        r"jukebox",
        r"note_block",
        r"honey_block",
        r"slime_block",
    ],
    "natural_block": [
        r"^stone$",
        r"^grass",
        r"^dirt",
        r"^sand",
        r"^gravel",
        r"^clay",
        r"^snow",
        r"^ice",
        r"water",
        r"lava",
        r"_log",
        r"_leaves",
        r"_sapling",
        r"flower",
        r"plant",
        r"mushroom",
        r"cactus",
        r"bamboo_large",
        r"bamboo_small",
        r"reeds?",
        r"kelp",
        r"seagrass",
        r"coral",
        r"lily",
        r"vine",
        r"podzol",
        r"mycelium",
        r"basalt",
        r"blackstone$",
        r"netherrack",
        r"end_stone",
        r"obsidian",
        r"^bedrock$",
        r"_ore",
        r"pumpkin",
        r"melon",
        r"hay_block",
        r"moss",
        r"pale",
        r"azalea",
        r"spore",
        r"roots",
        r"weeping",
        r"twisting",
        r"nylium",
        r"glowstone",
        r"nether_wart",
        r"sugar_cane",
        r"short_grass",
        r"tall_grass",
        r"fern",
        r"dead_bush",
        r"lilypad",
        r"scaffolding",
    ],
    "redstone_block": [
        r"_rail",
        r"redstone",
        r"repeater",
        r"comparator",
        r"lever",
        r"_button",
        r"pressure_plate",
        r"observer",
        r"piston",
        r"dispenser",
        r"dropper",
        r"hopper",
        r"daylight_detector",
        r"target",
        r"tripwire",
        r"sculk",
        r"calibrated_sculk",
        r"command_block",
        r"structure_block",
        r"jigsaw_block",
    ],
}

ITEM_RULES = {
    "combat": [
        r"_sword",
        r"_axe$",  # axe 既是工具也是武器
        r"_bow",
        r"crossbow",
        r"_shield",
        r"trident",
        r"arrow",
        r"_helmet",
        r"_chestplate",
        r"_leggings",
        r"_boots",
        r"_turtle_shell",
        r"horse_armor",
        r"wolf_armor",
        r"mace",
        r"fire_charge",
    ],
    "food_and_drink": [
        r"apple",
        r"bread",
        r"beef",
        r"chicken",
        r"porkchop",
        r"mutton",
        r"rabbit",
        r"fish",
        r"cod",
        r"salmon",
        r"tropical_fish",
        r"pufferfish",
        r"cooked_",
        r"potato",
        r"carrot$",
        r"beetroot",
        r"melon_slice",
        r"cake",
        r"cookie",
        r"pie",
        r"stew",
        r"soup",
        r"honey",
        r"milk",
        r"water_bucket$",
        r"potion",
        r"splash_potion",
        r"lingering_potion",
        r"berry",
        r"sweet_berries",
        r"glow_berries",
        r"dried_kelp",
        r"bamboo$",
        r"cactus$",
        r"egg$",  # 普通蛋，非 spawn_egg
        r"turtle_egg",
        r"sniffer_egg",
        r"bread",
        r"sugar",
        r"wheat",
        r"wheat_seeds",
        r"pumpkin_seeds",
        r"melon_seeds",
        r"beetroot_seeds",
        r"torchflower_seeds",
        r"pitcher_crop",
        r"chorus_fruit",
        r"golden_apple",
        r"enchanted_golden_apple",
        r"golden_carrot",
        r"glistering_melon",
        r"spider_eye",
        r"fermented_spider_eye",
        r"rotten_flesh",
        r"phantom_membrane",
        r"enchanted_book",  # 附魔书也算食物? 不，应该算 ingredient
    ],
    "ingredient": [
        r"ingot",
        r"nugget",
        r"diamond$",
        r"emerald$",
        r"lapis_lazuli",
        r"quartz$",
        r"amethyst_shard",
        r"prismarine_shard",
        r"prismarine_crystals",
        r"dust",
        r"redstone$",
        r"gunpowder",
        r"blaze_rod",
        r"blaze_powder",
        r"ender_pearl",
        r"ender_eye",
        r"eye_of_ender",
        r"slime_ball",
        r"slime_block",
        r"honeycomb",
        r"honey_block",
        r"stick",
        r"string",
        r"feather",
        r"leather",
        r"rabbit_hide",
        r"bone",
        r"bone_meal",
        r"flint",
        r"charcoal",
        r"^coal$",
        r"echo_shard",
        r"disc_fragment",
        r"goat_horn",
        r"scute",
        r"turtle_scute",
        r"armadillo_scute",
        r"paper",
        r"book$",
        r"enchanted_book",
        r"written_book",
        r"writable_book",
        r"experience_bottle",
        r"heart_of_the_sea",
        r"nautilus_shell",
        r"phantom_membrane",
        r"shulker_shell",
        r"dragon_breath",
        r"ghast_tear",
        r"magma_cream",
        r"nether_star",
        r"beacon$",
        r"crying_obsidian",
        r"respawn_anchor",
        r"lodestone",
        r"recovery_compass",
        r"wither_skeleton_skull",
        r"skeleton_skull",
        r"zombie_head",
        r"player_head",
        r"creeper_head",
        r"dragon_head",
        r"piglin_head",
        r"breeze_rod",
        r"heavy_core",
        r"resin_brick",
        r"resin_clump",
        r"copper_ingot",
        r"raw_copper",
        r"raw_gold",
        r"raw_iron",
        r"iron_ingot",
        r"gold_ingot",
        r"netherite_ingot",
        r"netherite_scrap",
        r"ancient_debris",
        r"^clay_ball$",
        r"brick$",
        r"nether_brick",
        r"bone_block",
        r"cactus$",  # 仙人掌也是原料
        r"kelp$",
        r"glowstone_dust",
        r"fermented",
        r"sugar_cane$",
        r"wheat$",
        r"sugar$",
    ],
    "spawn_egg": [
        r"spawn_egg",
    ],
    "tool_and_utility": [
        r"_pickaxe",
        r"_shovel",
        r"_hoe",
        r"shears",
        r"fishing_rod",
        r"flint_and_steel",
        r"compass",
        r"clock",
        r"map$",
        r"filled_map",
        r"bucket$",  # 普通桶
        r"water_bucket",
        r"lava_bucket",
        r"milk_bucket",
        r"powder_snow_bucket",
        r"axolotl_bucket",  # 生物桶算工具
        r"cod_bucket",
        r"salmon_bucket",
        r"pufferfish_bucket",
        r"tropical_fish_bucket",
        r"tadpole_bucket",
        r"lead",
        r"name_tag",
        r"saddle",
        r"elytra",
        r"firework",
        r"firework_rocket",
        r"firework_star",
        r"spyglass",
        r"goat_horn",
        r"recovery_compass",
        r"wind_charge",
        r"ominous_bottle",
        r"ominous_trial_key",
        r"trial_key",
        r"vault",
        r"brush",
        r"carrot_on_a_stick",
        r"warped_fungus_on_a_stick",
    ],
}

def matches_rules(name, rules):
    """检查名称是否匹配任一规则"""
    categories = []
    for category, patterns in rules.items():
        for pattern in patterns:
            if re.search(pattern, name):
                categories.append(category)
                break  # 该分类只添加一次
    return categories

def generate_config():
    base_path = os.path.dirname(os.path.abspath(__file__))
    assets_path = os.path.join(base_path, "Assets", "minecraft", "textures")

    config = {
        "blocks": {},
        "items": {},
    }

    # 处理方块
    block_dir = os.path.join(assets_path, "block", "block")
    if os.path.exists(block_dir):
        for filename in os.listdir(block_dir):
            if filename.endswith(".png"):
                name = filename[:-4]  # 去掉 .png
                categories = matches_rules(name, BLOCK_RULES)
                # 确保 "block_all" 不在 categories 中
                if "block_all" in categories:
                    categories.remove("block_all")
                config["blocks"][filename] = categories

    # 处理物品
    item_dir = os.path.join(assets_path, "item", "item")
    if os.path.exists(item_dir):
        for filename in os.listdir(item_dir):
            if filename.endswith(".png"):
                name = filename[:-4]
                categories = matches_rules(name, ITEM_RULES)
                config["items"][filename] = categories

    # 写入配置文件
    output_path = os.path.join(base_path, "Data", "icon_categories.json")
    with open(output_path, "w", encoding="utf-8") as f:
        json.dump(config, f, indent=2, ensure_ascii=False)

    print(f"配置文件已生成: {output_path}")
    print(f"方块数量: {len(config['blocks'])}")
    print(f"物品数量: {len(config['items'])}")

    # 统计各分类数量
    for type_name, data in [("blocks", config["blocks"]), ("items", config["items"])]:
        print(f"\n{type_name} 分类统计:")
        category_counts = {}
        for categories in data.values():
            for cat in categories:
                category_counts[cat] = category_counts.get(cat, 0) + 1
        for cat, count in sorted(category_counts.items()):
            print(f"  {cat}: {count}")

if __name__ == "__main__":
    generate_config()