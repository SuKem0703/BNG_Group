using System.Collections;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
public class ChestInteractable : MonoBehaviour, IInteractable, ITargetableInfo
{
    [Header("Chest Info")]
    public string chestName = "Rương gỗ";
    public Sprite chestIcon;

    [Header("Animation Settings")]
    public Sprite[] openFrames;
    public float frameDuration = 0.15f;

    [Header("Dissolve Settings")]
    [Tooltip("Thời gian chờ trước khi bắt đầu tan biến")]
    public float delayBeforeDissolve = 0.5f;
    [Tooltip("Thời gian hiệu ứng tan biến diễn ra")]
    public float dissolveDuration = 1.5f;

    private SpriteRenderer spriteRenderer;
    private BoxCollider2D boxCol;
    private bool isOpened = false;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        boxCol = GetComponent<BoxCollider2D>();
    }

    public bool CanInteract()
    {
        return !isOpened && GameStateManager.CanProcessInput() && SaveController.IsDataLoaded;
    }

    public void Interact()
    {
        if (isOpened) return;
        StartCoroutine(OpenChestSequence());
    }

    private IEnumerator OpenChestSequence()
    {
        isOpened = true;

        // Khóa input để người chơi không bấm lung tung
        GameStateManager.IsDialogueActive = true;

        // 1. Chạy hoạt ảnh mở nắp rương
        for (int i = 0; i < openFrames.Length; i++)
        {
            spriteRenderer.sprite = openFrames[i];
            yield return new WaitForSeconds(frameDuration);
        }

        // 2. Trao phần thưởng
        GiveReward();

        // 3. Delay một lúc để người chơi nhìn thấy rương đã mở
        yield return new WaitForSeconds(delayBeforeDissolve);

        // --- BẮT ĐẦU HIỆU ỨNG DISSOLVE ---

        // Tắt va chạm để không cản đường người chơi nữa
        if (boxCol != null) boxCol.enabled = false;

        // Lấy (clone) material hiện tại để không làm ảnh hưởng đến các rương khác
        Material chestMat = spriteRenderer.material;

        float elapsed = 0f;
        while (elapsed < dissolveDuration)
        {
            elapsed += Time.deltaTime;

            // Tính toán giá trị Dissolve chạy mượt từ 0 đến 1.1
            float currentAmount = Mathf.Lerp(0f, 1.1f, elapsed / dissolveDuration);

            // Gửi giá trị vào Shader (Chú ý: Đảm bảo Reference name trong Shader Graph là _DissolveAmount)
            chestMat.SetFloat("_DissolveAmount", currentAmount);

            yield return null;
        }

        // Đảm bảo giá trị cuối cùng chốt ở 1.1
        chestMat.SetFloat("_DissolveAmount", 1.1f);

        // Mở khóa input
        GameStateManager.IsDialogueActive = false;

        // 4. Phá hủy object
        Destroy(gameObject);
    }

    private void GiveReward()
    {
        Debug.Log($"[Chest] Đã mở {chestName}, nhận được vật phẩm!");
    }

    public TargetInfoData GetInfo()
    {
        return new TargetInfoData(chestName, chestIcon, "Mở rương", TargetType.NPC);
    }
}