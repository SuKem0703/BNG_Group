using System.Collections;
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

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class Chest : MonoBehaviour, IInteractable, ITargetableInfo
{
    [Header("Chest Identity")]
    [field: SerializeField] public string ChestID { get; private set; }
    public string chestName = "";
    public Sprite chestIcon;

    [Header("Item Drop Settings")]
    public GameObject itemPrefab;
    public RarityMode rarityMode = RarityMode.Random;
    public ItemRarity fixedRarity = ItemRarity.Common;
    public QualityFactorMode qualityMode = QualityFactorMode.Random;

    [Header("Animation Settings")]
    public Sprite[] openFrames;
    public float frameDuration = 0.15f;

    [Header("Dissolve Settings")]
    public float delayBeforeDissolve = 0.5f;
    public float dissolveDuration = 1.5f;

    [field: SerializeField] public bool IsOpened { get; private set; }

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCol;
    private bool isProcessing = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCol = GetComponent<BoxCollider2D>();

        if (string.IsNullOrEmpty(ChestID))
        {
            ChestID = GlobalHelper.GenerateUniqueID(gameObject);
        }
    }

    public bool CanInteract()
    {
        return !IsOpened && !isProcessing && GameStateManager.CanProcessInput() && SaveController.IsDataLoaded;
    }

    public void Interact()
    {
        if (!CanInteract()) return;
        StartCoroutine(OpenChestSequence());
    }

    private IEnumerator OpenChestSequence()
    {
        isProcessing = true;
        IsOpened = true;

        GameStateManager.IsDialogueActive = true;
        SoundEffectManager.Play("ChestOpen");

        if (openFrames != null && openFrames.Length > 0)
        {
            for (int i = 0; i < openFrames.Length; i++)
            {
                spriteRenderer.sprite = openFrames[i];
                yield return new WaitForSeconds(frameDuration);
            }
        }

        GiveReward();

        yield return new WaitForSeconds(delayBeforeDissolve);

        GameStateManager.IsDialogueActive = false;
        yield return StartCoroutine(DissolveRoutine());

        Destroy(gameObject);
    }

    private void GiveReward()
    {
        if (itemPrefab)
        {
            GameObject droppedItem = Instantiate(itemPrefab, transform.position + Vector3.down, Quaternion.identity);

            Item item = droppedItem.GetComponent<Item>();
            if (item != null)
            {
                item.rarity = (rarityMode == RarityMode.Fixed) ? fixedRarity : ItemGenerationHelper.GetRandomRarity();
                item.qualityFactor = (qualityMode == QualityFactorMode.Max) ? 1f : ItemGenerationHelper.GetWeightedQualityFactor();
            }

            BounceEffect bounce = droppedItem.GetComponent<BounceEffect>();
            if (bounce != null)
            {
                bounce.StartBounce();
            }

            SaveController saveController = FindFirstObjectByType<SaveController>();
            if (saveController != null)
            {
                saveController.SaveGame();
            }
        }
    }

    public void SetOpened(bool opened)
    {
        IsOpened = opened;
        if (IsOpened)
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator DissolveRoutine()
    {
        if (boxCol != null) boxCol.enabled = false;

        Material chestMat = spriteRenderer.material;
        float elapsed = 0f;

        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;
            float currentAmount = Mathf.Lerp(0f, 1.1f, elapsed / dissolveDuration);
            chestMat.SetFloat("_DissolveAmount", currentAmount);
            yield return null;
        }

        chestMat.SetFloat("_DissolveAmount", 1.1f);
    }

    public TargetInfoData GetInfo()
    {
        return new TargetInfoData(chestName, chestIcon, "Mở rương", TargetType.NPC);
    }
}