using UnityEngine;
using UnityEngine.Tilemaps;
using System.Collections;

public class DynamicSorting : MonoBehaviour
{
    [Header("Static Y Sort")]
    [SerializeField] private int sortingPrecision = 100;
    public int sortingBuffer = 0;
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
    private bool _isInitialized = false;
    private Coroutine _fadeCoroutine;

    void Awake()
    {
        if (frontRenderer == null && behindRenderer == null)
        {
            enabled = false;
            return;
        }

        if (frontRenderer != null) _frontTilemap = frontRenderer.GetComponent<Tilemap>();
        if (behindRenderer != null) _behindTilemap = behindRenderer.GetComponent<Tilemap>();

        _targetAlpha = normalAlpha;
    }

    void Start()
    {
        if (!_isInitialized) InitSorting(sortingBuffer);
    }

    public void InitSorting(int buffer)
    {
        sortingBuffer = buffer;

        int baseSortOrder = Mathf.RoundToInt((transform.position.y + yOffset) * -sortingPrecision) + sortingBuffer;

        if (frontRenderer != null) frontRenderer.sortingOrder = baseSortOrder;
        if (behindRenderer != null) behindRenderer.sortingOrder = baseSortOrder;

        _isInitialized = true;
    }

    public void SetFade(bool fade)
    {
        _targetAlpha = fade ? fadedAlpha : normalAlpha;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeRoutine());
    }

    private IEnumerator FadeRoutine()
    {
        float currentAlpha = normalAlpha;

        if (_behindTilemap != null) currentAlpha = _behindTilemap.color.a;
        else if (_frontTilemap != null) currentAlpha = _frontTilemap.color.a;

        while (Mathf.Abs(currentAlpha - _targetAlpha) > 0.01f)
        {
            currentAlpha = Mathf.MoveTowards(currentAlpha, _targetAlpha, fadeSpeed * Time.deltaTime);
            SetAlpha(currentAlpha);
            yield return null;
        }

        SetAlpha(_targetAlpha);
        _fadeCoroutine = null;
    }

    private void SetAlpha(float alpha)
    {
        if (_frontTilemap != null)
        {
            Color frontC = _frontTilemap.color;
            frontC.a = alpha;
            _frontTilemap.color = frontC;
        }

        if (_behindTilemap != null)
        {
            Color behindC = _behindTilemap.color;
            behindC.a = alpha;
            _behindTilemap.color = behindC;
        }
    }
}