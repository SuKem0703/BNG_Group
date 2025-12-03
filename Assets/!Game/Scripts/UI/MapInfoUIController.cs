using TMPro;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class MapInfoUIController : MonoBehaviour
{
    [Header("Components")]
    public TextMeshProUGUI mapNameText;
    public Transform background;

    [Header("Settings")]
    public float showDuration = 3f;
    public float animDuration = 0.5f;

    private Vector3 _originalPos;
    private Tween _currentTween;

    private void Awake()
    {
        // Tự động tìm reference
        if (background == null) background = transform.Find("BackGround");
        if (mapNameText == null && background != null)
            mapNameText = background.Find("TextInfo").GetComponent<TextMeshProUGUI>();

        if (background != null) _originalPos = background.localPosition;
    }

    public void ShowMapName(string mapName, ItemRarity rarity)
    {
        mapNameText.text = mapName;
        //Color themeColor = RarityColorHelper.GetColorByRarity(rarity);

        //if (mapNameText != null)
        //    mapNameText.color = themeColor;

        //if (background != null)
        //{
        //    Image bgImage = background.GetComponent<Image>();
        //    if (bgImage != null)
        //    {
        //        bgImage.color = themeColor;
        //    }
        //}

        // Đảm bảo Reset vị trí trước khi chạy tween
        RectTransform canvasRect = transform.root.GetComponent<RectTransform>();
        float offset = canvasRect != null ? canvasRect.rect.height / 2 : 500f;

        // Đặt vị trí xuất phát ở trên cao
        background.localPosition = new Vector3(_originalPos.x, _originalPos.y + offset, _originalPos.z);

        // Animation Xuất hiện
        _currentTween = background.DOLocalMove(_originalPos, animDuration)
            .SetEase(Ease.OutBack)
            .OnComplete(() =>
            {
                Invoke(nameof(HideAnim), showDuration);
            });
    }

    public void SetBackGroundColor()
    {
        return;
    }
    private void HideAnim()
    {
        if (_currentTween != null) _currentTween.Kill();

        RectTransform canvasRect = transform.root.GetComponent<RectTransform>();
        float offset = canvasRect != null ? canvasRect.rect.height / 2 : 500f;

        Vector3 endPos = new Vector3(_originalPos.x, _originalPos.y + offset, _originalPos.z);

        // Animation Ẩn -> Sau đó Destroy Gameobject
        _currentTween = background.DOLocalMove(endPos, animDuration)
            .SetEase(Ease.InBack)
            .OnComplete(() =>
            {
                Destroy(gameObject); // Tự hủy sau khi hoàn thành nhiệm vụ
            });
    }

    private void OnDestroy()
    {
        if (_currentTween != null) _currentTween.Kill();
    }
}