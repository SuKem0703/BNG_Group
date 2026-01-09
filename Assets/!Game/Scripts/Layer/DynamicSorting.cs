using UnityEngine;
using UnityEngine.Tilemaps;

public class DynamicSorting : MonoBehaviour
{
    [Header("Sorting Layer")]
    [SerializeField] private string inFrontLayer = "Walk in front";
    [SerializeField] private string behindLayer = "Walk behind";

    [Header("Y Offset")]
    [SerializeField] private float yOffset = -0.2f;

    [Header("Fade")]
    [Range(0f, 1f)][SerializeField] private float fadedAlpha = 0.7f;
    [SerializeField] private float normalAlpha = 1f;
    [SerializeField] private float fadeSpeed = 6f;

    [Header("Renderers")]
    [SerializeField] private TilemapRenderer frontRenderer;
    [SerializeField] private TilemapRenderer behindRenderer;

    private Tilemap _frontTilemap;
    private Tilemap _behindTilemap;
    private float _targetAlpha;

    [Header("Static Y Sort")]
    [SerializeField] private int sortingPrecision = 100;

    void Awake()
    {
        if (frontRenderer == null)
            frontRenderer = transform.Find("Front")?.GetComponent<TilemapRenderer>();

        if (behindRenderer == null)
            behindRenderer = transform.Find("Behind")?.GetComponent<TilemapRenderer>();

        if (frontRenderer == null || behindRenderer == null)
        {
            enabled = false;
            return;
        }

        _frontTilemap = frontRenderer.GetComponent<Tilemap>();
        _behindTilemap = behindRenderer.GetComponent<Tilemap>();

        _targetAlpha = normalAlpha;

        int sortOrder = Mathf.RoundToInt(transform.position.y * -sortingPrecision);
        frontRenderer.sortingOrder += sortOrder;
        behindRenderer.sortingOrder += sortOrder;
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

    void Update()
    {
        if (this == null) return;

        float current = _behindTilemap.color.a;
        if (Mathf.Abs(current - _targetAlpha) > 0.01f)
        {
            float newAlpha = Mathf.MoveTowards(current, _targetAlpha, fadeSpeed * Time.deltaTime);
            SetAlpha(newAlpha);
        }
    }

    // --- SORTING ---
    private void HandleYChanged(float playerY)
    {
        if (this == null) return;

        if (playerY < transform.position.y + yOffset)
            SetSortingLayer(inFrontLayer);
        else
            SetSortingLayer(behindLayer);
    }

    private void SetSortingLayer(string layer)
    {
        if (frontRenderer.sortingLayerName != layer)
            frontRenderer.sortingLayerName = layer;

        if (behindRenderer.sortingLayerName != layer)
            behindRenderer.sortingLayerName = layer;
    }

    // --- FADE API ---
    public void SetFade(bool fade)
    {
        _targetAlpha = fade ? fadedAlpha : normalAlpha;
    }

    private void SetAlpha(float alpha)
    {
        Color c;

        c = _frontTilemap.color;
        c.a = alpha;
        _frontTilemap.color = c;

        c = _behindTilemap.color;
        c.a = alpha;
        _behindTilemap.color = c;
    }
}
