using UnityEngine;
using UnityEngine.UI;

public class SeamlessBackground : MonoBehaviour
{
    [Header("Cấu hình")]
    [Tooltip("Tốc độ trôi (SỐ ÂM để sang trái, ví dụ -100)")]
    public float speed = -100f;

    [Tooltip("Tổng số lượng Background (Ví dụ 3 cái)")]
    public int totalBackgrounds = 3;

    private RectTransform rectTransform;
    private float objectWidth;

    void Start()
    {
        rectTransform = GetComponent<RectTransform>();

        if (rectTransform != null)
        {
            objectWidth = rectTransform.rect.width;
        }
        else
        {
            Debug.LogError("Thiếu RectTransform! Script này chỉ dùng cho UI.");
        }
    }

    void Update()
    {
        if (rectTransform == null) return;

        rectTransform.anchoredPosition += new Vector2(speed * Time.unscaledDeltaTime, 0);

        if (rectTransform.anchoredPosition.x <= -objectWidth)
        {
            RepositionBackground();
        }
    }

    void RepositionBackground()
    {
        Vector2 offset = new Vector2(objectWidth * totalBackgrounds, 0);
        rectTransform.anchoredPosition += offset;
    }
}