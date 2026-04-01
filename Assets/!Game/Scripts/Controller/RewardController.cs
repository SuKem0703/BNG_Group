using UnityEngine;

public class RewardController : MonoBehaviour
{
    public static RewardController Instance { get; private set; }
    private void Awake()
    {
        if (Instance == null) { Instance = this; }
        else { Destroy(gameObject); }
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

            if (item is EquipmentItem equip)
            {
                equip.rarity = ItemGenerationHelper.GetRandomRarity();
                equip.qualityFactor = ItemGenerationHelper.GetWeightedQualityFactor();
            }

            if (!InventoryController.Instance.AddItem(itemInstance))
            {
                GameObject dropItem = Instantiate(itemPrefab, transform.position + Vector3.down, Quaternion.identity);

                if (dropItem.GetComponent<Item>() is EquipmentItem dropEquip && item is EquipmentItem sourceEquip)
                {
                    dropEquip.rarity = sourceEquip.rarity;
                    dropEquip.qualityFactor = sourceEquip.qualityFactor;
                }

                dropItem.GetComponent<BounceEffect>()?.StartBounce();
            }
            else
            {
                item.ShowPopUp();
                Debug.Log($"Đã nhận phần thưởng: {item.Name} x{amount}");
            }
        }
    }

    public void GiveCoinReward(int amount)
    {
        EconomyService.Instance.EarnCurrency("Coin", amount, "Reward", (success) => 
        {
            if (success) Debug.Log($"Đã nhận thưởng {amount} Coin từ Server.");
        });
    }

    public void GiveGemReward(int amount)
    {
        EconomyService.Instance.EarnCurrency("Gem", amount, "Reward", (success) => 
        {
            if (success) Debug.Log($"Đã nhận thưởng {amount} Gem từ Server.");
        });
    }

    public void GiveEXPReward(int amount)
    {
        if (PlayerStats.Instance != null)
        {
            PlayerStats.Instance.AddEXP(amount);
            Debug.Log($"Đã nhận thưởng: {amount} EXP");
        }
    }
}