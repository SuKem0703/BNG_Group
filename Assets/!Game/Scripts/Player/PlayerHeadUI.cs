using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class PlayerHeadUI : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerCore core;
    public GameObject uiCanvasObject;

    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public Image healthFillImage;
    public Image ghostFillImage;

    [Header("Settings")]
    public bool hideWhenFull = false;
    public float lerpSpeed = 10f;

    [Header("Ghost Bar Settings")]
    public float ghostLerpSpeed = 3f;
    public float ghostDelay = 0.5f;

    [Header("Minimap Settings")]
    public SpriteRenderer minimapSpriteRenderer;
    private Color localColor = Color.green;
    private Color remoteColor = Color.blue;

    private float timeSinceLastHit;
    private float lastTargetFillAmount = -1f;
    private Transform mainCamera;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        mainCamera = Camera.main != null ? Camera.main.transform : null;

        if (minimapSpriteRenderer != null)
        {
            minimapSpriteRenderer.color = IsOwner ? localColor : remoteColor;
        }

        if (IsOwner)
        {
            if (uiCanvasObject != null) uiCanvasObject.SetActive(false);
        }
    }

    void LateUpdate()
    {
        if (IsOwner || core == null || core.playerStats == null || core.playerVitals == null) return;

        if (mainCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.position);
        }

        string pName = core.playerStats.netUsername.Value.ToString();
        if (nameText.text != pName)
        {
            nameText.text = pName;
        }

        bool isKnight = true;
        if (core.classController != null && core.classController.mageObject != null)
        {
            isKnight = core.classController.knightObject.activeInHierarchy;
        }

        float currentHP = isKnight ? core.playerVitals.netKnightHealth.Value : core.playerVitals.netMageHealth.Value;
        float maxHP = isKnight ? core.playerVitals.netMaxKnightHP.Value : core.playerVitals.netMaxMageHP.Value;
        float targetFillAmount = maxHP > 0 ? Mathf.Clamp01(currentHP / maxHP) : 0f;

        bool isFirstFrame = lastTargetFillAmount < 0f;

        UpdateHealthUI(targetFillAmount, isFirstFrame, currentHP);
    }

    private void UpdateHealthUI(float targetFillAmount, bool instant, float currentHP)
    {
        if (instant)
        {
            healthFillImage.fillAmount = targetFillAmount;
            if (ghostFillImage != null) ghostFillImage.fillAmount = targetFillAmount;
            lastTargetFillAmount = targetFillAmount;
        }
        else
        {
            if (targetFillAmount < lastTargetFillAmount)
            {
                timeSinceLastHit = 0f;
                lastTargetFillAmount = targetFillAmount;
            }
            else if (targetFillAmount > lastTargetFillAmount)
            {
                lastTargetFillAmount = targetFillAmount;
            }

            healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, targetFillAmount, Time.deltaTime * lerpSpeed);

            if (ghostFillImage != null)
            {
                if (ghostFillImage.fillAmount > targetFillAmount)
                {
                    timeSinceLastHit += Time.deltaTime;
                    if (timeSinceLastHit > ghostDelay)
                    {
                        ghostFillImage.fillAmount = Mathf.Lerp(ghostFillImage.fillAmount, targetFillAmount, Time.deltaTime * ghostLerpSpeed);
                    }
                }
                else
                {
                    ghostFillImage.fillAmount = targetFillAmount;
                }
            }
        }

        if (uiCanvasObject != null)
        {
            bool shouldShow = currentHP > 0;
            if (hideWhenFull && targetFillAmount >= 0.99f) shouldShow = false;

            if (uiCanvasObject.activeSelf != shouldShow)
            {
                uiCanvasObject.SetActive(shouldShow);
            }
        }
    }
}