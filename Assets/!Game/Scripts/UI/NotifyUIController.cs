using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;

public class NotifyUIController : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI notifyText;
    public Button okButton;

    private UnityAction onOkAction;

    private void Awake()
    {
        if (notifyText == null)
        {
            notifyText = transform.FindDeepChild("NotifyText").GetComponent<TextMeshProUGUI>();
            notifyText.text = "";
        }

        if (okButton == null)
            okButton = transform.FindDeepChild("OKButton").GetComponent<Button>();
    }

    public void Show(string message, UnityAction onOk = null)
    {
        notifyText.text = message;
        onOkAction = onOk;

        okButton.onClick.RemoveAllListeners();
        okButton.onClick.AddListener(OnOkClick);

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
    }

    private void OnOkClick()
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
                          onOkAction?.Invoke();
                          Destroy(gameObject);
                      });
        }
        else
        {
            onOkAction?.Invoke();
            Destroy(gameObject);
        }
    }
}