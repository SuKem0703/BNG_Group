using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class ChapterIntroSequence : MonoBehaviour
{
    [Header("Display")]
    public Image backgroundImage;
    public TextMeshProUGUI monologueText;

    [Header("Settings")]
    public float fadeDuration = 1f;
    public float displayDuration = 2.5f;

    [Header("Audio")]
    public AudioClip introAudioClip;

    [Header("Content")]
    [TextArea(2, 5)]
    public string[] monologueLines;

    [Header("Scene Logic")]
    public string sceneToLoad;
    public string uniqueID;

    private bool hasDataCheckCompleted = false;

    private void Awake()
    {
        var map = FindFirstObjectByType<MapController>();
        if (map != null) map.IsCutsceneMode = true;
    }

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
        {
            SaveController.OnDataLoaded += HandleDataLoaded;
            if (SaveController.IsDataLoaded) HandleDataLoaded();
        }
        else
        {
            StartCoroutine(PlaySequence());
        }
    }

    private void OnDisable()
    {
        if (string.IsNullOrEmpty(sceneToLoad))
            SaveController.OnDataLoaded -= HandleDataLoaded;

        GameStateManager.EndLoading();
    }

    private void HandleDataLoaded()
    {
        if (hasDataCheckCompleted) return;
        hasDataCheckCompleted = true;

        if (string.IsNullOrEmpty(uniqueID)) uniqueID = GenerateDeterministicID();

        var save = SaveController.Instance;
        if (save != null && save.IsCollected(SceneManager.GetActiveScene().name, uniqueID))
        {
            RestoreMapState();
            Destroy(gameObject);
        }
        else
        {
            StartCoroutine(PlaySequence());
        }
    }

    IEnumerator PlaySequence()
    {
        GameStateManager.StartLoading();

        if (introAudioClip != null)
            SoundEffectManager.PlayBGM(introAudioClip, false);
        else
            SoundEffectManager.StopBGM();

        if (monologueText != null) SetAlpha(monologueText, 0);

        if (backgroundImage != null) SetAlpha(backgroundImage, 1);

        for (int i = 0; i < monologueLines.Length; i++)
        {
            if (monologueText != null) monologueText.text = monologueLines[i];

            yield return StartCoroutine(FadeUI(monologueText, 0, 1, fadeDuration));
            yield return new WaitForSecondsRealtime(displayDuration);
            yield return StartCoroutine(FadeUI(monologueText, 1, 0, fadeDuration));
        }

        if (backgroundImage != null) SetAlpha(backgroundImage, 0);

        yield return StartCoroutine(SmoothExit());
    }

    IEnumerator SmoothExit()
    {
        if (monologueText != null) SetAlpha(monologueText, 0);
        if (backgroundImage != null) SetAlpha(backgroundImage, 0);

        GameStateManager.EndLoading();
        RestoreMapState();
        SaveDataLogic();

        yield return null;

        if (!string.IsNullOrEmpty(sceneToLoad))
            SceneManager.LoadScene(sceneToLoad);
        else
            Destroy(gameObject);
    }

    IEnumerator FadeUI(Graphic uiElement, float fromAlpha, float toAlpha, float duration)
    {
        if (uiElement == null) yield break;

        float elapsed = 0f;
        SetAlpha(uiElement, fromAlpha);

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            SetAlpha(uiElement, Mathf.Lerp(fromAlpha, toAlpha, t));

            if (toAlpha == 0 && uiElement.color.a < 0.01f) break;

            yield return null;
        }
        SetAlpha(uiElement, toAlpha);
    }

    private void SetAlpha(Graphic uiElement, float alpha)
    {
        if (uiElement == null) return;
        Color c = uiElement.color;
        c.a = alpha;
        uiElement.color = c;
    }

    private void SaveDataLogic()
    {
        if (!string.IsNullOrEmpty(uniqueID))
        {
            var save = SaveController.Instance;
            if (save != null)
            {
                save.MarkCollected(SceneManager.GetActiveScene().name, uniqueID);
                save.TriggerAutoSave();
            }
        }
    }

    private void RestoreMapState()
    {
        if (MapController.Instance != null)
        {
            MapController.Instance.IsCutsceneMode = false;
            MapController.Instance.PlayMapBGM();
        }
    }

    private string GenerateDeterministicID()
    {
        var p = transform.position;
        return $"{SceneManager.GetActiveScene().name}_Chapter_{Mathf.RoundToInt(p.x * 100)}_{Mathf.RoundToInt(p.y * 100)}";
    }
}