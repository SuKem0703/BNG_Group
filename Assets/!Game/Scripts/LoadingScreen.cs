using NUnit.Framework.Internal;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private TMP_Text tipText;
    [SerializeField] private Image rotatingImage;
    [SerializeField] private List<Sprite> loadingSprites;
    [SerializeField] private Image blocker;

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float spriteChangeInterval = 0.5f;
    [SerializeField] private float tipChangeInterval = 4f;
    [SerializeField] private float dotInterval = 0.5f;

    private int currentSpriteIndex = 0;
    private Coroutine tipCoroutine;
    private Coroutine spriteCoroutine;
    private Coroutine dotCoroutine;

    private void Awake()
    {
        FindObjectsFirst();
    }

    private void OnEnable()
    {
        if (blocker != null)
        {
            blocker.raycastTarget = true;
            blocker.gameObject.SetActive(true);
        }

        loadingText.text = "Đang kết nối";
        ChangeTip();

        tipCoroutine = StartCoroutine(TipRoutine());
        spriteCoroutine = StartCoroutine(SpriteRoutine());
        dotCoroutine = StartCoroutine(LoadingDotsRoutine());
    }

    private void OnDisable()
    {
        StopAllCoroutines();

        if (blocker != null)
        {
            blocker.raycastTarget = false;
            blocker.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        if (rotatingImage != null)
        {
            rotatingImage.transform.Rotate(Vector3.forward * -rotationSpeed * Time.unscaledDeltaTime);
        }
    }

    private void FindObjectsFirst()
    {
        // Dò tự động, không crash nếu thiếu
        if (loadingText == null)
        {
            Transform t = transform.Find("loadingText");
            if (t) loadingText = t.GetComponent<TMP_Text>();
            else Debug.LogWarning("[LoadingScreen] Không tìm thấy loadingText trong hierarchy!");
        }

        if (tipText == null)
        {
            Transform t = transform.Find("tipText");
            if (t) tipText = t.GetComponent<TMP_Text>();
            else Debug.LogWarning("[LoadingScreen] Không tìm thấy tipText trong hierarchy!");
        }

        if (rotatingImage == null)
        {
            Transform t = transform.Find("rotatingImage");
            if (t) rotatingImage = t.GetComponent<Image>();
            else Debug.LogWarning("[LoadingScreen] Không tìm thấy rotatingImage trong hierarchy!");
        }

        if (blocker == null)
        {
            Transform t = transform.Find("blocker");
            if (t) blocker = t.GetComponent<Image>();
            else Debug.LogWarning("[LoadingScreen] Không tìm thấy blocker trong hierarchy!");
        }
    }

    private IEnumerator SpriteRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(spriteChangeInterval);
            if (loadingSprites.Count > 0 && rotatingImage != null)
            {
                currentSpriteIndex = (currentSpriteIndex + 1) % loadingSprites.Count;
                rotatingImage.sprite = loadingSprites[currentSpriteIndex];
            }
        }
    }

    private IEnumerator TipRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(tipChangeInterval);
            ChangeTip();
        }
    }

    private IEnumerator LoadingDotsRoutine()
    {
        int dotCount = 0;
        string baseText = "Đang kết nối";
        while (true)
        {
            dotCount = (dotCount % 3) + 1;
            loadingText.text = baseText + new string('.', dotCount);
            yield return new WaitForSeconds(dotInterval);
        }
    }

    private void ChangeTip()
    {
        if (tipText != null)
        {
            tipText.text = TipData.GetRandomTip();
        }
    }

    public void SetLoadingText(string text)
    {
        if (loadingText != null)
            loadingText.text = text;
    }
}
