using UnityEngine;
using UnityEngine.UI;

public class ClassEquipmentUIManager : MonoBehaviour
{
    public Button knightButton;
    public Button mageButton;

    public GameObject knightPanel;
    public GameObject magePanel;

    private void OnEnable()
    {
        // Mỗi lần bật Equipment menu sẽ check lại
        if (GameFlags.HasRecruitedLyria())
        {
            if (!mageButton.gameObject.activeSelf)
                mageButton.gameObject.SetActive(true);

            mageButton.onClick.RemoveAllListeners();
            mageButton.onClick.AddListener(ShowMagePanel);
        }
        else
        {
            mageButton.gameObject.SetActive(false);
        }

        knightButton.onClick.RemoveAllListeners();
        knightButton.onClick.AddListener(ShowKnightPanel);
    }
    private void Start()
    {
        knightButton.onClick.AddListener(ShowKnightPanel);
        mageButton.onClick.AddListener(ShowMagePanel);

        ShowKnightPanel();
    }
    void ShowKnightPanel()
    {
        knightPanel.SetActive(true);
        magePanel.SetActive(false);

        SetButtonAlpha(knightButton, 1f);  // Nút đang chọn rõ
        SetButtonAlpha(mageButton, 0.4f);  // Nút không chọn mờ
    }

    void ShowMagePanel()
    {
        knightPanel.SetActive(false);
        magePanel.SetActive(true);

        SetButtonAlpha(knightButton, 0.4f);
        SetButtonAlpha(mageButton, 1f);
    }

    void SetButtonAlpha(Button button, float alpha)
    {
        Image img = button.GetComponent<Image>();
        if (img != null)
        {
            Color c = img.color;
            c.a = alpha;
            img.color = c;
        }
    }
}
