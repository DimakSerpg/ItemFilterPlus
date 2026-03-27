using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

namespace ItemFilterPlus
{
    public static class ItemHighlighter
    {
        private const string HIGHLIGHT_GAMEOBJECT_NAME = "ItemFilterPlusHighlightFrame";
        private static GameObject highlightPrefab = null;
        private static readonly Dictionary<ButtonGrid, GameObject> activeHighlights = new Dictionary<ButtonGrid, GameObject>();
        private static bool prefabCreationFailed = false;

        private static Sprite LoadSpriteFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return null;
            Texture2D tex = IO.LoadPNG(filePath, FilterMode.Bilinear);
            if (tex != null)
            {
                return Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f));
            }
            return null;
        }

        private static void CreateHighlightPrefab()
        {
            if (highlightPrefab != null || prefabCreationFailed) return;

            if (ItemFilterPlusPlugin.Instance == null || ItemFilterPlusPlugin.Instance.Info == null)
            {
                prefabCreationFailed = true;
                return;
            }

            string modFolder = Path.GetDirectoryName(ItemFilterPlusPlugin.Instance.Info.Location);
            string spritePath = Path.Combine(modFolder, "item_sprite.png");
            Sprite customSprite = LoadSpriteFromFile(spritePath);

            highlightPrefab = new GameObject(HIGHLIGHT_GAMEOBJECT_NAME, typeof(RectTransform), typeof(Image));
            RectTransform rect = highlightPrefab.GetComponent<RectTransform>();
            Image image = highlightPrefab.GetComponent<Image>();

            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = new Vector2(1f, 1f);
            rect.offsetMax = new Vector2(-1f, -1f);

            if (customSprite != null)
            {
                image.sprite = customSprite;
                image.type = Image.Type.Sliced;
            }
            else
            {
                image.color = new Color(1f, 0.9f, 0.4f, 0.7f);
            }

            image.raycastTarget = false;
            highlightPrefab.SetActive(false);
            Object.DontDestroyOnLoad(highlightPrefab);
            highlightPrefab.hideFlags = HideFlags.HideInHierarchy;
        }

        public static void UpdateHighlightForButton(ButtonGrid button)
        {
            if (highlightPrefab == null)
            {
                CreateHighlightPrefab();
                if (highlightPrefab == null) return;
            }

            bool shouldBeHighlighted = false;
            if (ItemFilterLogic.IsEnabled && ItemFilterLogic.HighlightEnabled && button.card is Thing item)
            {
                Window.SaveData filterData = EMono.player.dataPick;
                if (filterData != null && !string.IsNullOrEmpty(filterData.filter))
                {
                    shouldBeHighlighted = ItemFilterLogic.IsItemWhitelistedForInventory(item, filterData.filter);
                }
            }

            bool hasHighlight = activeHighlights.ContainsKey(button);

            if (shouldBeHighlighted)
            {
                if (!hasHighlight)
                {
                    GameObject newHighlight = Object.Instantiate(highlightPrefab, button.transform, false);
                    newHighlight.SetActive(true);
                    newHighlight.transform.SetAsFirstSibling();
                    activeHighlights[button] = newHighlight;
                }
            }
            else
            {
                if (hasHighlight)
                {
                    Object.Destroy(activeHighlights[button]);
                    activeHighlights.Remove(button);
                }
            }
        }

        // *** НОВЫЙ ПУБЛИЧНЫЙ МЕТОД ***
        // Этот метод будет вызываться из кода сортировки и из патчей
        public static void ApplyHighlightsToList(UIList list)
        {
            if (!ItemFilterLogic.IsEnabled || !ItemFilterLogic.HighlightEnabled || list?.buttons == null) return;

            foreach (var buttonPair in list.buttons)
            {
                if (buttonPair.component is ButtonGrid buttonGrid && buttonGrid.gameObject.activeInHierarchy)
                {
                    UpdateHighlightForButton(buttonGrid);
                }
            }
        }

        public static void CleanupStaleEntries()
        {
            List<ButtonGrid> staleKeys = new List<ButtonGrid>();
            foreach (var pair in activeHighlights)
            {
                if (pair.Key == null || pair.Value == null)
                {
                    if (pair.Value != null) Object.Destroy(pair.Value);
                    staleKeys.Add(pair.Key);
                }
            }
            foreach (var key in staleKeys)
            {
                activeHighlights.Remove(key);
            }
        }
    }
}
