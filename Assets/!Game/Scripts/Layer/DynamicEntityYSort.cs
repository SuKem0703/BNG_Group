using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DynamicEntityYSort : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private int sortingPrecision = 100;
    [SerializeField] private float yOffset = 0f;

    [SerializeField] private int entityWeight = 3;

    [Header("Optimization")]
    [SerializeField] private bool onlySortWhenVisible = true;

    private SpriteRenderer spriteRenderer;
    private float lastY = float.NaN;
    private bool isVisible = true;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void OnBecameVisible() => isVisible = true;
    private void OnBecameInvisible() => isVisible = false;

    void LateUpdate()
    {
        if (onlySortWhenVisible && !isVisible) return;

        float currentY = transform.position.y + yOffset;

        if (!float.IsNaN(lastY) && Mathf.Abs(currentY - lastY) < 0.01f) return;

        lastY = currentY;
        spriteRenderer.sortingOrder = Mathf.RoundToInt(currentY * -sortingPrecision) + entityWeight;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 pivotPos = new Vector3(transform.position.x, transform.position.y + yOffset, 0);
        Gizmos.DrawWireSphere(pivotPos, 0.1f);
        Gizmos.DrawLine(pivotPos + Vector3.left * 0.2f, pivotPos + Vector3.right * 0.2f);
    }
}