using System.Collections.Generic;
using UnityEngine;

public enum ToolType { FishingRod, Pickaxe }

public class ItemTool : Item
{
    [Header("Thông tin Công cụ")]
    public ToolType toolType;

    [Header("Câu Cá: Prefab & Chỉ số")]
    public GameObject fishingRodWorldPrefab;

    [Tooltip("Hệ số thời gian chờ cá cắn (VD: 0.8 = giảm 20% thời gian)")]
    public float waitTimeMultiplier = 1f;

    [Tooltip("Tỷ lệ % cộng thêm để câu được đồ hiếm")]
    public float rarityBonusRate = 0f;

    public override ItemType ItemType => ItemType.Tool;
    public override bool IsStackable => false;

    public override void UseItem()
    {
        switch (toolType)
        {
            case ToolType.FishingRod:
                UseFishingRodNearest();
                break;
            case ToolType.Pickaxe:
                UsePickaxeNearest();
                break;
        }
    }

    public void TryUseToolOnPlot(FarmPlot plot)
    {
        if (InteractionDetector.Instance == null || !InteractionDetector.Instance.IsPlotInRange(plot))
        {
            GameNotify.Show("Quá xa để sử dụng công cụ!");
            return;
        }

        if (toolType == ToolType.Pickaxe)
        {
            if (plot.isPlanted) FarmController.Instance.TryDestroyCrop(plot);
        }
    }

    public bool TryUseFishingRodOnWater(Vector2 waterWorldPosition)
    {
        Transform playerTransform = GetPlayerTransform();
        if (playerTransform == null) return false;

        if (Vector2.Distance(playerTransform.position, waterWorldPosition) > 2.5f)
        {
            GameNotify.Show("Vị trí thả cần quá xa!");
            return false;
        }

        SpawnFishingRodToWorld(waterWorldPosition);
        return true;
    }

    private void UseFishingRodNearest()
    {
        Transform playerTransform = GetPlayerTransform();
        if (playerTransform == null) return;

        Collider2D[] colliders = Physics2D.OverlapCircleAll(playerTransform.position, 2f);

        Vector2? closestWaterPos = null;
        float closestDistance = float.MaxValue;

        foreach (var col in colliders)
        {
            if (LayerMask.LayerToName(col.gameObject.layer) == "Water")
            {
                Vector2 closestPoint = col.ClosestPoint(playerTransform.position);
                float distance = Vector2.Distance(playerTransform.position, closestPoint);

                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestWaterPos = closestPoint;
                }
            }
        }

        if (closestWaterPos.HasValue)
        {
            SpawnFishingRodToWorld(closestWaterPos.Value);
        }
        else
        {
            GameNotify.Show("Phải đứng gần mặt nước để thả cần!");
        }
    }

    private void SpawnFishingRodToWorld(Vector2 spawnPosition)
    {
        if (fishingRodWorldPrefab == null) return;

        GameObject rodObj = Instantiate(fishingRodWorldPrefab, spawnPosition, Quaternion.identity);

        if (rodObj.TryGetComponent(out FishingRodWorld worldRod))
        {
            worldRod.InitializeRod(ID, icon, waitTimeMultiplier, rarityBonusRate);
        }

        SoundEffectManager.Play("CastingRod", true);

        var ramData = InventoryController.Instance.GetInventoryItemsData().Find(x => x.dbID == dbID);
        if (ramData != null)
        {
            InventoryController.Instance.GetInventoryItemsData().RemoveAll(x => x.dbID == dbID);
            InventoryService.Instance.CancelQuantityUpdate(dbID);
            InventoryService.Instance.RequestRemoveItem(dbID);
        }

        Slot parentSlot = GetComponentInParent<Slot>();
        if (parentSlot != null) parentSlot.currentItem = null;

        InventoryController.Instance.ReBuildItemCounts();
        Destroy(gameObject);
    }

    private void UsePickaxeNearest()
    {
        FarmPlot plotToDestroy = GetNearestPlantedPlot();
        if (plotToDestroy != null)
        {
            FarmController.Instance.TryDestroyCrop(plotToDestroy);
        }
        else
        {
            GameNotify.Show("Không có cây trồng nào ở gần để đập bỏ!");
        }
    }

    private FarmPlot GetNearestPlantedPlot()
    {
        Transform playerTransform = GetPlayerTransform();
        if (playerTransform == null) return null;

        float checkRadius = 2f;
        Collider2D[] colliders = Physics2D.OverlapCircleAll(playerTransform.position, checkRadius);

        FarmPlot closestPlot = null;
        float closestDistance = float.MaxValue;

        foreach (var col in colliders)
        {
            FarmPlot plot = col.GetComponent<FarmPlot>();

            if (plot != null && plot.isPlanted && InteractionDetector.Instance.IsPlotInRange(plot))
            {
                float distance = Vector2.Distance(playerTransform.position, plot.transform.position);
                if (distance < closestDistance)
                {
                    closestDistance = distance;
                    closestPlot = plot;
                }
            }
        }
        return closestPlot;
    }

    private Transform GetPlayerTransform()
    {
        if (Unity.Netcode.NetworkManager.Singleton != null &&
            Unity.Netcode.NetworkManager.Singleton.IsConnectedClient &&
            Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject != null)
        {
            return Unity.Netcode.NetworkManager.Singleton.LocalClient.PlayerObject.transform;
        }

        GameObject player = GameObject.FindGameObjectWithTag("PlayerController");
        return player != null ? player.transform : null;
    }
}