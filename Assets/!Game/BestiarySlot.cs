using UnityEngine;
using UnityEngine.UI;

public class BestiarySlot : MonoBehaviour
{
    [Header("UI References")]
    public Image monsterIcon;
    public Button slotButton;

    private EnemyData _enemyData;

    private readonly Color _lockedColor = new Color(0.157f, 0.157f, 0.157f, 1f);

    public void Setup(EnemyData data, int status)
    {
        _enemyData = data;

        if (monsterIcon == null) return;

        monsterIcon.sprite = data.enemyIcon;

        bool isDefeated = status >= 2;

        if (isDefeated)
        {
            monsterIcon.color = Color.white;
            slotButton.interactable = true;
        }
        else
        {
            monsterIcon.color = _lockedColor;

            slotButton.interactable = (status == 1);
        }

        slotButton.onClick.RemoveAllListeners();
        slotButton.onClick.AddListener(OnSlotClicked);
    }

    private void OnSlotClicked()
    {
        if (_enemyData == null) return;
        Debug.Log($"Đang xem chi tiết quái vật: {_enemyData.enemyName}");
    }
}