using System;
namespace ItemFilterPlus
{
    public static class ItemFilterPlusTranslations
    {
        public static string Get(string id, params object[] args)
        {
            string lang = EClass.core.config.lang;
            string translation;

            // Handle special pluralization case for "Found filters matching query"
            if (id == "Found filters matching query" && args != null && args.Length >= 2)
            {
                int count = (int)args[0];
                string query = args[1].ToString();
                
                switch (lang)
                {
                    case "RU":
                    case "Russian":
                        return GetRussianPluralized("фильтр", count) + " найдено для '" + query + "'";
                    default:
                        return GetEnglishPluralized("filter", count) + " found matching '" + query + "'";
                }
            }

            switch (lang)
            {
                case "RU":
                case "Russian":
                    translation = GetRussian(id);
                    break;

                case "JP":
                case "Japanese":
                    translation = GetJapanese(id);
                    break;

                case "CN":
                case "Chinese":
                    translation = GetChinese(id);
                    break;

                case "KR":
                case "한국어":
                    translation = GetKorean(id);
                    break;

                default:
                    translation = GetEnglish(id);
                    break;
            }
            if (args != null && args.Length > 0)
            {
                try
                {
                    return string.Format(translation, args);
                }
                catch (FormatException)
                {
                    return translation;
                }
            }

            return translation;
        }

        private static string GetPluralized(string word, int count)
        {
            if (count == 1)
                return $"{count} {word}";
            else
                return $"{count} {word}s";
        }

        private static string GetEnglishPluralized(string word, int count)
        {
            if (count == 1)
                return $"{count} {word}";
            else
                return $"{count} {word}s";
        }

        private static string GetEnglish(string id)
        {
            switch (id)
            {
                // Контекстные меню предметов
                case "Autopickup Filter": return "Autopickup Filter";
                case "This Chest's Filter": return "This Chest's Filter";
                case "Add '{0}' to whitelist": return "Add '{0}' to whitelist";
                case "Add '{0}' to blacklist": return "Add '{0}' to blacklist";
                case "Remove '{0}' from whitelist": return "Remove '{0}' from whitelist";
                case "Remove '{0}' from blacklist": return "Remove '{0}' from blacklist";
                case "Add {0} '{1}' to whitelist": return "Add {0} '{1}' to whitelist";
                case "Add {0} '{1}' to blacklist": return "Add {0} '{1}' to blacklist";
                case "Remove {0} '{1}' from filter": return "Remove {0} '{1}' from filter";

                // Окно управления фильтрами (FilterManagerWindow)
                case "Container Filters": return "Container Filters";
                case "Open": return "Open";
                case "Autopickup Filter Settings": return "Autopickup Filter Settings";
                case "Whitelist": return "Whitelist";
                case "Blacklist": return "Blacklist";
                case "All": return "All";
                case "Invalid filter format!": return "Invalid filter format!";
                case "No filters found.": return "No filters found.";
                case "Item: {0}": return "Item: {0}";
                case "Type: {0}": return "Type: {0}";
                case "Category: {0}": return "Category: {0}";
                case "Rarity: {0}": return "Rarity: {0}";
                case "State: {0}": return "State: {0}";
                case "Material: {0}": return "Material: {0}";
                case "Stat: {0} > {1}": return "Stat: {0} > {1}";
                case "Stat: {0} < {1}": return "Stat: {0} < {1}";
                case "Value: > {0} gp": return "Value: > {0} gp";
                case "Value: < {0} gp": return "Value: < {0} gp";
                case "Food Quality: > {0}": return "Food Quality: > {0}";
                case "Food Quality: < {0}": return "Food Quality: < {0}";
                case "Hardness: > {0}": return "Hardness: > {0}";
                case "Nutrition: > {0}": return "Nutrition: > {0}";
                case "Nutrition: < {0}": return "Nutrition: < {0}";
                case "Food Stat Greater Than": return "Food Stat: {0} > {1}";
                case "Food Stat Less Than": return "Food Stat: {0} < {1}";
                case "Potential Greater Than": return "Potential: {0} > {1}";
                case "Potential Less Than": return "Potential: {0} < {1}";


                // Новые фильтры по характеристикам
                case "Stat-based Whitelist": return "Stat-based Whitelist";
                case "Base Value...": return "Base Value...";
                case "Filter by Base Value": return "Filter by Base Value";
                case "Base value filter set to: > {0} gp": return "Base value filter set to: > {0} gp";
                case "Base value filter disabled.": return "Base value filter disabled.";
                case "Filter for {0} set to: > {1}": return "Filter for {0} set to: > {1}";
                case "Filter for {0} disabled.": return "Filter for {0} disabled.";
                case "Invalid number format!": return "Invalid number format!";
                case "Add Food Quality > to Whitelist": return "Add Food Quality > to Whitelist...";
                case "Add Food Quality < to Blacklist": return "Add Food Quality < to Blacklist...";
                case "Whitelist by Food Quality": return "Whitelist by Food Quality";
                case "Blacklist by Food Quality": return "Blacklist by Food Quality";
                case "Enter minimum quality": return "Whitelist items with quality greater than or equal to:";
                case "Enter maximum quality": return "Blacklist items with quality less than:";

                // Категории характеристик
                case "Main Attributes": return "Main Attributes";
                case "Combat Stats": return "Combat Stats";
                case "Resistances": return "Resistances";
                case "Combat and Weapon Skills": return "Combat and Weapon Skills";
                case "Magic Skills": return "Magic Skills";
                case "Peaceful Skills 1": return "Peaceful Skills 1";
                case "Peaceful Skills 2": return "Peaceful Skills 2";
                case "Melee Modifiers": return "Melee Modifiers";
                case "Ranged Modifiers": return "Ranged Modifiers";
                case "Sustain Attributes": return "Sustain Attributes";
                case "Status Effect Negation": return "Status Effect Negation";
                case "Bane (Slayer) Effects": return "Bane (Slayer) Effects";
                case "Elemental Conversion": return "Elemental Conversion";
                case "Elemental Damage": return "Elemental Damage";
                case "Utility & Exploration": return "Utility & Exploration";
                case "Special Abilities": return "Special Abilities";
                case "Miscellaneous Enchantments": return "Miscellaneous Enchantments";
                case "Vital Stats": return "Vital Stats";
                case "Combat Passives": return "Combat Passives";
                case "Material Proof": return "Material Proof";

                // Меню настроек
                case "Enable Mod": return "Enable Mod";
                case "Disable Mod": return "Disable Mod";
                case "Whitelist Highlight": return "Whitelist Highlight";
                case "Enable highlight": return "Enable highlight";
                case "Disable highlight": return "Disable highlight";
                case "Change to Aura effect": return "Change to Aura effect";
                case "Change to Light effect": return "Change to Light effect";
                case "Enable sound": return "Enable sound";
                case "Disable sound": return "Disable sound";
                case "Use default sound": return "Use default sound";
                case "Use custom sound": return "Use custom sound";
                case "Use custom sound with note": return "Use custom sound (Place 'drop.mp3' in mod folder)";
                case "Place 'drop.mp3' in the mod folder.": return "Place 'drop.mp3' in the mod folder.";
                case "Sound Volume...": return "Sound Volume...";
                case "Volume": return "Volume";
                case "Font Size": return "Font Size";
                case "Copy Filters": return "Copy Filters";
                case "Paste Filters": return "Paste Filters";
                case "World Highlight": return "Highlight an item on the ground";
                case "Inventory Outline": return "Outline of an item in the inventory";
                case "Enabled": return "Enabled";
                case "Disabled": return "Disabled";

                // Новые ключи для иерархического меню
                case "filter_menu_food": return "By Food...";
                case "filter_menu_food_properties": return "By Property";
                case "filter_menu_food_direct_stats": return "By Direct Stat";
                case "filter_menu_properties": return "By Property (General)";
                case "filter_menu_potential": return "By Potential Stat";
                case "filter_menu_category": return "By Category";
                case "filter_menu_rarity": return "By Rarity";
                case "filter_menu_state": return "By State";
                case "filter_menu_material": return "By Material";
                case "add_to_whitelist": return "Add to Whitelist";
                case "add_to_blacklist": return "Add to Blacklist";
                case "filter_prop_hardness_gt": return "Hardness >...";
                case "filter_prop_nutrition_gt": return "Nutrition >...";
                case "filter_prop_rotten": return "Is Rotten";
                case "filter_prop_human_flesh": return "Is Human Flesh";
                case "filter_prop_undead_flesh": return "Is Undead Flesh";
                case "filter_prop_cat_meat": return "Is Cat Meat";
                case "filter_prop_bug_meat": return "Is Bug Meat";
                case "filter_prop_raw_fish": return "Is Raw Fish";
                case "filter_prop_aphrodisiac": return "Is Aphrodisiac";
                case "filter_prop_blessed": return "Is Blessed";
                case "filter_prop_cursed": return "Is Cursed";
                case "filter_prop_godly": return "Is Godly";
                case "filter_prop_doomed": return "Is Doomed";
                case "filter_prop_equipped": return "Is Equipped";
                case "filter_prop_unequippable": return "Cannot Equip";
                case "filter_prop_identify": return "Is Identified";
                case "filter_prop_unidentified": return "Is Unidentified";
                case "filter_prop_precious": return "Is Precious";
                case "filter_prop_stolen": return "Is Stolen";

                // Ключи для правил
                case "type": return "type";
                case "category": return "category";
                case "rarity": return "rarity";
                case "state": return "state";
                case "material": return "material";
                case "property": return "property";
                case "property_display": return "Property: {0}";
                case "sort_button_default": return "Sorting";
                case "sort_menu_default": return "Default";
                case "sort_menu_properties": return "Properties";
                case "sort_menu_stats": return "Stats";
                case "sort_menu_potency": return "Stats Potency";
                case "sort_stat_hardness": return "Hardness";
                case "sort_suffix_potential": return " (Pot.)";

                // Global search functionality
                case "Global Search...": return "Global Search...";
                case "Search all filters...": return "Search all filters...";

                // Super category translations
                case "cat_super_armor": return "Armor";
                case "cat_super_weapon": return "Weapon";
                case "cat_super_build": return "Build";
                case "cat_super_consumable": return "Consumable";
                case "cat_super_tool": return "Tool";
                case "cat_super_resource": return "Resource";
                case "cat_super_misc": return "Misc";
                case "Search filters (type to search):": return "Search filters (type to search):";
                case "Search Results": return "Search Results";
                case "No matching filters found.": return "No matching filters found.";
                case "Search...": return "Search...";

                // Filter existence indication
                case "Remove from Whitelist": return "Remove from Whitelist";
                case "Remove from Blacklist": return "Remove from Blacklist";

                // Enhanced UI elements
                case "Back": return "Back";
                case "Navigation": return "Navigation";
                case "Equipment": return "Equipment";
                case "Food": return "Food";
                case "Food, Potential": return "Food, Potential";
                case "Potential": return "Potential";
                case "Choose action for": return "Choose action for";
                case "Enter value (0 to disable)": return "Enter value (0 to disable)";
                case "Choose a filter type to add:": return "Choose a filter type to add:";
                case "Found {0} filter{1} matching '{2}'": return "Found {0} filter{1} matching '{2}'";
                case "... and {0} more results. Refine your search for better results.": return "... and {0} more results. Refine your search for better results.";

                // Filter addition messages
                case "Added '{0}' to whitelist": return "Added '{0}' to whitelist";
                case "Added '{0}' to blacklist": return "Added '{0}' to blacklist";

                // Filter menu descriptions
                case "Weapons, Armor, Tools...": return "Weapons, Armor, Tools...";
                case "Wood, Metal, Stone...": return "Wood, Metal, Stone...";
                case "Common, Rare, Epic...": return "Common, Rare, Epic...";
                case "Blessed, Cursed, Normal...": return "Blessed, Cursed, Normal...";
                case "Food-specific filters...": return "Food-specific filters...";
                case "Item properties...": return "Item properties...";

                // Category groupings
                case "Super Categories": return "Super Categories";
                case "Other Categories": return "Other Categories";
                case "Items by Type": return "Items by Type";
                case "Materials - {0}": return "Materials - {0}";
                case "Item Rarities": return "Item Rarities";
                case "Item States": return "Item States";
                case "Stats - {0}": return "Stats - {0}";
                case "Food Stats": return "Food Stats";
                case "Potential Stats": return "Potential Stats";
                case "Food Properties": return "Food Properties";
                case "Item Properties": return "Item Properties";
                case "Other": return "Other";
                case "Specific Item": return "Specific Item";
                // Mod status warning
                case "Mod is turned off": return "Mod is turned off";

                default: return id; // Возвращаем ID, если перевод не найден
            }
        }

        private static string GetRussian(string id)
        {
            switch (id)
            {
                // Контекстные меню предметов
                case "Autopickup Filter": return "Фильтр автоподбора";
                case "This Chest's Filter": return "Фильтр этого сундука";
                case "Add '{0}' to whitelist": return "Добавить '{0}' в белый список";
                case "Add '{0}' to blacklist": return "Добавить '{0}' в черный список";
                case "Remove '{0}' from whitelist": return "Убрать '{0}' из белого списка";
                case "Remove '{0}' from blacklist": return "Убрать '{0}' из черного списка";
                case "Add {0} '{1}' to whitelist": return "Добавить {0} '{1}' в белый список";
                case "Add {0} '{1}' to blacklist": return "Добавить {0} '{1}' в черный список";
                case "Remove {0} '{1}' from filter": return "Убрать {0} '{1}' из фильтра";

                // Окно управления фильтрами (FilterManagerWindow)
                case "Container Filters": return "Фильтры контейнера";
                case "Open": return "Открыть";
                case "Autopickup Filter Settings": return "Настройки фильтра автоподбора";
                case "Whitelist": return "Белый список";
                case "Blacklist": return "Черный список";
                case "All": return "Все";
                case "Invalid filter format!": return "Неверный формат фильтра!";
                case "No filters found.": return "Фильтры не найдены.";
                case "Item: {0}": return "Предмет: {0}";
                case "Type: {0}": return "Тип: {0}";
                case "Category: {0}": return "Категория: {0}";
                case "Rarity: {0}": return "Редкость: {0}";
                case "State: {0}": return "Состояние: {0}";
                case "Material: {0}": return "Материал: {0}";
                case "Stat: {0} > {1}": return "Стат: {0} > {1}";
                case "Stat: {0} < {1}": return "Стат: {0} < {1}";
                case "Value: > {0} gp": return "Ценность: > {0} gp";
                case "Value: < {0} gp": return "Ценность: < {0} gp";
                case "Food Quality: > {0}": return "Качество еды: > {0}";
                case "Food Quality: < {0}": return "Качество еды: < {0}";
                case "Hardness: > {0}": return "Твёрдость: > {0}";
                case "Nutrition: > {0}": return "Питательность: > {0}";
                case "Nutrition: < {0}": return "Питательность: < {0}";


                // Новые фильтры по характеристикам
                case "Stat-based Whitelist": return "Белый список по характеристикам";
                case "Base Value...": return "Базовая стоимость...";
                case "Filter by Base Value": return "Фильтр по базовой стоимости";
                case "Base value filter set to: > {0} gp": return "Фильтр стоимости установлен: > {0} gp";
                case "Base value filter disabled.": return "Фильтр по стоимости отключен.";
                case "Filter for {0} set to: > {1}": return "Фильтр для {0} установлен: > {1}";
                case "Filter for {0} disabled.": return "Фильтр для {0} отключен.";
                case "Invalid number format!": return "Неверный формат числа!";
                case "Add Food Quality > to Whitelist": return "Добавить качество > в белый список...";
                case "Add Food Quality < to Blacklist": return "Добавить качество < в черный список...";
                case "Whitelist by Food Quality": return "Белый список по качеству еды";
                case "Blacklist by Food Quality": return "Черный список по качеству еды";
                case "Enter minimum quality": return "В белый список предметы с качеством не ниже:";
                case "Enter maximum quality": return "В черный список предметы с качеством ниже чем:";


                // Категории характеристик
                case "Main Attributes": return "Основные атрибуты";
                case "Combat Stats": return "Боевые характеристики";
                case "Resistances": return "Сопротивления";
                case "Combat and Weapon Skills": return "Боевые и оружейные навыки";
                case "Magic Skills": return "Магические навыки";
                case "Peaceful Skills 1": return "Мирные навыки 1";
                case "Peaceful Skills 2": return "Мирные навыки 2";
                case "Melee Modifiers": return "Модификаторы ближнего боя";
                case "Ranged Modifiers": return "Модификаторы дальнего боя";
                case "Sustain Attributes": return "Поддержание атрибутов";
                case "Status Effect Negation": return "Игнорирование статусов";
                case "Bane (Slayer) Effects": return "Против существ (Убийца)";
                case "Elemental Conversion": return "Конверсия стихий";
                case "Elemental Damage": return "Стихийный урон";
                case "Utility & Exploration": return "Полезные и исследовательские";
                case "Special Abilities": return "Особые способности";
                case "Miscellaneous Enchantments": return "Разные зачарования";
                case "Vital Stats": return "Жизненные показатели";
                case "Combat Passives": return "Боевые пассивные навыки";
                case "Material Proof": return "Защита материала";
                case "Faith & Divine": return "Вера и Божественное";

                // Меню настроек
                case "Enable Mod": return "Включить мод";
                case "Disable Mod": return "Отключить мод";
                case "Whitelist Highlight": return "Подсветка белого списка";
                case "Enable highlight": return "Включить подсветку";
                case "Disable highlight": return "Выключить подсветку";
                case "Change to Aura effect": return "Сменить эффект на Ауру";
                case "Change to Light effect": return "Сменить эффект на Свет";
                case "Enable sound": return "Включить звук";
                case "Disable sound": return "Выключить звук";
                case "Use default sound": return "Использовать звук по умолчанию";
                case "Use custom sound": return "Использовать кастомный звук";
                case "Use custom sound with note": return "Использовать кастомный звук (Поместите 'drop.mp3' в папку с модом)";
                case "Place 'drop.mp3' in the mod folder.": return "Поместите файл 'drop.mp3' в папку с модом.";
                case "Sound Volume...": return "Громкость звука...";
                case "Volume": return "Громкость";
                case "Font Size": return "Размер шрифта";
                case "Copy Filters": return "Копировать фильтры";
                case "Paste Filters": return "Вставить фильтры";
                case "World Highlight": return "Подсветка предмета на земле";
                case "Inventory Outline": return "Обводка предмета в инвентаре";
                case "Enabled": return "Включено";
                case "Disabled": return "Выключено";

                // Новые ключи для иерархического меню
                case "filter_menu_food": return "По еде...";
                case "filter_menu_food_properties": return "По свойству";
                case "filter_menu_food_direct_stats": return "По прямой характеристике";
                case "filter_menu_properties": return "По свойству (Общее)";
                case "filter_menu_potential": return "По потенциалу";
                case "filter_menu_category": return "По категории";
                case "filter_menu_rarity": return "По редкости";
                case "filter_menu_state": return "По состоянию";
                case "filter_menu_material": return "По материалу";
                case "add_to_whitelist": return "В белый список";
                case "add_to_blacklist": return "В черный список";
                case "filter_prop_hardness_gt": return "Твёрдость >...";
                case "filter_prop_nutrition_gt": return "Питательность >...";
                case "filter_prop_rotten": return "Гнилой";
                case "filter_prop_human_flesh": return "Человеческая плоть";
                case "filter_prop_undead_flesh": return "Плоть нежити";
                case "filter_prop_cat_meat": return "Кошачье мясо";
                case "filter_prop_bug_meat": return "Мясо жука";
                case "filter_prop_raw_fish": return "Сырая рыба";
                case "filter_prop_aphrodisiac": return "Афродизиак";
                case "filter_prop_blessed": return "Благословлённый";
                case "filter_prop_cursed": return "Проклятый";
                case "filter_prop_godly": return "Божественный";
                case "filter_prop_doomed": return "Обречённый";
                case "filter_prop_equipped": return "Экипирован";
                case "filter_prop_unequippable": return "Нельзя экипировать";
                case "filter_prop_identify": return "Опознан";
                case "filter_prop_unidentified": return "Не опознан";
                case "filter_prop_precious": return "Драгоценный";
                case "filter_prop_stolen": return "Краденый";

                // Ключи для правил
                case "type": return "тип";
                case "category": return "категорию";
                case "rarity": return "редкость";
                case "state": return "состояние";
                case "material": return "материал";
                case "property": return "свойство";
                case "property_display": return "Свойство: {0}";
                case "sort_button_default": return "Сортировка";
                case "sort_menu_default": return "По умолчанию";
                case "sort_menu_properties": return "Свойства";
                case "sort_menu_stats": return "Статы";
                case "sort_menu_potency": return "Потенциал характеристик";
                case "sort_stat_hardness": return "Твёрдость";
                case "sort_suffix_potential": return " (Потенциал)";

                // Global search functionality
                case "Global Search...": return "Глобальный поиск...";
                case "Search all filters...": return "Поиск по всем фильтрам...";

                // Super category translations
                case "cat_super_armor": return "Броня";
                case "cat_super_weapon": return "Оружие";
                case "cat_super_build": return "Строительство";
                case "cat_super_consumable": return "Расходники";
                case "cat_super_tool": return "Инструменты";
                case "cat_super_resource": return "Ресурсы";
                case "cat_super_misc": return "Разное";
                case "Search filters (type to search):": return "Поиск фильтров (введите для поиска):";
                case "Search Results": return "Результаты поиска";
                case "No matching filters found.": return "Подходящие фильтры не найдены.";
                case "Search...": return "Поиск...";

                // Filter existence indication
                case "Remove from Whitelist": return "Убрать из белого списка";
                case "Remove from Blacklist": return "Убрать из черного списка";

                // Enhanced UI elements
                case "Back": return "Назад";
                case "Navigation": return "Навигация";
                case "Equipment": return "Экипировка";
                case "Food": return "Еда";
                case "Food, Potential": return "Еда, Потенциал";
                case "Potential": return "Потенциал";
                case "Choose action for": return "Выберите действие для";
                case "Enter value (0 to disable)": return "Введите значение (0 для отключения)";
                case "Choose a filter type to add:": return "Выберите тип фильтра для добавления:";
                case "Found {0} filter{1} matching '{2}'": return "Найдено {0} фильтр{1}, соответствующих '{2}'";
                case "Food Stat Greater Than": return "Характеристика еды: {0} > {1}";
                case "Food Stat Less Than": return "Характеристика еды: {0} < {1}";
                case "Potential Greater Than": return "Потенциал: {0} > {1}";
                case "Potential Less Than": return "Потенциал: {0} < {1}";
                case "... and {0} more results. Refine your search for better results.": return "... и ещё {0} результатов. Уточните поиск для лучших результатов.";

                // Filter addition messages
                case "Added '{0}' to whitelist": return "Добавлено '{0}' в белый список";
                case "Added '{0}' to blacklist": return "Добавлено '{0}' в чёрный список";

                // Filter menu descriptions
                case "Weapons, Armor, Tools...": return "Оружие, Броня, Инструменты...";
                case "Wood, Metal, Stone...": return "Дерево, Металл, Камень...";
                case "Common, Rare, Epic...": return "Обычное, Редкое, Эпическое...";
                case "Blessed, Cursed, Normal...": return "Благословенное, Проклятое, Обычное...";
                case "Food-specific filters...": return "Фильтры для еды...";
                case "Item properties...": return "Свойства предметов...";

                // Category groupings
                case "Super Categories": return "Основные Категории";
                case "Other Categories": return "Другие Категории";
                case "Items by Type": return "Предметы по Типу";
                case "Materials - {0}": return "Материалы - {0}";
                case "Item Rarities": return "Редкость Предметов";
                case "Item States": return "Состояния Предметов";
                case "Stats - {0}": return "Статы - {0}";
                case "Food Stats": return "Статы Еды";
                case "Potential Stats": return "Статы Потенциала";
                case "Food Properties": return "Свойства Еды";
                case "Item Properties": return "Свойства Предметов";
                case "Other": return "Другое";
                case "Specific Item": return "Конкретный Предмет";
                // Mod status warning
                case "Mod is turned off": return "Мод выключен";

                default: return GetEnglish(id); // Возвращаем английский, если перевод не найден
            }
        }

        private static string GetRussianPluralized(string word, int count)
        {
            // Russian pluralization rules for "фильтр"
            if (word == "фильтр")
            {
                if (count % 10 == 1 && count % 100 != 11)
                    return $"{count} фильтр";
                else if ((count % 10 >= 2 && count % 10 <= 4) && (count % 100 < 10 || count % 100 >= 20))
                    return $"{count} фильтра";
                else
                    return $"{count} фильтров";
            }
            return $"{count} {word}";
        }

        private static string GetJapanese(string id)
        {
            switch (id)
            {
                // Контекстные меню предметов
                case "Autopickup Filter": return "自動取得フィルター";
                case "This Chest's Filter": return "このチェストのフィルター";
                case "Add '{0}' to whitelist": return "'{0}' をホワイトリストに追加";
                case "Add '{0}' to blacklist": return "'{0}' をブラックリストに追加";
                case "Remove '{0}' from whitelist": return "'{0}' をホワイトリストから削除";
                case "Remove '{0}' from blacklist": return "'{0}' をブラックリストから削除";
                case "Add {0} '{1}' to whitelist": return "{0} '{1}' をホワイトリストに追加";
                case "Add {0} '{1}' to blacklist": return "{0} '{1}' をブラックリストに追加";
                case "Remove {0} '{1}' from filter": return "{0} '{1}' をフィルターから削除";

                // Окно управления фильтрами (FilterManagerWindow)
                case "Container Filters": return "コンテナフィルター";
                case "Open": return "開く";
                case "Autopickup Filter Settings": return "自動取得フィルター設定";
                case "Whitelist": return "ホワイトリスト";
                case "Blacklist": return "ブラックリスト";
                case "All": return "すべて";
                case "Invalid filter format!": return "無効なフィルター形式です！";
                case "No filters found.": return "フィルターが見つかりません。";
                case "Item: {0}": return "アイテム: {0}";
                case "Type: {0}": return "タイプ: {0}";
                case "Category: {0}": return "カテゴリ: {0}";
                case "Rarity: {0}": return "レア度: {0}";
                case "State: {0}": return "状態: {0}";
                case "Material: {0}": return "素材: {0}";
                case "Stat: {0} > {1}": return "ステータス: {0} > {1}";
                case "Stat: {0} < {1}": return "ステータス: {0} < {1}";
                case "Value: > {0} gp": return "価値: > {0} gp";
                case "Value: < {0} gp": return "価値: < {0} gp";
                case "Food Quality: > {0}": return "食品品質: > {0}";
                case "Food Quality: < {0}": return "食品品質: < {0}";
                case "Nutrition: > {0}": return "栄養価: > {0}";
                case "Nutrition: < {0}": return "栄養価: < {0}";


                // 新しいフィルター по характеристикам
                case "Stat-based Whitelist": return "ステータス基準ホワイトリスト";
                case "Base Value...": return "基本価値...";
                case "Filter by Base Value": return "基本価値でフィルター";
                case "Base value filter set to: > {0} gp": return "基本価値フィルター設定: > {0} gp";
                case "Base value filter disabled.": return "基本価値フィルターは無効です。";
                case "Filter for {0} set to: > {1}": return "{0}のフィルター設定: > {1}";
                case "Filter for {0} disabled.": return "{0}のフィルターは無効です。";
                case "Invalid number format!": return "無効な数値形式です！";
                case "Add Food Quality > to Whitelist": return "食品品質 > をホワイトリストに追加...";
                case "Add Food Quality < to Blacklist": return "食品品質 < をブラックリストに追加...";
                case "Whitelist by Food Quality": return "食品品質でホワイトリスト";
                case "Blacklist by Food Quality": return "食品品質でブラックリスト";
                case "Enter minimum quality": return "ホワイトリストに追加する最小品質を入力してください:";
                case "Enter maximum quality": return "ブラックリストに追加する最大品質を入力してください:";

                // キャトегоリーの特性
                case "Main Attributes": return "主要能力値";
                case "Combat Stats": return "戦闘能力値";
                case "Resistances": return "耐性";
                case "Combat and Weapon Skills": return "戦闘・武器スキル";
                case "Magic Skills": return "魔法スキル";
                case "Peaceful Skills 1": return "平和スキル 1";
                case "Peaceful Skills 2": return "平和スキル 2";
                case "Melee Modifiers": return "近接戦闘補正";
                case "Ranged Modifiers": return "遠距離戦闘補正";
                case "Sustain Attributes": return "能力維持";
                case "Status Effect Negation": return "状態異常無効化";
                case "Bane (Slayer) Effects": return "特攻効果（スレイヤー）";
                case "Elemental Conversion": return "属性変換";
                case "Elemental Damage": return "属性ダメージ";
                case "Utility & Exploration": return "ユーティリティ・探索";
                case "Special Abilities": return "特殊能力";
                case "Miscellaneous Enchantments": return "その他のエンチャント";
                case "Vital Stats": return "生命力ステータス";
                case "Combat Passives": return "戦闘パッシブ";
                case "Material Proof": return "素材耐性";
                case "Faith & Divine": return "信仰と神聖";

                // 設定メニュー
                case "Enable Mod": return "MODを有効化";
                case "Disable Mod": return "MODを無効化";
                case "Whitelist Highlight": return "ホワイトリストのハイライト";
                case "Enable highlight": return "ハイライトを有効化";
                case "Disable highlight": return "ハイライトを無効化";
                case "Change to Aura effect": return "オーラエフェクトに変更";
                case "Change to Light effect": return "ライトエフェクトに変更";
                case "Enable sound": return "サウンドを有効化";
                case "Disable sound": return "サウンドを無効化";
                case "Use default sound": return "デフォルトサウンドを使用";
                case "Use custom sound": return "カスタムサウンドを使用";
                case "Use custom sound with note": return "カスタムサウンドを使用 (modフォルダに'drop.mp3'を配置)";
                case "Place 'drop.mp3' in the mod folder.": return "modフォルダに'drop.mp3'ファイルを配置してください。";
                case "Sound Volume...": return "音量...";
                case "Volume": return "音量";
                case "Font Size": return "フォントサイズ";
                case "Copy Filters": return "フィルターをコピー";
                case "Paste Filters": return "フィルターを貼り付け";
                case "World Highlight": return "地面のアイテムをハイライトする";
                case "Inventory Outline": return "インベントリのアイテムを枠表示する";
                case "Enabled": return "有効";
                case "Disabled": return "無効";

                // 新しい階層メニューのキー
                case "filter_menu_food": return "食品...";
                case "filter_menu_food_properties": return "特性";
                case "filter_menu_food_direct_stats": return "直接能力値";
                case "filter_menu_properties": return "特性 (一般)";
                case "filter_menu_potential": return "潜在能力";
                case "filter_menu_category": return "カテゴリ";
                case "filter_menu_rarity": return "レア度";
                case "filter_menu_state": return "状態";
                case "filter_menu_material": return "素材";
                case "add_to_whitelist": return "ホワイトリストに追加";
                case "add_to_blacklist": return "ブラックリストに追加";
                case "filter_prop_hardness_gt": return "硬度 >...";
                case "filter_prop_nutrition_gt": return "栄養価 >...";
                case "filter_prop_rotten": return "腐っている";
                case "filter_prop_human_flesh": return "人肉";
                case "filter_prop_undead_flesh": return "アンデッドの肉";
                case "filter_prop_cat_meat": return "猫の肉";
                case "filter_prop_bug_meat": return "虫の肉";
                case "filter_prop_raw_fish": return "生の魚";
                case "filter_prop_aphrodisiac": return "媚薬";
                case "filter_prop_blessed": return "祝福された";
                case "filter_prop_cursed": return "詛われた";
                case "filter_prop_godly": return "神聖な";
                case "filter_prop_doomed": return "絶望の";
                case "filter_prop_equipped": return "装備中";
                case "filter_prop_unequippable": return "装備不可";
                case "filter_prop_identify": return "鑑定済み";
                case "filter_prop_unidentified": return "未鑑定";
                case "filter_prop_precious": return "貴重な";
                case "filter_prop_stolen": return "盗まれた";
                case "Hardness: > {0}": return "硬度: > {0}";
                case "Potential {0}: > {1}": return "潜在能力: {0} > {1}";
                case "Potential {0}: < {1}": return "潜在能力: {0} < {1}";
                case "Food Stat: {0} > {1}": return "食品能力値: {0} > {1}";
                case "Food Stat: {0} < {1}": return "食品能力値: {0} < {1}";

                // ルールのキー
                case "type": return "タイプ";
                case "category": return "カテゴリ";
                case "rarity": return "レア度";
                case "state": return "状態";
                case "material": return "素材";
                case "property": return "特性";
                case "property_display": return "特性: {0}";
                case "sort_button_default": return "ソート";
                case "sort_menu_default": return "デフォルト";
                case "sort_menu_properties": return "特性";
                case "sort_menu_stats": return "能力値";
                case "sort_menu_potency": return "潜在能力";
                case "sort_stat_hardness": return "硬度";
                case "sort_suffix_potential": return " (潜在)";

                // グローバル検색機能
                case "Global Search...": return "グローバル検索...";
                case "Search all filters...": return "すべてのフィルターを検색...";
                case "Search filters (type to search):": return "フィルターを検색（入力して検索）：";
                case "Search Results": return "検색結果";
                case "No matching filters found.": return "一致するフィルターが見つかりません。";
                case "Search...": return "検색...";

                // Filter existence indication
                case "Remove from Whitelist": return "ホワイトリストから削除";
                case "Remove from Blacklist": return "ブラックリストから削除";

                // 強화されたUI要素
                case "Back": return "戻る";
                case "Navigation": return "ナビゲーション";
                case "Equipment": return "装備";
                case "Food": return "食品";
                case "Food, Potential": return "食品、潜在能力";
                case "Potential": return "潜在能力";

                // Super category translations
                case "cat_super_armor": return "アーマー";
                case "cat_super_weapon": return "武器";
                case "cat_super_build": return "建築";
                case "cat_super_consumable": return "消耗品";
                case "cat_super_tool": return "ツール";
                case "cat_super_resource": return "資源";
                case "cat_super_misc": return "その他";

                // Filter addition messages
                case "Added '{0}' to whitelist": return "ホワイトリストに'{0}'を追加";
                case "Added '{0}' to blacklist": return "ブラックリストに'{0}'を追加";

                // Category groupings
                case "Super Categories": return "スーパーカテゴリ";
                case "Other Categories": return "その他のカテゴリ";
                case "Items by Type": return "タイプ別アイテム";
                case "Materials - {0}": return "材料 - {0}";
                case "Item Rarities": return "アイテムレア度";
                case "Item States": return "アイテム状態";
                case "Stats - {0}": return "能力値 - {0}";
                case "Food Stats": return "食品能力値";
                case "Potential Stats": return "潜在能力값";
                case "Food Properties": return "食品特性";
                case "Item Properties": return "アイテム特性";
                case "Other": return "その他";
                case "Specific Item": return "特定アイテム";
                // Mod status warning
                case "Mod is turned off": return "MODがオフになっています";

                case "Found {0} filter{1} matching '{2}'": return "{2}に一致する{0}個のフィルター{1}が見つかりました";
                case "Food Stat Greater Than": return "食品能力値: {0} > {1}";
                case "Food Stat Less Than": return "食品能力値: {0} < {1}";
                case "Potential Greater Than": return "潜在能力: {0} > {1}";
                case "Potential Less Than": return "潜在能力: {0} < {1}";
                case "... and {0} more results. Refine your search for better results.": return "... また他に{0}個の結果があります。より良い結果を得るために検색を絞り込んでください。";

                default: return GetEnglish(id);
            }
        }

        private static string GetChinese(string id)
        {
            switch (id)
            {
                // Контекстные меню предметов
                case "Autopickup Filter": return "自动拾取过滤器";
                case "This Chest's Filter": return "此箱子的过滤器";
                case "Add '{0}' to whitelist": return "将“{0}”添加到白名单";
                case "Add '{0}' to blacklist": return "将“{0}”添加到黑名单";
                case "Remove '{0}' from whitelist": return "从白名单中移除“{0}”";
                case "Remove '{0}' from blacklist": return "从黑名单中移除“{0}”";
                case "Add {0} '{1}' to whitelist": return "将{0}“{1}”添加到白名单";
                case "Add {0} '{1}' to blacklist": return "将{0}“{1}”添加到黑名单";
                case "Remove {0} '{1}' from filter": return "从过滤器中移除{0}“{1}”";

                // Окно управления фильтрами (FilterManagerWindow)
                case "Container Filters": return "容器过滤器";
                case "Open": return "打开";
                case "Autopickup Filter Settings": return "自动拾取过滤器设置";
                case "Whitelist": return "白名单";
                case "Blacklist": return "黑名单";
                case "All": return "全部";
                case "Invalid filter format!": return "过滤器格式无效！";
                case "No filters found.": return "未找到过滤器。";
                case "Item: {0}": return "物品: {0}";
                case "Type: {0}": return "类型: {0}";
                case "Category: {0}": return "类别: {0}";
                case "Rarity: {0}": return "稀有度: {0}";
                case "State: {0}": return "状态: {0}";
                case "Material: {0}": return "材质: {0}";
                case "Stat: {0} > {1}": return "属性: {0} > {1}";
                case "Stat: {0} < {1}": return "属性: {0} < {1}";
                case "Value: > {0} gp": return "价值: > {0} gp";
                case "Value: < {0} gp": return "价值: < {0} gp";
                case "Food Quality: > {0}": return "食物品质: > {0}";
                case "Food Quality: < {0}": return "食物品质: < {0}";
                case "Nutrition: > {0}": return "营养: > {0}";
                case "Nutrition: < {0}": return "营养: < {0}";

                // Новые фильтры по характеристикам
                case "Stat-based Whitelist": return "基于属性的白名单";
                case "Base Value...": return "基础价值...";
                case "Filter by Base Value": return "按基础价值筛选";
                case "Base value filter set to: > {0} gp": return "基础价值过滤器已设为: > {0} gp";
                case "Base value filter disabled.": return "基础价值过滤器已禁用。";
                case "Filter for {0} set to: > {1}": return "{0} 的过滤器已设为: > {1}";
                case "Filter for {0} disabled.": return "{0} 的过滤器已禁用。";
                case "Invalid number format!": return "数字格式无效！";
                case "Add Food Quality > to Whitelist": return "将食物品质 > 添加到白名单...";
                case "Add Food Quality < to Blacklist": return "将食物品质 < 添加到黑名单...";
                case "Whitelist by Food Quality": return "按食物品质加入白名单";
                case "Blacklist by Food Quality": return "食物品质黑名单";
                case "Enter minimum quality": return "输入白名单的最低品質:";
                case "Enter maximum quality": return "输入黑名单的最高品質:";

                // Категории характеристик
                case "Main Attributes": return "主要属性";
                case "Combat Stats": return "战斗属性";
                case "Resistances": return "抗性";
                case "Combat and Weapon Skills": return "战斗与武器技能";
                case "Magic Skills": return "魔法技能";
                case "Peaceful Skills 1": return "生活技能 1";
                case "Peaceful Skills 2": return "生活技能 2";
                case "Melee Modifiers": return "近战修正";
                case "Ranged Modifiers": return "远程修正";
                case "Sustain Attributes": return "属性维持";
                case "Status Effect Negation": return "状态效果无效";
                case "Bane (Slayer) Effects": return "屠戮效果";
                case "Elemental Conversion": return "元素转换";
                case "Elemental Damage": return "元素伤害";
                case "Utility & Exploration": return "通用与探索";
                case "Special Abilities": return "特殊能力";
                case "Miscellaneous Enchantments": return "杂项附魔";
                case "Vital Stats": return "生命属性";
                case "Combat Passives": return "战斗被动";
                case "Material Proof": return "材料抗性";
                case "Faith & Divine": return "信仰与神圣";

                // Меню настроек
                case "Enable Mod": return "启用模组";
                case "Disable Mod": return "禁用模组";
                case "Whitelist Highlight": return "白名单高亮";
                case "Enable highlight": return "启用高亮";
                case "Disable highlight": return "禁用高亮";
                case "Change to Aura effect": return "切换为光环效果";
                case "Change to Light effect": return "切换为光效";
                case "Enable sound": return "启用声音";
                case "Disable sound": return "禁用声音";
                case "Use default sound": return "使用默认声音";
                case "Use custom sound": return "使用自定义声音";
                case "Use custom sound with note": return "使用自定义声音 (将'drop.mp3'放入模組文件夹)";
                case "Place 'drop.mp3' in the mod folder.": return "请将'drop.mp3'文件放入模組文件夹。";
                case "Sound Volume...": return "音量...";
                case "Volume": return "音량";
                case "Font Size": return "字体大小";
                case "Copy Filters": return "复制过滤器";
                case "Paste Filters": return "粘贴过滤器";
                case "World Highlight": return "高亮地面上的物品";
                case "Inventory Outline": return "勾勒物品栏中的物品";
                case "Enabled": return "已启用";
                case "Disabled": return "已禁用";

                // Новые ключи для иерархического меню
                case "filter_menu_food": return "按食物...";
                case "filter_menu_food_properties": return "按属性";
                case "filter_menu_food_direct_stats": return "按直接属性";
                case "filter_menu_properties": return "按属性 (通用)";
                case "filter_menu_potential": return "按潜力属性";
                case "filter_menu_category": return "按类别";
                case "filter_menu_rarity": return "按稀有度";
                case "filter_menu_state": return "按状态";
                case "filter_menu_material": return "按材质";
                case "add_to_whitelist": return "添加到白名单";
                case "add_to_blacklist": return "添加到黑名单";
                case "filter_prop_hardness_gt": return "硬度 >...";
                case "filter_prop_nutrition_gt": return "营养 >...";
                case "filter_prop_rotten": return "已腐烂";
                case "filter_prop_human_flesh": return "是人肉";
                case "filter_prop_undead_flesh": return "是不死族肉";
                case "filter_prop_cat_meat": return "是猫肉";
                case "filter_prop_bug_meat": return "是虫肉";
                case "filter_prop_raw_fish": return "是生鱼";
                case "filter_prop_aphrodisiac": return "是催情剂";
                case "filter_prop_blessed": return "已祝福";
                case "filter_prop_cursed": return "诅咒的";
                case "filter_prop_godly": return "神圣的";
                case "filter_prop_doomed": return "毁灭的";
                case "filter_prop_equipped": return "已装备";
                case "filter_prop_unequippable": return "无法装备";
                case "filter_prop_identify": return "已鉴定";
                case "filter_prop_unidentified": return "未鉴定";
                case "filter_prop_precious": return "珍贵的";
                case "filter_prop_stolen": return "被盗的";
                case "Hardness: > {0}": return "硬度: > {0}";
                case "Potential {0}: > {1}": return "潜力: {0} > {1}";
                case "Potential {0}: < {1}": return "潜力: {0} < {1}";
                case "Food Stat: {0} > {1}": return "食物属性: {0} > {1}";
                case "Food Stat: {0} < {1}": return "食物属性: {0} < {1}";

                // 规则键
                case "type": return "类型";
                case "category": return "类别";
                case "rarity": return "稀유도";
                case "state": return "状态";
                case "material": return "材质";
                case "property": return "属性";
                case "property_display": return "属性: {0}";
                case "sort_button_default": return "排序";
                case "sort_menu_default": return "默认";
                case "sort_menu_properties": return "属性";
                case "sort_menu_stats": return "属性값";
                case "sort_menu_potency": return "潜力";
                case "sort_stat_hardness": return "硬度";
                case "sort_suffix_potential": return " (潜力)";

                // 全局搜索 기능
                case "Global Search...": return "全局搜索...";
                case "Search all filters...": return "搜索所有过滤器...";
                case "Search filters (type to search):": return "搜索过滤기（입력 검색）：";
                case "Search Results": return "검색 결과";
                case "No matching filters found.": return "일치하는 필터를 찾을 수 없습니다.";
                case "Search...": return "검색...";

                // Filter existence indication
                case "Remove from Whitelist": return "화이트리스트에서 제거";
                case "Remove from Blacklist": return "블랙리스트에서 제거";

                // 강화된 UI 요소
                case "Back": return "뒤로";
                case "Navigation": return "이동";
                case "Equipment": return "장비";
                case "Food": return "음식";
                case "Food, Potential": return "음식, 잠재력";
                case "Potential": return "잠재력";

                // Super category translations
                case "cat_super_armor": return "방어구";
                case "cat_super_weapon": return "무기";
                case "cat_super_build": return "건설";
                case "cat_super_consumable": return "소모품";
                case "cat_super_tool": return "도구";
                case "cat_super_resource": return "자원";
                case "cat_super_misc": return "기타";

                // Filter addition messages
                case "Added '{0}' to whitelist": return "'{0}'를 화이트리스트에 추가";
                case "Added '{0}' to blacklist": return "'{0}'를 블랙리스트에 추가";

                // Category groupings
                case "Super Categories": return "슈퍼 카테고리";
                case "Other Categories": return "기타 카테고리";
                case "Items by Type": return "유형별 아이템";
                case "Materials - {0}": return "재료 - {0}";
                case "Item Rarities": return "아이템 희귀도";
                case "Item States": return "아이템 상태";
                case "Stats - {0}": return "능력치 - {0}";
                case "Food Stats": return "음식 능력치";
                case "Potential Stats": return "잠재 능력치";
                case "Food Properties": return "음식 특성";
                case "Item Properties": return "아이템 특성";
                case "Other": return "기타";
                case "Specific Item": return "특정 아이템";

                case "Found {0} filter{1} matching '{2}'": return "{2}와 일치하는 {0}개의 필터{1}를 찾았습니다";
                case "Food Stat Greater Than": return "음식 능력치: {0} > {1}";
                case "Food Stat Less Than": return "음식 능력치: {0} < {1}";
                case "Potential Greater Than": return "잠재력: {0} > {1}";
                case "Potential Less Than": return "잠재력: {0} < {1}";
                case "... and {0} more results. Refine your search for better results.": return "... 그리고 {0}개의 결과가 더 있습니다. 더 나은 결과를 위해 검색을 구체화하세요.";

                default: return GetEnglish(id);
            }
        }

        private static string GetKorean(string id)
        {
            switch (id)
            {
                // Контекстные меню предметов
                case "Autopickup Filter": return "자동 획득 필터";
                case "This Chest's Filter": return "이 상자 필터";
                case "Add '{0}' to whitelist": return "'{0}'를 화이트리스트에 추가";
                case "Add '{0}' to blacklist": return "'{0}'를 블랙리스트에 추가";
                case "Remove '{0}' from whitelist": return "'{0}'를 화이트리스트에서 제거";
                case "Remove '{0}' from blacklist": return "'{0}'를 블랙리스트에서 제거";
                case "Add {0} '{1}' to whitelist": return "{0} '{1}'를 화이트리스트에 추가";
                case "Add {0} '{1}' to blacklist": return "{0} '{1}'를 블랙리스트에 추가";
                case "Remove {0} '{1}' from filter": return "{0} '{1}'를 필터에서 제거";

                // Окно управления фильтрами (FilterManagerWindow)
                case "Container Filters": return "컨테이너 필터";
                case "Open": return "열기";
                case "Autopickup Filter Settings": return "자동 줍기 필터 설정";
                case "Whitelist": return "화이트리스트";
                case "Blacklist": return "블랙리스트";
                case "All": return "모두";
                case "Invalid filter format!": return "잘못된 필터 형식입니다!";
                case "No filters found.": return "필터를 찾을 수 없습니다.";
                case "Item: {0}": return "아이템: {0}";
                case "Type: {0}": return "유형: {0}";
                case "Category: {0}": return "카테고리: {0}";
                case "Rarity: {0}": return "희귀도: {0}";
                case "State: {0}": return "상태: {0}";
                case "Material: {0}": return "재질: {0}";
                case "Stat: {0} > {1}": return "능력치: {0} > {1}";
                case "Stat: {0} < {1}": return "능력치: {0} < {1}";
                case "Value: > {0} gp": return "가치: > {0} gp";
                case "Value: < {0} gp": return "가치: < {0} gp";
                case "Food Quality: > {0}": return "음식 품질: > {0}";
                case "Food Quality: < {0}": return "음식 품질: < {0}";
                case "Nutrition: > {0}": return "영양가: > {0}";
                case "Nutrition: < {0}": return "영양가: < {0}";

                // 새로운 필터로 특성
                case "Stat-based Whitelist": return "능력치 기반 화이트리스트";
                case "Base Value...": return "기본 가치...";
                case "Filter by Base Value": return "기본 가치로 필터링";
                case "Base value filter set to: > {0} gp": return "기본 가치 필터 설정됨: > {0} gp";
                case "Base value filter disabled.": return "기본 가치 필터 비활성화됨.";
                case "Filter for {0} set to: > {1}": return "{0} 필터 설정됨: > {1}";
                case "Filter for {0} disabled.": return "{0} 필터 비활성화됨.";
                case "Invalid number format!": return "잘못된 숫자 형식입니다!";
                case "Add Food Quality > to Whitelist": return "음식 품질 > 화이트리스트에 추가...";
                case "Add Food Quality < to Blacklist": return "음식 품질 < 블랙리스트에 추가...";
                case "Whitelist by Food Quality": return "음식 품질로 화이트리스트";
                case "Blacklist by Food Quality": return "음식 품질로 블랙리스트";
                case "Enter minimum quality": return "화이트리스트에 추가할 최소 품질을 입력하세요:";
                case "Enter maximum quality": return "블랙리스트에 추가할 최대 품질을 입력하세요:";

                // 특성 카테고리
                case "Main Attributes": return "주요 속성";
                case "Combat Stats": return "전투 능력치";
                case "Resistances": return "저항";
                case "Combat and Weapon Skills": return "전투 및 무기 기술";
                case "Magic Skills": return "마법 기술";
                case "Peaceful Skills 1": return "평화 기술 1";
                case "Peaceful Skills 2": return "평화 기술 2";
                case "Melee Modifiers": return "근접 전투 수정치";
                case "Ranged Modifiers": return "원거리 전투 수정치";
                case "Sustain Attributes": return "능력치 유지";
                case "Status Effect Negation": return "상태 이상 무효화";
                case "Bane (Slayer) Effects": return "특공 효과 (슬레이어)";
                case "Elemental Conversion": return "원소 변환";
                case "Elemental Damage": return "원소 피해";
                case "Utility & Exploration": return "유틸리티 및 탐험";
                case "Special Abilities": return "특수 능력";
                case "Miscellaneous Enchantments": return "기타 마법 부여";
                case "Vital Stats": return "생명력 능력치";
                case "Combat Passives": return "전투 패시브";
                case "Material Proof": return "소재 보호";
                case "Faith & Divine": return "신앙과 신성";

                // 설정 메뉴
                case "Enable Mod": return "모드 활성화";
                case "Disable Mod": return "모드 비활성화";
                case "Whitelist Highlight": return "화이트리스트 하이라이트";
                case "Enable highlight": return "하이라이트 활성화";
                case "Disable highlight": return "하이라이트 비활성화";
                case "Change to Aura effect": return "오라 효과로 변경";
                case "Change to Light effect": return "빛 효과로 변경";
                case "Enable sound": return "소리 활성화";
                case "Disable sound": return "소리 비활성화";
                case "Use default sound": return "기본 소리 사용";
                case "Use custom sound": return "사용자 지정 소리 사용";
                case "Use custom sound with note": return "사용자 지정 소리 사용 (모드 폴더에 'drop.mp3' 배치)";
                case "Place 'drop.mp3' in the mod folder.": return "모드 폴더에 'drop.mp3' 파일을 배치하세요.";
                case "Sound Volume...": return "소리 볼륨...";
                case "Volume": return "볼륨";
                case "Font Size": return "글꼴 크기";
                case "Copy Filters": return "필터 복사";
                case "Paste Filters": return "필터 붙여넣기";
                case "World Highlight": return "땅에 있는 아이템 하이라이트";
                case "Inventory Outline": return "인벤토리의 아이템 테두리 표시";
                case "Enabled": return "활성화됨";
                case "Disabled": return "비활성화됨";

                // 새로운 계층 메뉴 키
                case "filter_menu_food": return "음식 기준...";
                case "filter_menu_food_properties": return "속성 기준";
                case "filter_menu_food_direct_stats": return "직접 능력치 기준";
                case "filter_menu_properties": return "속성 기준 (일반)";
                case "filter_menu_potential": return "잠재 능력치 기준";
                case "filter_menu_category": return "카테고리 기준";
                case "filter_menu_rarity": return "희귀도 기준";
                case "filter_menu_state": return "상태 기준";
                case "filter_menu_material": return "재질 기준";
                case "add_to_whitelist": return "화이트리스트에 추가";
                case "add_to_blacklist": return "블랙리스트에 추가";
                case "filter_prop_hardness_gt": return "경도 >...";
                case "filter_prop_nutrition_gt": return "영양가 >...";
                case "filter_prop_rotten": return "썩음";
                case "filter_prop_human_flesh": return "인육";
                case "filter_prop_undead_flesh": return "언데드 살점";
                case "filter_prop_cat_meat": return "고양이 고기";
                case "filter_prop_bug_meat": return "벌레 고기";
                case "filter_prop_raw_fish": return "생선";
                case "filter_prop_aphrodisiac": return "최음제";
                case "filter_prop_blessed": return "축복된";
                case "filter_prop_cursed": return "저주된";
                case "filter_prop_godly": return "신성한";
                case "filter_prop_doomed": return "파멸의";
                case "filter_prop_equipped": return "장착됨";
                case "filter_prop_unequippable": return "장착 불가";
                case "filter_prop_identify": return "감정됨";
                case "filter_prop_unidentified": return "미감정";
                case "filter_prop_precious": return "귀중한";
                case "filter_prop_stolen": return "훔친";
                case "Hardness: > {0}": return "경도: > {0}";
                case "Potential {0}: > {1}": return "잠재력: {0} > {1}";
                case "Potential {0}: < {1}": return "잠재력: {0} < {1}";
                case "Food Stat: {0} > {1}": return "음식 능력치: {0} > {1}";
                case "Food Stat: {0} < {1}": return "음식 능력치: {0} < {1}";

                // 규칙 키
                case "type": return "유형";
                case "category": return "카테고리";
                case "rarity": return "희귀도";
                case "state": return "상태";
                case "material": return "재질";
                case "property": return "속성";
                case "property_display": return "속성: {0}";
                case "sort_button_default": return "정렬";
                case "sort_menu_default": return "기본값";
                case "sort_menu_properties": return "속성";
                case "sort_menu_stats": return "능력치";
                case "sort_menu_potency": return "잠재력";
                case "sort_stat_hardness": return "경도";
                case "sort_suffix_potential": return " (잠재)";

                // 글로벌 검색 기능
                case "Global Search...": return "글로벌 검색...";
                case "Search all filters...": return "모든 필터 검색...";
                case "Search filters (type to search):": return "필터 검색 (검색하려면 입력):";
                case "Search Results": return "검색 결과";
                case "No matching filters found.": return "일치하는 필터를 찾을 수 없습니다.";
                case "Search...": return "검색...";

                // Filter existence indication
                case "Remove from Whitelist": return "화이트리스트에서 제거";
                case "Remove from Blacklist": return "블랙리스트에서 제거";

                // 강화된 UI 요소
                case "Back": return "뒤로";
                case "Navigation": return "내비게이션";
                case "Equipment": return "장비";
                case "Food": return "음식";
                case "Food, Potential": return "음식, 잠재력";
                case "Potential": return "잠재력";

                // Super category translations
                case "cat_super_armor": return "방어구";
                case "cat_super_weapon": return "무기";
                case "cat_super_build": return "건설";
                case "cat_super_consumable": return "소모품";
                case "cat_super_tool": return "도구";
                case "cat_super_resource": return "자원";
                case "cat_super_misc": return "기타";

                // Filter addition messages
                case "Added '{0}' to whitelist": return "'{0}'를 화이트리스트에 추가";
                case "Added '{0}' to blacklist": return "'{0}'를 블랙리스트에 추가";

                // Category groupings
                case "Super Categories": return "슈퍼 카테고리";
                case "Other Categories": return "기타 카테고리";
                case "Items by Type": return "유형별 아이템";
                case "Materials - {0}": return "재료 - {0}";
                case "Item Rarities": return "아이템 희귀도";
                case "Item States": return "아이템 상태";
                case "Stats - {0}": return "능력치 - {0}";
                case "Food Stats": return "음식 능력치";
                case "Potential Stats": return "잠재 능력치";
                case "Food Properties": return "음식 특성";
                case "Item Properties": return "아이템 특성";
                case "Other": return "기타";
                case "Specific Item": return "특정 아이템";
                // Mod status warning
                case "Mod is turned off": return "모드가 꺼져 있습니다";

                case "Found {0} filter{1} matching '{2}'": return "'{2}'와 일치하는 {0}개의 필터{1}를 찾았습니다";
                case "Food Stat Greater Than": return "음식 능력치: {0} > {1}";
                case "Food Stat Less Than": return "음식 능력치: {0} < {1}";
                case "Potential Greater Than": return "잠재력: {0} > {1}";
                case "Potential Less Than": return "잠재력: {0} < {1}";
                case "... and {0} more results. Refine your search for better results.": return "... 그리고 {0}개의 결과가 더 있습니다. 더 나은 결과를 위해 검색을 구체화하세요.";

                default: return GetEnglish(id);
            }
        }
    }
}
