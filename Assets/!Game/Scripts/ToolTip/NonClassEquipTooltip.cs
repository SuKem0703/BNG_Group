using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class NecklaceEquipTooltip : MonoBehaviour
{
    public static NecklaceEquipTooltip Instance;

    public GameObject tooltipPanel;
    public TMP_Text nameText;
    public Image itemPortrait;
    public TMP_Text slotText;
    public TMP_Text lvl;
    public TMP_Text classText;
    public TMP_Text mpBonus;
    public TMP_Text strBonus, dexBonus, conBonus, intBonus;
    public TMP_Text descriptionText;

    void Awake()
    {
        Instance = this;
        tooltipPanel.SetActive(false);
    }

    public void Show(Item item)
    {
        if (item == null) return;
        if (item is not EquipmentItem equipItem) return;

        tooltipPanel.SetActive(true);
        tooltipPanel.transform.position = Input.mousePosition + new Vector3(10, -10);

        nameText.text = equipItem.Name;
        itemPortrait.sprite = equipItem.icon;

        slotText.text = "Loại: " + equipItem.equipSlot.ToString();
        lvl.text = "Lvl: " + equipItem.requiredLevel.ToString();
        classText.text = "Class: " + equipItem.classRestriction.ToString();

        mpBonus.text = (equipItem.classRestriction == ClassRestriction.Knight
    ? equipItem.mpKnightBonus
    : equipItem.mpMageBonus).ToString();

        strBonus.text = equipItem.bonusSTR.ToString();
        dexBonus.text = equipItem.bonusDEX.ToString();
        conBonus.text = equipItem.bonusCON.ToString();
        intBonus.text = equipItem.bonusINT.ToString();

        descriptionText.text = equipItem.description;
    }

    public void Hide()
    {
        tooltipPanel.SetActive(false);
    }

    void Update()
    {
        if (!tooltipPanel.activeSelf) return;

        RectTransform canvasRect = tooltipPanel.transform.root.GetComponent<Canvas>().GetComponent<RectTransform>();
        RectTransform tooltipRect = tooltipPanel.GetComponent<RectTransform>();

        // Pivot: góc dưới phải trùng chuột
        tooltipRect.pivot = new Vector2(1f, 0f);

        Vector2 mousePos = Input.mousePosition;

        // Offset nhẹ lên trái
        Vector2 offset = new Vector2(-10f, 10f);
        mousePos += offset;

        // Convert sang vị trí local trong Canvas
        Vector2 anchoredPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, mousePos, null, out anchoredPos);

        // Kích thước Tooltip
        float tooltipWidth = 600;
        float tooltipHeight = 800;

        float canvasWidth = canvasRect.rect.width;
        float canvasHeight = canvasRect.rect.height;

        // Clamp vị trí Tooltip để không bị tràn ra ngoài Canvas
        float minX = -canvasWidth / 2f + tooltipWidth;
        float maxX = canvasWidth / 2f;
        float minY = -canvasHeight / 2f;
        float maxY = canvasHeight / 2f - tooltipHeight;

        anchoredPos.x = Mathf.Clamp(anchoredPos.x, minX, maxX);
        anchoredPos.y = Mathf.Clamp(anchoredPos.y, minY, maxY);

        tooltipRect.anchoredPosition = anchoredPos;
    }


    /*
        Vị trí chuột mong muốn |   pivot   |   offset
        Chuột ở dưới trái      |   (0, 1)  | +20, -20
        Chuột ở trên trái      |   (0, 0)  | +20, +20
        Chuột ở trên phải      |   (1, 0)  | -20, +20
        Chuột ở dưới phải      |   (1, 1)  | -20, -20
    */

}
