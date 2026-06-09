using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerLight : MonoBehaviour
{
    [SerializeField] private Light2D playerLightSource;

    [Header("Flicker Settings")]
    [SerializeField] private float flickerSpeed = 3f;
    [SerializeField] private float intensityVariation = 0.15f;
    [SerializeField] private float radiusVariation = 0.2f;

    private float baseIntensity;
    private float baseRadius;
    private float noiseOffset;

    private void OnEnable()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged += CheckEquippedLightSource;
        }
    }

    private void OnDisable()
    {
        if (InventoryController.Instance != null)
        {
            InventoryController.Instance.OnInventoryChanged -= CheckEquippedLightSource;
        }
    }

    private void Start()
    {
        if (playerLightSource != null)
        {
            baseIntensity = playerLightSource.intensity;
            baseRadius = playerLightSource.pointLightOuterRadius;
            
            noiseOffset = Random.Range(0f, 100f); 
        }

        if (InventoryController.Instance != null)
        {
            CheckEquippedLightSource(InventoryController.Instance.GetInventoryItemsData(), 0);
        }
    }

    private void Update()
    {
        if (playerLightSource == null || !playerLightSource.enabled) return;

        float noise = Mathf.PerlinNoise((Time.time * flickerSpeed) + noiseOffset, 0f);

        float normalizedNoise = (noise * 2f) - 1f;

        playerLightSource.intensity = baseIntensity + (normalizedNoise * intensityVariation);
        playerLightSource.pointLightOuterRadius = baseRadius + (normalizedNoise * radiusVariation);
    }

    private void CheckEquippedLightSource(List<InventorySaveData> inventoryData, int slotCount)
    {
        if (playerLightSource == null || ItemDictionary.Instance == null) return;

        bool hasLightEquipped = false;

        foreach (var data in inventoryData)
        {
            if (!data.isEquipped) continue;

            GameObject itemPrefab = ItemDictionary.Instance.GetItemPrefab(data.itemID);
            if (itemPrefab != null)
            {
                EquipmentItem equipItem = itemPrefab.GetComponent<EquipmentItem>();
                
                if (equipItem != null && equipItem.equipSlot == EquipSlot.OffHand)
                {
                    LightSourceData lightData = itemPrefab.GetComponent<LightSourceData>();
                    if (lightData != null)
                    {
                        hasLightEquipped = true;
                        break; 
                    }
                }
            }
        }

        if (playerLightSource.enabled != hasLightEquipped)
        {
            playerLightSource.enabled = hasLightEquipped;
        }
    }
}