using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SpriteDynamicSorting : MonoBehaviour
{
    [Header("Static Y Sort")]
    [SerializeField] private int sortingPrecision = 100;
    public int sortingBuffer = 0;
    [SerializeField] private float yOffset = -0.2f;

    private SpriteRenderer spriteRenderer;
    private bool _isInitialized = false;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        if (!_isInitialized) InitSorting(sortingBuffer);
    }

    public void InitSorting(int buffer)
    {
        sortingBuffer = buffer;

        int baseSortOrder = Mathf.RoundToInt((transform.position.y + yOffset) * -sortingPrecision) + sortingBuffer;
        spriteRenderer.sortingOrder = baseSortOrder;

        _isInitialized = true;
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