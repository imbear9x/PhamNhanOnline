using GameShared.Models;
using PhamNhanOnline.Client.UI.Inventory;
using UnityEngine;
using UnityEngine.EventSystems;

namespace PhamNhanOnline.Client.UI.Common
{
    public enum UIDragPayloadKind
    {
        None = 0,
        InventoryItem = 1,
        Recipe = 2,
        Skill = 3,
        MartialArt = 4,
    }

    public enum UIDragSourceKind
    {
        None = 0,
        InventoryGridItem = 1,
        EquipmentSlot = 2,
        CraftRecipeListItem = 3,
        CraftRecipeSlot = 4,
        SkillListItem = 5,
        SkillLoadoutSlot = 6,
        MartialArtListItem = 7,
        ActiveMartialArtSlot = 8,
    }

    public interface IUIDragPayloadSource
    {
        bool TryCreateDragPayload(out UIDragPayload payload);
    }

    public readonly struct UIDragPayload
    {
        private UIDragPayload(
            UIDragPayloadKind kind,
            UIDragSourceKind sourceKind,
            InventoryItemModel inventoryItem,
            bool hasInventoryItem,
            LearnedPillRecipeModel recipe,
            bool hasRecipe,
            PlayerSkillModel skill,
            bool hasSkill,
            PlayerMartialArtModel martialArt,
            bool hasMartialArt,
            InventoryEquipmentSlot sourceEquipmentSlot,
            bool hasSourceEquipmentSlot,
            int sourceIndex,
            bool hasSourceIndex)
        {
            Kind = kind;
            SourceKind = sourceKind;
            InventoryItem = inventoryItem;
            HasInventoryItem = hasInventoryItem;
            Recipe = recipe;
            HasRecipe = hasRecipe;
            Skill = skill;
            HasSkill = hasSkill;
            MartialArt = martialArt;
            HasMartialArt = hasMartialArt;
            SourceEquipmentSlot = sourceEquipmentSlot;
            HasSourceEquipmentSlot = hasSourceEquipmentSlot;
            SourceIndex = sourceIndex;
            HasSourceIndex = hasSourceIndex;
        }

        public UIDragPayloadKind Kind { get; }
        public UIDragSourceKind SourceKind { get; }
        public InventoryItemModel InventoryItem { get; }
        public bool HasInventoryItem { get; }
        public LearnedPillRecipeModel Recipe { get; }
        public bool HasRecipe { get; }
        public PlayerSkillModel Skill { get; }
        public bool HasSkill { get; }
        public PlayerMartialArtModel MartialArt { get; }
        public bool HasMartialArt { get; }
        public InventoryEquipmentSlot SourceEquipmentSlot { get; }
        public bool HasSourceEquipmentSlot { get; }
        public int SourceIndex { get; }
        public bool HasSourceIndex { get; }

        public static UIDragPayload FromInventoryItem(
            InventoryItemModel inventoryItem,
            UIDragSourceKind sourceKind,
            InventoryEquipmentSlot? sourceEquipmentSlot = null)
        {
            return new UIDragPayload(
                UIDragPayloadKind.InventoryItem,
                sourceKind,
                inventoryItem,
                hasInventoryItem: true,
                default,
                hasRecipe: false,
                default,
                hasSkill: false,
                default,
                hasMartialArt: false,
                sourceEquipmentSlot ?? InventoryEquipmentSlot.None,
                sourceEquipmentSlot.HasValue,
                0,
                hasSourceIndex: false);
        }

        public static UIDragPayload FromRecipe(LearnedPillRecipeModel recipe, UIDragSourceKind sourceKind)
        {
            return new UIDragPayload(
                UIDragPayloadKind.Recipe,
                sourceKind,
                default,
                hasInventoryItem: false,
                recipe,
                hasRecipe: true,
                default,
                hasSkill: false,
                default,
                hasMartialArt: false,
                InventoryEquipmentSlot.None,
                hasSourceEquipmentSlot: false,
                0,
                hasSourceIndex: false);
        }

        public static UIDragPayload FromSkill(PlayerSkillModel skill, UIDragSourceKind sourceKind, int? sourceIndex = null)
        {
            return new UIDragPayload(
                UIDragPayloadKind.Skill,
                sourceKind,
                default,
                hasInventoryItem: false,
                default,
                hasRecipe: false,
                skill,
                hasSkill: true,
                default,
                hasMartialArt: false,
                InventoryEquipmentSlot.None,
                hasSourceEquipmentSlot: false,
                sourceIndex ?? 0,
                sourceIndex.HasValue);
        }

        public static UIDragPayload FromMartialArt(PlayerMartialArtModel martialArt, UIDragSourceKind sourceKind)
        {
            return new UIDragPayload(
                UIDragPayloadKind.MartialArt,
                sourceKind,
                default,
                hasInventoryItem: false,
                default,
                hasRecipe: false,
                default,
                hasSkill: false,
                martialArt,
                hasMartialArt: true,
                InventoryEquipmentSlot.None,
                hasSourceEquipmentSlot: false,
                0,
                hasSourceIndex: false);
        }
    }

    public static class UIDragPayloadResolver
    {
        public static bool TryResolve(PointerEventData eventData, out UIDragPayload payload)
        {
            if (eventData == null || eventData.pointerDrag == null)
            {
                payload = default;
                return false;
            }

            return TryResolve(eventData.pointerDrag.transform, out payload);
        }

        public static bool TryResolve(Transform transform, out UIDragPayload payload)
        {
            while (transform != null)
            {
                var components = transform.GetComponents<MonoBehaviour>();
                for (var i = 0; i < components.Length; i++)
                {
                    if (!(components[i] is IUIDragPayloadSource source))
                        continue;

                    if (source.TryCreateDragPayload(out payload))
                        return true;
                }

                    transform = transform.parent;
            }

            payload = default;
            return false;
        }
    }
}
