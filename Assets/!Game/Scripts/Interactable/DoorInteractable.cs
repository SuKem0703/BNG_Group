using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class DoorInteractable : MonoBehaviour, IInteractable
{
    [Header("Cài đặt Trạng thái")]
    public bool isOpen = false;

    [Header("Hình ảnh")]
    public SpriteRenderer spriteRenderer;
    public Sprite openSprite;
    public Sprite closedSprite;

    [Header("Blocker Collider")]
    public GameObject closedBlocker;
    public GameObject openedBlocker;

    [Header("Cài đặt Resize Player")]
    private readonly Vector2 TARGET_SIZE = new Vector2(0.4f, 0.7f);

    private Vector2 originalSizeCache;
    private bool hasCachedSize = false;

    private void Start()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;

        UpdateDoorState();
    }

    // ==================================================
    // IInteractable Implementation
    // ==================================================
    public void Interact()
    {
        isOpen = !isOpen;
        UpdateDoorState();
        // SoundEffectManager.Play(isOpen ? "DoorOpen" : "DoorClose");
    }

    public bool CanInteract() => true;

    // ==================================================
    // Logic Đóng/Mở
    // ==================================================
    private void UpdateDoorState()
    {
        if (spriteRenderer != null)
            spriteRenderer.sprite = isOpen ? openSprite : closedSprite;

        if (closedBlocker != null)
            closedBlocker.SetActive(!isOpen);

        if (openedBlocker != null)
            openedBlocker.SetActive(isOpen);
    }

    // ==================================================
    // Logic Resize (Xử lý ngay trên Trigger của Cha)
    // ==================================================

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (PlayerStats.Instance == null) return;

        CapsuleCollider2D playerCol = PlayerStats.Instance.playerCollider;
        if (playerCol == null) return;

        // Lưu size gốc
        if (!hasCachedSize)
        {
            originalSizeCache = playerCol.size;
            hasCachedSize = true;
        }

        playerCol.size = TARGET_SIZE;
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player")) return;
        if (PlayerStats.Instance == null) return;

        CapsuleCollider2D playerCol = PlayerStats.Instance.playerCollider;
        if (playerCol == null) return;

        if (hasCachedSize)
        {
            playerCol.size = originalSizeCache;
        }
    }
}