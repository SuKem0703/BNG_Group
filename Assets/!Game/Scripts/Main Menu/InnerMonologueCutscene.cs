using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

public class InnerMonologueCutscene : MonoBehaviour
{
    [Header("Display")]
    public TextMeshProUGUI monologueText;
    public float fadeDuration = 1f;
    public float displayDuration = 2.5f;

    [Header("Content")]
    [TextArea(2, 5)]
    public string[] monologueLines;

    [Header("Scene Logic")]
    [Tooltip("NẾU KHÔNG RỖNG: Tải scene này sau khi chơi. NẾU RỖNG: Cutscene chạy tại chỗ và tự hủy.")]
    public string sceneToLoad;

    [Header("Save State (cho cutscene tại chỗ)")]
    [Tooltip("Unique ID cho cutscene này. Nếu được đặt và 'Scene To Load' RỖNG, nó sẽ tự hủy nếu đã chơi.")]
    public string uniqueID;

    private bool hasDataCheckCompleted = false;

    private void OnEnable()
    {
        if (string.IsNullOrEmpty(sceneToLoad) && !string.IsNullOrEmpty(uniqueID))
        {
            SaveController.OnDataLoaded += HandleDataLoaded;

            if (SaveController.IsDataLoaded)
            {
                HandleDataLoaded();
            }
        }
    }

    private void OnDisable()
    {
        if (PauseController.IsGamePause)
        {
            PauseController.SetPause(false);
        }
        MenuController.CanOpenMenu = true;

        if (string.IsNullOrEmpty(sceneToLoad) && !string.IsNullOrEmpty(uniqueID))
        {
            SaveController.OnDataLoaded -= HandleDataLoaded;
        }
    }

    private void HandleDataLoaded()
    {
        if (hasDataCheckCompleted) return;
        hasDataCheckCompleted = true;

        if (string.IsNullOrEmpty(uniqueID))
            uniqueID = GenerateDeterministicID();

        var save = FindFirstObjectByType<SaveController>();

        if (save != null && save.IsCollected(SceneManager.GetActiveScene().name, uniqueID))
        {
            Destroy(gameObject);
        }
        else
        {
            StartCoroutine(PlayMonologue());
        }
    }

    IEnumerator PlayMonologue()
    {
        PauseController.SetPause(true);
        MenuController.CanOpenMenu = false;

        monologueText.color = new Color(monologueText.color.r, monologueText.color.g, monologueText.color.b, 0);

        foreach (string line in monologueLines)
        {
            monologueText.text = line;

            yield return StartCoroutine(FadeText(monologueText, 0, 1, fadeDuration));
            yield return new WaitForSecondsRealtime(displayDuration);

            yield return StartCoroutine(FadeText(monologueText, 1, 0, fadeDuration));
        }

        PauseController.SetPause(false);
        MenuController.CanOpenMenu = true;

        if (!string.IsNullOrEmpty(sceneToLoad))
        {
            SceneManager.LoadScene(sceneToLoad);
        }
        else
        {
            if (!string.IsNullOrEmpty(uniqueID))
            {
                var save = FindFirstObjectByType<SaveController>();
                if (save != null)
                {
                    save.MarkCollected(SceneManager.GetActiveScene().name, uniqueID);
                    save.SaveGame();
                }
                else
                {
                    Debug.LogError($"SaveController not found. Cutscene '{uniqueID}' sẽ chơi lại.", this);
                }
            }
            else
            {
                Debug.LogWarning($"Cutscene '{gameObject.name}' không có uniqueID. Nó sẽ chơi lại.", this);
            }

            Destroy(gameObject);
        }
    }

    IEnumerator FadeText(TextMeshProUGUI text, float fromAlpha, float toAlpha, float duration)
    {
        float elapsed = 0f;
        Color color = text.color;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            color.a = Mathf.Lerp(fromAlpha, toAlpha, t);
            text.color = color;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        color.a = toAlpha;
        text.color = color;
    }
    private string GenerateDeterministicID()
    {
        var p = transform.position;
        string id = $"{SceneManager.GetActiveScene().name}_Cutscene_{Mathf.RoundToInt(p.x * 100)}_{Mathf.RoundToInt(p.y * 100)}";
        Debug.LogWarning($"Cutscene '{gameObject.name}' không có uniqueID. Đã tạo: {id}", this);
        return id;
    }
}