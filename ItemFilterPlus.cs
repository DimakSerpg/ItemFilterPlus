using BepInEx;
using BepInEx.Configuration;
using HarmonyLib;
using ItemFilterPlus;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Networking;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class ModConfig
{
    // General Settings
    public static ConfigEntry<bool> ModEnabled { get; private set; }
    public static ConfigEntry<int> FontSizeModifier { get; private set; }

    // Whitelist Highlight Settings
    public static ConfigEntry<bool> HighlightEnabled { get; private set; }
    public static ConfigEntry<string> HighlightEffectName { get; private set; }
    public static ConfigEntry<bool> HighlightSoundEnabled { get; private set; }
    public static ConfigEntry<bool> UseCustomHighlightSound { get; private set; }
    public static ConfigEntry<float> CustomSoundVolume { get; private set; }
    public static ConfigEntry<string> SavedSortStates { get; private set; }

    public static void Initialize(ConfigFile config)
    {
        ModEnabled = config.Bind("1. General", "ModEnabled", true, "Globally enables or disables the mod filtering logic.");
        FontSizeModifier = config.Bind("1. General", "FontSizeModifier", 0, new ConfigDescription("Font size modifier for the filter list window.", new AcceptableValueRange<int>(0, 5)));
        HighlightEnabled = config.Bind("2. Whitelist Highlight", "HighlightEnabled", true, "Enables a visual effect for whitelisted items.");
        HighlightEffectName = config.Bind("2. Whitelist Highlight", "HighlightEffectName", "aura_heaven", "Name of the effect for the whitelisted items. Options: 'aura_heaven', 'hit_light'.");
        HighlightSoundEnabled = config.Bind("2. Whitelist Highlight", "HighlightSoundEnabled", true, "Turns on the sound for whitelisted items when they appear on the screen.");
        UseCustomHighlightSound = config.Bind("2. Whitelist Highlight", "UseCustomHighlightSound", false, "If true, uses 'drop.mp3' from the mod folder. If false, uses the standard sound.");
        CustomSoundVolume = config.Bind("2. Whitelist Highlight", "CustomSoundVolume", 70f, new ConfigDescription("Volume for custom whitelisted sound (0 to 100).", new AcceptableValueRange<float>(0f, 100f)));
        SavedSortStates = config.Bind("3. Internal", "SavedSortStates", "", "Stores the last used sort states for different UI contexts. Do not edit manually.");
    }
}


[BepInPlugin("dimakserpg.elin.itemfilterplus", "Item Filter Plus", "1.0.0")]
public class ItemFilterPlusPlugin : BaseUnityPlugin
{
    public static ItemFilterPlusPlugin Instance { get; private set; }
    private readonly Harmony harmony = new Harmony("dimakserpg.elin.itemfilterplus");

    void Awake()
    {
        ModConfig.Initialize(Config);
        ItemFilterPlus.IngredientSortManager.Initialize(ModConfig.SavedSortStates);

        Instance = this;
        harmony.PatchAll(typeof(FilterSubmenuPatch));
        harmony.PatchAll(typeof(ActMenuIntegrationPatch));
        harmony.PatchAll(typeof(ContainerFilterButtonPatch));
        harmony.PatchAll(typeof(ManualActionTrackingPatches));
        harmony.PatchAll(typeof(ItemFilteringPatches));
        harmony.PatchAll(typeof(ItemHighlightManager));
        harmony.PatchAll(typeof(WhitelistHighlightPatch_DropdownGrid));
        harmony.PatchAll(typeof(WhitelistHighlightPatch_Inventory));
        harmony.PatchAll(typeof(WhitelistHighlightPatch_CraftingIngredients));
        harmony.PatchAll(typeof(IngredientSortPatch));

        StartCoroutine(DelayedSoundInitialization());
        Logger.LogInfo("Item Filter Plus 1.0.0 loaded. Settings initialized. Waiting for game's ModManager to initialize...");
    }

    private System.Collections.IEnumerator DelayedSoundInitialization()
    {
        while (SoundManager.current == null) yield return null;
        StartCoroutine(LoadCustomSound());
        StartCoroutine(PrepareDefaultSound());
    }

    private System.Collections.IEnumerator LoadCustomSound()
    {
        string modPath = Path.GetDirectoryName(Info.Location);
        string soundPath = Path.Combine(modPath, "drop.mp3");

        if (File.Exists(soundPath))
        {
            using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + soundPath, AudioType.MPEG))
            {
                yield return www.SendWebRequest();
                if (www.result == UnityWebRequest.Result.Success)
                {
                    AudioClip clip = DownloadHandlerAudioClip.GetContent(www);
                    SoundData templateSound = SoundManager.current.GetData("drop");
                    if (templateSound != null)
                    {
                        ItemHighlightManager.customHighlightSoundData = Instantiate(templateSound);
                        ItemHighlightManager.customHighlightSoundData.clip = clip;
                        ItemHighlightManager.customHighlightSoundData.name = "CustomItemFilterDropSound";
                        ItemHighlightManager.customHighlightSoundData.pitch = 1f;
                        ItemHighlightManager.customHighlightSoundData.randomPitch = 0f;
                        Logger.LogInfo("Custom highlight sound 'drop.mp3' loaded and configured successfully.");
                    }
                    else Logger.LogError("Failed to find template sound 'drop'. Custom sound will not work.");
                }
                else Logger.LogError("Failed to load custom sound 'drop.mp3': " + www.error);
            }
        }
        else Logger.LogInfo("Custom highlight sound 'drop.mp3' not found. Using default sound.");
    }

    private System.Collections.IEnumerator PrepareDefaultSound()
    {
        SoundData original = SoundManager.current.GetData("godbless");
        if (original != null)
        {
            ItemHighlightManager.defaultHighlightSoundData = Instantiate(original);
            ItemHighlightManager.defaultHighlightSoundData.name = "CustomItemFilterGodblessSound";
            ItemHighlightManager.defaultHighlightSoundData.pitch = 1f;
            ItemHighlightManager.defaultHighlightSoundData.randomPitch = 0f;
        }
        else Logger.LogError("Failed to find template sound 'godbless' for default highlight sound.");
        yield return null;
    }
}

namespace ItemFilterPlus
{
    public static class StatFilterData
    {
        public static readonly Dictionary<string, int[]> Categories = new Dictionary<string, int[]>
        {
            { "Main Attributes", new[] { 70, 71, 72, 73, 74, 75, 76, 77, 78, 79, 80 } },
            { "Combat Stats", new[] { 64, 65, 66, 67, 68, 90, 91, 92, 93, 94, 300 } },
            { "Vital Stats", new[] { 60, 61, 62, 55, 56, 57, 151 } },
            { "Resistances", new[] { 950, 951, 952, 953, 954, 955, 956, 957, 958, 959, 960, 961, 962, 963, 964, 965, 970, 971, 972 } },
            { "Combat and Weapon Skills", new[] { 100, 101, 102, 103, 104, 105, 106, 107, 108, 109, 110, 111, 120, 122, 123, 130, 131, 132, 133, 134, 135 } },
            { "Magic Skills", new[] { 301, 302, 303, 304, 305, 306, 307 } },
            { "Peaceful Skills 1", new[] { 150, 152, 200, 207, 210, 220, 225, 226, 227, 230, 235, 237, 240, 241, 242, 245 } },
            { "Peaceful Skills 2", new[] { 250, 255, 256, 257, 258, 259, 260, 261, 280, 281, 285, 286, 287, 288, 289, 290, 291, 292, 293 } },
            { "Melee Modifiers", new[] { 620, 621, 622, 623, 624, 608 } },
            { "Ranged Modifiers", new[] { 600, 601, 602, 603, 604, 605, 606, 607, 609 } },
            { "Sustain Attributes", new[] { 440, 441, 442, 443, 444, 445, 446, 447, 450 } },
            { "Status Effect Negation", new[] { 400, 406, 420, 421, 422, 423, 424, 425, 426, 427, 430, 431, 491 } },
            { "Combat Passives", new[] { 380, 381, 382, 383, 435, 436, 437, 438, 439 } },
            { "Bane (Slayer) Effects", new[] { 460, 461, 462, 463, 464, 465, 466, 467, 468 } },
            { "Elemental Conversion", new[] { 850, 851, 852, 865 } },
            { "Elemental Damage", new[] { 910, 911, 912, 913, 914, 915, 916, 917, 918, 919, 920, 921, 922, 923, 924, 925, 926 } },
            { "Material Proof", new[] { 50, 51 } },
            { "Utility & Exploration", new[] { 402, 403, 404, 405, 407, 408, 480, 481, 483, 486, 487, 489, 490 } },
            { "Special Abilities", new[] { 401, 410, 415, 416, 418, 428, 429, 432, 664, 665, 666, 667 } },
            { "Miscellaneous Enchantments", new[] { 409, 411, 412, 414, 417, 419, 482, 484, 485, 488, 640, 641, 650, 651, 652, 653, 654, 655, 656, 660, 661, 662, 663 } },
            { "Faith & Divine", new[] { 1636, 1300, 1305, 1310, 1315, 1320, 1325, 1330, 1335, 1340, 1345, 1350, 1355, 1407, 1408 } }
        };
    }
}

public static class ItemFilterLogic
{
    private const int HUMAN_FLESH_ID = 708;
    private const int UNDEAD_FLESH_ID = 709;
    private const int CAT_MEAT_ID = 701;
    private const int BUG_MEAT_ID = 704;
    private const int RAW_FISH_ID = 707;
    private const int APHRODISIAC_ID = 758;
    private const int FOOD_QUALITY_ID = 2;

    public static bool IsEnabled { get => ModConfig.ModEnabled.Value; set => ModConfig.ModEnabled.Value = value; }
    public static bool HighlightEnabled { get => ModConfig.HighlightEnabled.Value; set => ModConfig.HighlightEnabled.Value = value; }
    public static bool HighlightSoundEnabled { get => ModConfig.HighlightSoundEnabled.Value; set => ModConfig.HighlightSoundEnabled.Value = value; }
    public static bool UseCustomHighlightSound { get => ModConfig.UseCustomHighlightSound.Value; set => ModConfig.UseCustomHighlightSound.Value = value; }
    public static float CustomSoundVolume { get => ModConfig.CustomSoundVolume.Value; set => ModConfig.CustomSoundVolume.Value = value; }
    public static string HighlightEffectName { get => ModConfig.HighlightEffectName.Value; set => ModConfig.HighlightEffectName.Value = value; }

    private static readonly Dictionary<string, List<string>> filterCache = new Dictionary<string, List<string>>();

    public static List<string> GetRules(string filterString)
    {
        if (string.IsNullOrEmpty(filterString)) return new List<string>();
        if (filterCache.TryGetValue(filterString, out var cachedRules)) return cachedRules;
        var rules = filterString.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).ToList();
        filterCache[filterString] = rules;
        return rules;
    }

    private static int GetRuleSpecificity(string rule)
    {
        string cleanRule = rule.TrimStart('+', '-');
        if (cleanRule.StartsWith("stat:")) return 6;
        if (cleanRule.StartsWith("name:") || cleanRule.StartsWith("state:") || cleanRule.StartsWith("name_loc:")) return 5;
        if (cleanRule.StartsWith("tainted:") || cleanRule.StartsWith("human:") || cleanRule.StartsWith("undead:") || cleanRule.StartsWith("catmeat:") || cleanRule.StartsWith("bugmeat:") || cleanRule.StartsWith("rawfish:") || cleanRule.StartsWith("fresh:") || cleanRule.StartsWith("rotting:")) return 5;
        if (cleanRule.StartsWith("potential:") || cleanRule.StartsWith("foodstat:")) return 4;
        if (cleanRule.StartsWith("hardness:")) return 4;
        if (cleanRule.StartsWith("rarity:")) return 4;
        if (cleanRule.StartsWith("mat:")) return 3;
        if (cleanRule.StartsWith("quality_")) return 3;
        if (cleanRule.StartsWith("type:")) return 2;
        if (cleanRule.StartsWith("cat:")) return 1;
        return 0;
    }

    public static string GetReorderedFilterString(List<string> rules)
    {
        var sortedRules = rules.OrderBy(r => GetRuleSpecificity(r)).ToList();
        return string.Join(",", sortedRules);
    }

    private enum FilterStatus { Allowed, Blocked, NoMatch }

    private static FilterStatus GetItemStatus(Thing t, string filterString)
    {
        var rules = GetRules(filterString);
        if (rules.Count == 0) return FilterStatus.NoMatch;

        for (int i = rules.Count - 1; i >= 0; i--)
        {
            var rule = rules[i];
            if (CheckRuleMatch(t, rule.Split('#')[0]))
            {
                if (rule.StartsWith("+")) return FilterStatus.Allowed;
                if (rule.StartsWith("-")) return FilterStatus.Blocked;
            }
        }
        return FilterStatus.NoMatch;
    }



    public static bool IsItemWhitelisted(Thing t, string filterString)
    {
        if (!IsEnabled || string.IsNullOrEmpty(filterString)) return false;
        return GetItemStatus(t, filterString) == FilterStatus.Allowed;
    }

    public static bool IsItemAllowed(Thing t, string filterString)
    {
        if (!IsEnabled) return true;
        if (string.IsNullOrEmpty(filterString)) return true;
        return GetItemStatus(t, filterString) != FilterStatus.Blocked;
    }

    public static bool IsItemWhitelistedForWorld(Thing t, string filterString)
    {
        var rules = GetRules(filterString);
        if (rules.Count == 0) return false;
        for (int i = rules.Count - 1; i >= 0; i--)
        {
            var rule = rules[i];
            if (CheckRuleMatch(t, rule.Split('#')[0]))
            {
                if (rule.StartsWith("+") && !rule.Contains("#noworld")) return true;
                return false;
            }
        }
        return false;
    }

    public static bool IsItemWhitelistedForInventory(Thing t, string filterString)
    {
        var rules = GetRules(filterString);
        if (rules.Count == 0) return false;
        for (int i = rules.Count - 1; i >= 0; i--)
        {
            var rule = rules[i];
            if (CheckRuleMatch(t, rule.Split('#')[0]))
            {
                if (rule.StartsWith("+") && !rule.Contains("#noinv")) return true;
                return false;
            }
        }
        return false;
    }



    private static int GetFoodStatValue(Thing t, int statId)
    {
        int rawValue = t.elements.Value(statId);
        return CalculateDisplayedFoodTraitLevel(rawValue);
    }

    private static int GetPotentialValue(Thing t, int statId)
    {
        foreach (var element in t.elements.dict.Values)
        {
            var foodEffect = element.source.foodEffect;
            if (!foodEffect.IsEmpty() && foodEffect[0] == "pot" && foodEffect.Length > 1)
            {
                if (EClass.sources.elements.alias.TryGetValue(foodEffect[1], out var targetStatSource) && targetStatSource.id == statId)
                {
                    return CalculateDisplayedFoodTraitLevel(element.Value);
                }
            }
        }
        return 0;
    }
    private static int CalculateDisplayedFoodTraitLevel(int rawValue)
    {
        if (rawValue == 0) return 0;
        int displayedLevel = rawValue / 10;
        return (rawValue < 0) ? (displayedLevel - 1) : (displayedLevel + 1);
    }


    private static bool CheckRuleMatch(Thing t, string rule)
    {
        string cleanRule = rule.TrimStart('+', '-');
        var parts = cleanRule.Split(new[] { ':' }, 2);
        if (parts.Length < 2) return false;

        string key = parts[0];
        string value = parts[1];

        switch (key)
        {
            case "stat":
                if (!t.IsEquipmentOrRangedOrAmmo) return false;
                var statParts = value.Split('|');
                if (statParts.Length != 2) return false;
                if (int.TryParse(statParts[0], out int statId) && int.TryParse(statParts[1], out int requiredValue))
                {
                    int actualValue = statId == -1 ? t.GetPrice(CurrencyType.Money, false, PriceType.Default) : t.elements.Value(statId);
                    if (rule.StartsWith("-"))
                    {
                        // For blacklist: only match items that HAVE the stat (value > 0) AND value < threshold
                        // Items without the stat should not be blocked
                        return actualValue > 0 && actualValue < requiredValue;
                    }
                    return actualValue >= requiredValue;
                }
                return false;

            case "foodstat":
                if (!t.IsInheritFoodTraits) return false;
                var foodStatParts = value.Split('|');
                if (foodStatParts.Length != 2) return false;
                if (int.TryParse(foodStatParts[0], out int foodStatId) && int.TryParse(foodStatParts[1], out int foodRequiredValue))
                {
                    int statValue = GetFoodStatValue(t, foodStatId);
                    if (rule.StartsWith("-"))
                    {
                        return statValue > 0 && statValue < foodRequiredValue;
                    }
                    return statValue >= foodRequiredValue;
                }
                return false;

            case "potential":
                if (!t.IsInheritFoodTraits) return false;
                var potParts = value.Split('|');
                if (potParts.Length != 2) return false;
                if (int.TryParse(potParts[0], out int potStatId) && int.TryParse(potParts[1], out int potRequiredValue))
                {
                    int potentialValue = GetPotentialValue(t, potStatId);
                    if (rule.StartsWith("-"))
                    {
                        return potentialValue > 0 && potentialValue < potRequiredValue;
                    }
                    return potentialValue >= potRequiredValue;
                }
                return false;

            case "hardness":
                if (value.StartsWith(">") && int.TryParse(value.Substring(1), out int requiredHardness))
                {
                    int actualHardness = t.material?.hardness ?? 0;
                    if (rule.StartsWith("-"))
                    {
                        return actualHardness > 0 && actualHardness < requiredHardness;
                    }
                    return actualHardness >= requiredHardness;
                }
                return false;

            case "name_loc": return string.Equals(t.GetName(NameStyle.Simple, 1).Trim(), value, StringComparison.OrdinalIgnoreCase);
            case "name":
                var nameParts = value.Split('|');
                if (nameParts.Length != 2) return false;
                return string.Equals(t.id, nameParts[0], StringComparison.OrdinalIgnoreCase) && string.Equals(t.c_idRefCard, nameParts[1], StringComparison.OrdinalIgnoreCase);
            case "type": return string.Equals(t.id, value, StringComparison.OrdinalIgnoreCase);
            case "cat": return EClass.sources.categories.map.TryGetValue(value, out var categoryRow) && t.category.IsChildOf(categoryRow);
            case "mat": return int.TryParse(value, out int ruleMatId) && t.material != null && t.material.id == ruleMatId;
            case "rarity": return string.Equals(t.rarity.ToString(), value, StringComparison.OrdinalIgnoreCase);
            case "state": return string.Equals(t.blessedState.ToString(), value, StringComparison.OrdinalIgnoreCase);

            case "tainted": return t.IsDecayed == bool.Parse(value);
            case "human": return t.elements.dict.ContainsKey(HUMAN_FLESH_ID) == bool.Parse(value);
            case "undead": return t.elements.dict.ContainsKey(UNDEAD_FLESH_ID) == bool.Parse(value);
            case "catmeat": return t.elements.dict.ContainsKey(CAT_MEAT_ID) == bool.Parse(value);
            case "bugmeat": return t.elements.dict.ContainsKey(BUG_MEAT_ID) == bool.Parse(value);
            case "rawfish": return t.elements.dict.ContainsKey(RAW_FISH_ID) == bool.Parse(value);
            case "aphrodisiac": return t.elements.dict.ContainsKey(APHRODISIAC_ID) == bool.Parse(value);
            case "quality_gt":
                if (int.TryParse(value, out int gtValue)) return t.Evalue(FOOD_QUALITY_ID) >= gtValue;
                return false;
            case "quality_lt":
                if (int.TryParse(value, out int ltValue)) return t.Evalue(FOOD_QUALITY_ID) < ltValue;
                return false;

            case "identify": return t.IsIdentified == bool.Parse(value);
            case "stolen": return t.isStolen == bool.Parse(value);
            case "fresh": 
                // Check if item has decay mechanics (trait.Decay != 0), not just category
                if (t.trait == null || t.trait.Decay == 0) return false;
                return t.IsFresn == bool.Parse(value);  // Note: IsFresn is the correct spelling in the game code
            case "rotting": 
                // Check if item has decay mechanics (trait.Decay != 0), not just category
                if (t.trait == null || t.trait.Decay == 0) return false;
                return t.IsRotting == bool.Parse(value);
        }
        return false;
    }
}
[HarmonyPatch]
public static class ManualActionTrackingPatches
{
    public static bool IsManualActionInProgress { get; private set; }

    [HarmonyPrefix, HarmonyPatch(typeof(ActPick), nameof(ActPick.Perform))]
    public static void BeforeActPick() => IsManualActionInProgress = true;

    [HarmonyPostfix, HarmonyPatch(typeof(ActPick), nameof(ActPick.Perform))]
    public static void AfterActPick() => IsManualActionInProgress = false;

    [HarmonyPrefix, HarmonyPatch(typeof(Chara), nameof(Chara.HoldCard))]
    public static void BeforeHoldCard() => IsManualActionInProgress = true;

    [HarmonyPostfix, HarmonyPatch(typeof(Chara), nameof(Chara.HoldCard))]
    public static void AfterHoldCard() => IsManualActionInProgress = false;
}

[HarmonyPatch]
public static class ItemFilteringPatches
{
    public static void ReapplyFilterToAllItems()
    {
        if (EClass.game?.activeZone?.map?.props?.roaming?.Things == null) return;
        foreach (var thing in EClass.game.activeZone.map.props.roaming.Things) ApplyIgnoreFlag(thing);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Zone), nameof(Zone.Activate))]
    private static void ReapplyFilterOnZoneActivation(Zone __instance)
    {
        if (!__instance.IsActiveZone || __instance.map?.props?.roaming?.Things == null) return;
        foreach (var thing in __instance.map.props.roaming.Things) ApplyIgnoreFlag(thing);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Map), nameof(Map.OnCardAddedToZone))]
    private static void FilterItemOnMapPlacement(Card t)
    {
        if (t is Thing thing && thing.placeState == PlaceState.roaming) ApplyIgnoreFlag(thing);
    }

    [HarmonyPrefix, HarmonyPatch(typeof(Chara), nameof(Chara.PickOrDrop), new Type[] { typeof(Point), typeof(Thing), typeof(bool) })]
    private static bool FilterDirectPickup(Chara __instance, Point p, Thing t)
    {
        if (!__instance.IsPC || !ItemFilterLogic.IsEnabled) return true;
        if (ItemFilterLogic.IsItemAllowed(t, EMono.player?.dataPick?.filter))
        {
            t.ignoreAutoPick = false;
            return true;
        }
        EClass._zone.AddCard(t, p);
        return false;
    }

    private static void ApplyIgnoreFlag(Thing thing)
    {
        if (!ItemFilterLogic.IsEnabled) { thing.ignoreAutoPick = false; return; }
        var filterString = EMono.player?.dataPick?.filter;
        if (string.IsNullOrEmpty(filterString)) { thing.ignoreAutoPick = false; return; }
        thing.ignoreAutoPick = !ItemFilterLogic.IsItemAllowed(thing, filterString);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(TaskDump), nameof(TaskDump.ListThingsToPut))]
    public static void ApplyContainerFiltersToDump(ref List<Thing> __result, Thing c)
    {
        if (!ItemFilterLogic.IsEnabled) return;
        
        var container = c.trait is TraitShippingChest ? EClass.game.cards.container_shipping : c;
        var data = container.GetWindowSaveData();
        var filterString = data?.filter;
        
        // Capture __result to a local variable for use in lambdas
        var resultList = __result;
        
        // Build a set of UIDs of items that are in excluded containers in player inventory
        // This is more reliable than checking parent references at filtering time
        var excludedItemUIDs = new HashSet<int>();
        CollectExcludedContainerItemUIDs(EClass.pc.things, excludedUIDs: excludedItemUIDs);
        
        // Filter out items that are in excluded containers
        if (excludedItemUIDs.Count > 0)
        {
            resultList.RemoveAll(item => excludedItemUIDs.Contains(item.uid));
        }
        
        // If this container has a filter, apply special whitelist logic
        if (!string.IsNullOrEmpty(filterString))
        {
            // Remove items that are blacklisted by this container
            resultList.RemoveAll(item => !ItemFilterLogic.IsItemAllowed(item, filterString));
            
            // Add items that are whitelisted by this container but weren't included by game's normal distribution
            // This makes whitelisted items bypass the normal game distribution rules
            // But skip items in excluded containers
            EClass.pc.things.Foreach(item => {
                if (!excludedItemUIDs.Contains(item.uid) && !IsExcludedFromDump(item, data) && ItemFilterLogic.IsItemWhitelisted(item, filterString))
                {
                    if (!resultList.Contains(item))
                    {
                        resultList.Add(item);
                    }
                }
            });
        }
        
        // Check if items are whitelisted in OTHER containers - if so, they should only go there
        // This prevents whitelisted items from being distributed to non-matching containers
        if (resultList.Count > 0)
        {
            var containersWithWhitelists = GetContainersWithWhitelists();
            resultList.RemoveAll(item => {
                foreach (var otherContainer in containersWithWhitelists)
                {
                    if (otherContainer.uid == container.uid) continue; // Skip current container
                    var otherData = otherContainer.GetWindowSaveData();
                    if (otherData == null) continue;
                    
                    // If item is whitelisted in another container, don't put it in this one unless also whitelisted here
                    if (ItemFilterLogic.IsItemWhitelisted(item, otherData.filter))
                    {
                        // Item is whitelisted elsewhere - only allow if also whitelisted here
                        if (!ItemFilterLogic.IsItemWhitelisted(item, filterString))
                        {
                            return true; // Remove from this container's list
                        }
                    }
                }
                return false;
            });
        }
    }
    
    // Recursively collect UIDs of all items inside containers that have "Exclude from auto-dump" enabled
    private static void CollectExcludedContainerItemUIDs(ThingContainer things, HashSet<int> excludedUIDs)
    {
        foreach (Thing item in things)
        {
            // Check any container in player inventory
            if (item.IsContainer)
            {
                var saveData = item.c_windowSaveData; // Direct access, not GetWindowSaveData()
                
                // Check excludeDump property (this is "Exclude items from auto-dumping" in config menu)
                bool hasExclude = saveData != null && saveData.excludeDump;
                
                if (hasExclude)
                {
                    // This container is excluded - add all items inside it (recursively)
                    AddAllItemUIDs(item.things, excludedUIDs);
                }
                else if (item.CanSearchContents)
                {
                    // Container not excluded - check its contents for excluded sub-containers
                    CollectExcludedContainerItemUIDs(item.things, excludedUIDs);
                }
            }
        }
    }
    
    // Add UIDs of all items in a container (recursively) to the set
    private static void AddAllItemUIDs(ThingContainer things, HashSet<int> uids)
    {
        foreach (Thing item in things)
        {
            uids.Add(item.uid);
            if (item.IsContainer && item.CanSearchContents)
            {
                AddAllItemUIDs(item.things, uids);
            }
        }
    }
    
    private static bool IsExcludedFromDump(Thing t, Window.SaveData data)
    {
        if (t.isEquipped || t.c_isImportant || t.trait.CanOnlyCarry || !t.trait.CanBeDropped || t.IsHotItem || t.trait is TraitToolBelt || t.trait is TraitAbility)
            return true;
        if (t.IsContainer && t.things.Count > 0)
            return true;
        if (data != null)
        {
            if (data.noRotten && t.IsDecayed)
                return true;
            if (data.onlyRottable && t.trait.Decay == 0)
                return true;
        }
        // Check if item is in any ancestor container that has "exclude from auto-dumping" enabled
        // Need to traverse up the parent chain to find containers with excludeDump set
        if (IsInExcludedContainer(t))
            return true;
        return false;
    }
    
    private static bool IsInExcludedContainer(Thing t)
    {
        // Check if the item's direct parent is a container with excludeDump enabled
        // This is the "Exclude items from auto-dumping" setting in container config
        if (t.parent is Card parent)
        {
            // If parent is PC, the item is directly in player inventory (not in a container)
            if (parent.IsPC)
                return false;
            
            // If parent is a Thing (container), check its excludeDump setting
            if (parent is Thing parentContainer)
            {
                var saveData = parentContainer.GetWindowSaveData();
                if (saveData != null && saveData.excludeDump)
                    return true;
                
                // Also check if the parent container itself is in an excluded container (recursive)
                if (IsInExcludedContainer(parentContainer))
                    return true;
            }
        }
        return false;
    }
    
    private static List<Thing> GetContainersWithWhitelists()
    {
        var result = new List<Thing>();
        if (EClass._map?.things == null) return result;
        
        EClass._map.things.ForEach(t => {
            if (!t.ExistsOnMap || !t.IsInstalled || !t.CanSearchContents) return;
            var container = t.trait is TraitShippingChest ? EClass.game.cards.container_shipping : t;
            var saveData = container.GetWindowSaveData();
            if (saveData != null && !string.IsNullOrEmpty(saveData.filter) && saveData.filter.Contains("+"))
            {
                result.Add(container);
            }
        });
        return result;
    }
}

[HarmonyPatch]
public static class ItemHighlightManager
{
    private static readonly HashSet<Thing> itemsToWatchForHighlight = new HashSet<Thing>();
    private static readonly Dictionary<Thing, Effect> highlightEffects = new Dictionary<Thing, Effect>();
    private static readonly HashSet<int> alreadyPlayedSoundForUIDs = new HashSet<int>();

    private static float soundTimer = 0f;
    private static int soundCountInInterval = 0;
    private const int MAX_SOUNDS_IN_INTERVAL = 6;
    private const float SOUND_RESET_INTERVAL = 3.0f;
    public static SoundData customHighlightSoundData;
    public static SoundData defaultHighlightSoundData;

    [HarmonyPostfix, HarmonyPatch(typeof(Map), nameof(Map.OnCardAddedToZone))]
    private static void OnItemPlacedOnMap(Card t)
    {
        if (t is Thing thing) CheckIfNeedsWatching(thing);
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Fov), nameof(Fov.Perform))]
    private static void OnFovUpdated(Fov __instance)
    {
        if (!__instance.isPC || itemsToWatchForHighlight.Count == 0) return;
        var newlyVisibleItems = new List<Thing>();
        foreach (var thing in itemsToWatchForHighlight)
        {
            if (thing != null && !thing.isDestroyed && thing.pos.cell.isSeen) newlyVisibleItems.Add(thing);
        }
        if (newlyVisibleItems.Count > 0)
        {
            foreach (var thing in newlyVisibleItems)
            {
                AddHighlightAndSound(thing);
                itemsToWatchForHighlight.Remove(thing);
            }
        }
    }

    [HarmonyPostfix, HarmonyPatch(typeof(Scene), "OnUpdate")]
    public static void UpdateAndCleanup(Scene __instance)
    {
        if (__instance.mode != Scene.Mode.Zone) ClearAll();
        soundTimer += Time.deltaTime;
        if (soundTimer >= SOUND_RESET_INTERVAL)
        {
            soundCountInInterval = 0;
            soundTimer = 0f;
        }
    }

    private static void CheckIfNeedsWatching(Thing thing)
    {
        string filterString = EMono.player?.dataPick?.filter;
        if (ItemFilterLogic.IsEnabled && ItemFilterLogic.HighlightEnabled && !string.IsNullOrEmpty(filterString) && ItemFilterLogic.IsItemWhitelistedForWorld(thing, filterString))
        {
            if (thing.pos.cell.isSeen) AddHighlightAndSound(thing);
            else itemsToWatchForHighlight.Add(thing);
        }
    }

    private static void AddHighlightAndSound(Thing thing)
    {
        if (highlightEffects.ContainsKey(thing)) return;
        Effect highlight = Effect.Get(ItemFilterLogic.HighlightEffectName);
        if (highlight != null)
        {
            highlight.Play(thing.pos);
            if (ItemFilterLogic.HighlightEffectName == "aura_heaven")
            {
                highlight.SetParticleColor(new Color(1f, 0.9f, 0.4f, 0.5f));
                highlight.SetScale(0.6f);
            }
            highlightEffects[thing] = highlight;
        }
        if (ItemFilterLogic.HighlightSoundEnabled && !alreadyPlayedSoundForUIDs.Contains(thing.uid))
        {
            if (soundCountInInterval < MAX_SOUNDS_IN_INTERVAL)
            {
                soundCountInInterval++;
                if (ItemFilterLogic.UseCustomHighlightSound && customHighlightSoundData != null)
                {
                    customHighlightSoundData.volume = ItemFilterLogic.CustomSoundVolume / 100f;
                    SoundManager.current.Play(customHighlightSoundData);
                }
                else if (defaultHighlightSoundData != null)
                {
                    defaultHighlightSoundData.volume = 0.05f;
                    SoundManager.current.Play(defaultHighlightSoundData);
                }
            }
            alreadyPlayedSoundForUIDs.Add(thing.uid);
        }
    }

    private static void ClearAll()
    {
        itemsToWatchForHighlight.Clear();
        foreach (var effect in highlightEffects.Values) effect?.Kill();
        highlightEffects.Clear();
        alreadyPlayedSoundForUIDs.Clear();
    }
}

#region UI & Integration Patches

[HarmonyPatch]
public static class ContainerFilterButtonPatch
{
    [HarmonyPostfix, HarmonyPatch(typeof(UIInventory), nameof(UIInventory.RefreshMenu))]
    public static void AddContainerFilterButton(UIInventory __instance)
    {
        var data = __instance.window.saveData;
        if (__instance.owner.Container.IsPC || !__instance.owner.Container.IsContainer || data == null) return;
        var settingsButton = __instance.window.buttonSort;
        if (settingsButton == null) return;
        settingsButton.onClick.AddListener(() =>
        {
            EClass.core.actionsNextFrame.Add(() =>
            {
                var contextMenu = UIContextMenu.Current;
                if (contextMenu != null)
                {
                    var filterSubmenu = contextMenu.AddChild(ItemFilterPlusTranslations.Get("Container Filters"), TextAnchor.UpperRight);
                    filterSubmenu.AddButton(ItemFilterPlusTranslations.Get("Open"), () => FilterManagerWindow.Toggle(data));
                }
            });
        });
    }
}

[HarmonyPatch]
public static class FilterSubmenuPatch
{
    private enum FilterAction { Whitelist, Blacklist, Remove }

    private static void ModifyFilter(Window.SaveData filterData, string keyword, FilterAction action)
    {
        if (filterData == null) return;
        var rules = ItemFilterLogic.GetRules(filterData.filter).ToList();
        rules.RemoveAll(r => r.TrimStart('+', '-') == keyword);
        if (action != FilterAction.Remove)
        {
            string prefix = action == FilterAction.Whitelist ? "+" : "-";
            rules.Add(prefix + keyword);
        }
        filterData.filter = ItemFilterLogic.GetReorderedFilterString(rules);
        filterData._filterStrs = null;
        ItemFilteringPatches.ReapplyFilterToAllItems();
    }

    private static void BuildFilterSubmenu(UIContextMenu menu, Window.SaveData targetData, Thing thing)
    {
        var customFilterSet = new HashSet<string>((targetData.filter ?? "").Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()));
        Func<string, string> findRule = (key) => customFilterSet.FirstOrDefault(r => r.TrimStart('+', '-') == key);

        string itemDisplayName = thing.GetName(NameStyle.Simple, 1).Trim();
        string specificItemKeyword = !string.IsNullOrEmpty(thing.c_idRefCard) ? $"name:{thing.id}|{thing.c_idRefCard}" : $"name_loc:{itemDisplayName}";

        string existingSpecificRule = findRule(specificItemKeyword);
        if (existingSpecificRule != null)
        {
            string text = existingSpecificRule.StartsWith("+") ? "Remove '{0}' from whitelist" : "Remove '{0}' from blacklist";
            menu.AddButton(ItemFilterPlusTranslations.Get(text, itemDisplayName), () => ModifyFilter(targetData, specificItemKeyword, FilterAction.Remove));
        }
        else
        {
            menu.AddButton(ItemFilterPlusTranslations.Get("Add '{0}' to whitelist", itemDisplayName), () => ModifyFilter(targetData, specificItemKeyword, FilterAction.Whitelist));
            menu.AddButton(ItemFilterPlusTranslations.Get("Add '{0}' to blacklist", itemDisplayName), () => ModifyFilter(targetData, specificItemKeyword, FilterAction.Blacklist));
        }

        if (thing.IsInheritFoodTraits)
        {
            menu.AddSeparator();
            var foodSubmenu = menu.AddChild(ItemFilterPlusTranslations.Get("filter_menu_food"));

            // Add food-specific filter options
            foodSubmenu.AddButton(ItemFilterPlusTranslations.Get("Add Food Quality > to Whitelist"), () => FilterManagerWindow.ShowQualityFilterDialog(targetData, true));
            foodSubmenu.AddButton(ItemFilterPlusTranslations.Get("Add Food Quality < to Blacklist"), () => FilterManagerWindow.ShowQualityFilterDialog(targetData, false));

            // Add food property filters
            if (thing.IsDecayed)
                foodSubmenu.AddButton(ItemFilterPlusTranslations.Get("filter_prop_rotten"), () => ModifyFilter(targetData, "tainted:true", FilterAction.Blacklist));
            
            // Always show fresh/rotting options for food items
            foodSubmenu.AddButton("isFresh".lang(), () => ModifyFilter(targetData, "fresh:true", FilterAction.Whitelist));
            foodSubmenu.AddButton("rotting".lang(), () => ModifyFilter(targetData, "rotting:true", FilterAction.Blacklist));
            if (thing.elements.dict.ContainsKey(708))
                foodSubmenu.AddButton(ItemFilterPlusTranslations.Get("filter_prop_human_flesh"), () => ModifyFilter(targetData, "human:true", FilterAction.Blacklist));
            if (thing.elements.dict.ContainsKey(709))
                foodSubmenu.AddButton(ItemFilterPlusTranslations.Get("filter_prop_undead_flesh"), () => ModifyFilter(targetData, "undead:true", FilterAction.Blacklist));
            if (thing.elements.dict.ContainsKey(701))
                foodSubmenu.AddButton(ItemFilterPlusTranslations.Get("filter_prop_cat_meat"), () => ModifyFilter(targetData, "catmeat:true", FilterAction.Blacklist));
            if (thing.elements.dict.ContainsKey(704))
                foodSubmenu.AddButton(ItemFilterPlusTranslations.Get("filter_prop_bug_meat"), () => ModifyFilter(targetData, "bugmeat:true", FilterAction.Blacklist));
            if (thing.elements.dict.ContainsKey(707))
                foodSubmenu.AddButton(ItemFilterPlusTranslations.Get("filter_prop_raw_fish"), () => ModifyFilter(targetData, "rawfish:true", FilterAction.Blacklist));
            if (thing.elements.dict.ContainsKey(758))
                foodSubmenu.AddButton(ItemFilterPlusTranslations.Get("filter_prop_aphrodisiac"), () => ModifyFilter(targetData, "aphrodisiac:true", FilterAction.Blacklist));
        }

        menu.AddSeparator();

        Action<string, string, string> addOption = (typeName, valueName, keyword) =>
        {
            if (findRule(keyword) != null)
            {
                menu.AddButton(ItemFilterPlusTranslations.Get("Remove {0} '{1}' from filter", typeName, valueName), () => ModifyFilter(targetData, keyword, FilterAction.Remove));
            }
            else
            {
                menu.AddButton(ItemFilterPlusTranslations.Get("Add {0} '{1}' to whitelist", typeName, valueName), () => ModifyFilter(targetData, keyword, FilterAction.Whitelist));
                menu.AddButton(ItemFilterPlusTranslations.Get("Add {0} '{1}' to blacklist", typeName, valueName), () => ModifyFilter(targetData, keyword, FilterAction.Blacklist));
            }
        };

        if (thing.source != null) addOption(ItemFilterPlusTranslations.Get("type"), thing.source.GetName(), "type:" + thing.id);
        if (thing.category != null) addOption(ItemFilterPlusTranslations.Get("category"), thing.category.GetName(), "cat:" + thing.category.id);
        if (thing.rarity != Rarity.Normal && thing.rarity != Rarity.Artifact) addOption(ItemFilterPlusTranslations.Get("rarity"), FilterManagerWindow.GetLocalizedValue("rarity", thing.rarity.ToString()), "rarity:" + thing.rarity);
        if (thing.blessedState != BlessedState.Normal) addOption(ItemFilterPlusTranslations.Get("state"), FilterManagerWindow.GetLocalizedValue("state", thing.blessedState.ToString()), "state:" + thing.blessedState);
        if (thing.material != null && thing.material.id != thing.sourceCard.DefaultMaterial.id) addOption(ItemFilterPlusTranslations.Get("material"), thing.material.GetName(), "mat:" + thing.material.id);

        string propertyTypeName = ItemFilterPlusTranslations.Get("property");
        if (thing.IsDecayed) addOption(propertyTypeName, "rotten".lang(), "tainted:true");
        // Add fresh property for all items (rotting is already handled elsewhere)
        addOption(propertyTypeName, "isFresh".lang(), "fresh:true");
        if (thing.elements.dict.ContainsKey(708)) addOption(propertyTypeName, EClass.sources.elements.map[708].GetName(), "human:true");
        if (thing.elements.dict.ContainsKey(709)) addOption(propertyTypeName, EClass.sources.elements.map[709].GetName(), "undead:true");
        if (thing.elements.dict.ContainsKey(701)) addOption(propertyTypeName, EClass.sources.elements.map[701].GetName(), "catmeat:true");
        if (thing.elements.dict.ContainsKey(704)) addOption(propertyTypeName, EClass.sources.elements.map[704].GetName(), "bugmeat:true");
        if (thing.elements.dict.ContainsKey(707)) addOption(propertyTypeName, EClass.sources.elements.map[707].GetName(), "rawfish:true");
        if (thing.elements.dict.ContainsKey(758)) addOption(propertyTypeName, EClass.sources.elements.map[758].GetName(), "aphrodisiac:true");

        if (thing.IsInheritFoodTraits && thing.Evalue(2) != 0)
        {
            menu.AddButton(ItemFilterPlusTranslations.Get("Add Food Quality > to Whitelist"), () => FilterManagerWindow.ShowQualityFilterDialog(targetData, true));
            menu.AddButton(ItemFilterPlusTranslations.Get("Add Food Quality < to Blacklist"), () => FilterManagerWindow.ShowQualityFilterDialog(targetData, false));
        }
    }

    [HarmonyPostfix]
    [HarmonyPatch(typeof(InvOwner), nameof(InvOwner.ListInteractions), new Type[] { typeof(ButtonGrid), typeof(bool) })]
    public static void AddFilterSubmenuButton(ref InvOwner.ListInteraction __result, ButtonGrid b, bool context)
    {
        if (!context || __result == null || b?.card is not Thing thing) return;
        __result.Add(ItemFilterPlusTranslations.Get("Autopickup Filter"), 1000, () =>
        {
            Vector2 clickPosition = EInput.uiMousePosition;
            UIContextMenu submenu = EClass.ui.CreateContextMenuInteraction();
            BuildFilterSubmenu(submenu, EMono.player.dataPick, thing);
            submenu.Show(clickPosition);
        });
        if (!b.invOwner.owner.IsPC && b.invOwner.Container.IsContainer)
        {
            __result.Add(ItemFilterPlusTranslations.Get("This Chest's Filter"), 1001, () =>
            {
                Vector2 clickPosition = EInput.uiMousePosition;
                UIContextMenu submenu = EClass.ui.CreateContextMenuInteraction();
                BuildFilterSubmenu(submenu, b.invOwner.Container.GetWindowSaveData(), thing);
                submenu.Show(clickPosition);
            });
        }
    }
}

[HarmonyPatch]
public static class ActMenuIntegrationPatch
{
    private static string SettingsActionName => ItemFilterPlusTranslations.Get("Autopickup Filter Settings");

    [HarmonyPrefix, HarmonyPatch(typeof(ActPlan), nameof(ActPlan.ShowContextMenu))]
    public static void AddFilterSettingsButton(ActPlan __instance)
    {
        if (!__instance.pos.Equals(EClass.pc.pos)) return;
        var filterAction = new DynamicAct(SettingsActionName, () => { FilterManagerWindow.Toggle(EMono.player.dataPick); return false; }, false);
        __instance.list.Add(new ActPlan.Item() { act = filterAction });
    }

    [HarmonyPrefix, HarmonyPatch(typeof(ActPlan.Item), nameof(ActPlan.Item.Perform))]
    public static bool PerformFilterAction(ActPlan.Item __instance)
    {
        if (__instance.act is DynamicAct act && act.id == SettingsActionName)
        {
            act.Perform();
            return false;
        }
        return true;
    }
}

public static class FilterManagerWindow
{
    private enum DisplayFilter { All, Whitelist, Blacklist }
    private static DisplayFilter currentFilter = DisplayFilter.All;
    private static GameObject windowGo;
    private static Transform gridContent;
    private static GameObject statusTextContainer;
    private static Window.SaveData currentTargetData;
    public static bool IsOpen => windowGo != null;
    private static Button btnAll, btnWhitelist, btnBlacklist;
    private static readonly Color activeButtonColor = new Color(0.7f, 0.7f, 0.7f, 1f);
    private static readonly Color inactiveButtonColor = new Color(0.95f, 0.95f, 0.95f, 1f);
    private static Sprite panelSprite;
    private static Sprite buttonSprite;
    private static Font gameFont;
    private static int fontSizeModifier { get => ModConfig.FontSizeModifier.Value; set => ModConfig.FontSizeModifier.Value = value; }
    private static readonly HashSet<string> excludedCategories = new HashSet<string> { "root", "all", "none", "newbie" };

    // Global search functionality
    private static InputField globalSearchField;
    private static List<SearchableFilterItem> allSearchableItems;
    private static bool isGlobalSearchMode = false;

    private class SearchableFilterItem
    {
        public string DisplayName { get; set; }
        public string RuleId { get; set; }
        public string SearchText { get; set; }
        public string CategoryPath { get; set; }
        public string Category => CategoryPath; // Alias for compatibility
        public string Type { get; set; }
        public bool IsCategory { get; set; } = false;
        public bool AllowWhitelist { get; set; } = true;
        public bool AllowBlacklist { get; set; } = true;
        public bool RequiresDialog { get; set; } = false;
    }

    private static List<Action<GameObject, Window.SaveData>> menuBuilderHistory = new List<Action<GameObject, Window.SaveData>>();
    private static List<GameObject> currentMenuItems = new List<GameObject>();
    private static readonly Dictionary<string, string[]> superCategories = new Dictionary<string, string[]>
    {
        { "cat_super_armor", new[] { "armor", "head", "neck", "back", "hand", "waist", "leg", "arm", "ring", "amulet", "talisman" } },
        { "cat_super_weapon", new[] { "weapon_melee", "weapon_ranged", "ammo", "shield" } },
        { "cat_super_build", new[] { "block", "fence", "support", "floor", "wall", "foundation" } },
        { "cat_super_consumable", new[] { "food", "drink", "potion", "scroll", "spellbook", "seed", "reagent" } },
        { "cat_super_tool", new[] { "tool", "light" } },
        { "cat_super_resource", new[] { "resource", "junk" } },
        { "cat_super_misc", new[] { "furniture", "container", "key", "special", "bodypart" } }
    };

    public static void Toggle(Window.SaveData targetData = null) { if (windowGo != null) { Object.Destroy(windowGo); windowGo = null; currentTargetData = null; } else if (targetData != null) { Create(targetData); } }

    private static void BuildSearchableFilterDatabase()
    {
        allSearchableItems = new List<SearchableFilterItem>();

        // Add categories with proper game localization and super-category grouping
        foreach (var superCat in superCategories)
        {
            string superCategoryName = superCat.Key; // Use the localized super category name
            string localizedSuperCatName = ItemFilterPlusTranslations.Get(superCat.Key); // Use proper translation

            // Don't add super-categories as individual searchable items since they need sub-menus
            // Super categories will be handled by the main category menu instead

            foreach (var catId in superCat.Value)
            {
                var category = EClass.sources.categories.rows.FirstOrDefault(c => c.id == catId);
                if (category == null || excludedCategories.Contains(category.id)) continue;

                var item = new SearchableFilterItem
                {
                    DisplayName = category.GetName(), // Uses game localization
                    RuleId = $"cat:{category.id}",
                    // Include category.id for fallback matching
                    SearchText = (category.GetName() + " " + category.id + " " + localizedSuperCatName).ToLower() + " category",
                    CategoryPath = localizedSuperCatName,
                    IsCategory = true
                };
                allSearchableItems.Add(item);
            }
        }

        // Add individual categories not in super categories
        foreach (var category in EClass.sources.categories.rows)
        {
            if (excludedCategories.Contains(category.id)) continue;
            bool isInSuperCategory = superCategories.Values.Any(cats => cats.Contains(category.id));
            if (isInSuperCategory) continue;

            var item = new SearchableFilterItem
            {
                DisplayName = category.GetName(),
                RuleId = $"cat:{category.id}",
                // Include category.id for fallback matching
                SearchText = (category.GetName() + " " + category.id).ToLower() + " category",
                CategoryPath = ItemFilterPlusTranslations.Get("Other Categories"),
                IsCategory = true
            };
            allSearchableItems.Add(item);
        }

        // Add Things/Types with proper game localization - include ALL items for complete search coverage
        foreach (var thing in EClass.sources.things.rows)
        {
            if (string.IsNullOrEmpty(thing.id)) continue;

            var item = new SearchableFilterItem
            {
                DisplayName = thing.GetName(), // Uses game localization
                RuleId = $"type:{thing.id}",
                // Include thing.id in SearchText for fallback matching when localized names fail
                SearchText = (thing.GetName() + " " + thing.id).ToLower() + " type thing item",
                CategoryPath = ItemFilterPlusTranslations.Get("Items by Type")
            };
            allSearchableItems.Add(item);
        }

        // Add materials grouped by category with game localization
        // Note: Include ALL materials including those without a category (like dark matter)
        var materialsWithCategory = EClass.sources.materials.rows
            .Where(m => !string.IsNullOrEmpty(m.category))
            .GroupBy(m => m.category);

        foreach (var group in materialsWithCategory)
        {
            string categoryName = group.Key.lang(); // Uses game localization

            foreach (var material in group)
            {
                var item = new SearchableFilterItem
                {
                    DisplayName = material.GetName(), // Uses game localization
                    RuleId = $"mat:{material.id}",
                    // Include material.id and alias for fallback matching
                    SearchText = (material.GetName() + " " + material.alias + " " + categoryName).ToLower() + " material",
                    CategoryPath = ItemFilterPlusTranslations.Get("Materials - {0}", categoryName)
                };
                allSearchableItems.Add(item);
            }
        }

        // Add materials without a category (like dark matter) to an "Other" category
        var materialsWithoutCategory = EClass.sources.materials.rows
            .Where(m => string.IsNullOrEmpty(m.category) && !string.IsNullOrEmpty(m.alias));

        foreach (var material in materialsWithoutCategory)
        {
            var item = new SearchableFilterItem
            {
                DisplayName = material.GetName(), // Uses game localization
                RuleId = $"mat:{material.id}",
                // Include material.id and alias for fallback matching
                SearchText = (material.GetName() + " " + material.alias).ToLower() + " material",
                CategoryPath = ItemFilterPlusTranslations.Get("Materials - {0}", ItemFilterPlusTranslations.Get("Other"))
            };
            allSearchableItems.Add(item);
        }

        // Add rarities with proper game localization
        foreach (Rarity rarity in Enum.GetValues(typeof(Rarity)))
        {
            if (rarity == Rarity.Normal || rarity == Rarity.Random) continue;

            string rarityName = GetLocalizedValue("rarity", rarity.ToString());
            var item = new SearchableFilterItem
            {
                DisplayName = rarityName,
                RuleId = $"rarity:{rarity}",
                SearchText = rarityName.ToLower() + " rarity quality",
                CategoryPath = ItemFilterPlusTranslations.Get("Item Rarities")
            };
            allSearchableItems.Add(item);
        }

        // Add blessed states with proper game localization
        foreach (BlessedState state in Enum.GetValues(typeof(BlessedState)))
        {
            if (state == BlessedState.Normal) continue;

            string stateName = GetLocalizedValue("state", state.ToString());
            var item = new SearchableFilterItem
            {
                DisplayName = stateName,
                RuleId = $"state:{state}",
                SearchText = stateName.ToLower() + " state blessing curse",
                CategoryPath = ItemFilterPlusTranslations.Get("Item States")
            };
            allSearchableItems.Add(item);
        }

        // Add stat-based filters with localized category names
        foreach (var category in ItemFilterPlus.StatFilterData.Categories)
        {
            string localizedCategoryName = ItemFilterPlusTranslations.Get(category.Key);

            foreach (var elementId in category.Value)
            {
                var element = EClass.sources.elements.map[elementId];
                var item = new SearchableFilterItem
                {
                    DisplayName = element.GetName() + " (" + ItemFilterPlusTranslations.Get("Equipment") + ")", // Add Equipment label
                    RuleId = $"stat:{elementId}|1",
                    SearchText = (element.GetName() + " " + localizedCategoryName).ToLower() + " stat attribute equipment",
                    CategoryPath = ItemFilterPlusTranslations.Get("Stats - {0}", localizedCategoryName)
                };
                allSearchableItems.Add(item);
            }
        }

        // Add food stat filters
        var mainAttributes = ItemFilterPlus.StatFilterData.Categories.FirstOrDefault(c => c.Key == "Main Attributes");
        if (mainAttributes.Value != null)
        {
            foreach (var elementId in mainAttributes.Value)
            {
                var element = EClass.sources.elements.map[elementId];
                var item = new SearchableFilterItem
                {
                    DisplayName = element.GetName() + " (" + ItemFilterPlusTranslations.Get("Food") + ")", // Add Food label
                    RuleId = $"foodstat:{elementId}|1",
                    SearchText = element.GetName().ToLower() + " food stat nutrition",
                    CategoryPath = ItemFilterPlusTranslations.Get("Food Stats")
                };
                allSearchableItems.Add(item);
            }
        }

        // Add potential stat filters
        var potentialTargets = new Dictionary<int, SourceElement.Row>();
        foreach (var source in EClass.sources.elements.rows)
        {
            var foodEffect = source.foodEffect;
            if (!foodEffect.IsEmpty() && foodEffect[0] == "pot" && foodEffect.Length > 1)
            {
                if (EClass.sources.elements.alias.TryGetValue(foodEffect[1], out var targetStatSource))
                {
                    if (!potentialTargets.ContainsKey(targetStatSource.id))
                    {
                        potentialTargets[targetStatSource.id] = targetStatSource;
                    }
                }
            }
        }

        foreach (var statSource in potentialTargets.Values)
        {
            string suffix = " (" + ItemFilterPlusTranslations.Get("Food, Potential") + ")"; // Add Food, Potential label
            var item = new SearchableFilterItem
            {
                DisplayName = statSource.GetName() + suffix,
                RuleId = $"potential:{statSource.id}|1",
                SearchText = statSource.GetName().ToLower() + " potential boost training",
                CategoryPath = ItemFilterPlusTranslations.Get("Potential Stats")
            };
            allSearchableItems.Add(item);
        }

        // Add general property filters that might be missing
        var generalProperties = new[]
        {
            new { Name = ItemFilterPlusTranslations.Get("filter_prop_blessed"), Rule = "state:Blessed", Keywords = "blessed holy divine sacred" },
            new { Name = ItemFilterPlusTranslations.Get("filter_prop_cursed"), Rule = "state:Cursed", Keywords = "cursed damned evil dark" },
            new { Name = ItemFilterPlusTranslations.Get("filter_prop_doomed"), Rule = "state:Doomed", Keywords = "doomed fate destiny death" },
            new { Name = ItemFilterPlusTranslations.Get("filter_prop_godly"), Rule = "state:Godly", Keywords = "godly divine supreme ultimate" },
            new { Name = ItemFilterPlusTranslations.Get("filter_prop_identify"), Rule = "identify:true", Keywords = "identified known analyzed examined" },
            new { Name = ItemFilterPlusTranslations.Get("filter_prop_unidentified"), Rule = "identify:false", Keywords = "unidentified unknown mystery hidden" },
            new { Name = ItemFilterPlusTranslations.Get("filter_prop_precious"), Rule = "precious:true", Keywords = "precious valuable rare important" },
            new { Name = ItemFilterPlusTranslations.Get("filter_prop_stolen"), Rule = "stolen:true", Keywords = "stolen thief criminal illegal" }
        };

        foreach (var prop in generalProperties)
        {
            var item = new SearchableFilterItem
            {
                DisplayName = prop.Name,
                RuleId = prop.Rule,
                SearchText = (prop.Name + " " + prop.Keywords).ToLower(),
                CategoryPath = ItemFilterPlusTranslations.Get("Item Properties")
            };
            allSearchableItems.Add(item);
        }

        // Add food property filters
        var foodProperties = new[]
        {
            new { Name = ItemFilterPlusTranslations.Get("filter_prop_rotten"), Rule = "tainted:true", Keywords = "spoiled bad rotten decay" },
            new { Name = "isFresh".lang(), Rule = "fresh:true", Keywords = "fresh new good quality" },
            new { Name = "rotting".lang(), Rule = "rotting:true", Keywords = "rotting spoiling decaying" },
            new { Name = ItemFilterPlusTranslations.Get("filter_prop_human_flesh"), Rule = "human:true", Keywords = "human flesh meat corpse" },
            new { Name = ItemFilterPlusTranslations.Get("filter_prop_undead_flesh"), Rule = "undead:true", Keywords = "undead zombie flesh meat corpse" },
            new { Name = ItemFilterPlusTranslations.Get("filter_prop_cat_meat"), Rule = "catmeat:true", Keywords = "cat meat feline" },
            new { Name = ItemFilterPlusTranslations.Get("filter_prop_bug_meat"), Rule = "bugmeat:true", Keywords = "bug insect meat" },
            new { Name = ItemFilterPlusTranslations.Get("filter_prop_raw_fish"), Rule = "rawfish:true", Keywords = "raw fish uncooked" },
            new { Name = ItemFilterPlusTranslations.Get("filter_prop_aphrodisiac"), Rule = "aphrodisiac:true", Keywords = "aphrodisiac love potion drug" }
        };

        foreach (var prop in foodProperties)
        {
            var item = new SearchableFilterItem
            {
                DisplayName = prop.Name,
                RuleId = prop.Rule,
                SearchText = (prop.Name + " " + prop.Keywords).ToLower(),
                CategoryPath = ItemFilterPlusTranslations.Get("Food Properties")
            };
            allSearchableItems.Add(item);
        }

        // Add hardness filter
        var hardnessItem = new SearchableFilterItem
        {
            DisplayName = ItemFilterPlusTranslations.Get("filter_prop_hardness_gt"),
            RuleId = "hardness:1",
            SearchText = "hardness durability toughness strength",
            CategoryPath = ItemFilterPlusTranslations.Get("Item Properties")
        };
        allSearchableItems.Add(hardnessItem);

        // Add nutrition filter
        var nutritionItem = new SearchableFilterItem
        {
            DisplayName = ItemFilterPlusTranslations.Get("filter_prop_nutrition_gt"),
            RuleId = "stat:10|1",
            SearchText = "nutrition food value satiation hunger",
            CategoryPath = ItemFilterPlusTranslations.Get("Food Properties")
        };
        allSearchableItems.Add(nutritionItem);

        // Add value-based filter
        var valueItem = new SearchableFilterItem
        {
            DisplayName = ItemFilterPlusTranslations.Get("Base Value..."),
            RuleId = "stat:-1|1",
            SearchText = "value price cost gold money gp worth",
            CategoryPath = ItemFilterPlusTranslations.Get("Item Properties")
        };
        allSearchableItems.Add(valueItem);

        // Add quality filters for different quality levels
        for (int i = 1; i <= 9; i++)
        {
            var qualityGtItem = new SearchableFilterItem
            {
                DisplayName = $"Quality > {i}",
                RuleId = $"quality_gt:{i}",
                SearchText = $"quality {i} level grade tier rank",
                CategoryPath = ItemFilterPlusTranslations.Get("Item Properties")
            };
            allSearchableItems.Add(qualityGtItem);

            var qualityLtItem = new SearchableFilterItem
            {
                DisplayName = $"Quality < {i}",
                RuleId = $"quality_lt:{i}",
                SearchText = $"quality {i} level grade tier rank",
                CategoryPath = ItemFilterPlusTranslations.Get("Item Properties")
            };
            allSearchableItems.Add(qualityLtItem);
        }
    }

    private static void CreateGlobalSearchField(Transform parent)
    {
        var searchContainer = new GameObject("GlobalSearchContainer", typeof(RectTransform), typeof(LayoutElement));
        searchContainer.transform.SetParent(parent, false);
        searchContainer.GetComponent<LayoutElement>().minHeight = 50;
        searchContainer.GetComponent<LayoutElement>().preferredHeight = 50;

        var searchGo = new GameObject("GlobalSearchInput", typeof(Image), typeof(InputField));
        searchGo.transform.SetParent(searchContainer.transform, false);

        var searchImage = searchGo.GetComponent<Image>();
        searchImage.sprite = buttonSprite;
        searchImage.type = Image.Type.Sliced;
        searchImage.color = new Color(0.95f, 0.95f, 0.95f, 1f);

        var searchRect = searchGo.GetComponent<RectTransform>();
        searchRect.anchorMin = Vector2.zero;
        searchRect.anchorMax = Vector2.one;
        // Reduce width significantly - use only 60% of container width and center it
        searchRect.sizeDelta = new Vector2(-300, -10); // Much narrower search field
        searchRect.anchoredPosition = Vector2.zero;

        globalSearchField = searchGo.GetComponent<InputField>();
        
        // Create better placeholder text with clear search indication
        var placeholderText = $"🔍 {ItemFilterPlusTranslations.Get("Search...")}"; 
        var placeholderGo = CreateText(placeholderText, searchGo.transform, 16, new Color(0.0f, 0.0f, 0.0f, 1f), TextAnchor.MiddleLeft);
        globalSearchField.placeholder = placeholderGo.GetComponent<Text>();

        // Create text component with better visibility
        var textGo = CreateText("", searchGo.transform, 16, Color.black, TextAnchor.MiddleLeft);
        globalSearchField.textComponent = textGo.GetComponent<Text>();
        
        // Ensure text is visible and properly positioned
        var placeholderRect = placeholderGo.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10, 0);
        placeholderRect.offsetMax = new Vector2(-10, 0);

        var textRect = textGo.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 0);
        textRect.offsetMax = new Vector2(-10, 0);
        
        // Ensure text components have proper settings for visibility
        var placeholderTextComp = placeholderGo.GetComponent<Text>();
        placeholderTextComp.raycastTarget = false;
        
        var inputTextComp = textGo.GetComponent<Text>();
        inputTextComp.raycastTarget = false;
        inputTextComp.color = Color.black; // Ensure black text for visibility
        
        // Configure input field for better user experience
        globalSearchField.characterLimit = 50;
        globalSearchField.onValueChanged.AddListener(OnGlobalSearchChanged);
    }

    private static void OnGlobalSearchChanged(string query)
    {
        if (string.IsNullOrEmpty(query))
        {
            isGlobalSearchMode = false;
            RefreshRules();
            return;
        }

        isGlobalSearchMode = true;
        ShowGlobalSearchResults(query);
    }

    private static void ShowGlobalSearchResults(string query)
    {
        if (gridContent == null) return;

        // Clear existing content
        foreach (Transform child in gridContent) Object.Destroy(child.gameObject);

        string[] searchTerms = query.ToLower().Split(' ').Where(term => !string.IsNullOrWhiteSpace(term)).ToArray();

        var matchingItems = allSearchableItems
            .Where(item => {
                // Check if all search terms are found in display name, search text, rule ID, or category path
                // Including RuleId ensures items are findable even when display names don't resolve properly
                return searchTerms.All(term =>
                    item.DisplayName.ToLower().Contains(term) ||
                    item.SearchText.Contains(term) ||
                    item.RuleId.ToLower().Contains(term) ||
                    item.CategoryPath.ToLower().Contains(term)
                );
            })
            .OrderBy(item => {
                // Prioritize exact matches, then partial matches in display name, then category matches
                if (item.DisplayName.ToLower() == query.ToLower()) return 0;
                if (item.DisplayName.ToLower().StartsWith(query.ToLower())) return 1;
                if (item.DisplayName.ToLower().Contains(query.ToLower())) return 2;
                return 3;
            })
            .ThenBy(item => item.CategoryPath)
            .ThenBy(item => item.DisplayName)
            .ToList();

        if (matchingItems.Count == 0)
        {
            // Clear main content and show status in dedicated container
            foreach (Transform child in statusTextContainer.transform) Object.Destroy(child.gameObject);
            var noResultsText = CreateText(ItemFilterPlusTranslations.Get("No matching filters found."), statusTextContainer.transform, 18, Color.black);
            noResultsText.GetComponent<LayoutElement>().minHeight = 30;
            return;
        }

        // Clear main content and show search result info in status container
        foreach (Transform child in statusTextContainer.transform) Object.Destroy(child.gameObject);
        var countText = CreateText(ItemFilterPlusTranslations.Get("Found filters matching query", matchingItems.Count, query), statusTextContainer.transform, 16, Color.black);
        countText.GetComponent<LayoutElement>().minHeight = 25;

        int resultCount = 0;
        const int MAX_RESULTS = 40; // Increased limit for better search coverage

        foreach (var item in matchingItems.Take(MAX_RESULTS))
        {
            // Create filter button directly without any category headers
            var buttonGo = CreateButton("", gridContent);
            var buttonImage = buttonGo.GetComponent<Image>();

            // Set button color based on filter type
            if (item.IsCategory)
            {
                if (string.IsNullOrEmpty(item.RuleId))
                {
                    // Super category - show as info button
                    buttonImage.color = new Color(0.3f, 0.4f, 0.6f, 0.8f);
                    buttonGo.GetComponent<Button>().interactable = false;
                }
                else
                {
                    // Regular category
                    buttonImage.color = new Color(0.85f, 0.85f, 0.85f, 1f);
                }
            }
            else
            {
                buttonImage.color = GetColorForFilter("+" + item.RuleId);
            }

            var textComponent = buttonGo.GetComponentInChildren<Text>();
            textComponent.alignment = TextAnchor.MiddleLeft;
            var textRect = textComponent.rectTransform;
            textRect.offsetMin = new Vector2(15, textRect.offsetMin.y);
            textRect.offsetMax = new Vector2(-90, textRect.offsetMax.y);

            // Use FormatRuleForDisplay for proper rule formatting
            string displayText;
            if (!string.IsNullOrEmpty(item.RuleId) && !item.IsCategory)
            {
                // Create a properly formatted rule string for FormatRuleForDisplay
                string ruleForDisplay = "+" + item.RuleId;
                displayText = FormatRuleForSearchDisplay(ruleForDisplay);
            }
            else
            {
                // For categories or items without rules, use display name
                displayText = item.DisplayName;
            }
            
            // Highlight search terms in the formatted display text
            foreach (var term in searchTerms)
            {
                if (displayText.ToLower().Contains(term))
                {
                    int index = displayText.ToLower().IndexOf(term);
                    if (index >= 0)
                    {
                        displayText = displayText.Substring(0, index) +
                                    "<color=yellow>" + displayText.Substring(index, term.Length) + "</color>" +
                                    displayText.Substring(index + term.Length);
                        break; // Only highlight first occurrence to avoid nested tags
                    }
                }
            }
            
            // Check if filter already exists and add indicator
            var existingFilterInfo = GetExistingFilterInfo(item.RuleId, currentTargetData);
            if (existingFilterInfo.Exists)
            {
                string indicator = existingFilterInfo.IsWhitelisted ? " [W]" : " [B]";
                displayText += indicator;
            }
            
            textComponent.text = displayText;
            textComponent.supportRichText = true;

            // Add action buttons only for actual filters (not super categories)
            if (!string.IsNullOrEmpty(item.RuleId))
            {
                var addButtonContainer = new GameObject("ActionButtons", typeof(HorizontalLayoutGroup));
                addButtonContainer.transform.SetParent(buttonGo.transform, false);
                var hlg = addButtonContainer.GetComponent<HorizontalLayoutGroup>();
                hlg.spacing = 5;

                var containerRect = addButtonContainer.GetComponent<RectTransform>();
                containerRect.anchorMin = new Vector2(1, 0.5f);
                containerRect.anchorMax = new Vector2(1, 0.5f);
                containerRect.pivot = new Vector2(1, 0.5f);
                containerRect.sizeDelta = new Vector2(80, 30);
                containerRect.anchoredPosition = new Vector2(-10, 0);

                // Check if this filter accepts numeric values
                bool isNumericFilter = item.RuleId.StartsWith("stat:") || 
                                     item.RuleId.StartsWith("foodstat:") || 
                                     item.RuleId.StartsWith("potential:") || 
                                     item.RuleId.Contains("hardness:") ||
                                     item.RuleId.Contains("quality_");

                var btnWhitelist = CreateButton("+", addButtonContainer.transform);
                btnWhitelist.GetComponent<LayoutElement>().preferredWidth = 35;
                btnWhitelist.GetComponent<Image>().color = new Color(0.7f, 1f, 0.7f, 1f);
                btnWhitelist.GetComponent<Button>().onClick.AddListener(() => {
                    if (isNumericFilter)
                    {
                        ShowNumericInputDialog(item, true, currentTargetData);
                    }
                    else
                    {
                        AddFilterRule(item.RuleId, true, currentTargetData);
                        // Add message notification with sound for search results
                        string displayName = FormatRuleForSearchDisplay("+" + item.RuleId);
                        Msg.Say(ItemFilterPlusTranslations.Get("Added '{0}' to whitelist", displayName));
                        EClass.Sound.Play("ui_ok");
                    }
                    // Don't clear search - keep results visible for multiple additions
                    ShowGlobalSearchResults(globalSearchField.text);
                });

                var btnBlacklist = CreateButton("-", addButtonContainer.transform);
                btnBlacklist.GetComponent<LayoutElement>().preferredWidth = 35;
                btnBlacklist.GetComponent<Image>().color = new Color(1f, 0.7f, 0.7f, 1f);
                btnBlacklist.GetComponent<Button>().onClick.AddListener(() => {
                    if (isNumericFilter)
                    {
                        ShowNumericInputDialog(item, false, currentTargetData);
                    }
                    else
                    {
                        AddFilterRule(item.RuleId, false, currentTargetData);
                        // Add message notification with sound for search results
                        string displayName = FormatRuleForSearchDisplay("-" + item.RuleId);
                        Msg.Say(ItemFilterPlusTranslations.Get("Added '{0}' to blacklist", displayName));
                        EClass.Sound.Play("ui_ok");
                    }
                    // Don't clear search - keep results visible for multiple additions
                    ShowGlobalSearchResults(globalSearchField.text);
                });
            }

            resultCount++;
        }

        // Add "more results" indicator to status container if needed
        if (matchingItems.Count > MAX_RESULTS)
        {
            var moreText = CreateText(ItemFilterPlusTranslations.Get("... and {0} more results. Refine your search for better results.", matchingItems.Count - MAX_RESULTS), statusTextContainer.transform, 14, Color.black, TextAnchor.MiddleCenter);
            moreText.GetComponent<LayoutElement>().minHeight = 20;
        }
    }

    private static void ToggleModState() { 
        ItemFilterLogic.IsEnabled = !ItemFilterLogic.IsEnabled; 
        EClass.Sound.Play("ui_ok"); 
        
        // Refresh the window to show/hide the warning
        if (windowGo != null && currentTargetData != null) 
        {
            Toggle(); // Close current window
            Toggle(currentTargetData); // Reopen with updated state
        }
    }
    private static void ToggleHighlightState() { ItemFilterLogic.HighlightEnabled = !ItemFilterLogic.HighlightEnabled; EClass.Sound.Play("ui_ok"); }
    private static void ToggleHighlightSoundState() { ItemFilterLogic.HighlightSoundEnabled = !ItemFilterLogic.HighlightSoundEnabled; EClass.Sound.Play("ui_ok"); }
    private static void ToggleCustomSoundState() { ItemFilterLogic.UseCustomHighlightSound = !ItemFilterLogic.UseCustomHighlightSound; EClass.Sound.Play("ui_ok"); }
    private static void ToggleHighlightEffect() { ItemFilterLogic.HighlightEffectName = ItemFilterLogic.HighlightEffectName == "hit_light" ? "aura_heaven" : "hit_light"; EClass.Sound.Play("ui_ok"); }

    private static void ShowVolumeSlider()
    {
        Vector2 clickPosition = EInput.uiMousePosition;
        UIContextMenu menu = EClass.ui.CreateContextMenuInteraction();
        menu.hideOnMouseLeave = false;
        menu.alwaysPopLeft = true;
        menu.AddSlider(ItemFilterPlusTranslations.Get("Volume"), (val) => $"{Mathf.RoundToInt(val)}%", ItemFilterLogic.CustomSoundVolume, (newVal) => { ItemFilterLogic.CustomSoundVolume = newVal; }, 0f, 100f, true, false, false);
        menu.Show(clickPosition);
    }

    private static void Create(Window.SaveData targetData)
    {
        currentTargetData = targetData;

        // Build searchable filter database
        BuildSearchableFilterDatabase();

        UIContextMenu tempMenu = EClass.ui.CreateContextMenuInteraction(); panelSprite = tempMenu.bg.sprite; buttonSprite = tempMenu.defaultButton.GetComponent<Image>().sprite; gameFont = SkinManager.Instance.fontSet.ui.source.font; Object.Destroy(tempMenu.gameObject); Sprite gearIcon = null; Sprite closeIcon = null; var playerInvLayer = LayerInventory.listInv.FirstOrDefault(inv => inv.Inv.Container.IsPC); if (playerInvLayer != null && playerInvLayer.invs.Count > 0) { var invWindow = playerInvLayer.invs[0].window; if (invWindow != null) { if (invWindow.buttonSort != null) gearIcon = invWindow.buttonSort.image.sprite; if (invWindow.buttonClose != null) closeIcon = invWindow.buttonClose.image.sprite; } }
        if (gearIcon == null) gearIcon = buttonSprite; if (closeIcon == null) closeIcon = buttonSprite; windowGo = new GameObject("FilterManagerWindow", typeof(RectTransform), typeof(Image), typeof(VerticalLayoutGroup), typeof(OutsideClickHandler), typeof(UIDragPanel)); windowGo.transform.SetParent(EClass.ui.rectLayers, false); windowGo.transform.localPosition = Vector3.zero; var image = windowGo.GetComponent<Image>(); image.sprite = panelSprite; image.type = Image.Type.Sliced; var rect = windowGo.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(750, 600);
        
        // Configure native UIDragPanel for window dragging
        var dragPanel = windowGo.GetComponent<UIDragPanel>();
        dragPanel.target = rect;
        dragPanel.bound = rect;
        dragPanel.container = EClass.ui.rectLayers;
        dragPanel.axisX = true;
        dragPanel.axisY = true;
        dragPanel.clamp = true;
        dragPanel.enable = true;
        
        var vlg = windowGo.GetComponent<VerticalLayoutGroup>(); vlg.padding = new RectOffset(10, 10, 40, 30); vlg.spacing = 0; vlg.childControlWidth = true; vlg.childForceExpandHeight = false; var closeButtonGo = CreateButton("", windowGo.transform); var closeButtonImage = closeButtonGo.GetComponent<Image>(); closeButtonImage.sprite = closeIcon; closeButtonImage.color = new Color(1f, 0.85f, 0.85f, 1f); var closeButtonRect = closeButtonGo.GetComponent<RectTransform>(); closeButtonRect.anchorMin = new Vector2(1, 1); closeButtonRect.anchorMax = new Vector2(1, 1); closeButtonRect.pivot = new Vector2(1, 1); closeButtonRect.sizeDelta = new Vector2(40, 40); closeButtonRect.anchoredPosition = new Vector2(2, -25); closeButtonGo.GetComponent<LayoutElement>().ignoreLayout = true; closeButtonGo.GetComponentInChildren<Text>().text = ""; closeButtonGo.GetComponent<Button>().onClick.AddListener(() => Toggle()); var settingsButtonGo = CreateButton("", windowGo.transform); var settingsButtonImage = settingsButtonGo.GetComponent<Image>(); settingsButtonImage.sprite = gearIcon; settingsButtonImage.color = Color.white; var settingsButtonRect = settingsButtonGo.GetComponent<RectTransform>(); settingsButtonRect.anchorMin = new Vector2(1, 1); settingsButtonRect.anchorMax = new Vector2(1, 1); settingsButtonRect.pivot = new Vector2(1, 1); settingsButtonRect.sizeDelta = new Vector2(40, 40); settingsButtonRect.anchoredPosition = new Vector2(2, -60); settingsButtonGo.GetComponent<LayoutElement>().ignoreLayout = true; settingsButtonGo.GetComponentInChildren<Text>().text = "";

        // Add global search field to main window
        CreateGlobalSearchField(windowGo.transform);

        // Add mod disabled warning if mod is turned off
        if (!ItemFilterLogic.IsEnabled)
        {
            var warningContainer = new GameObject("ModDisabledWarning", typeof(RectTransform), typeof(LayoutElement));
            warningContainer.transform.SetParent(windowGo.transform, false);
            warningContainer.GetComponent<LayoutElement>().minHeight = 60;
            warningContainer.GetComponent<LayoutElement>().preferredHeight = 60;

            var warningBackground = new GameObject("WarningBackground", typeof(Image));
            warningBackground.transform.SetParent(warningContainer.transform, false);
            var warningImage = warningBackground.GetComponent<Image>();
            warningImage.sprite = panelSprite;
            warningImage.type = Image.Type.Sliced;
            warningImage.color = new Color(0.8f, 0.2f, 0.2f, 0.9f); // Red background

            var warningRect = warningBackground.GetComponent<RectTransform>();
            warningRect.anchorMin = Vector2.zero;
            warningRect.anchorMax = Vector2.one;
            warningRect.sizeDelta = new Vector2(-20, -10);
            warningRect.anchoredPosition = Vector2.zero;

            var warningTextGo = CreateText("⚠ " + ItemFilterPlusTranslations.Get("Mod is turned off") + " ⚠", warningBackground.transform, 18, Color.white, TextAnchor.MiddleCenter);
            var warningTextRect = warningTextGo.GetComponent<RectTransform>();
            warningTextRect.anchorMin = Vector2.zero;
            warningTextRect.anchorMax = Vector2.one;
            warningTextRect.anchoredPosition = Vector2.zero;
            warningTextRect.sizeDelta = Vector2.zero;
            
            // Make the text bold and add some visual emphasis
            var textComponent = warningTextGo.GetComponent<Text>();
            textComponent.fontStyle = FontStyle.Bold;
            textComponent.fontSize = 18 + fontSizeModifier;
        }

        settingsButtonGo.GetComponent<Button>().onClick.AddListener(() => {
            // Capture mouse position immediately at click time to prevent menu flying to wrong position
            Vector2 clickPosition = EInput.uiMousePosition;
            
            if (windowGo != null) windowGo.GetComponent<OutsideClickHandler>().enabled = false;
            UIContextMenu menu = EClass.ui.CreateContextMenuInteraction();
            menu.hideOnMouseLeave = false;
            menu.alwaysPopLeft = true;
            menu.onDestroy = () => { if (windowGo != null) windowGo.GetComponent<OutsideClickHandler>().enabled = true; };
            menu.AddButton(ItemFilterLogic.IsEnabled ? ItemFilterPlusTranslations.Get("Disable Mod") : ItemFilterPlusTranslations.Get("Enable Mod"), ToggleModState, hideAfter: true);

            var statFilterSubmenu = menu.AddChild(ItemFilterPlusTranslations.Get("Stat-based Whitelist"));

            int currentValue = 0;
            string valueRule = ItemFilterLogic.GetRules(currentTargetData.filter).FirstOrDefault(r => r.StartsWith("+stat:-1|"));
            if (valueRule != null) int.TryParse(valueRule.Split('|').Last(), out currentValue);
            string valueText = currentValue > 0 ? $" (> {currentValue} gp)" : "";
            statFilterSubmenu.AddButton(ItemFilterPlusTranslations.Get("Base Value...") + valueText, () => ShowValueFilterDialog(currentTargetData), hideAfter: true);
            foreach (var category in ItemFilterPlus.StatFilterData.Categories)
            {
                BuildStatSubmenu(statFilterSubmenu, ItemFilterPlusTranslations.Get(category.Key), category.Value, currentTargetData);
            }

            menu.AddSeparator();
            var highlightSubmenu = menu.AddChild(ItemFilterPlusTranslations.Get("Whitelist Highlight"));
            highlightSubmenu.AddButton(ItemFilterLogic.HighlightEnabled ? ItemFilterPlusTranslations.Get("Disable highlight") : ItemFilterPlusTranslations.Get("Enable highlight"), ToggleHighlightState, hideAfter: true);
            highlightSubmenu.AddButton(ItemFilterLogic.HighlightEffectName == "hit_light" ? ItemFilterPlusTranslations.Get("Change to Aura effect") : ItemFilterPlusTranslations.Get("Change to Light effect"), ToggleHighlightEffect, hideAfter: true);
            highlightSubmenu.AddButton(ItemFilterLogic.HighlightSoundEnabled ? ItemFilterPlusTranslations.Get("Disable sound") : ItemFilterPlusTranslations.Get("Enable sound"), ToggleHighlightSoundState, hideAfter: true);
            var customSoundButton = highlightSubmenu.AddButton(ItemFilterLogic.UseCustomHighlightSound ? ItemFilterPlusTranslations.Get("Use default sound") : ItemFilterPlusTranslations.Get("Use custom sound with note"), ToggleCustomSoundState, hideAfter: true);
            customSoundButton.tooltip.lang = ItemFilterPlusTranslations.Get("Place 'drop.mp3' in the mod folder.");
            highlightSubmenu.AddButton(ItemFilterPlusTranslations.Get("Sound Volume..."), ShowVolumeSlider, hideAfter: true);
            menu.AddButton(ItemFilterPlusTranslations.Get("Font Size"), CycleFontSize, hideAfter: false);
            menu.AddSeparator();
            menu.AddButton(ItemFilterPlusTranslations.Get("Copy Filters"), CopyFilters, hideAfter: true);
            menu.AddButton(ItemFilterPlusTranslations.Get("Paste Filters"), PasteFilters, hideAfter: true);
            menu.Show(clickPosition);
        });

        var filterButtonsContainer = new GameObject("FilterButtons", typeof(HorizontalLayoutGroup), typeof(LayoutElement)); filterButtonsContainer.transform.SetParent(windowGo.transform, false); var fbcLayout = filterButtonsContainer.GetComponent<HorizontalLayoutGroup>(); fbcLayout.spacing = 10; filterButtonsContainer.GetComponent<LayoutElement>().minHeight = 40;
        btnWhitelist = CreateButton(ItemFilterPlusTranslations.Get("Whitelist"), filterButtonsContainer.transform).GetComponent<Button>();
        btnBlacklist = CreateButton(ItemFilterPlusTranslations.Get("Blacklist"), filterButtonsContainer.transform).GetComponent<Button>();
        btnAll = CreateButton(ItemFilterPlusTranslations.Get("All"), filterButtonsContainer.transform).GetComponent<Button>();
        btnWhitelist.onClick.AddListener(() => SetDisplayFilter(DisplayFilter.Whitelist)); btnBlacklist.onClick.AddListener(() => SetDisplayFilter(DisplayFilter.Blacklist)); btnAll.onClick.AddListener(() => SetDisplayFilter(DisplayFilter.All)); var rectTransform = filterButtonsContainer.GetComponent<RectTransform>(); rectTransform.anchorMin = new Vector2(0.5f, 1); rectTransform.anchorMax = new Vector2(0.5f, 1); rectTransform.anchoredPosition = new Vector2(0, -50f); var btnWhitelistLE = btnWhitelist.gameObject.AddComponent<LayoutElement>(); btnWhitelistLE.preferredWidth = 100; var btnBlacklistLE = btnBlacklist.gameObject.AddComponent<LayoutElement>(); btnBlacklistLE.preferredWidth = 100; var btnAllLE = btnAll.gameObject.AddComponent<LayoutElement>(); btnAllLE.preferredWidth = 100; rectTransform.localScale = new Vector3(0.9f, 0.9f, 1f);

        // Create status text container below filter buttons with absolute positioning
        statusTextContainer = new GameObject("StatusText", typeof(RectTransform), typeof(VerticalLayoutGroup));
        statusTextContainer.transform.SetParent(windowGo.transform, false);
        var statusLayout = statusTextContainer.GetComponent<VerticalLayoutGroup>();
        statusLayout.childForceExpandWidth = true;
        statusLayout.childControlWidth = true;
        var statusRect = statusTextContainer.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0.5f, 1);
        statusRect.anchorMax = new Vector2(0.5f, 1);
        statusRect.anchoredPosition = new Vector2(0, -90f);
        statusRect.sizeDelta = new Vector2(700, 60);
        // Remove LayoutElement to prevent affecting parent layout
        
        var scrollView = CreateScrollView(windowGo.transform); 
        gridContent = scrollView.content;

        var gridLayout = gridContent.gameObject.AddComponent<GridLayoutGroup>();
        gridLayout.padding = new RectOffset(42, 30, 30, 15);
        gridLayout.spacing = new Vector2(7, 10);
        gridLayout.cellSize = new Vector2(320, 45); // Restore original cell size for 2-column layout
        gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        gridLayout.constraintCount = 2; // Restore 2-column layout
        gridLayout.startAxis = GridLayoutGroup.Axis.Horizontal; // Fill horizontally first to keep category groups together

        var sizeFitter = gridContent.gameObject.AddComponent<ContentSizeFitter>();
        sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize; currentFilter = DisplayFilter.All; RefreshRules();
    }

    private static void AddFilterRule(string ruleBody, bool isWhitelist, Window.SaveData data)
    {
        var rules = ItemFilterLogic.GetRules(data.filter).ToList();
        rules.RemoveAll(r => r.TrimStart('+', '-') == ruleBody);
        string prefix = isWhitelist ? "+" : "-";
        rules.Add(prefix + ruleBody);
        data.filter = ItemFilterLogic.GetReorderedFilterString(rules);
        data._filterStrs = null;
        ItemFilteringPatches.ReapplyFilterToAllItems();
        RefreshRules();
        
        // Add message notification with sound when not in search mode (search mode handles its own messages)
        if (!isGlobalSearchMode)
        {
            // Create a display name from the rule for the message
            string displayName = FormatRuleForDisplay(prefix + ruleBody);
            // Remove the X prefix and clean up the display
            if (displayName.StartsWith("<color=red>X</color> "))
            {
                displayName = displayName.Substring("<color=red>X</color> ".Length);
            }
            // Remove any rich text tags for cleaner message display
            displayName = System.Text.RegularExpressions.Regex.Replace(displayName, "<.*?>", "");
            
            string messageKey = isWhitelist ? "Added '{0}' to whitelist" : "Added '{0}' to blacklist";
            Msg.Say(ItemFilterPlusTranslations.Get(messageKey, displayName));
        }
        
        EClass.Sound.Play("ui_ok");
    }



    private static void ShowSuperCategoryFilters(Window.SaveData data)
    {
        // Show super categories (Build, Consumable, Tool, etc.) instead of individual category items
        var layer = EClass.ui.AddLayer<LayerList>()
            .SetHeader(ItemFilterPlusTranslations.Get("filter_menu_category"))
            .SetSize(400f, 500f);
            
        var superCategoryList = new List<(string name, string description, Action action)>();
        
        foreach (var superCat in superCategories)
        {
            string localizedSuperCatName = ItemFilterPlusTranslations.Get(superCat.Key); // Use proper translation
            var subCategories = allSearchableItems?.Where(x => x.CategoryPath == localizedSuperCatName).ToList() ?? new List<SearchableFilterItem>();
            
            if (subCategories.Count > 0)
            {
                superCategoryList.Add((localizedSuperCatName, $"{subCategories.Count} items", () => {
                    ShowFilterList(localizedSuperCatName, subCategories, data, false, true);
                }));
            }
        }
        
        // Add individual categories not in super categories
        var otherCategories = allSearchableItems?.Where(x => x.CategoryPath == ItemFilterPlusTranslations.Get("Other Categories")).ToList() ?? new List<SearchableFilterItem>();
        if (otherCategories.Count > 0)
        {
            superCategoryList.Add((ItemFilterPlusTranslations.Get("Other Categories"), $"{otherCategories.Count} items", () => {
                ShowFilterList(ItemFilterPlusTranslations.Get("Other Categories"), otherCategories, data, false, true);
            }));
        }
        
        layer.SetStringList(
            () => superCategoryList.Select(c => c.name).ToList(),
            (index, name) => {
                if (index >= 0 && index < superCategoryList.Count)
                {
                    superCategoryList[index].action();
                }
            },
            autoClose: true
        );
    }
    
    private static void ShowCategoryFilters(Window.SaveData data)
    {
        var categoryItems = allSearchableItems?.Where(x => x.CategoryPath?.Contains("Categories") == true).ToList() ?? new List<SearchableFilterItem>();
        ShowFilterList(ItemFilterPlusTranslations.Get("filter_menu_category"), categoryItems, data, false, true);
    }
    
    private static void ShowMaterialFilters(Window.SaveData data)
    {
        var materialItems = allSearchableItems?.Where(x => x.CategoryPath?.Contains("Materials") == true).ToList() ?? new List<SearchableFilterItem>();
        ShowFilterList(ItemFilterPlusTranslations.Get("filter_menu_material"), materialItems, data, false, true);
    }
    
    private static void ShowRarityFilters(Window.SaveData data)
    {
        var rarityItems = allSearchableItems?.Where(x => x.CategoryPath?.Contains("Rarities") == true).ToList() ?? new List<SearchableFilterItem>();
        ShowFilterList(ItemFilterPlusTranslations.Get("filter_menu_rarity"), rarityItems, data, false, true);
    }
    
    private static void ShowStateFilters(Window.SaveData data)
    {
        var stateItems = allSearchableItems?.Where(x => x.CategoryPath?.Contains("States") == true).ToList() ?? new List<SearchableFilterItem>();
        ShowFilterList(ItemFilterPlusTranslations.Get("filter_menu_state"), stateItems, data, false, true);
    }
    
    private static void ShowFoodFilters(Window.SaveData data)
    {
        var foodItems = allSearchableItems?.Where(x => x.CategoryPath?.Contains("Food") == true).ToList() ?? new List<SearchableFilterItem>();
        ShowFilterList(ItemFilterPlusTranslations.Get("filter_menu_food"), foodItems, data, false, true);
    }
    
    private static void ShowPropertyFilters(Window.SaveData data)
    {
        // Show common boolean property filters
        var layer = EClass.ui.AddLayer<LayerList>()
            .SetHeader(ItemFilterPlusTranslations.Get("filter_menu_properties"))
            .SetSize(400f, 500f);
            
        var properties = new List<(string name, string rule, bool isWhitelist)>
        {
            (ItemFilterPlusTranslations.Get("filter_prop_blessed"), "state:Blessed", true),
            (ItemFilterPlusTranslations.Get("filter_prop_cursed"), "state:Cursed", false),
            (ItemFilterPlusTranslations.Get("filter_prop_doomed"), "state:Doomed", false),
            (ItemFilterPlusTranslations.Get("filter_prop_equipped"), "equipped:true", true),
            (ItemFilterPlusTranslations.Get("filter_prop_unequippable"), "equipped:false", false),
            (ItemFilterPlusTranslations.Get("filter_prop_identify"), "identify:true", true),
            (ItemFilterPlusTranslations.Get("filter_prop_unidentified"), "identify:false", false),
            (ItemFilterPlusTranslations.Get("filter_prop_precious"), "precious:true", true),
            (ItemFilterPlusTranslations.Get("filter_prop_stolen"), "stolen:true", false),
            (ItemFilterPlusTranslations.Get("filter_prop_rotten"), "tainted:true", false)
        };
            
        layer.SetList2(
            properties,
            prop => $"{prop.name} ({(prop.isWhitelist ? "+" : "-")})",
            (prop, button) => {
                AddFilterRule(prop.rule, prop.isWhitelist, data);
                EClass.Sound.Play("ui_ok");
            },
            (prop, button) => {
                button.button1.mainText.text = $"{prop.name} ({(prop.isWhitelist ? "+" : "-")})";
                button.DisableIcon();
                button.Build();
            },
            autoClose: true
        );
    }
    

    
    private static void ShowSearchResults(string searchText, Window.SaveData data)
    {
        if (allSearchableItems == null || allSearchableItems.Count == 0)
        {
            BuildSearchableFilterDatabase();
        }
        
        var searchLower = searchText.ToLower();
        var matchingItems = allSearchableItems
            .Where(item => item.DisplayName.ToLower().Contains(searchLower) || 
                          item.CategoryPath.ToLower().Contains(searchLower) ||
                          item.SearchText.ToLower().Contains(searchLower) ||
                          item.RuleId.ToLower().Contains(searchLower)) // Include RuleId for better discoverability
            .OrderBy(item => item.DisplayName)
            .Take(200) // Limit results for performance
            .ToList();
            
        if (matchingItems.Count == 0)
        {
            Dialog.YesNo(ItemFilterPlusTranslations.Get("No filters found matching '") + searchText + "'.", () => { /* OK button action */ });
            return;
        }
        
        ShowFilterList($"{ItemFilterPlusTranslations.Get("Search Results")} ({matchingItems.Count})", matchingItems, data, false, true);
    }
    
    private static void ShowFilterList(string title, List<SearchableFilterItem> items, Window.SaveData data, bool enableSearch = false, bool showBackButton = true)
    {
        if (allSearchableItems == null || allSearchableItems.Count == 0)
        {
            BuildSearchableFilterDatabase();
        }
        
        var layer = EClass.ui.AddLayer<LayerList>()
            .SetHeader(title)
            .SetSize(450f, 600f);
            
        // Prepare items list with navigation options
        var displayItems = new List<SearchableFilterItem>();
        
        // Add Back button if requested
        if (showBackButton)
        {
            displayItems.Add(new SearchableFilterItem 
            { 
                DisplayName = ItemFilterPlusTranslations.Get("Back"), 
                RuleId = "__BACK__", 
                CategoryPath = ItemFilterPlusTranslations.Get("Navigation") 
            });
        }
        
        // If this is a search interface, add a search option first
        if (enableSearch)
        {
            displayItems.Add(new SearchableFilterItem 
            { 
                DisplayName = ItemFilterPlusTranslations.Get("Search all filters..."), 
                RuleId = "__SEARCH__", 
                CategoryPath = "System" // Keep System as internal category 
            });
            displayItems.AddRange(items.Take(50)); // Limit initial display to 50 items for performance
        }
        else
        {
            displayItems.AddRange(items);
        }
            
        // Group items by category for better display
        var groupedItems = displayItems.GroupBy(item => item.CategoryPath ?? "Other").ToList();
        
        // If there's only one category or few items, use regular display
        if (groupedItems.Count == 1 || displayItems.Count <= 10)
        {
            layer.SetList2(
                displayItems,
                item => item.DisplayName,
                (item, button) => {
                    if (item.RuleId == "__BACK__")
                    {
                        layer.Close();
                        // Just close - no need to go to main filter menu since add_filter_menu was removed
                    }
                    else if (item.RuleId == "__SEARCH__")
                    {
                        layer.Close();
                        // Removed call to ShowSearchDialog(data) as it's an unused method
                    }
                    else
                    {
                        layer.Close();
                        ShowFilterActionDialog(item, data);
                    }
                },
                (item, button) => {
                    button.button1.mainText.text = item.DisplayName;
                    if (button.button1.subText != null && item.RuleId != "__SEARCH__" && item.RuleId != "__BACK__")
                    {
                        button.button1.subText.text = $"[{item.CategoryPath}]";
                        button.button1.subText.color = new Color(0.7f, 0.7f, 0.7f, 1f);
                    }
                    button.DisableIcon();
                    button.Build();
                },
                autoClose: false
            );
        }
        else
        {
            // Use categories for better organization with many items
            var categoryList = groupedItems.Select(g => $"{g.Key} ({g.Count()})").ToList();
            layer.SetStringList(
                () => categoryList,
                (index, name) => {
                    if (index >= 0 && index < groupedItems.Count)
                    {
                        var categoryItems = groupedItems[index].ToList();
                        ShowFilterList(groupedItems[index].Key, categoryItems, data, false, true);
                    }
                },
                autoClose: true
            );
        }
    }
    
    private static (bool Exists, bool IsWhitelisted) GetExistingFilterInfo(string ruleId, Window.SaveData data)
    {
        var rules = ItemFilterLogic.GetRules(data.filter);
        string ruleBase = ruleId.Contains("|") ? ruleId.Split('|')[0] : ruleId;
        
        foreach (var rule in rules)
        {
            string cleanRule = rule.TrimStart('+', '-');
            string cleanRuleBase = cleanRule.Contains("|") ? cleanRule.Split('|')[0] : cleanRule;
            
            if (cleanRuleBase == ruleBase)
            {
                return (true, rule.StartsWith("+"));
            }
        }
        
        return (false, false);
    }
    
    private static void RemoveFilterRule(string ruleId, Window.SaveData data)
    {
        var rules = ItemFilterLogic.GetRules(data.filter).ToList();
        string ruleBase = ruleId.Contains("|") ? ruleId.Split('|')[0] : ruleId;
        
        rules.RemoveAll(r => {
            string cleanRule = r.TrimStart('+', '-');
            string cleanRuleBase = cleanRule.Contains("|") ? cleanRule.Split('|')[0] : cleanRule;
            return cleanRuleBase == ruleBase;
        });
        
        data.filter = ItemFilterLogic.GetReorderedFilterString(rules);
        data._filterStrs = null;
        ItemFilteringPatches.ReapplyFilterToAllItems();
        RefreshRules();
    }

    private static void ShowFilterActionDialog(SearchableFilterItem filterItem, Window.SaveData data)
    {
        // Check if this filter already exists
        var existingFilterInfo = GetExistingFilterInfo(filterItem.RuleId, data);
        string titleSuffix = "";
        if (existingFilterInfo.Exists)
        {
            titleSuffix = existingFilterInfo.IsWhitelisted ? " [Currently in Whitelist]" : " [Currently in Blacklist]";
        }
        
        Dialog.Choice(ItemFilterPlusTranslations.Get("Choose action for") + $" '{filterItem.DisplayName}'{titleSuffix}:", (dialog) => {
            // Check if this filter accepts numeric values
            bool isNumericFilter = filterItem.RuleId.StartsWith("stat:") || 
                                 filterItem.RuleId.StartsWith("foodstat:") || 
                                 filterItem.RuleId.StartsWith("potential:") || 
                                 filterItem.RuleId.Contains("hardness:") ||
                                 filterItem.RuleId.Contains("quality_");
            
            if (isNumericFilter)
            {
                dialog.AddButton(ItemFilterPlusTranslations.Get("add_to_whitelist") + " (>", () => {
                    ShowNumericInputDialog(filterItem, true, data);
                });
                
                dialog.AddButton(ItemFilterPlusTranslations.Get("add_to_blacklist") + " (<)", () => {
                    ShowNumericInputDialog(filterItem, false, data);
                });
            }
            else
            {
                dialog.AddButton(ItemFilterPlusTranslations.Get("add_to_whitelist"), () => {
                    AddFilterRule(filterItem.RuleId, true, data);
                    EClass.Sound.Play("ui_ok");
                });
                
                dialog.AddButton(ItemFilterPlusTranslations.Get("add_to_blacklist"), () => {
                    AddFilterRule(filterItem.RuleId, false, data);
                    EClass.Sound.Play("ui_ok");
                });
            }
            
            // Add Remove option if filter exists
            if (existingFilterInfo.Exists)
            {
                string removeText = existingFilterInfo.IsWhitelisted ? 
                    ItemFilterPlusTranslations.Get("Remove from Whitelist") : 
                    ItemFilterPlusTranslations.Get("Remove from Blacklist");
                dialog.AddButton(removeText, () => {
                    RemoveFilterRule(filterItem.RuleId, data);
                    EClass.Sound.Play("ui_ok");
                });
            }
            
            // Add Back button
            dialog.AddButton(ItemFilterPlusTranslations.Get("Back"), () => {
                // Go back to previous menu - no action needed as dialog closes automatically
            });
        });
    }
    
    private static void ShowNumericInputDialog(SearchableFilterItem filterItem, bool isWhitelist, Window.SaveData data)
    {
        string prompt = isWhitelist ? 
            $"{ItemFilterPlusTranslations.Get("add_to_whitelist")} {filterItem.DisplayName} >" :
            $"{ItemFilterPlusTranslations.Get("add_to_blacklist")} {filterItem.DisplayName} <";
            
        Dialog.InputName(
            prompt + " " + ItemFilterPlusTranslations.Get("Enter value (0 to disable)"),
            "1",
            (cancelled, text) => {
                if (!cancelled && int.TryParse(text, out int value) && value >= 0)
                {
                    if (value == 0)
                    {
                        // Remove filter if value is 0
                        var rules = ItemFilterLogic.GetRules(data.filter).ToList();
                        rules.RemoveAll(r => r.TrimStart('+', '-').StartsWith(filterItem.RuleId.Split('|')[0]));
                        data.filter = ItemFilterLogic.GetReorderedFilterString(rules);
                        Msg.Say(ItemFilterPlusTranslations.Get("Removed filter for {0}", filterItem.DisplayName));
                    }
                    else
                    {
                        // Add filter with numeric value
                        string ruleWithValue = filterItem.RuleId.Contains('|') ? 
                            filterItem.RuleId.Split('|')[0] + "|" + value :
                            filterItem.RuleId + "|" + value;
                        AddFilterRule(ruleWithValue, isWhitelist, data);
                        
                        // Add specific message notification for numeric filters
                        string messageKey = isWhitelist ? "Added '{0}' to whitelist" : "Added '{0}' to blacklist";
                        string displayName = filterItem.DisplayName + " > " + value;
                        Msg.Say(ItemFilterPlusTranslations.Get(messageKey, displayName));
                    }
                    data._filterStrs = null;
                    ItemFilteringPatches.ReapplyFilterToAllItems();
                    EClass.Sound.Play("ui_ok");
                    
                    // If in global search mode, refresh the search results to keep them visible
                    if (isGlobalSearchMode && globalSearchField != null && !string.IsNullOrEmpty(globalSearchField.text))
                    {
                        ShowGlobalSearchResults(globalSearchField.text);
                    }
                }
                else if (!cancelled)
                {
                    EClass.Sound.Play("beep_small");
                    Dialog.YesNo(ItemFilterPlusTranslations.Get("Invalid number format!"), () => { });
                }
            }
        );
    }
    
    private static List<SearchableFilterItem> GetSearchableFilterData()
    {
        // Ensure the database is built
        if (allSearchableItems == null || allSearchableItems.Count == 0)
        {
            BuildSearchableFilterDatabase();
        }
        
        // Return all items ordered by category and name
        return allSearchableItems.OrderBy(x => x.CategoryPath).ThenBy(x => x.DisplayName).ToList();
    }



    private static void BuildStateSubmenu(GameObject menuGo, Window.SaveData data)
    {
        CreateSearchField(menuGo, data);
        // Replaced with direct content creation
        var content = new GameObject("Content", typeof(VerticalLayoutGroup));
        content.transform.SetParent(menuGo.transform, false);
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 5;
        foreach (BlessedState state in Enum.GetValues(typeof(BlessedState)))
        {
            if (state == BlessedState.Normal) continue;
            string stateName = GetLocalizedValue("state", state.ToString());
            var rowGo = CreateFilterRuleRow(content.transform, stateName, $"state:{state}", data);
            currentMenuItems.Add(rowGo);
        }
    }

    public static void BuildFoodSubmenu(GameObject menuGo, Window.SaveData data)
    {
        var gridGo = new GameObject("Grid", typeof(GridLayoutGroup));
        gridGo.transform.SetParent(menuGo.transform, false);
        var grid = gridGo.GetComponent<GridLayoutGroup>();
        grid.cellSize = new Vector2(300, 40);
        grid.spacing = new Vector2(10, 10);
        grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        grid.constraintCount = 1;

        Action<string, Action<GameObject, Window.SaveData>> createNavButton = (title, builder) => {
            var btn = CreateButton(ItemFilterPlusTranslations.Get(title), grid.transform);
            btn.GetComponent<Button>().onClick.AddListener(() => {
                menuBuilderHistory.Add(BuildFoodSubmenu);
                // Replaced with direct menu building
                foreach (Transform child in menuGo.transform)
                {
                    if (child.name != "Grid") Object.Destroy(child.gameObject);
                }
                builder(menuGo, data);
            });
        };

        createNavButton("filter_menu_food_properties", BuildFoodPropertiesSubmenu);
        createNavButton("filter_menu_food_direct_stats", BuildFoodStatSubmenu);
        createNavButton("filter_menu_potential", BuildPotentialStatSubmenu);
    }

    private static void BuildPropertiesSubmenu(GameObject menuGo, Window.SaveData data)
    {
        var btn = CreateButton(ItemFilterPlusTranslations.Get("filter_prop_hardness_gt"), menuGo.transform);
        btn.GetComponent<Button>().onClick.AddListener(() => ShowHardnessFilterDialog(data));
    }

    private static void BuildFoodPropertiesSubmenu(GameObject menuGo, Window.SaveData data)
    {
        // Replaced with direct content creation
        var content = new GameObject("Content", typeof(VerticalLayoutGroup));
        content.transform.SetParent(menuGo.transform, false);
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 5;

        var btnQualityWhitelist = CreateButton(ItemFilterPlusTranslations.Get("Add Food Quality > to Whitelist"), content.transform);
        btnQualityWhitelist.GetComponent<Button>().onClick.AddListener(() => ShowQualityFilterDialog(data, true));

        var btnNutritionWhitelist = CreateButton(ItemFilterPlusTranslations.Get("filter_prop_nutrition_gt"), content.transform);
        btnNutritionWhitelist.GetComponent<Button>().onClick.AddListener(() => ShowStatFilterDialog(ItemFilterPlusTranslations.Get("Nutrition"), 10, data, true));

        AddBooleanFilterButton(content.transform, ItemFilterPlusTranslations.Get("filter_prop_human_flesh"), "human:true", data, true);
        AddBooleanFilterButton(content.transform, ItemFilterPlusTranslations.Get("filter_prop_undead_flesh"), "undead:true", data, true);
        // ... and so on for other whitelist properties

        var separator = new GameObject("Separator", typeof(Image));
        separator.transform.SetParent(content.transform, false);
        separator.GetComponent<Image>().color = Color.gray;
        separator.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 2);
        separator.AddComponent<LayoutElement>().minHeight = 10;

        var btnQualityBlacklist = CreateButton(ItemFilterPlusTranslations.Get("Add Food Quality < to Blacklist"), content.transform);
        btnQualityBlacklist.GetComponent<Button>().onClick.AddListener(() => ShowQualityFilterDialog(data, false));

        AddBooleanFilterButton(content.transform, ItemFilterPlusTranslations.Get("filter_prop_rotten"), "tainted:true", data, false);
        // ... and so on for other blacklist properties
    }

    private static void AddBooleanFilterButton(Transform parent, string displayName, string rule, Window.SaveData data, bool isWhitelist)
    {
        var btn = CreateButton(displayName, parent);
        btn.GetComponent<Button>().onClick.AddListener(() => AddFilterRule(rule, isWhitelist, data));
    }

    private static void BuildFoodStatSubmenu(GameObject menuGo, Window.SaveData data)
    {
        // Replaced with direct content creation
        var content = new GameObject("Content", typeof(VerticalLayoutGroup));
        content.transform.SetParent(menuGo.transform, false);
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 5;

        var mainAttributesCategory = StatFilterData.Categories.FirstOrDefault(c => c.Key == "Main Attributes");
        if (mainAttributesCategory.Value != null)
        {
            foreach (var elementId in mainAttributesCategory.Value)
            {
                var element = EClass.sources.elements.map[elementId];
                var btn = CreateButton(element.GetName(), content.transform);
                btn.GetComponent<Button>().onClick.AddListener(() => {
                    var actionMenu = EClass.ui.CreateContextMenuInteraction();
                    actionMenu.AddButton(ItemFilterPlusTranslations.Get("add_to_whitelist"), () => ShowFoodStatFilterDialog(element.GetName(), element.id, data, true), hideAfter: true);
                    actionMenu.AddButton(ItemFilterPlusTranslations.Get("add_to_blacklist"), () => ShowFoodStatFilterDialog(element.GetName(), element.id, data, false), hideAfter: true);
                    actionMenu.Show(btn.transform.position);
                });
            }
        }
    }

    private static void BuildPotentialStatSubmenu(GameObject menuGo, Window.SaveData data)
    {
        // Replaced with direct content creation
        var content = new GameObject("Content", typeof(VerticalLayoutGroup));
        content.transform.SetParent(menuGo.transform, false);
        var vlg = content.GetComponent<VerticalLayoutGroup>();
        vlg.padding = new RectOffset(10, 10, 10, 10);
        vlg.spacing = 5;

        var potentialTargets = new Dictionary<int, SourceElement.Row>();
        foreach (var source in EClass.sources.elements.rows)
        {
            var foodEffect = source.foodEffect;
            if (!foodEffect.IsEmpty() && foodEffect[0] == "pot" && foodEffect.Length > 1)
            {
                if (EClass.sources.elements.alias.TryGetValue(foodEffect[1], out var targetStatSource))
                {
                    if (!potentialTargets.ContainsKey(targetStatSource.id))
                    {
                        potentialTargets[targetStatSource.id] = targetStatSource;
                    }
                }
            }
        }

        if (potentialTargets.Any())
        {
            string suffix = ItemFilterPlusTranslations.Get("sort_suffix_potential");
            foreach (var statSource in potentialTargets.Values.OrderBy(s => s.GetName()))
            {
                var btn = CreateButton(statSource.GetName() + suffix, content.transform);
                btn.GetComponent<Button>().onClick.AddListener(() => {
                    var actionMenu = EClass.ui.CreateContextMenuInteraction();
                    actionMenu.AddButton(ItemFilterPlusTranslations.Get("add_to_whitelist"), () => ShowPotentialStatFilterDialog(statSource.GetName(), statSource.id, data, true), hideAfter: true);
                    actionMenu.AddButton(ItemFilterPlusTranslations.Get("add_to_blacklist"), () => ShowPotentialStatFilterDialog(statSource.GetName(), statSource.id, data, false), hideAfter: true);
                    actionMenu.Show(btn.transform.position);
                });
            }
        }
    }

    public static void ShowQualityFilterDialog(Window.SaveData data, bool isWhitelist)
    {
        string title = isWhitelist ? ItemFilterPlusTranslations.Get("Whitelist by Food Quality") : ItemFilterPlusTranslations.Get("Blacklist by Food Quality");
        string prompt = isWhitelist ? ItemFilterPlusTranslations.Get("Enter minimum quality") : ItemFilterPlusTranslations.Get("Enter maximum quality");
        string fullTitle = $"{title}\n\n{prompt}";

        Dialog.InputName(fullTitle, "0", (cancel, text) =>
        {
            if (cancel) return;
            if (int.TryParse(text, out int newValue) && newValue >= 0)
            {
                var rules = ItemFilterLogic.GetRules(data.filter).ToList();
                rules.RemoveAll(r => r.Contains("quality_gt:") || r.Contains("quality_lt:"));

                if (newValue > 0)
                {
                    string rule = isWhitelist ? $"quality_gt:{newValue}" : $"quality_lt:{newValue}";
                    AddFilterRule(rule, isWhitelist, data);
                }
                else
                {
                    data.filter = ItemFilterLogic.GetReorderedFilterString(rules);
                    data._filterStrs = null;
                    EClass.Sound.Play("ui_ok");
                    ItemFilteringPatches.ReapplyFilterToAllItems();
                    if (FilterManagerWindow.IsOpen) RefreshRules();
                }
            }
            else
            {
                EClass.Sound.Play("beep_small");
                Msg.Say(ItemFilterPlusTranslations.Get("Invalid number format!"));
            }
        });
    }

    private static void ShowHardnessFilterDialog(Window.SaveData data)
    {
        Dialog.InputName(ItemFilterPlusTranslations.Get("filter_prop_hardness"), "0", (cancel, text) => {
            if (cancel) return;
            if (int.TryParse(text, out int newValue) && newValue >= 0)
            {
                var rules = ItemFilterLogic.GetRules(data.filter).ToList();
                rules.RemoveAll(r => r.StartsWith("+hardness:"));
                if (newValue > 0)
                {
                    rules.Add($"+hardness:>{newValue}");
                }
                data.filter = ItemFilterLogic.GetReorderedFilterString(rules);
                data._filterStrs = null;
                EClass.Sound.Play("ui_ok");
                RefreshRules();
                ItemFilteringPatches.ReapplyFilterToAllItems();
            }
            else
            {
                EClass.Sound.Play("beep_small");
                Msg.Say(ItemFilterPlusTranslations.Get("Invalid number format!"));
            }
        });
    }

    private static void ShowFoodStatFilterDialog(string statName, int statId, Window.SaveData data, bool isWhitelist)
    {
        string title = ItemFilterPlusTranslations.Get("Filter by {0}", statName);
        Dialog.InputName(title, "1", (cancel, text) =>
        {
            if (cancel) return;
            if (int.TryParse(text, out int newValue) && newValue != 0)
            {
                string ruleBody = $"foodstat:{statId}|{newValue}";
                AddFilterRule(ruleBody, isWhitelist, data);
            }
            else
            {
                EClass.Sound.Play("beep_small");
                Msg.Say(ItemFilterPlusTranslations.Get("Invalid number format!"));
            }
        });
    }

    private static void ShowPotentialStatFilterDialog(string statName, int statId, Window.SaveData data, bool isWhitelist)
    {
        string title = ItemFilterPlusTranslations.Get("Filter by {0}", statName + ItemFilterPlusTranslations.Get("sort_suffix_potential"));
        Dialog.InputName(title, "1", (cancel, text) =>
        {
            if (cancel) return;
            if (int.TryParse(text, out int newValue) && newValue != 0)
            {
                string ruleBody = $"potential:{statId}|{newValue}";
                AddFilterRule(ruleBody, isWhitelist, data);
            }
            else
            {
                EClass.Sound.Play("beep_small");
                Msg.Say(ItemFilterPlusTranslations.Get("Invalid number format!"));
            }
        });
    }

    private static void ShowValueFilterDialog(Window.SaveData data)
    {
        int currentValue = 0;
        string valueRule = ItemFilterLogic.GetRules(data.filter).FirstOrDefault(r => r.StartsWith("+stat:-1|"));
        if (valueRule != null) int.TryParse(valueRule.Split('|').Last(), out currentValue);
        Dialog.InputName(ItemFilterPlusTranslations.Get("Filter by Base Value"), currentValue.ToString(), (cancel, text) =>
        {
            if (cancel) return;
            if (int.TryParse(text, out int newValue) && newValue >= 0)
            {
                var rules = ItemFilterLogic.GetRules(data.filter).ToList();
                rules.RemoveAll(r => r.StartsWith("+stat:-1|"));
                if (newValue > 0) rules.Add($"+stat:-1|{newValue}");
                else Msg.Say(ItemFilterPlusTranslations.Get("Base value filter disabled."));
                data.filter = ItemFilterLogic.GetReorderedFilterString(rules);
                data._filterStrs = null;
                EClass.Sound.Play("ui_ok");
                RefreshRules();
                ItemFilteringPatches.ReapplyFilterToAllItems();
            }
            else
            {
                EClass.Sound.Play("beep_small");
                Msg.Say(ItemFilterPlusTranslations.Get("Invalid number format!"));
            }
        });
    }

    private static void BuildStatSubmenu(UIContextMenu parentMenu, string menuName, IEnumerable<int> elementIds, Window.SaveData data)
    {
        var submenu = parentMenu.AddChild(menuName);
        foreach (var elementId in elementIds)
        {
            var element = EClass.sources.elements.map[elementId];
            int currentValue = 0;
            string statRule = ItemFilterLogic.GetRules(data.filter).FirstOrDefault(r => r.StartsWith($"+stat:{elementId}|"));
            if (statRule != null) int.TryParse(statRule.Split('|').Last(), out currentValue);
            string valueText = currentValue > 0 ? $" (> {currentValue})" : "";
            submenu.AddButton(element.GetName() + valueText, () => ShowStatFilterDialog(element.GetName(), elementId, data, true), hideAfter: true);
        }
    }

    private static void ShowStatFilterDialog(string statName, int statId, Window.SaveData data, bool isWhitelist)
    {
        string title = ItemFilterPlusTranslations.Get("Filter by {0}", statName);

        int currentValue = 0;
        string statRule = ItemFilterLogic.GetRules(data.filter).FirstOrDefault(r => r.StartsWith($"+stat:{statId}|"));
        if (statRule != null) int.TryParse(statRule.Split('|').Last(), out currentValue);
        string defaultValue = currentValue > 0 ? currentValue.ToString() : "0";

        Dialog.InputName(title, defaultValue, (cancel, text) =>
        {
            if (cancel) return;
            if (int.TryParse(text, out int newValue) && newValue >= 0)
            {
                var rules = ItemFilterLogic.GetRules(data.filter).ToList();
                rules.RemoveAll(r => r.TrimStart('+', '-').StartsWith($"stat:{statId}|"));

                if (newValue > 0)
                {
                    rules.Add($"+stat:{statId}|{newValue}");
                }

                data.filter = ItemFilterLogic.GetReorderedFilterString(rules);
                data._filterStrs = null;
                EClass.Sound.Play("ui_ok");
                RefreshRules();
                ItemFilteringPatches.ReapplyFilterToAllItems();
            }
            else
            {
                EClass.Sound.Play("beep_small");
                Msg.Say(ItemFilterPlusTranslations.Get("Invalid number format!"));
            }
        });
    }

    private static void CreateSearchField(GameObject parent, Window.SaveData data)
    {
        var searchGo = new GameObject("SearchInput", typeof(Image), typeof(InputField));
        searchGo.transform.SetParent(parent.transform, false);
        var searchImage = searchGo.GetComponent<Image>();
        searchImage.sprite = buttonSprite;
        searchImage.type = Image.Type.Sliced;
        searchImage.color = new Color(0.9f, 0.9f, 0.9f, 1f);

        var searchRect = searchGo.GetComponent<RectTransform>();
        searchRect.sizeDelta = new Vector2(0, 35);

        var inputField = searchGo.GetComponent<InputField>();
        var placeholderGo = CreateText(ItemFilterPlusTranslations.Get("Search..."), searchGo.transform, 18, Color.black, TextAnchor.MiddleLeft);
        inputField.placeholder = placeholderGo.GetComponent<Text>();

        var textGo = CreateText("", searchGo.transform, 18, Color.black, TextAnchor.MiddleLeft);
        inputField.textComponent = textGo.GetComponent<Text>();

        inputField.onValueChanged.AddListener(OnSearchChanged);
    }

    private static void OnSearchChanged(string query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            // Show all items when search is empty
            foreach (var itemGo in currentMenuItems)
            {
                itemGo.SetActive(true);
                // Reset any text highlighting
                var textComp = itemGo.GetComponentInChildren<Text>();
                if (textComp != null && textComp.supportRichText)
                {
                    // Remove any existing highlighting
                    string originalText = textComp.text;
                    if (originalText.Contains("<color="))
                    {
                        textComp.text = System.Text.RegularExpressions.Regex.Replace(originalText, "<color=[^>]*>|</color>", "");
                    }
                }
            }
            return;
        }

        string[] searchTerms = query.ToLower().Split(' ').Where(term => !string.IsNullOrWhiteSpace(term)).ToArray();

        foreach (var itemGo in currentMenuItems)
        {
            var textComp = itemGo.GetComponentInChildren<Text>();
            if (textComp != null)
            {
                string itemText = textComp.text;
                // Remove any existing highlighting to get clean text
                string cleanText = System.Text.RegularExpressions.Regex.Replace(itemText, "<color=[^>]*>|</color>", "");

                bool matches = searchTerms.All(term => cleanText.ToLower().Contains(term));

                if (matches)
                {
                    itemGo.SetActive(true);

                    // Add highlighting if supported
                    if (textComp.supportRichText)
                    {
                        string highlightedText = cleanText;
                        foreach (var term in searchTerms)
                        {
                            if (cleanText.ToLower().Contains(term))
                            {
                                int index = cleanText.ToLower().IndexOf(term);
                                if (index >= 0)
                                {
                                    string before = highlightedText.Substring(0, index);
                                    string match = highlightedText.Substring(index, term.Length);
                                    string after = highlightedText.Substring(index + term.Length);
                                    highlightedText = before + "<color=yellow>" + match + "</color>" + after;
                                    break; // Only highlight first occurrence to avoid nested tags
                                }
                            }
                        }
                        textComp.text = highlightedText;
                    }
                }
                else
                {
                    itemGo.SetActive(false);
                }
            }
            else
            {
                // If no text component, hide by default during search
                itemGo.SetActive(false);
            }
        }
    }

    private static GameObject CreateFilterRuleRow(Transform parent, string labelText, string ruleId, Window.SaveData data)
    {
        var rowGo = new GameObject(labelText, typeof(HorizontalLayoutGroup), typeof(Image), typeof(LayoutElement));
        rowGo.transform.SetParent(parent, false);

        var hlg = rowGo.GetComponent<HorizontalLayoutGroup>();
        hlg.padding = new RectOffset(10, 10, 5, 5);
        hlg.spacing = 10;
        hlg.childControlWidth = false;
        hlg.childForceExpandWidth = false;

        var rowImage = rowGo.GetComponent<Image>();
        rowImage.sprite = buttonSprite;
        rowImage.type = Image.Type.Sliced;
        rowImage.color = new Color(0.25f, 0.25f, 0.25f, 1f);

        var label = CreateText(labelText, rowGo.transform, 20, Color.white, TextAnchor.MiddleLeft);
        label.GetComponent<LayoutElement>().flexibleWidth = 1;

        var btnWhitelist = CreateButton("+", rowGo.transform);
        btnWhitelist.GetComponent<LayoutElement>().minWidth = 40;
        btnWhitelist.GetComponent<Button>().onClick.AddListener(() => AddFilterRule(ruleId, true, data));

        var btnBlacklist = CreateButton("-", rowGo.transform);
        btnBlacklist.GetComponent<LayoutElement>().minWidth = 40;
        btnBlacklist.GetComponent<Button>().onClick.AddListener(() => AddFilterRule(ruleId, false, data));

        return rowGo;
    }

    private static void CycleFontSize() { fontSizeModifier = (fontSizeModifier + 1) % 6; RefreshRules(); }
    private static void CopyFilters() { if (currentTargetData == null) return; var te = new TextEditor { text = currentTargetData.filter }; te.SelectAll(); te.Copy(); EClass.Sound.Play("ui_ok"); }
    private static bool IsValidFilterString(string text) { if (string.IsNullOrEmpty(text)) return true; var rules = text.Split(','); var allowedKeys = new HashSet<string> { "type", "mat", "rarity", "state", "name", "cat", "name_loc", "stat", "foodstat", "potential", "hardness", "quality_gt", "quality_lt", "tainted", "human", "undead", "catmeat", "bugmeat", "rawfish" }; foreach (var rule in rules) { var trimmedRule = rule.Trim(); if (trimmedRule.Length < 2 || (trimmedRule[0] != '+' && trimmedRule[0] != '-')) return false; string body = trimmedRule.Substring(1); if (body.Contains(":")) { var parts = body.Split(new[] { ':' }, 2); if (parts.Length != 2 || string.IsNullOrEmpty(parts[0]) || string.IsNullOrEmpty(parts[1])) return false; if (!allowedKeys.Contains(parts[0])) return false; } } return true; }
    private static void PasteFilters() { if (currentTargetData == null) return; var te = new TextEditor(); te.Paste(); string clipboardText = te.text; if (IsValidFilterString(clipboardText)) { currentTargetData.filter = clipboardText; currentTargetData._filterStrs = null; EClass.Sound.Play("ui_ok"); RefreshRules(); ItemFilteringPatches.ReapplyFilterToAllItems(); } else { EClass.Sound.Play("beep_small"); Msg.Say(ItemFilterPlusTranslations.Get("Invalid filter format!")); } }
    private static void SetDisplayFilter(DisplayFilter filter) { 
        currentFilter = filter; 
        // Reset global search when switching filter types
        if (isGlobalSearchMode && globalSearchField != null && !string.IsNullOrEmpty(globalSearchField.text))
        {
            globalSearchField.text = "";
            isGlobalSearchMode = false;
        }
        RefreshRules(); 
    }

    private static ScrollRect CreateScrollView(Transform parent) { var svGo = new GameObject("FilterScrollView", typeof(ScrollRect), typeof(LayoutElement)); svGo.transform.SetParent(parent, false); svGo.GetComponent<LayoutElement>().flexibleHeight = 9999; var scrollRect = svGo.GetComponent<ScrollRect>(); var viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask)); viewport.transform.SetParent(svGo.transform, false); viewport.GetComponent<Mask>().showMaskGraphic = false; var viewportImage = viewport.GetComponent<Image>(); viewportImage.sprite = panelSprite; viewportImage.type = Image.Type.Sliced; viewportImage.color = new Color(0, 0, 0, 0.01f); viewportImage.raycastTarget = false; var rtViewport = viewport.GetComponent<RectTransform>(); rtViewport.anchorMin = Vector2.zero; rtViewport.anchorMax = Vector2.one; rtViewport.pivot = Vector2.up; rtViewport.sizeDelta = Vector2.zero; rtViewport.anchoredPosition = Vector2.zero; var content = new GameObject("Content", typeof(RectTransform)); content.transform.SetParent(viewport.transform, false); var rtContent = content.GetComponent<RectTransform>(); rtContent.anchorMin = new Vector2(0, 1); rtContent.anchorMax = new Vector2(1, 1); rtContent.pivot = new Vector2(0.5f, 1); rtContent.sizeDelta = new Vector2(0, 0); scrollRect.viewport = rtViewport; scrollRect.content = rtContent; scrollRect.horizontal = false; scrollRect.verticalScrollbar = CreateScrollbar(svGo.transform); scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHideAndExpandViewport; scrollRect.verticalScrollbarSpacing = -3; return scrollRect; }

    public static void RefreshRules()
    {
        if (gridContent == null) return;

        // If in global search mode, don't refresh - let search handle the display
        if (isGlobalSearchMode && globalSearchField != null && !string.IsNullOrEmpty(globalSearchField.text))
        {
            return;
        }

        btnAll.interactable = currentFilter != DisplayFilter.All;
        btnWhitelist.interactable = currentFilter != DisplayFilter.Whitelist;
        btnBlacklist.interactable = currentFilter != DisplayFilter.Blacklist;
        btnAll.GetComponent<Image>().color = (currentFilter == DisplayFilter.All) ? activeButtonColor : inactiveButtonColor;
        btnWhitelist.GetComponent<Image>().color = (currentFilter == DisplayFilter.Whitelist) ? activeButtonColor : inactiveButtonColor;
        btnBlacklist.GetComponent<Image>().color = (currentFilter == DisplayFilter.Blacklist) ? activeButtonColor : inactiveButtonColor;

        foreach (Transform child in gridContent) Object.Destroy(child.gameObject);
        // Clear status text container
        foreach (Transform child in statusTextContainer.transform) Object.Destroy(child.gameObject);
        
        List<string> rules = ItemFilterLogic.GetRules(currentTargetData.filter);
        if (currentFilter == DisplayFilter.Whitelist) rules = rules.Where(r => r.StartsWith("+")).ToList();
        else if (currentFilter == DisplayFilter.Blacklist) rules = rules.Where(r => r.StartsWith("-")).ToList();

        if (rules.Count == 0)
        {
            // Show status text in dedicated container under filter buttons
            var text = CreateText(ItemFilterPlusTranslations.Get("No filters found."), statusTextContainer.transform, 18, Color.black);
            text.GetComponent<LayoutElement>().minHeight = 30;
        }
        else
        {
            foreach (var rule in rules)
            {
                var buttonGo = CreateButton("", gridContent);
                buttonGo.GetComponent<Image>().color = GetColorForFilter(rule);
                var textComponent = buttonGo.GetComponentInChildren<Text>();
                textComponent.alignment = TextAnchor.MiddleLeft;
                var textRect = textComponent.rectTransform;
                textRect.offsetMin = new Vector2(15, textRect.offsetMin.y);
                textRect.offsetMax = new Vector2(-15, textRect.offsetMax.y);
                textComponent.text = FormatRuleForDisplay(rule);
                textComponent.supportRichText = true;
                var handler = buttonGo.AddComponent<FilterRuleClickHandler>();
                handler.rule = rule;
                handler.data = currentTargetData;
            }
        }
    }

    private static GameObject CreateButton(string text, Transform parent) { var btnGo = new GameObject("Button", typeof(Image), typeof(Button), typeof(LayoutElement)); btnGo.transform.SetParent(parent, false); var layoutElement = btnGo.GetComponent<LayoutElement>(); if (text != "X" && text != "") layoutElement.flexibleWidth = 1; var img = btnGo.GetComponent<Image>(); img.sprite = buttonSprite; img.type = Image.Type.Sliced; var btn = btnGo.GetComponent<Button>(); var colors = btn.colors; colors.highlightedColor = new Color(1f, 1f, 1f, 0.7f); colors.pressedColor = new Color(0.8f, 0.8f, 0.8f, 1f); colors.disabledColor = new Color(0.7f, 0.7f, 0.7f, 0.5f); btn.colors = colors; int baseFontSize = 20; var txtGo = CreateText(text, btnGo.transform, baseFontSize, Color.black); var txtRect = txtGo.GetComponent<RectTransform>(); txtRect.anchorMin = Vector2.zero; txtRect.anchorMax = Vector2.one; txtRect.offsetMin = new Vector2(10, 0); txtRect.offsetMax = new Vector2(-5, 0); return btnGo; }
    private static Scrollbar CreateScrollbar(Transform parent) { var sbGo = new GameObject("Scrollbar", typeof(Scrollbar)); sbGo.transform.SetParent(parent, false); var rtSb = sbGo.GetComponent<RectTransform>(); rtSb.anchorMin = new Vector2(1, 0); rtSb.anchorMax = new Vector2(1, 1); rtSb.pivot = new Vector2(1, 1); rtSb.sizeDelta = new Vector2(30, -10); rtSb.anchoredPosition = new Vector2(0, -20); var handleGo = new GameObject("Handle", typeof(Image)); handleGo.transform.SetParent(sbGo.transform, false); var rtHandle = handleGo.GetComponent<RectTransform>(); rtHandle.anchorMin = Vector2.zero; rtHandle.anchorMax = Vector2.one; rtHandle.sizeDelta = new Vector2(-10, -10); var imgHandle = handleGo.GetComponent<Image>(); imgHandle.sprite = buttonSprite; imgHandle.type = Image.Type.Sliced; imgHandle.color = new Color(0.8f, 0.8f, 0.8f, 0.8f); var sb = sbGo.GetComponent<Scrollbar>(); sb.handleRect = rtHandle; sb.direction = Scrollbar.Direction.BottomToTop; return sb; }
    private static GameObject CreateText(string text, Transform parent, int baseFontSize, Color color, TextAnchor align = TextAnchor.MiddleCenter) { var txtGo = new GameObject("Text", typeof(Text), typeof(LayoutElement)); txtGo.transform.SetParent(parent, false); var txt = txtGo.GetComponent<Text>(); txt.font = gameFont; int[] sizeMods = { -6, -4, -2, 2, 4, 6 }; int finalSize = baseFontSize; if (parent.parent == gridContent) { finalSize += sizeMods[fontSizeModifier]; } txt.fontSize = finalSize; txt.color = color; txt.alignment = align; txt.text = text; txt.horizontalOverflow = HorizontalWrapMode.Wrap; return txtGo; }
    public static string GetLocalizedValue(string type, string key) { try { switch (type) { case "mat": if (int.TryParse(key, out int matId) && EClass.sources.materials.map.TryGetValue(matId, out var matSource)) return matSource.GetName(); break; case "type": case "name": if (EClass.sources.things.map.TryGetValue(key, out var thingSource)) return thingSource.GetName(); break; case "cat": if (EClass.sources.categories.map.TryGetValue(key, out var catSource)) return catSource.GetName(); break; case "rarity": if (Enum.TryParse<Rarity>(key, true, out var rarityEnum)) { string[] qualityList = Lang.GetList("quality"); int index = Mathf.Clamp((int)rarityEnum + 1, 0, qualityList.Length - 1); return qualityList[index]; } break; case "state": return ("bs" + key).lang(); } } catch (Exception e) { Debug.LogError($"[ItemFilterPlus] Error getting localized value for {type}:{key}. Error: {e.Message}"); } return key; }

    public static void EditStatFilter(string rule, Window.SaveData data)
    {
        string prefix = rule.StartsWith("+") ? "+" : "-";
        string cleanRule = rule.TrimStart('+', '-');

        var parts = cleanRule.Split(':');
        if (parts.Length < 2) return;

        string type = parts[0];
        string valuePart = parts[1];
        var valueParts = valuePart.Split('|');
        if (valueParts.Length != 2) return;

        if (int.TryParse(valueParts[0], out int statId) && int.TryParse(valueParts[1], out int currentValue))
        {
            string title = "";
            if (type == "stat" && statId == -1) title = ItemFilterPlusTranslations.Get("Filter by Base Value");
            else if (type == "potential") title = ItemFilterPlusTranslations.Get("Filter by {0}", EClass.sources.elements.map[statId].GetName() + ItemFilterPlusTranslations.Get("sort_suffix_potential"));
            else title = ItemFilterPlusTranslations.Get("Filter by {0}", EClass.sources.elements.map[statId].GetName());

            Dialog.InputName(title, currentValue.ToString(), (cancel, text) =>
            {
                if (cancel) return;
                if (int.TryParse(text, out int newValue) && newValue >= 0)
                {
                    var rules = ItemFilterLogic.GetRules(data.filter).Where(r => r != rule).ToList();
                    if (newValue > 0) rules.Add($"{prefix}{type}:{statId}|{newValue}");
                    data.filter = ItemFilterLogic.GetReorderedFilterString(rules);
                    data._filterStrs = null;
                    EClass.Sound.Play("ui_ok");
                    RefreshRules();
                    ItemFilteringPatches.ReapplyFilterToAllItems();
                }
                else
                {
                    EClass.Sound.Play("beep_small");
                    Msg.Say(ItemFilterPlusTranslations.Get("Invalid number format!"));
                }
            });
        }
    }

    public static string FormatRuleForDisplay(string rule)
    {
        var parts = rule.Split('#');
        string coreRule = parts[0];
        var flags = new HashSet<string>(parts.Skip(1));
        string cleanRule = coreRule.TrimStart('+', '-');
        string details;
        string propertyTypeName = ItemFilterPlusTranslations.Get("property_display");

        if (cleanRule.StartsWith("stat:"))
        {
            var statParts = cleanRule.Substring(5).Split('|');
            if (statParts.Length == 2 && int.TryParse(statParts[0], out int statId) && int.TryParse(statParts[1], out int value))
            {
                // Find which category this stat belongs to
                string categoryName = "Stat";
                foreach (var category in StatFilterData.Categories)
                {
                    if (category.Value.Contains(statId))
                    {
                        categoryName = category.Key;
                        break;
                    }
                }

                if (coreRule.StartsWith("-"))
                {
                    // Blacklist uses < comparison
                    if (statId == 10) details = ItemFilterPlusTranslations.Get("Nutrition: < {0}", value);
                    else if (statId == -1) details = ItemFilterPlusTranslations.Get("Value: < {0} gp", value);
                    else details = ItemFilterPlusTranslations.Get("{0}: {1} < {2}", categoryName, EClass.sources.elements.map[statId].GetName(), value);
                }
                else
                {
                    // Whitelist uses > comparison
                    if (statId == 10) details = ItemFilterPlusTranslations.Get("Nutrition: > {0}", value);
                    else if (statId == -1) details = ItemFilterPlusTranslations.Get("Value: > {0} gp", value);
                    else details = ItemFilterPlusTranslations.Get("{0}: {1} > {2}", categoryName, EClass.sources.elements.map[statId].GetName(), value);
                }
            }
            else details = cleanRule;
        }
        else if (cleanRule.StartsWith("foodstat:"))
        {
            var statParts = cleanRule.Substring(9).Split('|');
            if (statParts.Length == 2 && int.TryParse(statParts[0], out int statId) && int.TryParse(statParts[1], out int value))
            {
                if (coreRule.StartsWith("-"))
                {
                    details = ItemFilterPlusTranslations.Get("Food Stat Less Than", EClass.sources.elements.map[statId].GetName(), value);
                }
                else
                {
                    details = ItemFilterPlusTranslations.Get("Food Stat Greater Than", EClass.sources.elements.map[statId].GetName(), value);
                }
            }
            else details = cleanRule;
        }
        else if (cleanRule.StartsWith("hardness:")) { details = string.Format(ItemFilterPlusTranslations.Get("Hardness: > {0}"), cleanRule.Substring(10)); }
        else if (cleanRule.StartsWith("potential:"))
        {
            var potParts = cleanRule.Substring(10).Split('|');
            if (potParts.Length == 2 && int.TryParse(potParts[0], out int statId) && int.TryParse(potParts[1], out int value))
            {
                if (coreRule.StartsWith("-"))
                {
                    details = ItemFilterPlusTranslations.Get("Potential Less Than", EClass.sources.elements.map[statId].GetName(), value);
                }
                else
                {
                    details = ItemFilterPlusTranslations.Get("Potential Greater Than", EClass.sources.elements.map[statId].GetName(), value);
                }
            }
            else details = cleanRule;
        }
        else if (cleanRule.StartsWith("name_loc:")) { details = string.Format(ItemFilterPlusTranslations.Get("Item: {0}"), cleanRule.Substring(9)); }
        else if (cleanRule.StartsWith("name:")) { var nameParts = cleanRule.Substring(5).Split('|'); if (nameParts.Length == 2) { var tempThing = ThingGen.Create(nameParts[0]); tempThing.c_idRefCard = nameParts[1]; details = string.Format(ItemFilterPlusTranslations.Get("Item: {0}"), tempThing.GetName(NameStyle.Simple, 1)); } else details = cleanRule; }
        else if (cleanRule.StartsWith("type:")) details = string.Format(ItemFilterPlusTranslations.Get("Type: {0}"), GetLocalizedValue("type", cleanRule.Substring(5)));
        else if (cleanRule.StartsWith("cat:")) details = string.Format(ItemFilterPlusTranslations.Get("Category: {0}"), GetLocalizedValue("cat", cleanRule.Substring(4)));
        else if (cleanRule.StartsWith("rarity:")) details = string.Format(ItemFilterPlusTranslations.Get("Rarity: {0}"), GetLocalizedValue("rarity", cleanRule.Substring(7)));
        else if (cleanRule.StartsWith("state:")) details = string.Format(ItemFilterPlusTranslations.Get("State: {0}"), GetLocalizedValue("state", cleanRule.Substring(6)));
        else if (cleanRule.StartsWith("mat:")) details = string.Format(ItemFilterPlusTranslations.Get("Material: {0}"), GetLocalizedValue("mat", cleanRule.Substring(4)));
        else if (cleanRule.StartsWith("tainted:")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_rotten"));
        else if (cleanRule.StartsWith("human:")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_human_flesh"));
        else if (cleanRule.StartsWith("undead:")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_undead_flesh"));
        else if (cleanRule.StartsWith("catmeat:")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_cat_meat"));
        else if (cleanRule.StartsWith("bugmeat:")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_bug_meat"));
        else if (cleanRule.StartsWith("rawfish:")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_raw_fish"));
        else if (cleanRule.StartsWith("aphrodisiac:")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_aphrodisiac"));
        else if (cleanRule.StartsWith("fresh:")) details = string.Format(propertyTypeName, "isFresh".lang());
        else if (cleanRule.StartsWith("rotting:")) details = string.Format(propertyTypeName, "rotting".lang());
        else if (cleanRule.StartsWith("identify:true")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_identify"));
        else if (cleanRule.StartsWith("identify:false")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_unidentified"));
        else if (cleanRule.StartsWith("equipped:true")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_equipped"));
        else if (cleanRule.StartsWith("equipped:false")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_unequippable"));
        else if (cleanRule.StartsWith("precious:true")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_precious"));
        else if (cleanRule.StartsWith("stolen:true")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_stolen"));
        else if (cleanRule.StartsWith("quality_gt:")) details = string.Format(ItemFilterPlusTranslations.Get("Food Quality: > {0}"), cleanRule.Substring(11));
        else if (cleanRule.StartsWith("quality_lt:")) details = string.Format(ItemFilterPlusTranslations.Get("Food Quality: < {0}"), cleanRule.Substring(11));
        else details = GetLocalizedValue("type", cleanRule);

        var displayParts = new List<string>();
        displayParts.Add("<color=red>X</color>");
        if (coreRule.StartsWith("+") && !cleanRule.StartsWith("stat:") && !cleanRule.StartsWith("foodstat:") && currentTargetData == EMono.player.dataPick)
        {
            string worldSymbol = flags.Contains("noworld") ? "<color=grey>☆</color>" : "<color=yellow>★</color>";
            string invSymbol = flags.Contains("noinv") ? "<color=grey>⊠</color>" : "<color=white>□</color>";
            displayParts.Add($"{worldSymbol} {invSymbol}");
        }
        displayParts.Add(details);
        return string.Join(" ", displayParts);
    }

    // Simplified version for search results without symbols
    public static string FormatRuleForSearchDisplay(string rule)
    {
        var parts = rule.Split('#');
        string coreRule = parts[0];
        string cleanRule = coreRule.TrimStart('+', '-');
        string details;
        string propertyTypeName = ItemFilterPlusTranslations.Get("property_display");

        if (cleanRule.StartsWith("stat:"))
        {
            var statParts = cleanRule.Substring(5).Split('|');
            if (statParts.Length == 2 && int.TryParse(statParts[0], out int statId) && int.TryParse(statParts[1], out int value))
            {
                if (coreRule.StartsWith("-"))
                {
                    // Blacklist uses < comparison
                    if (statId == 10) details = ItemFilterPlusTranslations.Get("Nutrition: < {0}", value);
                    else if (statId == -1) details = ItemFilterPlusTranslations.Get("Value: < {0} gp", value);
                    else details = ItemFilterPlusTranslations.Get("Stat: {0} < {1}", EClass.sources.elements.map[statId].GetName(), value);
                }
                else
                {
                    // Whitelist uses > comparison
                    if (statId == 10) details = ItemFilterPlusTranslations.Get("Nutrition: > {0}", value);
                    else if (statId == -1) details = ItemFilterPlusTranslations.Get("Value: > {0} gp", value);
                    else details = ItemFilterPlusTranslations.Get("Stat: {0} > {1}", EClass.sources.elements.map[statId].GetName(), value);
                }
            }
            else details = cleanRule;
        }
        else if (cleanRule.StartsWith("foodstat:"))
        {
            var statParts = cleanRule.Substring(9).Split('|');
            if (statParts.Length == 2 && int.TryParse(statParts[0], out int statId) && int.TryParse(statParts[1], out int value))
            {
                if (coreRule.StartsWith("-"))
                {
                    details = ItemFilterPlusTranslations.Get("Food Stat Less Than", EClass.sources.elements.map[statId].GetName(), value);
                }
                else
                {
                    details = ItemFilterPlusTranslations.Get("Food Stat Greater Than", EClass.sources.elements.map[statId].GetName(), value);
                }
            }
            else details = cleanRule;
        }
        else if (cleanRule.StartsWith("hardness:")) { details = string.Format(ItemFilterPlusTranslations.Get("Hardness: > {0}"), cleanRule.Substring(10)); }
        else if (cleanRule.StartsWith("potential:"))
        {
            var potParts = cleanRule.Substring(10).Split('|');
            if (potParts.Length == 2 && int.TryParse(potParts[0], out int statId) && int.TryParse(potParts[1], out int value))
            {
                if (coreRule.StartsWith("-"))
                {
                    details = ItemFilterPlusTranslations.Get("Potential Less Than", EClass.sources.elements.map[statId].GetName(), value);
                }
                else
                {
                    details = ItemFilterPlusTranslations.Get("Potential Greater Than", EClass.sources.elements.map[statId].GetName(), value);
                }
            }
            else details = cleanRule;
        }
        else if (cleanRule.StartsWith("name_loc:")) { details = string.Format(ItemFilterPlusTranslations.Get("Item: {0}"), cleanRule.Substring(9)); }
        else if (cleanRule.StartsWith("name:")) { var nameParts = cleanRule.Substring(5).Split('|'); if (nameParts.Length == 2) { var tempThing = ThingGen.Create(nameParts[0]); tempThing.c_idRefCard = nameParts[1]; details = string.Format(ItemFilterPlusTranslations.Get("Item: {0}"), tempThing.GetName(NameStyle.Simple, 1)); } else details = cleanRule; }
        else if (cleanRule.StartsWith("type:")) details = string.Format(ItemFilterPlusTranslations.Get("Type: {0}"), GetLocalizedValue("type", cleanRule.Substring(5)));
        else if (cleanRule.StartsWith("cat:")) details = string.Format(ItemFilterPlusTranslations.Get("Category: {0}"), GetLocalizedValue("cat", cleanRule.Substring(4)));
        else if (cleanRule.StartsWith("rarity:")) details = string.Format(ItemFilterPlusTranslations.Get("Rarity: {0}"), GetLocalizedValue("rarity", cleanRule.Substring(7)));
        else if (cleanRule.StartsWith("state:")) details = string.Format(ItemFilterPlusTranslations.Get("State: {0}"), GetLocalizedValue("state", cleanRule.Substring(6)));
        else if (cleanRule.StartsWith("mat:")) details = string.Format(ItemFilterPlusTranslations.Get("Material: {0}"), GetLocalizedValue("mat", cleanRule.Substring(4)));
        else if (cleanRule.StartsWith("tainted:")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_rotten"));
        else if (cleanRule.StartsWith("human:")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_human_flesh"));
        else if (cleanRule.StartsWith("undead:")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_undead_flesh"));
        else if (cleanRule.StartsWith("catmeat:")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_cat_meat"));
        else if (cleanRule.StartsWith("bugmeat:")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_bug_meat"));
        else if (cleanRule.StartsWith("rawfish:")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_raw_fish"));
        else if (cleanRule.StartsWith("aphrodisiac:")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_aphrodisiac"));
        else if (cleanRule.StartsWith("fresh:")) details = string.Format(propertyTypeName, "isFresh".lang());
        else if (cleanRule.StartsWith("rotting:")) details = string.Format(propertyTypeName, "rotting".lang());
        else if (cleanRule.StartsWith("identify:true")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_identify"));
        else if (cleanRule.StartsWith("identify:false")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_unidentified"));
        else if (cleanRule.StartsWith("precious:true")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_precious"));
        else if (cleanRule.StartsWith("stolen:true")) details = string.Format(propertyTypeName, ItemFilterPlusTranslations.Get("filter_prop_stolen"));
        else if (cleanRule.StartsWith("quality_gt:")) details = string.Format(ItemFilterPlusTranslations.Get("Food Quality: > {0}"), cleanRule.Substring(11));
        else if (cleanRule.StartsWith("quality_lt:")) details = string.Format(ItemFilterPlusTranslations.Get("Food Quality: < {0}"), cleanRule.Substring(11));
        else details = GetLocalizedValue("type", cleanRule);

        // Return just the details without X prefix or symbols for search results
        return details;
    }

    private static Color GetColorForFilter(string rule)
    {
        string cleanRule = rule.TrimStart('+', '-');
        if (cleanRule.StartsWith("stat:")) return new Color(0.85f, 1.0f, 1.0f);
        if (cleanRule.StartsWith("foodstat:") || cleanRule.StartsWith("potential:") || cleanRule.StartsWith("quality_")) return new Color(0.85f, 1.0f, 0.85f);
        if (cleanRule.StartsWith("hardness:")) return new Color(0.9f, 0.9f, 0.9f);
        if (cleanRule.StartsWith("name_loc:") || cleanRule.StartsWith("type:") || cleanRule.StartsWith("name:")) return new Color(0.85f, 0.92f, 1.0f);
        if (cleanRule.StartsWith("rarity:")) return new Color(0.92f, 0.85f, 1.0f);
        if (cleanRule.StartsWith("cat:")) return new Color(0.85f, 0.85f, 0.85f);
        if (cleanRule.StartsWith("state:")) return new Color(0.85f, 1.0f, 0.92f);
        if (cleanRule.StartsWith("mat:")) return new Color(1.0f, 0.95f, 0.85f);
        if (cleanRule.StartsWith("tainted:")) return new Color(0.7f, 0.6f, 0.5f);
        if (cleanRule.StartsWith("human:") || cleanRule.StartsWith("undead:")) return new Color(1.0f, 0.7f, 0.7f);
        if (cleanRule.StartsWith("catmeat:") || cleanRule.StartsWith("bugmeat:")) return new Color(0.9f, 0.8f, 0.7f);
        if (cleanRule.StartsWith("rawfish:")) return new Color(0.7f, 0.9f, 1.0f);
        return new Color(0.9f, 0.9f, 0.9f);
    }

    public static void RemoveRule(string ruleToRemove, Window.SaveData data)
    {
        if (data == null) return;
        var rules = ItemFilterLogic.GetRules(data.filter).ToList();
        if (rules.Remove(ruleToRemove))
        {
            data.filter = ItemFilterLogic.GetReorderedFilterString(rules);
            data._filterStrs = null;
            EClass.Sound.Play("ui_ok");
            ItemFilteringPatches.ReapplyFilterToAllItems();
        }
    }
}
public class FilterRuleClickHandler : MonoBehaviour, IPointerClickHandler
{
    public string rule;
    public Window.SaveData data;

    public void OnPointerClick(PointerEventData eventData)
    {
        if (eventData.button == PointerEventData.InputButton.Left)
        {
            FilterManagerWindow.RemoveRule(rule, data);
            FilterManagerWindow.RefreshRules();
        }
        if (eventData.button == PointerEventData.InputButton.Right)
        {
            var cleanRule = rule.TrimStart('+', '-');
            if (cleanRule.StartsWith("stat:") || cleanRule.StartsWith("foodstat:") || cleanRule.StartsWith("potential:") || cleanRule.StartsWith("hardness:") || cleanRule.StartsWith("quality_"))
            {
                FilterManagerWindow.EditStatFilter(rule, data);
            }
            else if (rule.StartsWith("+"))
            {
                ShowContextMenu();
            }
            else
            {
                EClass.Sound.Play("beep_small");
            }
        }
    }

    private void ShowContextMenu()
    {
        Vector2 clickPosition = EInput.uiMousePosition;
        UIContextMenu menu = EClass.ui.CreateContextMenuInteraction();
        bool worldHighlightDisabled = rule.Contains("#noworld");
        bool invHighlightDisabled = rule.Contains("#noinv");
        string worldText = ItemFilterPlusTranslations.Get("World Highlight") + ": " + (worldHighlightDisabled ? ItemFilterPlusTranslations.Get("Disabled") : ItemFilterPlusTranslations.Get("Enabled"));
        string invText = ItemFilterPlusTranslations.Get("Inventory Outline") + ": " + (invHighlightDisabled ? ItemFilterPlusTranslations.Get("Disabled") : ItemFilterPlusTranslations.Get("Enabled"));
        menu.AddButton(worldText, () => ToggleHighlightFlag(menu, "#noworld"), hideAfter: false);
        menu.AddButton(invText, () => ToggleHighlightFlag(menu, "#noinv"), hideAfter: false);
        menu.Show(clickPosition);
    }

    private void ToggleHighlightFlag(UIContextMenu menu, string flag)
    {
        var rules = ItemFilterLogic.GetRules(data.filter).ToList();
        int ruleIndex = rules.IndexOf(rule);
        if (ruleIndex != -1)
        {
            string updatedRule = ToggleRuleFlag(ruleIndex, data, flag);
            if (updatedRule != null) this.rule = updatedRule;
        }
        if (FilterManagerWindow.IsOpen) FilterManagerWindow.RefreshRules();
        menu?.Root?.Hide();
    }

    private static string ToggleRuleFlag(int ruleIndex, Window.SaveData data, string flag)
    {
        var rules = ItemFilterLogic.GetRules(data.filter).ToList();
        if (ruleIndex < 0 || ruleIndex >= rules.Count) return null;
        string oldRule = rules[ruleIndex];
        string newRule = oldRule.Contains(flag) ? oldRule.Replace(flag, "") : oldRule + flag;
        rules[ruleIndex] = newRule;
        data.filter = ItemFilterLogic.GetReorderedFilterString(rules);
        data._filterStrs = null;
        ItemFilteringPatches.ReapplyFilterToAllItems();
        if (LayerInventory.listInv != null)
        {
            foreach (var layer in LayerInventory.listInv)
            {
                if (layer?.invs == null) continue;
                foreach (var uiInv in layer.invs)
                {
                    if (uiInv != null && uiInv.window != null && uiInv.window.gameObject.activeInHierarchy) uiInv.list.Redraw();
                }
            }
        }
        ItemHighlighter.CleanupStaleEntries();
        EClass.Sound.Play("ui_ok");
        return newRule;
    }
}

[HarmonyPatch]
public static class WhitelistHighlightPatch_Inventory
{
    private static float timer = 0f;
    private const float UPDATE_INTERVAL = 0.15f;

    [HarmonyPostfix, HarmonyPatch(typeof(UIInventory), "Update")]
    public static void ThrottledUpdateHighlights(UIInventory __instance)
    {
        timer += Time.deltaTime;
        if (timer < UPDATE_INTERVAL) return;
        timer = 0f;

        if (__instance?.list?.buttons == null) return;
        if (Time.frameCount % 120 == 0) ItemHighlighter.CleanupStaleEntries();
        foreach (var buttonPair in __instance.list.buttons)
        {
            if (buttonPair.component is ButtonGrid buttonGrid && buttonGrid.gameObject.activeInHierarchy) ItemHighlighter.UpdateHighlightForButton(buttonGrid);
        }
    }
}

[HarmonyPatch]
public static class WhitelistHighlightPatch_DropdownGrid
{
    [HarmonyPostfix, HarmonyPatch(typeof(DropdownGrid), nameof(DropdownGrid.Activate))]
    public static void ApplyHighlightToIngredientDropdown(DropdownGrid __instance)
    {
        if (!ItemFilterLogic.IsEnabled || !ItemFilterLogic.HighlightEnabled) return;
        var buttonList = __instance.listDrop?.buttons;
        if (buttonList == null) return;
        foreach (var buttonPair in buttonList)
        {
            if (buttonPair.component is ButtonGrid buttonGrid && buttonGrid.gameObject.activeInHierarchy) ItemHighlighter.UpdateHighlightForButton(buttonGrid);
        }
    }
}
[HarmonyPatch]
public static class WhitelistHighlightPatch_CraftingIngredients
{
    [HarmonyPostfix, HarmonyPatch(typeof(UIDragGridIngredients), nameof(UIDragGridIngredients.Refresh))]
    public static void ApplyHighlightToCraftingIngredients(UIDragGridIngredients __instance)
    {
        ItemHighlighter.ApplyHighlightsToList(__instance.list);
    }
}

public class OutsideClickHandler : MonoBehaviour
{
    private RectTransform windowRect;
    void Awake() { windowRect = GetComponent<RectTransform>(); }
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (UIContextMenu.Current != null && UIContextMenu.Current.gameObject == this.gameObject)
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(windowRect, Input.mousePosition, null))
                {
                    UIContextMenu.Current.Hide();
                }
            }
            else
            {
                if (!RectTransformUtility.RectangleContainsScreenPoint(windowRect, Input.mousePosition, null))
                {
                    if (this.name.Contains("FilterManagerWindow"))
                    {
                        FilterManagerWindow.Toggle();
                    }
                    else
                    {
                        Object.Destroy(this.gameObject);
                    }
                }
            }
        }
    }
}
#endregion
