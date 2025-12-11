using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class EnemyYSort : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Độ chính xác, PHẢI GIỐNG với Player (thường là 100)")]
    [SerializeField] private int sortingPrecision = 100;

    [Tooltip("Điều chỉnh điểm 'chân' của quái. Kéo xuống âm nếu quái bị Player đè lên quá sớm.")]
    [SerializeField] private float yOffset = 0f;

    [Tooltip("Có ép buộc chuyển về Layer Player không?")]
    [SerializeField] private bool forceSortingLayer = true;
    [SerializeField] private string targetLayerName = "Player";

    [Header("Optimization")]
    [Tooltip("Chỉ tính toán khi quái nằm trong màn hình camera")]
    [SerializeField] private bool onlySortWhenVisible = true;

    private SpriteRenderer spriteRenderer;
    private float lastY = float.NaN;
    private bool isVisible = true;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (forceSortingLayer && spriteRenderer != null)
        {
            spriteRenderer.sortingLayerName = targetLayerName;
        }
    }

    private void OnBecameVisible() => isVisible = true;
    private void OnBecameInvisible() => isVisible = false;

    void LateUpdate()
    {
        if (onlySortWhenVisible && !isVisible) return;
        if (spriteRenderer == null) return;

        float currentY = transform.position.y + yOffset;

        if (!float.IsNaN(lastY) && Mathf.Abs(currentY - lastY) < 0.01f)
            return;

        lastY = currentY;

        int newOrder = Mathf.RoundToInt(currentY * -sortingPrecision);

        if (spriteRenderer.sortingOrder != newOrder)
        {
            spriteRenderer.sortingOrder = newOrder;
        }
    }

    // Hàm hỗ trợ Debug trong Editor để nhìn thấy điểm pivot
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 pivotPos = transform.position;
        pivotPos.y += yOffset;
        Gizmos.DrawWireSphere(pivotPos, 0.1f);
        Gizmos.DrawLine(pivotPos + Vector3.left * 0.2f, pivotPos + Vector3.right * 0.2f);
    }
}