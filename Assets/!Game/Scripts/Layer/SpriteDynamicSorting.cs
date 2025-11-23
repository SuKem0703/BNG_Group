using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteDynamicSorting : MonoBehaviour
{
    [Header("Tên Layer (Phải khớp trong Project Settings)")]
    [SerializeField] private string inFrontLayer = "Walk in front";
    [SerializeField] private string behindLayer = "Walk behind";

    [Header("Cài đặt Vùng đệm")]
    [Tooltip("Điểm mốc để so sánh với chân Player. Với ô 1x1 thường là thấp hơn tâm một chút.")]
    [SerializeField] private float yOffset = -0.2f;

    [Header("Y-Sort Tĩnh (Để xếp chồng các vật tĩnh với nhau)")]
    [SerializeField] private int sortingPrecision = 100;

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        int baseOrder = spriteRenderer.sortingOrder;
        int newYSortOrder = Mathf.RoundToInt(transform.position.y * -sortingPrecision);
        spriteRenderer.sortingOrder = baseOrder + newYSortOrder;
    }

    void OnEnable()
    {
        if (YSortController.Instance != null)
            YSortController.OnPlayerYChanged += HandleYChanged;
    }

    void OnDisable()
    {
        if (YSortController.Instance != null)
            YSortController.OnPlayerYChanged -= HandleYChanged;
    }

    private void HandleYChanged(float playerY)
    {
        if (this == null || transform == null) return;

        if (playerY < (transform.position.y + yOffset))
        {
            SetSortingLayer(inFrontLayer);
        }
        else
        {
            SetSortingLayer(behindLayer);
        }
    }

    private void SetSortingLayer(string layerName)
    {
        if (spriteRenderer.sortingLayerName != layerName)
        {
            spriteRenderer.sortingLayerName = layerName;
        }
    }
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(
            new Vector3(transform.position.x - 0.5f, transform.position.y + yOffset, 0),
            new Vector3(transform.position.x + 0.5f, transform.position.y + yOffset, 0)
        );
    }
}