using UnityEngine;
using UnityEngine.UI;

public class EnemyHealthBar : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("Kéo ảnh 'Fill' (ảnh màu đỏ tươi hiển thị máu) vào đây.")]
    public Image healthFillImage;

    [Header("Settings")]
    [Tooltip("Khoảng cách thanh máu nằm trên đầu quái")]
    public Vector3 offset = new Vector3(0, 1.5f, 0);

    [Tooltip("Có ẩn thanh máu khi đầy không?")]
    public bool hideWhenFull = false;

    [Tooltip("Tốc độ cập nhật thanh máu (càng cao càng nhanh)")]
    public float lerpSpeed = 10f;

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

        UpdateHealthUI(true);
    }

    void LateUpdate()
    {
        if (enemyChase == null || healthFillImage == null) return;

        transform.position = enemyChase.transform.position + offset;
        if (mainCamera != null)
        {
            transform.rotation = Quaternion.LookRotation(transform.position - mainCamera.position);
        }

        UpdateHealthUI(false);
    }

    private void UpdateHealthUI(bool instant)
    {
        float targetFillAmount = (float)enemyChase.currentHealth / enemyChase.maxHealth;

        targetFillAmount = Mathf.Clamp01(targetFillAmount);

        if (instant)
        {
            healthFillImage.fillAmount = targetFillAmount;
        }
        else
        {
            healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, targetFillAmount, Time.deltaTime * lerpSpeed);
        }

        if (canvas != null)
        {
            bool shouldShow = enemyChase.currentHealth > 0;
            if (hideWhenFull && targetFillAmount >= 0.99f) shouldShow = false;
            canvas.enabled = shouldShow;
        }
    }
}