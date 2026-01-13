using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

public class StoryScrollController : MonoBehaviour
{
    public enum ScrollDirection { BottomToTop, TopToBottom }

    [Header("UI References")]
    [SerializeField] private RectTransform contentRect;
    [SerializeField] private TextMeshProUGUI textComponent;
    [SerializeField] private CanvasGroup canvasGroup;
    [SerializeField] private Image backgroundImage;

    [Header("Settings")]
    public ScrollDirection direction = ScrollDirection.BottomToTop;
    public float scrollSpeed = 50f;
    public float fastForwardMultiplier = 5f;
    public float paddingOffset = 100f;

    [Header("Audio")]
    public AudioClip scrollAudioClip;

    [Header("Save & Scene Logic")]
    public string uniqueID;
    public string sceneToLoad;

    private float contentHeight;
    private float stopPosition;
    private string finalID;

    // 👉 THÊM
    private bool isAppPaused = false;

    void Awake()
    {
        if (canvasGroup != null)
            canvasGroup.alpha = 0f;
        else if (textComponent != null)
            textComponent.alpha = 0f;
    }

    void Start()
    {
        finalID = !string.IsNullOrEmpty(uniqueID)
            ? uniqueID
            : $"{SceneManager.GetActiveScene().name}_StoryScroll";

        if (contentRect == null) contentRect = GetComponent<RectTransform>();
        if (textComponent == null) textComponent = GetComponent<TextMeshProUGUI>();

        if (!SaveController.IsDataLoaded)
            SaveController.OnDataLoaded += HandleDataLoaded;
        else
            HandleDataLoaded();
    }

    void OnDestroy()
    {
        SaveController.OnDataLoaded -= HandleDataLoaded;
        GameStateManager.EndLoading();
    }

    // ===============================
    // OS / APP PAUSE HANDLING
    // ===============================
    void OnApplicationFocus(bool hasFocus)
    {
        isAppPaused = !hasFocus;
    }

    void OnApplicationPause(bool pauseStatus)
    {
        isAppPaused = pauseStatus;
    }

    private void HandleDataLoaded()
    {
        SaveController.OnDataLoaded -= HandleDataLoaded;

        if (SaveController.Instance != null &&
            SaveController.Instance.IsCollected(SceneManager.GetActiveScene().name, finalID))
        {
            RestoreMapState();
            Destroy(gameObject);
            return;
        }

        StartCoroutine(PlaySequence());
    }

    IEnumerator PlaySequence()
    {
        // --- STEP 1: Chờ Chapter Intro ---
        var chapterIntro = FindFirstObjectByType<ChapterIntroSequence>();
        if (chapterIntro != null)
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            if (backgroundImage != null) backgroundImage.enabled = true;
            yield return new WaitUntil(() => chapterIntro == null);
        }

        // --- STEP 2: Setup ---
        GameStateManager.StartLoading();
        if (MapController.Instance != null)
            MapController.Instance.IsCutsceneMode = true;

        Canvas.ForceUpdateCanvases();
        yield return null;

        contentHeight = contentRect.rect.height;
        SetupStartPosition();

        if (textComponent != null)
            textComponent.alpha = 1f;

        // --- STEP 3: Audio ---
        if (scrollAudioClip != null)
            SoundEffectManager.PlayBGM(scrollAudioClip, false);

        // --- STEP 4: Fade In ---
        if (backgroundImage != null)
            backgroundImage.enabled = true;

        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0f;
            while (canvasGroup.alpha < 1f)
            {
                canvasGroup.alpha += Time.unscaledDeltaTime * 2f;
                yield return null;
            }
        }

        // --- STEP 5: Scroll ---
        while (true)
        {
            // ⛔ App bị pause → không update
            if (isAppPaused)
            {
                yield return null;
                continue;
            }

            bool isFast = Input.GetMouseButton(0) || Input.GetKey(KeyCode.Space);
            float speed = isFast ? scrollSpeed * fastForwardMultiplier : scrollSpeed;

            float move = speed * Time.unscaledDeltaTime;
            Vector2 pos = contentRect.anchoredPosition;

            if (direction == ScrollDirection.BottomToTop)
            {
                pos.y += move;
                if (pos.y >= stopPosition) break;
            }
            else
            {
                pos.y -= move;
                if (pos.y <= stopPosition) break;
            }

            contentRect.anchoredPosition = pos;
            yield return null;
        }

        // --- STEP 6: Fade Out ---
        if (canvasGroup != null)
        {
            while (canvasGroup.alpha > 0f)
            {
                canvasGroup.alpha -= Time.unscaledDeltaTime * 2f;
                yield return null;
            }
        }

        FinishSequence();
    }

    void SetupStartPosition()
    {
        RectTransform viewport = contentRect.parent as RectTransform;
        float viewHeight = viewport != null ? viewport.rect.height : Screen.height;

        if (direction == ScrollDirection.BottomToTop)
        {
            float startY = -(viewHeight / 2f) - paddingOffset;
            stopPosition = (viewHeight / 2f) + contentHeight;
            contentRect.anchoredPosition = new Vector2(0, startY);
        }
        else
        {
            float startY = (viewHeight / 2f) + contentHeight + paddingOffset;
            stopPosition = -(viewHeight / 2f) - paddingOffset;
            contentRect.anchoredPosition = new Vector2(0, startY);
        }
    }

    private void FinishSequence()
    {
        if (SaveController.Instance != null)
        {
            SaveController.Instance.MarkCollected(
                SceneManager.GetActiveScene().name, finalID);
            SaveController.Instance.TriggerAutoSave();
        }

        GameStateManager.EndLoading();

        if (!string.IsNullOrEmpty(sceneToLoad))
            SceneManager.LoadScene(sceneToLoad);
        else
        {
            RestoreMapState();
            Destroy(gameObject);
        }
    }

    private void RestoreMapState()
    {
        if (MapController.Instance == null) return;

        MapController.Instance.IsCutsceneMode = false;
        MapController.Instance.PlayMapBGM();
        MapController.Instance.ShowMapNameUI();
    }
}
