using UnityEngine;

public class FishingRodWorld : MonoBehaviour, IInteractable
{
    [Header("References")]
    public SpriteRenderer rodRenderer;
    public GameObject biteIndicatorVFX;

    private int originalRodItemID;
    private float waitTimeMultiplier;
    private float rarityBonusRate;

    private float timer = 0f;
    private float targetBiteTime;

    private enum RodState { Waiting, Biting, Finished }
    private RodState currentState = RodState.Waiting;

    public void InitializeRod(int rodID, Sprite rodIcon, float waitMult, float rarityBonus)
    {
        originalRodItemID = rodID;

        if (rodRenderer != null && rodIcon != null)
        {
            rodRenderer.sprite = rodIcon;
        }

        waitTimeMultiplier = waitMult;
        rarityBonusRate = rarityBonus;

        targetBiteTime = Random.Range(10f, 20f) * waitTimeMultiplier;

        if (biteIndicatorVFX != null) biteIndicatorVFX.SetActive(false);
    }

    private void Update()
    {
        if (currentState == RodState.Waiting)
        {
            timer += Time.deltaTime;
            if (timer >= targetBiteTime)
            {
                FishBite();
            }
        }
    }

    private void FishBite()
    {
        currentState = RodState.Biting;

        if (biteIndicatorVFX != null) biteIndicatorVFX.SetActive(true);
        // SoundEffectManager.Play("FishBite", true);

        // Có thể thêm logic: Sau 5s nếu không kéo cần, cá sổng mất, quay lại trạng thái Waiting
    }

    public void Interact()
    {
        GameObject rodPrefab = ItemDictionary.Instance.GetItemPrefab(originalRodItemID);
        if (rodPrefab != null)
        {
            Item rodData = rodPrefab.GetComponent<Item>();
            InventoryController.Instance.PredictAddHarvestItem(rodData, 1);
        }

        if (currentState == RodState.Biting)
        {
            GiveReward();
        }
        else
        {
            GameNotify.Show("Thu hồi cần câu (chưa có cá).");
        }

        Destroy(gameObject);
    }

    private void GiveReward()
    {
        GameNotify.Show("Đã câu được cá!");

        // InventoryController.Instance.PredictAddHarvestItem(fishData, 1);
    }

    public bool CanInteract()
    {
        return true;
    }
}