using UnityEngine;

public enum ToolType { FishingRod, Pickaxe }

public class ItemTool : Item
{
    [Header("Thông tin Công cụ")]
    public ToolType toolType;

    public override ItemType ItemType => ItemType.Tool;
    public override bool IsStackable => false;

    public override void UseItem()
    {
        switch (toolType)
        {
            case ToolType.FishingRod:
                UseFishingRod();
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

        switch (toolType)
        {
            case ToolType.Pickaxe:
                if (plot.isPlanted) FarmController.Instance.TryDestroyCrop(plot);
                break;
        }
    }

    private void UseFishingRod() { }

    private void UseHoe() { }

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