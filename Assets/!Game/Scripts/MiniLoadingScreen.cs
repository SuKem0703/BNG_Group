using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniLoadingScreen : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private Image rotatingImage;
    [SerializeField] private List<Sprite> loadingSprites;
    [SerializeField] private Image blocker;

    [Header("Settings")]
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float spriteChangeInterval = 0.5f;
    [SerializeField] private float dotInterval = 0.5f;

    private int currentSpriteIndex = 0;
    private Coroutine spriteCoroutine;
    private Coroutine dotCoroutine;

    private void Awake()
    {
        FindObjectsFirst();

        // Đảm bảo blocker trong suốt nhưng vẫn chặn raycast
        if (blocker != null)
        {
            Color c = blocker.color;
            c.a = 0f;
            blocker.color = c;
        }
    }

    private void OnEnable()
    {
        if (blocker != null)
        {
            blocker.raycastTarget = true;
            blocker.gameObject.SetActive(true);
        }

        if (loadingText != null)
            loadingText.text = "Đang kết nối";

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
        if (loadingText == null)
        {
            Transform t = transform.Find("loadingText");
            if (t) loadingText = t.GetComponent<TMP_Text>();
            else Debug.LogWarning("[MiniLoadingScreen] Không tìm thấy loadingText!");
        }

        if (rotatingImage == null)
        {
            Transform t = transform.Find("rotatingImage");
            if (t) rotatingImage = t.GetComponent<Image>();
            else Debug.LogWarning("[MiniLoadingScreen] Không tìm thấy rotatingImage!");
        }

        if (blocker == null)
        {
            Transform t = transform.Find("blocker");
            if (t) blocker = t.GetComponent<Image>();
            else Debug.LogWarning("[MiniLoadingScreen] Không tìm thấy blocker!");
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

    private IEnumerator LoadingDotsRoutine()
    {
        int dotCount = 0;
        string baseText = "Đang kết nối";
        while (true)
        {
            dotCount = (dotCount % 3) + 1;
            if (loadingText != null)
                loadingText.text = baseText + new string('.', dotCount);
            yield return new WaitForSeconds(dotInterval);
        }
    }

    public void SetLoadingText(string text)
    {
        if (loadingText != null)
            loadingText.text = text;
    }
}
