using UnityEngine;

public class RewardController : MonoBehaviour
{
    public static RewardController Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }
    public void GiveQuestReward(Quest quest)
    {
        if (quest?.questRewards == null) return;

        foreach (var reward in quest.questRewards)
        {
            switch (reward.rewardType)
            {
                case RewardType.Item:
                    GiveItemReward(reward.rewardID, reward.amount);
                    break;
                case RewardType.Coin:
                    GiveCoinReward(reward.amount);
                    break;
                case RewardType.Gem:
                    GiveGemReward(reward.amount);
                    break;
                case RewardType.Experience:
                    GiveEXPReward(reward.amount);
                    break;
                case RewardType.Custom:
                    break;
            }
        }
    }

    public void GiveItemReward(int itemID, int amount = 1)
    {
        var itemPrefab = FindAnyObjectByType<ItemDictionary>()?.GetItemPrefab(itemID);

        if (itemPrefab == null) return;

        for (int i = 0; i < amount; i++)
        {
            GameObject itemInstance = Instantiate(itemPrefab);

            Item item = itemInstance.GetComponent<Item>();
            if (item.itemType == ItemType.Equipment)
            {
                item.rarity = ItemGenerationHelper.GetRandomRarity();
                item.qualityFactor = ItemGenerationHelper.GetWeightedQualityFactor();
            }

            if (!InventoryController.Instance.AddItem(itemInstance))
            {
                GameObject dropItem = Instantiate(itemPrefab, transform.position + Vector3.down, Quaternion.identity);
                dropItem.GetComponent<BounceEffect>().StartBounce();
            }
            else {
                itemPrefab.GetComponent<Item>().ShowPopUp();
                Debug.Log($"Đã nhận phần thưởng: {itemInstance.name} x{amount}");
            }
        }
    }
    public void GiveCoinReward(int amount)
    {
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null) return;
        playerStats.AddCoin(amount);
        Debug.Log($"Đã nhận phần thưởng: {amount} coin");
    }
    public void GiveGemReward(int amount)
    {
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null) return;
        playerStats.AddGem(amount);
        Debug.Log($"Đã nhận phần thưởng: {amount} gem");
    }
    public void GiveEXPReward(int amount)
    {
        PlayerStats playerStats = FindFirstObjectByType<PlayerStats>();
        if (playerStats == null) return;
        playerStats.AddEXP(amount);
        Debug.Log($"Đã nhận phần thưởng: {amount} EXP");
    }
}
