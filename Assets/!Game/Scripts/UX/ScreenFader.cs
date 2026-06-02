using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    private static ScreenFader _instance;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public static void FadeAndExecute(float fadeDuration, Action actionToExecuteInBlackScreen)
    {
        if (_instance == null)
        {
            GameObject faderObj = new GameObject("ScreenFaderSingleton");
            _instance = faderObj.AddComponent<ScreenFader>();
        }
        _instance.StartCoroutine(_instance.FadeRoutine(fadeDuration, actionToExecuteInBlackScreen));
    }

    private IEnumerator FadeRoutine(float duration, Action action)
    {
        GameStateManager.StartLoading();

        GameObject canvasObj = new GameObject("TemporaryFaderCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 9999;

        Image fadeImage = canvasObj.AddComponent<Image>();
        fadeImage.color = new Color(0, 0, 0, 0);

        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }
        fadeImage.color = Color.black;

        action?.Invoke();

        yield return new WaitForSeconds(0.2f);

        elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeImage.color = new Color(0, 0, 0, 1f - Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        Destroy(canvasObj);
        GameStateManager.EndLoading();
    }
}