using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI References")]
    public Image healthFillImage;
    public Image ghostFillImage;

    [Tooltip("Có ẩn thanh máu khi đầy không?")]
    public bool hideWhenFull = false;

    public float lerpSpeed = 10f;

    [Header("Ghost Bar Settings")]
    public float ghostLerpSpeed = 3f;
    public float ghostDelay = 0.5f;
    private float timeSinceLastHit;

    private float lastTargetFillAmount = 1f;

    private Enemy enemyChase;
    private Transform mainCamera;
    private Canvas canvas;

    void Start()
    {
        enemyChase = GetComponentInParent<Enemy>();

        if (enemyChase != null && enemyChase.enemyRank == EnemyRank.Boss)
        {
            Destroy(gameObject);
            return;
        }

        canvas = GetComponent<Canvas>();
        mainCamera = Camera.main.transform;

        if (healthFillImage == null)
        {
            Debug.LogError($"Chưa gán Health Fill Image vào script trên {gameObject.name}!");
            enabled = false;
            return;
        }

        if (healthFillImage.type != Image.Type.Filled)
        {
            Debug.LogWarning($"Ảnh {healthFillImage.name} chưa chuyển Image Type sang 'Filled'. Script sẽ tự chuyển.");
            healthFillImage.type = Image.Type.Filled;
            healthFillImage.fillMethod = Image.FillMethod.Horizontal;
        }

        if (ghostFillImage != null && ghostFillImage.type != Image.Type.Filled)
        {
            ghostFillImage.type = Image.Type.Filled;
            ghostFillImage.fillMethod = Image.FillMethod.Horizontal;
        }

        if (enemyChase != null)
        {
            lastTargetFillAmount = (float)enemyChase.netHealth.Value / enemyChase.maxHealth;
        }

        UpdateHealthUI(true);
    }

    void LateUpdate()
    {
        if (enemyChase == null || healthFillImage == null) return;

        if (mainCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.position);
        }

        UpdateHealthUI(false);
    }

    private void UpdateHealthUI(bool instant)
    {
        float targetFillAmount = (float)enemyChase.netHealth.Value / enemyChase.maxHealth;
        targetFillAmount = Mathf.Clamp01(targetFillAmount);

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

        if (canvas != null)
        {
            bool shouldShow = enemyChase.netHealth.Value > 0;
            if (hideWhenFull && targetFillAmount >= 0.99f) shouldShow = false;
            canvas.enabled = shouldShow;
        }
    }
}