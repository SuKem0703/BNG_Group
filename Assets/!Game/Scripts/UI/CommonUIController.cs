using UnityEngine;

public class CommonUIController : MonoBehaviour
{
    public static CommonUIController Instance { get; private set; }

    public GameObject hotBar;
    public GameObject itemPopupContainer;
    public GameObject commonBar;
    public GameObject miniMenu;
    public GameObject effectGrid;
    public GameObject TargetInfoDisplayUI;
    void Awake()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
    }

    public void SetUIVisible(bool visible)
    {
        if (hotBar != null) hotBar.SetActive(visible);
        if (itemPopupContainer != null) itemPopupContainer.SetActive(visible);
        if (commonBar != null) commonBar.SetActive(visible);
        if (miniMenu != null) miniMenu.SetActive(visible);
        if (effectGrid != null) effectGrid.SetActive(visible);
        if (TargetInfoDisplayUI != null) TargetInfoDisplayUI.SetActive(visible);
    }
}
