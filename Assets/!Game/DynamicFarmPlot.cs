using UnityEngine;
using System.Collections.Generic;

public class FarmTileController : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showDebug = true;
    public float rayOffset = 0.6f;
    public float rayDistance = 0.1f;

    [Header("--- CẤU HÌNH OFFSET ---")]
    public Vector3 topOverlayOffset = new Vector3(-0.5f, 0.5f, 0f);
    public Vector3 rightOverlayOffset = new Vector3(0.5f, -0.5f, 0f);
    public Vector3 cornerOverlayOffset = new Vector3(0.5f, 0.5f, 0f);

    [Header("1. Base Sprites")]
    [Tooltip("Góc Dưới Trái (Bo tròn - Base riêng)")]
    public Sprite bottomLeftSprite;

    [Tooltip("Viền Trái (Base cho cạnh trái và góc trên trái)")]
    public Sprite[] leftEdgeSprites;

    [Tooltip("Viền Dưới (Base cho cạnh dưới và góc dưới phải)")]
    public Sprite[] bottomEdgeSprites;

    [Tooltip("Đất trung tâm")]
    public Sprite[] centerVariations;

    [Header("2. Overlay Sprites (Quan trọng)")]
    [Tooltip("Overlay đặc biệt cho Góc Trên Trái")]
    public Sprite topLeftOverlay;

    [Tooltip("Overlay đặc biệt cho Góc Dưới Phải")]
    public Sprite bottomRightOverlay;

    [Tooltip("Overlay viền trên thường")]
    public Sprite[] topEdgeOverlays;

    [Tooltip("Overlay viền phải thường")]
    public Sprite[] rightEdgeOverlays;

    [Tooltip("Overlay góc chéo (Cho góc trên phải)")]
    public Sprite topRightCornerOverlay;

    [Header("Settings")]
    public LayerMask plotLayer;
    [SerializeField] private SpriteRenderer _baseRenderer;
    private List<GameObject> _currentOverlays = new List<GameObject>();

    private void Awake()
    {
        if (_baseRenderer == null) _baseRenderer = GetComponentInChildren<SpriteRenderer>();
    }

    private void Start()
    {
        UpdateVisuals();
        NotifyNeighbors();
    }

    public void UpdateVisuals()
    {
        if (_baseRenderer == null) return;
        ClearOverlays();

        bool hasUp = HasNeighbor(Vector2.up);
        bool hasDown = HasNeighbor(Vector2.down);
        bool hasLeft = HasNeighbor(Vector2.left);
        bool hasRight = HasNeighbor(Vector2.right);

        if (!hasDown && !hasLeft)
        {
            _baseRenderer.sprite = bottomLeftSprite;
        }
        else if (!hasLeft)
        {
            _baseRenderer.sprite = GetRandomSprite(leftEdgeSprites);
        }
        else if (!hasDown)
        {
            _baseRenderer.sprite = GetRandomSprite(bottomEdgeSprites);
        }
        else
        {
            _baseRenderer.sprite = GetRandomSprite(centerVariations);
        }

        if (!hasUp)
        {
            if (!hasLeft)
            {
                CreateOverlay(topLeftOverlay, "Overlay_TopLeft", topOverlayOffset);
            }
            else
            {
                CreateOverlay(GetRandomSprite(topEdgeOverlays), "Overlay_Top", topOverlayOffset);
            }
        }

        if (!hasRight)
        {
            if (!hasDown)
            {
                CreateOverlay(bottomRightOverlay, "Overlay_BottomRight", rightOverlayOffset);
            }
            else
            {
                CreateOverlay(GetRandomSprite(rightEdgeOverlays), "Overlay_Right", rightOverlayOffset);
            }
        }

        if (!hasUp && !hasRight)
        {
            CreateOverlay(topRightCornerOverlay, "Overlay_TopRight_Corner", cornerOverlayOffset);
        }
    }

    private void CreateOverlay(Sprite sprite, string name, Vector3 offset)
    {
        if (sprite == null) return;
        GameObject overlayObj = new GameObject(name);
        overlayObj.transform.SetParent(transform);
        overlayObj.transform.localPosition = offset;

        SpriteRenderer sr = overlayObj.AddComponent<SpriteRenderer>();
        sr.sprite = sprite;
        sr.sortingLayerName = _baseRenderer.sortingLayerName;
        sr.sortingOrder = _baseRenderer.sortingOrder;

        _currentOverlays.Add(overlayObj);
    }

    private bool HasNeighbor(Vector2 dir)
    {
        Vector2 startPos = (Vector2)transform.position + (dir * rayOffset);
        RaycastHit2D hit = Physics2D.Raycast(startPos, dir, rayDistance, plotLayer);
        return hit.collider != null;
    }

    private void NotifyNeighbors()
    {
        UpdateNeighbor(Vector2.up); UpdateNeighbor(Vector2.down);
        UpdateNeighbor(Vector2.left); UpdateNeighbor(Vector2.right);
    }

    private void UpdateNeighbor(Vector2 dir)
    {
        Vector2 startPos = (Vector2)transform.position + (dir * rayOffset);
        RaycastHit2D hit = Physics2D.Raycast(startPos, dir, rayDistance, plotLayer);
        if (hit.collider != null)
        {
            var neighbor = hit.collider.GetComponent<FarmTileController>();
            if (neighbor != null) neighbor.UpdateVisuals();
        }
    }

    private Sprite GetRandomSprite(Sprite[] list)
    {
        if (list == null || list.Length == 0) return null;
        int seed = Mathf.Abs((int)(transform.position.x * 10 + transform.position.y * 100));
        System.Random rnd = new System.Random(seed);
        return list[rnd.Next(0, list.Length)];
    }

    private void ClearOverlays()
    {
        foreach (var obj in _currentOverlays) { if (obj != null) Destroy(obj); }
        _currentOverlays.Clear();
    }

    private void OnDrawGizmos()
    {
        if (!showDebug) return;
        DrawDebugRay(Vector2.up); DrawDebugRay(Vector2.down);
        DrawDebugRay(Vector2.left); DrawDebugRay(Vector2.right);
    }
    private void DrawDebugRay(Vector2 dir)
    {
        Vector2 startPos = (Vector2)transform.position + (dir * rayOffset);
        bool isHit = Physics2D.Raycast(startPos, dir, rayDistance, plotLayer);
        Gizmos.color = isHit ? Color.green : Color.red;
        Gizmos.DrawLine(startPos, startPos + (dir * rayDistance));
        Gizmos.DrawSphere(startPos, 0.05f);
    }
}