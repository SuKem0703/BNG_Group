using UnityEngine;
using UnityEngine.Tilemaps;

public class DynamicSorting : MonoBehaviour
{
    [Header("Tên Layer")]
    [SerializeField] private string inFrontLayer = "Walk in front";
    [SerializeField] private string behindLayer = "Walk behind";

    [Header("Cài đặt Vùng đệm")]
    [SerializeField] private float yOffset = -0.2f;

    [Header("Renderers Cần Cập Nhật")]
    [SerializeField] private TilemapRenderer behindRenderer;
    [SerializeField] private TilemapRenderer frontRenderer;

    [Header("Y-Sort (Fix chồng chéo)")]
    [SerializeField] private int sortingPrecision = 100;
    void Awake()
    {
        if (behindRenderer == null)
        {
            Transform behindChild = transform.Find("Behind");
            if (behindChild != null)
                behindRenderer = behindChild.GetComponent<TilemapRenderer>();
        }
        if (frontRenderer == null)
        {
            Transform frontChild = transform.Find("Front");
            if (frontChild != null)
                frontRenderer = frontChild.GetComponent<TilemapRenderer>();
        }
        if (behindRenderer == null || frontRenderer == null)
        {
            Debug.LogError($"[{name}] Không tìm thấy TilemapRenderer cho Front/Behind.", this);
            enabled = false;
            return;
        }

        int frontBaseOrder = frontRenderer.sortingOrder;
        int behindBaseOrder = behindRenderer.sortingOrder;

        int newYSortOrder = Mathf.RoundToInt(transform.position.y * -sortingPrecision);

        behindRenderer.sortingOrder = behindBaseOrder + newYSortOrder;
        frontRenderer.sortingOrder = frontBaseOrder + newYSortOrder;
    }

    void OnEnable()
    {
        if (YSortController.Instance != null)
            YSortController.OnPlayerYChanged += HandleYChanged;
    }

    void OnDisable()
    {
        YSortController.OnPlayerYChanged -= HandleYChanged;
    }

    private void HandleYChanged(float playerY)
    {
        if (playerY < (transform.position.y + yOffset))
            SetSortingLayer(behindRenderer, inFrontLayer);
        else
            SetSortingLayer(behindRenderer, behindLayer);
    }

    private void SetSortingLayer(TilemapRenderer renderer, string layerName)
    {
        if (renderer.sortingLayerName != layerName)
            renderer.sortingLayerName = layerName;
    }
}