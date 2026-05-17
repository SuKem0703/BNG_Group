using UnityEngine;

[ExecuteAlways]
public class FarmTileController : MonoBehaviour
{
    [Header("Debug Settings")]
    public bool showDebug = true;
    public float rayOffset = 0.6f;
    public float rayDistance = 0.1f;
    public LayerMask plotLayer;

    [Header("CẤU HÌNH SPRITE")]
    [Header("4 Góc")]
    public Sprite topLeftCorner;
    public Sprite topRightCorner;
    public Sprite bottomLeftCorner;
    public Sprite bottomRightCorner;

    [Header("4 Cạnh (Mỗi cạnh 2 biến thể)")]
    public Sprite[] topEdges;
    public Sprite[] bottomEdges;
    public Sprite[] leftEdges;
    public Sprite[] rightEdges;

    [Header("Trung tâm (4 biến thể)")]
    public Sprite[] centerTiles;

    [SerializeField] private SpriteRenderer _baseRenderer;
    private Vector3 _lastPosition;

    private void Awake()
    {
        if (_baseRenderer == null) _baseRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        _lastPosition = transform.position;
        UpdateVisuals();
        NotifyNeighbors();
    }

    private void Update()
    {
        if (!Application.isPlaying)
        {
            if (transform.position != _lastPosition)
            {
                _lastPosition = transform.position;
                UpdateVisuals();
                NotifyNeighbors();
            }
        }
    }

    public void UpdateVisuals()
    {
        if (_baseRenderer == null) return;

        bool hasUp = HasNeighbor(Vector2.up);
        bool hasDown = HasNeighbor(Vector2.down);
        bool hasLeft = HasNeighbor(Vector2.left);
        bool hasRight = HasNeighbor(Vector2.right);

        bool isWidthOne = !hasLeft && !hasRight;
        bool isHeightOne = !hasUp && !hasDown;

        if (isWidthOne || isHeightOne)
        {
            _baseRenderer.sprite = GetRandomSprite(centerTiles);
            return;
        }

        if (!hasUp && !hasLeft) _baseRenderer.sprite = topLeftCorner;
        else if (!hasUp && !hasRight) _baseRenderer.sprite = topRightCorner;
        else if (!hasDown && !hasLeft) _baseRenderer.sprite = bottomLeftCorner;
        else if (!hasDown && !hasRight) _baseRenderer.sprite = bottomRightCorner;
        else if (!hasUp) _baseRenderer.sprite = GetRandomSprite(topEdges);
        else if (!hasDown) _baseRenderer.sprite = GetRandomSprite(bottomEdges);
        else if (!hasLeft) _baseRenderer.sprite = GetRandomSprite(leftEdges);
        else if (!hasRight) _baseRenderer.sprite = GetRandomSprite(rightEdges);
        else _baseRenderer.sprite = GetRandomSprite(centerTiles);
    }

    private bool HasNeighbor(Vector2 dir)
    {
        Vector2 startPos = (Vector2)transform.position + (dir * rayOffset);
        RaycastHit2D hit = Physics2D.Raycast(startPos, dir, rayDistance, plotLayer);
        return hit.collider != null;
    }

    private void NotifyNeighbors()
    {
        UpdateNeighbor(Vector2.up);
        UpdateNeighbor(Vector2.down);
        UpdateNeighbor(Vector2.left);
        UpdateNeighbor(Vector2.right);
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
        if (list.Length == 1) return list[0];

        int seed = Mathf.Abs(Mathf.RoundToInt(transform.position.x * 10f) + Mathf.RoundToInt(transform.position.y * 100f));
        System.Random rnd = new System.Random(seed);
        return list[rnd.Next(0, list.Length)];
    }

    private void OnDrawGizmos()
    {
        if (!showDebug) return;
        DrawDebugRay(Vector2.up);
        DrawDebugRay(Vector2.down);
        DrawDebugRay(Vector2.left);
        DrawDebugRay(Vector2.right);
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