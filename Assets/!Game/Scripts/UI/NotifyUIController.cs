using TMPro;
using UnityEngine;
using DG.Tweening;

[RequireComponent(typeof(CanvasGroup))]
public class NotifyUIController : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI notifyText;

    [Header("Settings")]
    public float displayDuration = 2.0f;
    public float fadeDuration = 0.5f;

    private CanvasGroup canvasGroup;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (notifyText == null)
        {
            notifyText = GetComponentInChildren<TextMeshProUGUI>(true);
        }
    }
    void OnDestroy()
    {
        transform.DOKill();
    }
    public void Show(string message)
    {
        if (notifyText != null)
        {
            notifyText.text = message;
        }

        gameObject.SetActive(true);

        canvasGroup.alpha = 0f;

        Vector3 originalPos = transform.localPosition;
        transform.localPosition = originalPos - new Vector3(0, 50, 0);

        Sequence mySequence = DOTween.Sequence();

        mySequence.Append(canvasGroup.DOFade(1f, fadeDuration));
        mySequence.Join(transform.DOLocalMove(originalPos, fadeDuration).SetEase(Ease.OutBack));

        mySequence.AppendInterval(displayDuration);

        mySequence.Append(canvasGroup.DOFade(0f, fadeDuration));

        mySequence.OnComplete(() =>
        {
            Destroy(gameObject);
        });
    }
}