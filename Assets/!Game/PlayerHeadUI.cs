using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.Netcode;

public class PlayerHeadUI : NetworkBehaviour
{
    [Header("References")]
    public PlayerStats playerStats;
    public ClassController classController;
    public GameObject uiCanvasObject;

    [Header("UI Elements")]
    public TextMeshProUGUI nameText;
    public Image healthFillImage;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            uiCanvasObject.SetActive(false);
        }
    }

    void Update()
    {
        if (IsOwner || playerStats == null) return;

        if (nameText.text != playerStats.netUsername.Value.ToString())
        {
            nameText.text = playerStats.netUsername.Value.ToString();
        }

        bool isKnight = true;
        if (classController != null && classController.mageObject != null)
        {
            isKnight = classController.knightObject.activeInHierarchy;
        }

        float currentHP = isKnight ? playerStats.netKnightHealth.Value : playerStats.netMageHealth.Value;
        float maxHP = isKnight ? playerStats.netMaxKnightHP.Value : playerStats.netMaxMageHP.Value;

        if (maxHP > 0)
        {
            float targetFill = currentHP / maxHP;
            healthFillImage.fillAmount = Mathf.Lerp(healthFillImage.fillAmount, targetFill, Time.deltaTime * 10f);
        }
    }
}