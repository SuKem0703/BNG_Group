using UnityEngine;
public enum RarityMode
{
    Fixed,
    Random
}

public enum QualityFactorMode
{
    Max,
    Random
}

public class Chest : MonoBehaviour, IInteractable
{
    [field: SerializeField] public bool IsOpened { get; private set; }
    [field: SerializeField] public string ChestID { get; private set; }

    [Header("Prefab vật phẩm rơi ra")]
    public GameObject itemPrefab;

    [Header("Sprite")]
    public Sprite openedSprite;

    [Header("Phẩm chất")]
    public RarityMode rarityMode = RarityMode.Random;
    public ItemRarity fixedRarity = ItemRarity.Common;

    [Header("Hệ số chỉ số")]
    public QualityFactorMode qualityMode = QualityFactorMode.Random;

    void Awake()
    {
        if (string.IsNullOrEmpty(ChestID))
        {
            ChestID = GlobalHelper.GenerateUniqueID(gameObject);
        }
    }
    public bool CanInteract()
    {
        return !IsOpened;
    }

    public void Interact()
    {
        if (!CanInteract())
        {
            return;
        }

        SoundEffectManager.Play("ChestOpen");

        OpenChest();
    }
    private void OpenChest()
    {
        SetOpened(true);

        if (itemPrefab)
        {
            GameObject droppedItem = Instantiate(itemPrefab, transform.position + Vector3.down, Quaternion.identity);

            // Thiết lập phẩm chất và hệ số
            Item item = droppedItem.GetComponent<Item>();
            if (item != null)
            {
                // Gán phẩm chất
                item.rarity = (rarityMode == RarityMode.Fixed)
                    ? fixedRarity
                    : ItemGenerationHelper.GetRandomRarity();

                // Gán hệ số
                item.qualityFactor = (qualityMode == QualityFactorMode.Max)
                    ? 1f
                    : ItemGenerationHelper.GetWeightedQualityFactor();
            }

            droppedItem.GetComponent<BounceEffect>().StartBounce();

            SaveController saveController = FindFirstObjectByType<SaveController>();
            if (saveController != null)
            {
                saveController.SaveGame();
            }
        }
    }
    public void SetOpened(bool opened)
    {
        if (IsOpened = opened)
        {
            GetComponent<SpriteRenderer>().sprite = openedSprite;
        }
    }
}
