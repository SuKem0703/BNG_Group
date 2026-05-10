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
    private Sequence currentSequence;
    private Vector3 originalPos;
    private bool isInitialized = false;

    private void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        if (notifyText == null)
            notifyText = GetComponentInChildren<TextMeshProUGUI>(true);
    }

    private void InitBasePosition()
    {
        if (!isInitialized)
        {
            originalPos = transform.localPosition;
            isInitialized = true;
        }
    }

    void OnDestroy()
    {
        if (currentSequence != null) currentSequence.Kill();
        transform.DOKill();
    }

    public void Show(string message)
    {
        InitBasePosition();

        if (notifyText != null)
            notifyText.text = message;

        if (currentSequence != null && currentSequence.IsActive())
        {
            currentSequence.Kill();
        }

        transform.localScale = Vector3.one;
        gameObject.SetActive(true);

        currentSequence = DOTween.Sequence();

        if (canvasGroup.alpha > 0.1f)
        {
            currentSequence.Append(transform.DOPunchScale(new Vector3(0.1f, 0.1f, 0f), 0.2f, 10, 1f));
        }
        else
        {
            canvasGroup.alpha = 0f;
            transform.localPosition = originalPos - new Vector3(0, 50, 0);

            currentSequence.Append(canvasGroup.DOFade(1f, fadeDuration));
            currentSequence.Join(transform.DOLocalMove(originalPos, fadeDuration).SetEase(Ease.OutBack));
        }

        currentSequence.AppendInterval(displayDuration);

        currentSequence.Append(canvasGroup.DOFade(0f, fadeDuration));

        currentSequence.OnComplete(() =>
        {
            gameObject.SetActive(false);
        });
    }
}