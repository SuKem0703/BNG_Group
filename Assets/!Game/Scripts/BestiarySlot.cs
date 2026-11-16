
using UnityEngine;
using UnityEngine.UI;

public class BestiarySlot : MonoBehaviour
{
    [Header("UI References")]
    public Image monsterIcon;
    public Button slotButton;

    private EnemyData _enemyData;

    private readonly Color _lockedColor =
        new Color(0.157f, 0.157f, 0.157f, 1f);

    public void Setup(EnemyData data, int status)
    {
        _enemyData = data;

        if (monsterIcon == null || slotButton == null)
            return;

        monsterIcon.sprite = data.enemyIcon;

        // Status 1: Đã gặp
        // Status 2: Đã tiêu diệt
        bool isDiscovered = status >= 1;

        monsterIcon.color = isDiscovered
            ? Color.white
            : _lockedColor;

        slotButton.interactable = isDiscovered;

        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(OnSlotClicked);
    }

    private void OnSlotClicked()
    {
        if (_enemyData == null)
            return;

        Debug.Log(
            $"Đang xem chi tiết quái vật: {_enemyData.enemyName}"
        );
    }
}
