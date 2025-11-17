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

        tooltipPanel.SetActive(true);
        tooltipPanel.transform.position = Input.mousePosition + new Vector3(10, -10);

        nameText.text = item.Name;
        itemPortrait.sprite = item.icon;

        slotText.text = "Loại: " + item.equipSlot.ToString();
        lvl.text = "Lvl: " + item.requiredLevel.ToString();
        classText.text = "Class: " + item.classRestriction.ToString();

        mpBonus.text = (item.classRestriction == ClassRestriction.Knight
    ? item.mpKnightBonus
    : item.mpMageBonus).ToString();

        strBonus.text = item.bonusSTR.ToString();
        dexBonus.text = item.bonusDEX.ToString();
        conBonus.text = item.bonusCON.ToString();
        intBonus.text = item.bonusINT.ToString();

        descriptionText.text = item.description;
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
