using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;

    public float moveSpeed = 2f;
    public float lifetime = 1f;

    // [MỚI] Kích thước gốc để scale khi crit
    private Vector3 originalScale;

    private void Awake()
    {
        // Lấy component TextMeshPro
        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
        {
            Debug.LogError("DamagePopup: Không tìm thấy TextMeshPro component!");
            return;
        }

        originalScale = transform.localScale; // Lưu kích thước gốc
        disappearTimer = lifetime;
        textColor = textMesh.color;
    }

    public void Setup(int amount, DamageSourceType damageSourceType, bool isCritical = false)
    {
        if (textMesh == null) return;

        // 🟢 Xử lý hiển thị text
        if (damageSourceType == DamageSourceType.Heal || damageSourceType == DamageSourceType.MPRestore)
            textMesh.SetText(amount.ToString());
        else
            textMesh.SetText("-" + amount);

        Color newColor;

        if (isCritical)
        {
            ColorUtility.TryParseHtmlString("#FFD700", out newColor);

            transform.localScale = originalScale * 1.2f;

            textMesh.SetText(textMesh.text);
        }
        else
        {
            transform.localScale = originalScale;
            textMesh.fontStyle = FontStyles.Normal;

            switch (damageSourceType)
            {
                case DamageSourceType.Knight:
                    ColorUtility.TryParseHtmlString("#FF3B3B", out newColor);
                    break;
                case DamageSourceType.Mage:
                    ColorUtility.TryParseHtmlString("#3B8BFF", out newColor);
                    break;
                case DamageSourceType.Heal:
                    ColorUtility.TryParseHtmlString("#3BFF7E", out newColor);
                    break;
                case DamageSourceType.Enemy:
                    ColorUtility.TryParseHtmlString("#FF8C3B", out newColor);
                    break;
                case DamageSourceType.Environment:
                default:
                    newColor = textMesh.color;
                    break;
            }
        }

        textMesh.color = newColor;
        textColor = newColor;
    }

    private void Update()
    {
        if (textMesh == null) return;

        // 1. Di chuyển lên
        transform.position += new Vector3(0, moveSpeed * Time.deltaTime);

        // 2. Mờ dần (Fade out)
        disappearTimer -= Time.deltaTime;

        if (disappearTimer < 0) disappearTimer = 0;

        textColor.a = disappearTimer / lifetime; // Alpha giảm dần theo thời gian
        textMesh.color = textColor;

        // 3. Tự hủy khi hết giờ
        if (disappearTimer <= 0)
        {
            Destroy(gameObject);
        }
    }
}