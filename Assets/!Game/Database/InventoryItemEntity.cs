using SQLite;

[Table("InventoryItems")]
public class InventoryItemEntity
{
    [PrimaryKey, AutoIncrement]
    public int DbId { get; set; }

    [Indexed]
    public string ProfileId { get; set; }

    public int ItemId { get; set; }
    public int Quantity { get; set; }
    public int SlotIndex { get; set; }
    public bool IsEquipped { get; set; }
    public int Rarity { get; set; }
    public float QualityFactor { get; set; }

    [Indexed]
    public string ChestId { get; set; }
}