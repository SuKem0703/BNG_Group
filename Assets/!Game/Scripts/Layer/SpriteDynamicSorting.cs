using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteDynamicSorting : MonoBehaviour
{
    [Header("Renderers")]
    [SerializeField] private string inFrontLayer = "Walk in front";
    [SerializeField] private string behindLayer = "Walk behind";

    [Header("Y Offset")]
    [SerializeField] private float yOffset = -0.2f;

    [Header("Static Y Sort")]
    [SerializeField] private int sortingPrecision = 100;

    public int sortingBuffer = 0;

    private SpriteRenderer spriteRenderer;
    private bool isInitialized = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    public void InitSorting(int buffer)
    {
        sortingBuffer = buffer;

        int sortOrder = Mathf.RoundToInt(transform.position.y * -sortingPrecision) + sortingBuffer;
        spriteRenderer.sortingOrder = sortOrder;

        isInitialized = true;

        UpdateLayerImmediate();
    }

    void OnEnable()
    {
        if (YSortController.Instance != null)
            YSortController.OnPlayerYChanged += HandleYChanged;

        if (isInitialized) UpdateLayerImmediate();
    }

    void OnDisable()
    {
        if (YSortController.Instance != null)
            YSortController.OnPlayerYChanged -= HandleYChanged;
    }

    private void UpdateLayerImmediate()
    {
        if (YSortController.Instance == null) return;

        GameObject player = GameObject.FindGameObjectWithTag(YSortController.Instance.playerTag);
        if (player != null)
        {
            HandleYChanged(player.transform.position.y);
        }
    }

    private void HandleYChanged(float playerY)
    {
        if (this == null || transform == null) return;

        string targetLayer = (playerY < (transform.position.y + yOffset)) ? inFrontLayer : behindLayer;

        if (spriteRenderer.sortingLayerName != targetLayer)
        {
            spriteRenderer.sortingLayerName = targetLayer;
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