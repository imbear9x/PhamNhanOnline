using System;
using System.Collections.Generic;
using GameShared.Models;
using UnityEngine;

namespace PhamNhanOnline.Client.UI.Inventory
{
    [CreateAssetMenu(
        fileName = "InventoryItemPresentationCatalog",
        menuName = "PhamNhanOnline/UI/Inventory Item Presentation Catalog")]
    public sealed class InventoryItemPresentationCatalog : ScriptableObject
    {
        [Serializable]
        public sealed class SpriteKeyEntry
        {
            [SerializeField] private string key = string.Empty;
            [SerializeField] private Sprite sprite;

            public string Key => key;
            public Sprite Sprite => sprite;
        }

        [Serializable]
        public sealed class TemplateOverrideEntry
        {
            [SerializeField] private int itemTemplateId;
            [SerializeField] private Sprite iconSprite;
            [SerializeField] private Sprite backgroundSprite;

            public int ItemTemplateId => itemTemplateId;
            public Sprite IconSprite => iconSprite;
            public Sprite BackgroundSprite => backgroundSprite;
        }

        [Serializable]
        public sealed class RarityBackgroundEntry
        {
            [SerializeField] private InventoryItemRarity rarity = InventoryItemRarity.Common;
            [SerializeField] private Sprite sprite;

            public InventoryItemRarity Rarity => rarity;
            public Sprite Sprite => sprite;
        }

        [Serializable]
        public sealed class ItemTypeBackgroundEntry
        {
            [SerializeField] private InventoryItemType itemType = InventoryItemType.Equipment;
            [SerializeField] private Sprite sprite;

            public InventoryItemType ItemType => itemType;
            public Sprite Sprite => sprite;
        }

        [Serializable]
        public sealed class RarityColorEntry
        {
            [SerializeField] private InventoryItemRarity rarity = InventoryItemRarity.Common;
            [SerializeField] private Color color = Color.white;

            public InventoryItemRarity Rarity => rarity;
            public Color Color => color;
        }

        [Header("Fallbacks")]
        [SerializeField] private Sprite defaultIconSprite;
        [SerializeField] private Sprite defaultBackgroundSprite;
        [SerializeField] private Color defaultNameColor = Color.white;

        [Header("Direct Key Mapping")]
        [SerializeField] private List<SpriteKeyEntry> iconEntries = new List<SpriteKeyEntry>();
        [SerializeField] private List<SpriteKeyEntry> backgroundEntries = new List<SpriteKeyEntry>();

        [Header("Template Overrides")]
        [SerializeField] private List<TemplateOverrideEntry> templateOverrides = new List<TemplateOverrideEntry>();

        [Header("Fallback By Rarity")]
        [SerializeField] private List<RarityBackgroundEntry> rarityBackgrounds = new List<RarityBackgroundEntry>();
        [SerializeField] private List<RarityColorEntry> rarityColors = new List<RarityColorEntry>();

        [Header("Fallback By Item Type")]
        [SerializeField] private List<ItemTypeBackgroundEntry> itemTypeBackgrounds = new List<ItemTypeBackgroundEntry>();

        public InventoryItemPresentation Resolve(InventoryItemModel item)
        {
            var iconSprite = ResolveIcon(item);
            var backgroundSprite = ResolveBackground(item);
            var nameColor = ResolveNameColor(item.Rarity);
            return new InventoryItemPresentation(iconSprite, backgroundSprite, nameColor);
        }

        public InventoryItemPresentation Resolve(GroundRewardItemModel item)
        {
            var iconSprite = ResolveIcon(item.ItemTemplateId, item.Icon);
            var backgroundSprite = ResolveBackground(item.ItemTemplateId, item.BackgroundIcon, item.Rarity, item.ItemType);
            var nameColor = ResolveNameColor(item.Rarity);
            return new InventoryItemPresentation(iconSprite, backgroundSprite, nameColor);
        }

        public static string GetRarityLabel(int rarity)
        {
            switch ((InventoryItemRarity)rarity)
            {
                case InventoryItemRarity.Common:
                    return "Common";
                case InventoryItemRarity.Uncommon:
                    return "Uncommon";
                case InventoryItemRarity.Rare:
                    return "Rare";
                case InventoryItemRarity.Epic:
                    return "Epic";
                case InventoryItemRarity.Legendary:
                    return "Legendary";
                default:
                    return "Unknown";
            }
        }

        public static string GetItemTypeLabel(int itemType)
        {
            switch ((InventoryItemType)itemType)
            {
                case InventoryItemType.Equipment:
                    return "Trang bi";
                case InventoryItemType.Consumable:
                    return "Dan duoc";
                case InventoryItemType.Material:
                    return "Nguyen lieu";
                case InventoryItemType.Talisman:
                    return "Phap bao";
                case InventoryItemType.MartialArtBook:
                    return "Cong phap";
                case InventoryItemType.Currency:
                    return "Linh thach";
                case InventoryItemType.QuestItem:
                    return "Nhiem vu";
                case InventoryItemType.PillRecipeBook:
                    return "Dan phuong";
                case InventoryItemType.HerbSeed:
                    return "Hat giong";
                case InventoryItemType.HerbMaterial:
                    return "Duoc lieu";
                case InventoryItemType.Soil:
                    return "Linh tho";
                case InventoryItemType.HerbPlant:
                    return "Cay song";
                default:
                    return "Vat pham";
            }
        }

        public static string GetEquipmentSlotLabel(int? equippedSlot)
        {
            if (!equippedSlot.HasValue)
                return string.Empty;

            switch ((InventoryEquipmentSlot)equippedSlot.Value)
            {
                case InventoryEquipmentSlot.Weapon:
                    return "Vu khi";
                case InventoryEquipmentSlot.Armor:
                    return "Ao";
                case InventoryEquipmentSlot.Pants:
                    return "Quan";
                case InventoryEquipmentSlot.Shoes:
                    return "Giay";
                default:
                    return string.Empty;
            }
        }

        private Sprite ResolveIcon(InventoryItemModel item)
        {
            return ResolveIcon(item.ItemTemplateId, item.Icon);
        }

        private Sprite ResolveIcon(int itemTemplateId, string iconKey)
        {
            TemplateOverrideEntry templateEntry;
            if (TryGetTemplateOverride(itemTemplateId, out templateEntry) && templateEntry.IconSprite != null)
                return templateEntry.IconSprite;

            Sprite sprite;
            if (TryResolveByKey(iconEntries, iconKey, out sprite))
                return sprite;

            return defaultIconSprite;
        }

        private Sprite ResolveBackground(InventoryItemModel item)
        {
            return ResolveBackground(item.ItemTemplateId, item.BackgroundIcon, item.Rarity, item.ItemType);
        }

        private Sprite ResolveBackground(int itemTemplateId, string backgroundIconKey, int rarity, int itemType)
        {
            TemplateOverrideEntry templateEntry;
            if (TryGetTemplateOverride(itemTemplateId, out templateEntry) && templateEntry.BackgroundSprite != null)
                return templateEntry.BackgroundSprite;

            Sprite sprite;
            if (TryResolveByKey(backgroundEntries, backgroundIconKey, out sprite))
                return sprite;

            if (TryResolveRarityBackground(rarity, out sprite))
                return sprite;

            if (TryResolveItemTypeBackground(itemType, out sprite))
                return sprite;

            return defaultBackgroundSprite;
        }

        private Color ResolveNameColor(int rarity)
        {
            var resolvedRarity = (InventoryItemRarity)rarity;
            for (var i = 0; i < rarityColors.Count; i++)
            {
                var entry = rarityColors[i];
                if (entry == null || entry.Rarity != resolvedRarity)
                    continue;

                return entry.Color;
            }

            return defaultNameColor;
        }

        private bool TryResolveRarityBackground(int rarity, out Sprite sprite)
        {
            var resolvedRarity = (InventoryItemRarity)rarity;
            for (var i = 0; i < rarityBackgrounds.Count; i++)
            {
                var entry = rarityBackgrounds[i];
                if (entry == null || entry.Rarity != resolvedRarity || entry.Sprite == null)
                    continue;

                sprite = entry.Sprite;
                return true;
            }

            sprite = null;
            return false;
        }

        private bool TryResolveItemTypeBackground(int itemType, out Sprite sprite)
        {
            var resolvedType = (InventoryItemType)itemType;
            for (var i = 0; i < itemTypeBackgrounds.Count; i++)
            {
                var entry = itemTypeBackgrounds[i];
                if (entry == null || entry.ItemType != resolvedType || entry.Sprite == null)
                    continue;

                sprite = entry.Sprite;
                return true;
            }

            sprite = null;
            return false;
        }

        private bool TryGetTemplateOverride(int itemTemplateId, out TemplateOverrideEntry entry)
        {
            for (var i = 0; i < templateOverrides.Count; i++)
            {
                var current = templateOverrides[i];
                if (current == null || current.ItemTemplateId != itemTemplateId)
                    continue;

                entry = current;
                return true;
            }

            entry = null;
            return false;
        }

        private static bool TryResolveByKey(IReadOnlyList<SpriteKeyEntry> entries, string key, out Sprite sprite)
        {
            if (!string.IsNullOrWhiteSpace(key))
            {
                for (var i = 0; i < entries.Count; i++)
                {
                    var entry = entries[i];
                    if (entry == null || entry.Sprite == null)
                        continue;

                    if (!string.Equals(entry.Key, key, StringComparison.OrdinalIgnoreCase))
                        continue;

                    sprite = entry.Sprite;
                    return true;
                }
            }

            sprite = null;
            return false;
        }
    }
}
