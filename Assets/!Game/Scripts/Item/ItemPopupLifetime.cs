using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CanvasGroup))]
public class ItemPopupLifetime : MonoBehaviour
{
    public float lifetime = 2.0f;
    public float fadeDuration = 1.0f;

    private float age = 0f;
    private CanvasGroup canvasGroup;
    private bool isFading = false;
    private float fadeTimer = 0f;

    void Start()
    {
        age = 0f;
        canvasGroup = GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1f;
    }

    void Update()
    {
        age += Time.unscaledDeltaTime;

        if (isFading || age > lifetime)
        {
            isFading = true;
            fadeTimer += Time.unscaledDeltaTime;

            canvasGroup.alpha = 1.0f - (fadeTimer / fadeDuration);

            if (canvasGroup.alpha <= 0)
            {
                Destroy(gameObject);
            }
        }
    }
    public void StartFadingNow()
    {
        isFading = true;
    }
}