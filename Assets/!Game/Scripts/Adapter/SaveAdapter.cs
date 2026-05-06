using UnityEngine;

public class SaveAdapter : MonoBehaviour
{
    [Header("Controllers")]
    public InventoryController inventoryController;
    public HotbarController hotbarController;
    public FarmController farmController;

    [Header("Equipment Panels")]
    public KnightEquipmentPanel knightEquipmentPanel;
    public MageEquipmentPanel mageEquipmentPanel;
    public SharedEquipmentPanel sharedEquipmentPanel;

    private void Awake()
    {
        inventoryController ??= FindFirstObjectByType<InventoryController>(FindObjectsInactive.Include);
        hotbarController ??= FindFirstObjectByType<HotbarController>();
        farmController ??= FindFirstObjectByType<FarmController>();

        knightEquipmentPanel ??= FindFirstObjectByType<KnightEquipmentPanel>(FindObjectsInactive.Include);
        mageEquipmentPanel ??= FindFirstObjectByType<MageEquipmentPanel>(FindObjectsInactive.Include);
        sharedEquipmentPanel ??= FindFirstObjectByType<SharedEquipmentPanel>(FindObjectsInactive.Include);
    }

    private void Start()
    {
        if (SaveController.Instance != null)
        {
            SaveController.Instance.RegisterUIAdapter(this);
        }
    }
}