using BepInEx.Configuration;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using UnityEngine.UI;

namespace ItemFilterPlus
{
    // --- МЕНЕДЖЕР СОРТИРОВКИ ---
    public static class IngredientSortManager
    {
        private static readonly Dictionary<string, (int, bool)> lastSortStates = new Dictionary<string, (int, bool)>();
        public static readonly IngredientStatComparer Comparer = new IngredientStatComparer();
        public static int CurrentSortStatId = -1;
        public static bool IsSortingByPotential = false;
        private static ConfigEntry<string> savedStatesConfig;

        public static void Initialize(ConfigEntry<string> configEntry)
        {
            savedStatesConfig = configEntry;
            LoadStates();
        }

        private static void LoadStates()
        {
            lastSortStates.Clear();
            if (savedStatesConfig == null || string.IsNullOrEmpty(savedStatesConfig.Value)) return;

            var entries = savedStatesConfig.Value.Split(';');
            foreach (var entry in entries)
            {
                if (string.IsNullOrEmpty(entry)) continue;
                var parts = entry.Split(':');
                if (parts.Length != 2) continue;
                var valueParts = parts[1].Split(',');
                if (valueParts.Length != 2) continue;

                if (int.TryParse(valueParts[0], out int statId) && bool.TryParse(valueParts[1], out bool isPotential))
                {
                    lastSortStates[parts[0]] = (statId, isPotential);
                }
            }
        }

        private static void SaveStates()
        {
            if (savedStatesConfig == null) return;
            var sb = new StringBuilder();
            foreach (var pair in lastSortStates)
            {
                sb.Append($"{pair.Key}:{pair.Value.Item1},{pair.Value.Item2};");
            }
            if (sb.Length > 0) sb.Length--;
            savedStatesConfig.Value = sb.ToString();
        }

        public static void SetCurrentContext(string context)
        {
            if (lastSortStates.TryGetValue(context, out var savedState))
            {
                CurrentSortStatId = savedState.Item1;
                IsSortingByPotential = savedState.Item2;
            }
            else
            {
                CurrentSortStatId = -1;
                IsSortingByPotential = false;
            }
        }

        public static void SaveSortState(string context, int statId, bool isPotential)
        {
            lastSortStates[context] = (statId, isPotential);
            CurrentSortStatId = statId;
            IsSortingByPotential = isPotential;
            SaveStates();
        }
    }

    // --- Логика сравнения ---
    public class IngredientStatComparer : IComparer<object>
    {
        public int Compare(object x, object y)
        {
            Thing thingX = x as Thing; Thing thingY = y as Thing;
            if (thingX == null || thingY == null) return 0;
            int result = 0;
            if (IngredientSortManager.CurrentSortStatId != -1)
            {
                int valueX = GetValueForStat(thingX, IngredientSortManager.CurrentSortStatId);
                int valueY = GetValueForStat(thingY, IngredientSortManager.CurrentSortStatId);
                result = valueY.CompareTo(valueX);
            }
            if (result == 0) result = thingX.category._index.CompareTo(thingY.category._index);
            if (result == 0) result = thingX.sourceCard._index.CompareTo(thingY.sourceCard._index);
            if (result == 0) result = thingY.Num.CompareTo(thingX.Num);
            return result;
        }

        private int GetValueForStat(Thing thing, int statId)
        {
            if (statId == -2) return thing.material?.hardness ?? 0;
            if (IngredientSortManager.IsSortingByPotential)
            {
                foreach (var element in thing.elements.dict.Values)
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
            else
            {
                int rawValue = thing.elements.Value(statId);
                // For food traits (including quality), use displayed level for consistent sorting
                if (EClass.sources.elements.map.TryGetValue(statId, out var source) &&
                    (source.category == "food" || source.tag.Contains("trait")))
                {
                    return CalculateDisplayedFoodTraitLevel(rawValue);
                }
                return rawValue;
            }
        }

        // Convert raw food trait value to displayed level (same as ItemFilterLogic)
        private int CalculateDisplayedFoodTraitLevel(int rawValue)
        {
            if (rawValue == 0) return 0;
            int displayedLevel = rawValue / 10;
            return (rawValue < 0) ? (displayedLevel - 1) : (displayedLevel + 1);
        }
    }

    // --- Общая логика создания меню ---
    public static class IngredientSortMenuBuilder
    {
        private static bool IsHardnessRelevant(Thing thing)
        {
            return (thing.category.IsChildOf("throw") || thing.category.IsChildOf("resource") || thing.trait.IsTool) && !(thing.trait is TraitAbility);
        }

        public static bool IsSortApplicable(UIList list, int statId, bool isPotential)
        {
            if (statId == -1) return true;
            foreach (var itemObj in list.items)
            {
                if (itemObj is Thing thing)
                {
                    if (statId == -2) { if (IsHardnessRelevant(thing) && thing.material?.hardness > 0) return true; }
                    else if (isPotential)
                    {
                        foreach (var element in thing.elements.dict.Values)
                        {
                            var foodEffect = element.source.foodEffect;
                            if (!foodEffect.IsEmpty() && foodEffect[0] == "pot" && foodEffect.Length > 1)
                            {
                                if (EClass.sources.elements.alias.TryGetValue(foodEffect[1], out var targetStatSource) && targetStatSource.id == statId) return true;
                            }
                        }
                    }
                    else { if (thing.elements.Value(statId) != 0) return true; }
                }
            }
            return false;
        }

        public static void BuildAndShowMenu(UIList targetList, Action<int> applyRegularSortAction, Action<int> applyPotentialSortAction)
        {
            if (targetList == null) return;
            Vector2 clickPosition = EInput.uiMousePosition;
            UIContextMenu menu = EClass.ui.CreateContextMenuInteraction();

            var stats = new List<SourceElement.Row>();
            var potentialTargets = new List<SourceElement.Row>();
            var namedProperties = new List<SourceElement.Row>();
            bool hasHardness = false;

            foreach (var itemObj in targetList.items)
            {
                if (itemObj is Thing ingredient)
                {
                    if (!hasHardness && IsHardnessRelevant(ingredient)) hasHardness = true;

                    foreach (var element in ingredient.elements.dict.Values)
                    {
                        if (element.Value == 0) continue;
                        var source = element.source;
                        var foodEffect = source.foodEffect;

                        if (!foodEffect.IsEmpty() && foodEffect[0] == "pot" && foodEffect.Length > 1)
                        {
                            string targetStatAlias = foodEffect[1];
                            if (EClass.sources.elements.alias.TryGetValue(targetStatAlias, out var targetStatSource))
                            {
                                if (!potentialTargets.Any(s => s.id == targetStatSource.id)) potentialTargets.Add(targetStatSource);
                            }
                        }

                        // Используем GetNameWithFallback, чтобы отсеять полностью безымянные служебные элементы
                        if (string.IsNullOrEmpty(GetNameWithFallback(source)) || source.tag.Contains("flag")) continue;

                        if (source.category == "attribute" || source.category == "skill")
                        {
                            if (!stats.Any(s => s.id == source.id)) stats.Add(source);
                        }
                        else if (source.category == "food" || source.tag.Contains("trait"))
                        {
                            if (!namedProperties.Any(p => p.id == source.id)) namedProperties.Add(source);
                        }
                    }
                }
            }

            if (hasHardness)
            {
                namedProperties.Add(new SourceElement.Row { id = -2, name = ItemFilterPlusTranslations.Get("sort_stat_hardness") });
            }
            
            // Add Aphrodisiac (element 758) as a manual entry if not already present
            // It's a food property with "love" effect
            if (!namedProperties.Any(p => p.id == 758))
            {
                foreach (var itemObj in targetList.items)
                {
                    if (itemObj is Thing ingredient && ingredient.elements.Value(758) != 0)
                    {
                        if (EClass.sources.elements.map.TryGetValue(758, out var aphrodisiacSource))
                        {
                            namedProperties.Add(aphrodisiacSource);
                        }
                        break;
                    }
                }
            }

            const int threshold = 4;
            var sortedStats = stats.OrderBy(s => GetNameWithFallback(s)).ToList();
            var sortedPotentialTargets = potentialTargets.OrderBy(s => GetNameWithFallback(s)).ToList();
            var sortedNamedProperties = namedProperties.OrderBy(p => GetNameWithFallback(p)).ToList();

            menu.AddButton(ItemFilterPlusTranslations.Get("sort_menu_default"), () => applyRegularSortAction(-1));

            if (sortedNamedProperties.Any())
            {
                menu.AddSeparator();
                if (sortedNamedProperties.Count > threshold)
                {
                    var subMenu = menu.AddChild(ItemFilterPlusTranslations.Get("sort_menu_properties"));
                    foreach (var prop in sortedNamedProperties) subMenu.AddButton(GetNameWithFallback(prop), () => applyRegularSortAction(prop.id));
                }
                else
                {
                    foreach (var prop in sortedNamedProperties) menu.AddButton(GetNameWithFallback(prop), () => applyRegularSortAction(prop.id));
                }
            }

            if (sortedStats.Any())
            {
                menu.AddSeparator();
                if (sortedStats.Count > threshold)
                {
                    var subMenu = menu.AddChild(ItemFilterPlusTranslations.Get("sort_menu_stats"));
                    foreach (var statSource in sortedStats) subMenu.AddButton(GetNameWithFallback(statSource), () => applyRegularSortAction(statSource.id));
                }
                else
                {
                    foreach (var statSource in sortedStats) menu.AddButton(GetNameWithFallback(statSource), () => applyRegularSortAction(statSource.id));
                }
            }

            if (sortedPotentialTargets.Any())
            {
                menu.AddSeparator();
                string potentialSuffix = ItemFilterPlusTranslations.Get("sort_suffix_potential");
                if (sortedPotentialTargets.Count > threshold)
                {
                    var subMenu = menu.AddChild(ItemFilterPlusTranslations.Get("sort_menu_potency"));
                    foreach (var statSource in sortedPotentialTargets)
                    {
                        subMenu.AddButton(GetNameWithFallback(statSource) + potentialSuffix, () => applyPotentialSortAction(statSource.id));
                    }
                }
                else
                {
                    foreach (var statSource in sortedPotentialTargets)
                    {
                        menu.AddButton(GetNameWithFallback(statSource) + potentialSuffix, () => applyPotentialSortAction(statSource.id));
                    }
                }
            }
            menu.Show(clickPosition);
        }

        // Функция-помощник для получения имени с фолбэком на английский
        private static string GetNameWithFallback(SourceElement.Row source)
        {
            if (source == null) return "";
            // Специальный случай для нашей "Твёрдости"
            if (source.id == -2) return source.name;

            string localizedName = source.GetName();
            return string.IsNullOrEmpty(localizedName) ? source.name : localizedName;
        }
    }

    // --- Абстрактный базовый класс для контроллеров ---
    public abstract class BaseIngredientSortController : MonoBehaviour
    {
        protected UIList targetList;
        protected GameObject sortButtonInstance;
        protected Text sortButtonText;
        protected string sortContext;
        private static bool isInitializedStatically = false;
        protected static Sprite buttonSprite;
        protected static Font gameFont;

        public void SetContext(string newContext)
        {
            this.sortContext = newContext;
        }

        protected static void StaticInitialize()
        {
            if (isInitializedStatically) return;
            var tempMenu = EClass.ui.CreateContextMenuInteraction();
            if (tempMenu != null)
            {
                if (tempMenu.defaultButton != null) buttonSprite = tempMenu.defaultButton.GetComponent<Image>().sprite;
                if (SkinManager.Instance?.fontSet?.ui?.source?.font != null) gameFont = SkinManager.Instance.fontSet.ui.source.font;
                Destroy(tempMenu.gameObject);
            }
            isInitializedStatically = true;
        }

        protected void OnSortButtonClick()
        {
            IngredientSortManager.SetCurrentContext(sortContext);
            IngredientSortMenuBuilder.BuildAndShowMenu(targetList, ApplyRegularSort, ApplyPotentialSort);
        }

        private void ApplyRegularSort(int statId) => DoSort(statId, false);
        private void ApplyPotentialSort(int statId) => DoSort(statId, true);

        private void DoSort(int statId, bool isPotential)
        {
            if (targetList == null) return;
            IngredientSortManager.SaveSortState(sortContext, statId, isPotential);
            targetList.items.Sort(IngredientSortManager.Comparer);
            targetList.Refresh();
            ItemHighlighter.ApplyHighlightsToList(targetList);
            UpdateText();
        }

        public void ApplySavedSortAndHighlights()
        {
            StartCoroutine(ApplySavedSortAndHighlightsCoroutine());
        }

        private IEnumerator ApplySavedSortAndHighlightsCoroutine()
        {
            yield return null;
            if (targetList == null) yield break;

            IngredientSortManager.SetCurrentContext(sortContext);

            bool isApplicable = IngredientSortMenuBuilder.IsSortApplicable(targetList, IngredientSortManager.CurrentSortStatId, IngredientSortManager.IsSortingByPotential);

            if (isApplicable && IngredientSortManager.CurrentSortStatId != -1)
            {
                targetList.items.Sort(IngredientSortManager.Comparer);
                targetList.Refresh();
                yield return null;
            }

            UpdateTextBasedOnApplicability(isApplicable);
            ItemHighlighter.ApplyHighlightsToList(targetList);
        }

        public void UpdateTextBasedOnApplicability(bool wasSortApplied)
        {
            if (sortButtonText == null) return;

            if (!wasSortApplied)
            {
                sortButtonText.text = ItemFilterPlusTranslations.Get("sort_button_default");
                return;
            }

            IngredientSortManager.SetCurrentContext(sortContext);
            int currentId = IngredientSortManager.CurrentSortStatId;
            string text = ItemFilterPlusTranslations.Get("sort_button_default");

            if (currentId == -2) { text = ItemFilterPlusTranslations.Get("sort_stat_hardness"); }
            else if (EClass.sources.elements.map.TryGetValue(currentId, out var source))
            {
                // Используем тот же фолбэк и для основной кнопки
                string localizedName = source.GetName();
                text = string.IsNullOrEmpty(localizedName) ? source.name : localizedName;
            }

            if (IngredientSortManager.IsSortingByPotential && currentId != -1)
            {
                text += ItemFilterPlusTranslations.Get("sort_suffix_potential");
            }
            sortButtonText.text = text;
        }

        public void UpdateText()
        {
            UpdateTextBasedOnApplicability(true);
        }

        void OnDestroy()
        {
            if (sortButtonInstance != null) Destroy(sortButtonInstance);
            OnControllerDestroy();
        }

        protected virtual void OnControllerDestroy() { }
    }

    // --- Конкретные реализации контроллеров ---
    public class IngredientSortController : BaseIngredientSortController
    {
        public void Initialize(string context, UIList list, Transform buttonParent, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            this.sortContext = context;
            this.targetList = list;
            StaticInitialize();
            CreateButton(buttonParent, anchoredPosition, sizeDelta);
        }

        private void CreateButton(Transform parent, Vector2 anchoredPosition, Vector2 sizeDelta)
        {
            if (buttonSprite == null || gameFont == null) return;
            sortButtonInstance = new GameObject("IngredientSortButton", typeof(Image), typeof(Button));
            sortButtonInstance.transform.SetParent(parent, false);
            var image = sortButtonInstance.GetComponent<Image>();
            image.sprite = buttonSprite; image.type = Image.Type.Sliced;
            var button = sortButtonInstance.GetComponent<Button>();
            button.onClick.AddListener(OnSortButtonClick);
            var rect = sortButtonInstance.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1); rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = anchoredPosition; rect.sizeDelta = sizeDelta;
            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(sortButtonInstance.transform, false);
            sortButtonText = textGo.GetComponent<Text>();
            sortButtonText.font = gameFont; sortButtonText.fontSize = 18;
            sortButtonText.color = Color.black; sortButtonText.alignment = TextAnchor.MiddleCenter;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
        }
    }

    public class DropdownSortButtonController : BaseIngredientSortController
    {
        private GridLayoutGroup targetGridLayout;
        private bool paddingApplied = false;
        private const int PADDING_TOP_ADDITION = 40;

        public void Initialize(string context, UIList list, RectTransform buttonParent)
        {
            this.sortContext = context;
            this.targetList = list;
            StaticInitialize();
            CreateButton(buttonParent);

            targetGridLayout = list.GetComponent<GridLayoutGroup>();
            if (targetGridLayout != null)
            {
                targetGridLayout.padding.top += PADDING_TOP_ADDITION;
                paddingApplied = true;
                LayoutRebuilder.MarkLayoutForRebuild(list.transform as RectTransform);
            }
        }

        private void CreateButton(Transform parent)
        {
            if (buttonSprite == null || gameFont == null) return;
            sortButtonInstance = new GameObject("DropdownSortButton", typeof(Image), typeof(Button));
            sortButtonInstance.transform.SetParent(parent, false);
            var image = sortButtonInstance.GetComponent<Image>();
            image.sprite = buttonSprite; image.type = Image.Type.Sliced;
            var button = sortButtonInstance.GetComponent<Button>();
            button.onClick.AddListener(OnSortButtonClick);
            var rect = sortButtonInstance.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 0f); rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(90, 5); rect.sizeDelta = new Vector2(160, 35);
            var textGo = new GameObject("Text", typeof(Text));
            textGo.transform.SetParent(sortButtonInstance.transform, false);
            this.sortButtonText = textGo.GetComponent<Text>();
            this.sortButtonText.font = gameFont;
            this.sortButtonText.fontSize = 18;
            this.sortButtonText.color = Color.black;
            this.sortButtonText.alignment = TextAnchor.MiddleCenter;
            var textRect = textGo.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero; textRect.anchorMax = Vector2.one;
            textRect.offsetMin = Vector2.zero; textRect.offsetMax = Vector2.zero;
        }

        protected override void OnControllerDestroy()
        {
            if (paddingApplied && targetGridLayout != null)
                targetGridLayout.padding.top -= PADDING_TOP_ADDITION;
        }
    }

    // --- ПАТЧИ ---
    [HarmonyPatch]
    public static class IngredientSortPatch
    {
        [HarmonyPostfix]
        [HarmonyPatch(typeof(LayerDragGrid), nameof(LayerDragGrid.OnOpen))]
        public static void AttachControllerToDragGrid(LayerDragGrid __instance)
        {
            if (__instance == null || __instance.uiIngredients == null) return;

            string context = "default_craft";
            if (__instance.owner is InvOwnerCraft craftOwner)
            {
                context = craftOwner.crafter.IdSource;
            }

            var controller = __instance.gameObject.GetComponent<IngredientSortController>();
            if (controller == null)
            {
                controller = __instance.gameObject.AddComponent<IngredientSortController>();
                Vector2 position = new Vector2(10, -188);
                Vector2 size = new Vector2(200, 35);
                controller.Initialize(context, __instance.uiIngredients.list, __instance.uiIngredients.transform, position, size);
            }
            else
            {
                controller.SetContext(context);
            }
            controller.ApplySavedSortAndHighlights();
        }

        [HarmonyPostfix]
        [HarmonyPatch(typeof(DropdownGrid), nameof(DropdownGrid.Activate))]
        public static void AttachControllerToDropdownGrid(DropdownGrid __instance, Recipe.Ingredient ingredient)
        {
            if (__instance == null || __instance.rectDrop == null || __instance.listDrop.items.Count == 0) return;

            string context = "default_dropdown";
            if (ingredient != null && !string.IsNullOrEmpty(ingredient.id))
            {
                context = ingredient.id;
            }

            GameObject panelObject = __instance.rectDrop.gameObject;
            var controller = panelObject.GetComponent<DropdownSortButtonController>();
            if (controller == null)
            {
                controller = panelObject.AddComponent<DropdownSortButtonController>();
                controller.Initialize(context, __instance.listDrop, __instance.rectDropContent);
            }
            else
            {
                controller.SetContext(context);
            }
            controller.ApplySavedSortAndHighlights();
        }
    }
}
