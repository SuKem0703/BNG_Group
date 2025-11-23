using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MiniLoadingScreen : MonoBehaviour
{
    [Header("UI References (Gán trong Prefab)")]
    [SerializeField] private TMP_Text loadingText;
    [SerializeField] private Image rotatingImage;
    [SerializeField] private Image blocker;

    [Header("Settings")]
    [SerializeField] private List<Sprite> loadingSprites;
    [SerializeField] private float rotationSpeed = 120f;
    [SerializeField] private float spriteChangeInterval = 0.5f;
    [SerializeField] private float dotInterval = 0.5f;

    private int currentSpriteIndex = 0;

    private void OnEnable()
    {
        if (blocker != null)
        {
            blocker.raycastTarget = true;
            blocker.gameObject.SetActive(true);
        }

        if (loadingText != null) loadingText.text = "Đang kết nối";
        currentSpriteIndex = 0;
        if (rotatingImage != null && loadingSprites.Count > 0)
            rotatingImage.sprite = loadingSprites[0];

        StartCoroutine(SpriteRoutine());
        StartCoroutine(LoadingDotsRoutine());
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
    private IEnumerator SpriteRoutine()
    {
        if (loadingSprites == null || loadingSprites.Count == 0 || rotatingImage == null)
            yield break;

        WaitForSeconds wait = new WaitForSeconds(spriteChangeInterval); // Cache lại để tối ưu
        while (true)
        {
            yield return wait;
            currentSpriteIndex = (currentSpriteIndex + 1) % loadingSprites.Count;
            rotatingImage.sprite = loadingSprites[currentSpriteIndex];
        }
    }

    private IEnumerator LoadingDotsRoutine()
    {
        if (loadingText == null) yield break;

        int dotCount = 0;
        string baseText = loadingText.text;
        WaitForSeconds wait = new WaitForSeconds(dotInterval);

        while (true)
        {
            dotCount = (dotCount % 3) + 1;
            loadingText.text = baseText + new string('.', dotCount);
            yield return wait;
        }
    }

    public void SetLoadingText(string text)
    {
        if (loadingText != null)
        {
            loadingText.text = text;
            StopAllCoroutines();
            StartCoroutine(SpriteRoutine());
            StartCoroutine(LoadingDotsRoutine());
        }
    }
}