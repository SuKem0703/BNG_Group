using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
public class ConfirmUIController : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI confirmText;
    public Button yesButton;
    public Button noButton;

    private UnityAction onYesAction;

    private void Awake()
    {
        if (confirmText == null)
        {
            confirmText = transform.FindDeepChild("ConfirmText").GetComponent<TextMeshProUGUI>();
            confirmText.text = "";
        }

        if (yesButton == null)
            yesButton = transform.FindDeepChild("YesButton").GetComponent<Button>();

        if (noButton == null)
            noButton = transform.FindDeepChild("NoButton").GetComponent<Button>();
    }
    public void Show(string message, UnityAction onYes)
    {
        confirmText.text = message;

        onYesAction = onYes;

        yesButton.onClick.RemoveAllListeners();
        yesButton.onClick.AddListener(OnYesClick);

        noButton.onClick.RemoveAllListeners();
        noButton.onClick.AddListener(OnNoClick);

        gameObject.SetActive(true);

        Transform background = transform.Find("BackGround");
        if (background != null)
        {
            Vector3 targetPosition = background.localPosition;
            RectTransform canvasRect = transform.root.GetComponent<RectTransform>();
            float canvasHeight = canvasRect != null ? canvasRect.rect.height : 1080f;

            background.localPosition = new Vector3(targetPosition.x, targetPosition.y + canvasHeight, targetPosition.z);
            background.DOLocalMove(targetPosition, 0.4f).SetEase(Ease.OutBack);
        }
        else
        {
            Debug.LogWarning("Không tìm thấy object 'BackGround' để chạy DOTween.");
        }
    }
    private void OnYesClick()
    {
        Transform background = transform.Find("BackGround");
        if (background != null)
        {
            RectTransform canvasRect = transform.root.GetComponent<RectTransform>();
            float canvasHeight = canvasRect != null ? canvasRect.rect.height : 1080f;
            Vector3 endPosition = new Vector3(background.localPosition.x, background.localPosition.y + canvasHeight, background.localPosition.z);

            background.DOLocalMove(endPosition, 0.3f)
                      .SetEase(Ease.InBack)
                      .OnComplete(() => {
                          onYesAction?.Invoke();
                          Destroy(gameObject);
                      });
        }
        else
        {
            onYesAction?.Invoke();
            Destroy(gameObject);
        }
    }

    private void OnNoClick()
    {
        Transform background = transform.Find("BackGround");
        if (background != null)
        {
            RectTransform canvasRect = transform.root.GetComponent<RectTransform>();
            float canvasHeight = canvasRect != null ? canvasRect.rect.height : 1080f;
            Vector3 endPosition = new Vector3(background.localPosition.x, background.localPosition.y + canvasHeight, background.localPosition.z);

            background.DOLocalMove(endPosition, 0.3f)
                      .SetEase(Ease.InBack)
                      .OnComplete(() => {
                          Destroy(gameObject);
                      });
        }
        else
        {
            Destroy(gameObject);
        }
    }
}