using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PlayerYSort : MonoBehaviour
{
    [SerializeField] private int sortingPrecision = 100;
    [Tooltip("Điều chỉnh 'chân' của Player")]
    [SerializeField] private float yOffset = 0f;

    [Tooltip("Tùy chọn: đặt order gốc, nếu là 0 thì sử dụng renderer hiện tại")]
    [SerializeField] private int baseOrder = 0;

    private SpriteRenderer spriteRenderer;
    private float lastY = float.NaN;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
        {
            Debug.LogWarning("PlayerYSort yêu cầu SpriteRenderer.", this);
            enabled = false;
            return;
        }

        if (baseOrder == 0)
            baseOrder = spriteRenderer.sortingOrder;
    }

    void LateUpdate()
    {
        float newY = transform.position.y + yOffset;

        if (!float.IsNaN(lastY) && Mathf.Abs(newY - lastY) < 0.01f)
            return;

        lastY = newY;

        int newOrder = baseOrder + Mathf.RoundToInt(newY * -sortingPrecision);
        if (spriteRenderer.sortingOrder != newOrder)
            spriteRenderer.sortingOrder = newOrder;
    }
}