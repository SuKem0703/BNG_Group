using UnityEngine;
using TMPro;

public class DamagePopup : MonoBehaviour
{
    private TextMeshPro textMesh;
    private float disappearTimer;
    private Color textColor;

    public float moveSpeed = 2f;
    public float lifetime = 1f;

    private Vector3 originalScale;
    private bool isInitialized = false;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        if (isInitialized) return;

        textMesh = GetComponent<TextMeshPro>();
        if (textMesh == null)
        {
            Debug.LogError("DamagePopup: Không tìm thấy TextMeshPro component!");
            return;
        }

        originalScale = transform.localScale;
        isInitialized = true;
    }

    public void Setup(int amount, DamageSourceType damageSourceType, bool isCritical = false)
    {
        Init();

        disappearTimer = lifetime;

        if (damageSourceType == DamageSourceType.Heal || damageSourceType == DamageSourceType.MPRestore)
            textMesh.SetText(amount.ToString());
        else
            textMesh.SetText("-" + amount);

        Color newColor;

        if (isCritical)
        {
            ColorUtility.TryParseHtmlString("#FFD700", out newColor);
            transform.localScale = originalScale * 1.2f;
            textMesh.fontStyle = FontStyles.Bold;
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
                    newColor = Color.white;
                    break;
            }
        }

        newColor.a = 1f;
        textMesh.color = newColor;
        textColor = newColor;
    }

    private void Update()
    {
        if (textMesh == null) return;

        transform.position += new Vector3(0, moveSpeed * Time.deltaTime);

        disappearTimer -= Time.deltaTime;

        if (disappearTimer < 0) disappearTimer = 0;

        textColor.a = disappearTimer / lifetime;
        textMesh.color = textColor;

        if (disappearTimer <= 0)
        {
            if (DamagePopupPool.Instance != null)
            {
                DamagePopupPool.Instance.ReturnPopup(this);
            }
            else
            {
                Destroy(gameObject);
            }
        }
    }
}