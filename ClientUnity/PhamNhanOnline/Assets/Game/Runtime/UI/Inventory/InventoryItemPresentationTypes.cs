namespace PhamNhanOnline.Client.UI.Inventory
{
    public enum InventoryItemType
    {
        Unknown = 0,
        Equipment = 1,
        Consumable = 2,
        Material = 3,
        Talisman = 4,
        MartialArtBook = 5,
        Currency = 6,
        QuestItem = 7,
        PillRecipeBook = 8,
        HerbSeed = 9,
        HerbMaterial = 10,
        Soil = 11,
        HerbPlant = 12
    }

    public enum InventoryItemRarity
    {
        Unknown = 0,
        Common = 1,
        Uncommon = 2,
        Rare = 3,
        Epic = 4,
        Legendary = 5
    }

    public enum InventoryEquipmentSlot
    {
        None = 0,
        Weapon = 1,
        Armor = 2,
        Pants = 3,
        Shoes = 4
    }
}
