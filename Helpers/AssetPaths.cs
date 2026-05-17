namespace CraftSharp.Helpers
{
    /// <summary>
    /// 图片资源路径定义
    /// </summary>
    public static class AssetPaths
    {
        // 快捷栏
        public const string Hotbar = "assets/minecraft/textures/gui/sprites/hud/hotbar/hotbar.png";
        public const string HotbarSelection = "assets/minecraft/textures/gui/sprites/hud/hotbar/hotbar_selection.png";
        public const string HotbarOffhand = "assets/minecraft/textures/gui/sprites/hud/hotbar/hotbar_offhand.png";

        // 经验条
        public const string ExperienceBarBackground = "assets/minecraft/textures/gui/sprites/hud/experience_bar/experience_bar_background.png";
        public const string ExperienceBarProgress = "assets/minecraft/textures/gui/sprites/hud/experience_bar/experience_bar_progress.png";
        public const string JumpBarBackground = "assets/minecraft/textures/gui/sprites/hud/experience_bar/jump_bar_background.png";
        public const string JumpBarProgress = "assets/minecraft/textures/gui/sprites/hud/experience_bar/jump_bar_progress.png";

        /// <summary>
        /// 根据 IconStyle 获取经验条图标路径
        /// IconStyle: experience_bar_progress, jump_bar_progress
        /// </summary>
        public static string GetExpBarPath(string iconStyle, string suffix)
        {
            // suffix: background, progress
            if (string.IsNullOrEmpty(iconStyle) || iconStyle == "experience_bar_progress")
            {
                return $"assets/minecraft/textures/gui/sprites/hud/experience_bar/experience_bar_{suffix}.png";
            }
            // jump_bar_progress → jump_bar_{suffix}.png
            return $"assets/minecraft/textures/gui/sprites/hud/experience_bar/jump_bar_{suffix}.png";
        }

        // 背包
        public const string Inventory = "assets/minecraft/textures/gui/container/inventory.png";

        // 生命值（默认图标）
        public const string HeartFull = "assets/minecraft/textures/gui/sprites/hud/heart/full.png";
        public const string HeartHalf = "assets/minecraft/textures/gui/sprites/hud/heart/half.png";
        public const string HeartContainer = "assets/minecraft/textures/gui/sprites/hud/heart/container.png";
        public const string HeartFullBlinking = "assets/minecraft/textures/gui/sprites/hud/heart/full_blinking.png";
        public const string HeartHalfBlinking = "assets/minecraft/textures/gui/sprites/hud/heart/half_blinking.png";
        public const string HeartContainerBlinking = "assets/minecraft/textures/gui/sprites/hud/heart/container_blinking.png";

        // 饥饿值（默认图标）
        public const string FoodFull = "assets/minecraft/textures/gui/sprites/hud/food/food_full.png";
        public const string FoodHalf = "assets/minecraft/textures/gui/sprites/hud/food/food_half.png";
        public const string FoodEmpty = "assets/minecraft/textures/gui/sprites/hud/food/food_empty.png";

        // 饱和度
        public const string SaturationFull = "assets/minecraft/textures/gui/sprites/hud/food/saturation_full.png";
        public const string SaturationHalf = "assets/minecraft/textures/gui/sprites/hud/food/saturation_half.png";

        // 盔甲值
        public const string ArmorFull = "assets/minecraft/textures/gui/sprites/hud/armor/armor_full.png";
        public const string ArmorHalf = "assets/minecraft/textures/gui/sprites/hud/armor/armor_half.png";
        public const string ArmorEmpty = "assets/minecraft/textures/gui/sprites/hud/armor/armor_empty.png";

        // 空气值
        public const string Air = "assets/minecraft/textures/gui/sprites/hud/air/air.png";
        public const string AirEmpty = "assets/minecraft/textures/gui/sprites/hud/air/air_empty.png";
        public const string AirBursting = "assets/minecraft/textures/gui/sprites/hud/air/air_bursting.png";

        // 伤害吸收值
        public const string AbsorbingFull = "assets/minecraft/textures/gui/sprites/hud/heart/absorbing_full.png";
        public const string AbsorbingHalf = "assets/minecraft/textures/gui/sprites/hud/heart/absorbing_half.png";

        // 占位图标（用于文件丢失时显示）
        public const string PlaceholderBarrier = "assets/minecraft/textures/item/item/barrier.png";

        // 准星
        public const string Crosshair = "assets/minecraft/textures/gui/sprites/hud/crosshair/crosshair.png";
        public const string CrosshairAttackIndicatorBackground = "assets/minecraft/textures/gui/sprites/hud/crosshair/crosshair_attack_indicator_background.png";
        public const string CrosshairAttackIndicatorFull = "assets/minecraft/textures/gui/sprites/hud/crosshair/crosshair_attack_indicator_full.png";
        public const string CrosshairAttackIndicatorProgress = "assets/minecraft/textures/gui/sprites/hud/crosshair/crosshair_attack_indicator_progress.png";

        /// <summary>
        /// 根据 IconStyle 获取生命值图标路径
        /// IconStyle: full, hardcore_full, poisoned_full, withered_full, frozen_full, vehicle_full,
        ///            poisoned_hardcore_full, withered_hardcore_full, frozen_hardcore_full, vehicle_hardcore_full,
        ///            absorbing_full, absorbing_hardcore_full
        /// </summary>
        public static string GetHeartPath(string iconStyle, string suffix)
        {
            // suffix: full, half, container, full_blinking, half_blinking, container_blinking
            // 如果 iconStyle 为空或默认，使用原始路径
            if (string.IsNullOrEmpty(iconStyle) || iconStyle == "full")
            {
                return $"assets/minecraft/textures/gui/sprites/hud/heart/{suffix}.png";
            }

            // 从 iconStyle 提取 baseName
            // 例如: "hardcore_full" → "hardcore"
            // "poisoned_hardcore_full" → "poisoned_hardcore"
            // "absorbing_full" → "absorbing"
            string baseName = iconStyle.Replace("_full", "");

            // 根据不同的 suffix 使用不同的命名规则
            // 实际文件命名：
            // - container: container.png, container_hardcore.png, container_hardcore_blinking.png
            // - full/half: {baseName}_full.png, {baseName}_half.png
            // - blinking: {baseName}_full_blinking.png, {baseName}_half_blinking.png
            //
            // 特殊情况：
            // - container 只有 hardcore 和 vehicle 有特定版本
            // - poisoned_hardcore、withered_hardcore、frozen_hardcore、absorbing、absorbing_hardcore 没有 container 版本

            if (suffix == "container")
            {
                // container 只有 hardcore 和 vehicle 有特定版本
                if (baseName == "hardcore" || baseName == "vehicle")
                {
                    return $"assets/minecraft/textures/gui/sprites/hud/heart/container_{baseName}.png";
                }
                // 其他样式没有 container 版本，使用默认
                return $"assets/minecraft/textures/gui/sprites/hud/heart/container.png";
            }
            else if (suffix == "container_blinking")
            {
                // container_blinking 只有 hardcore 有特定版本
                if (baseName == "hardcore")
                {
                    return $"assets/minecraft/textures/gui/sprites/hud/heart/container_{baseName}_blinking.png";
                }
                // 其他使用默认
                return $"assets/minecraft/textures/gui/sprites/hud/heart/container_blinking.png";
            }
            else if (suffix.Contains("_blinking"))
            {
                // full_blinking, half_blinking
                return $"assets/minecraft/textures/gui/sprites/hud/heart/{baseName}_{suffix}.png";
            }
            else
            {
                // full, half
                return $"assets/minecraft/textures/gui/sprites/hud/heart/{baseName}_{suffix}.png";
            }
        }

        /// <summary>
        /// 检查生命值图标文件是否存在，不存在则返回默认图标路径
        /// </summary>
        public static string GetHeartPathWithFallback(string iconStyle, string suffix)
        {
            string specificPath = GetHeartPath(iconStyle, suffix);
            string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, specificPath);

            if (System.IO.File.Exists(fullPath))
            {
                return specificPath;
            }

            // 文件不存在，回退到默认图标
            return $"assets/minecraft/textures/gui/sprites/hud/heart/{suffix}.png";
        }

        /// <summary>
        /// 根据 IconStyle 获取饥饿值图标路径
        /// IconStyle: food_full, food_full_hunger
        /// </summary>
        public static string GetFoodPath(string iconStyle, string suffix)
        {
            // suffix: full, half, empty
            // 如果 iconStyle 为空或默认，使用原始路径
            if (string.IsNullOrEmpty(iconStyle) || iconStyle == "food_full")
            {
                return $"assets/minecraft/textures/gui/sprites/hud/food/food_{suffix}.png";
            }

            // hunger 后缀样式
            // 例如: iconStyle="food_full_hunger", suffix="half" → "food_half_hunger.png"
            return $"assets/minecraft/textures/gui/sprites/hud/food/food_{suffix}_hunger.png";
        }

        /// <summary>
        /// 根据 IconStyle 获取伤害吸收值图标路径
        /// IconStyle: absorbing_full, absorbing_hardcore_full
        /// </summary>
        public static string GetAbsorbingPath(string iconStyle, string suffix)
        {
            // suffix: full, half
            // 如果 iconStyle 为空或默认，使用原始路径
            if (string.IsNullOrEmpty(iconStyle) || iconStyle == "absorbing_full")
            {
                return $"assets/minecraft/textures/gui/sprites/hud/heart/absorbing_{suffix}.png";
            }

            // absorbing_hardcore_full → absorbing_hardcore_{suffix}.png
            string baseName = iconStyle.Replace("_full", "");
            return $"assets/minecraft/textures/gui/sprites/hud/heart/{baseName}_{suffix}.png";
        }

        /// <summary>
        /// 检查伤害吸收值图标文件是否存在，不存在则返回默认图标路径
        /// </summary>
        public static string GetAbsorbingPathWithFallback(string iconStyle, string suffix)
        {
            string specificPath = GetAbsorbingPath(iconStyle, suffix);
            string fullPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, specificPath);

            if (System.IO.File.Exists(fullPath))
            {
                return specificPath;
            }

            // 文件不存在，回退到默认图标
            return $"assets/minecraft/textures/gui/sprites/hud/heart/absorbing_{suffix}.png";
        }

        // BOSS血条
        /// <summary>
        /// 根据 IconType 获取 BOSS血条图标路径
        /// IconType: blue, green, red, pink, purple, white, yellow
        /// suffix: background, progress
        /// </summary>
        public static string GetBossBarPath(string iconType, string suffix = "progress")
        {
            if (string.IsNullOrEmpty(iconType))
            {
                return $"assets/minecraft/textures/gui/sprites/hud/boss_bar/blue_{suffix}.png";
            }
            return $"assets/minecraft/textures/gui/sprites/hud/boss_bar/{iconType}_{suffix}.png";
        }

        /// <summary>
        /// 根据 NotchType 获取 Notch图标路径
        /// NotchType: notched_6, notched_10, notched_12, notched_20
        /// suffix: background, progress
        /// </summary>
        public static string GetNotchPath(string notchType, string suffix = "progress")
        {
            if (string.IsNullOrEmpty(notchType))
            {
                return "";
            }
            return $"assets/minecraft/textures/gui/sprites/hud/boss_bar/{notchType}_{suffix}.png";
        }
    }
}