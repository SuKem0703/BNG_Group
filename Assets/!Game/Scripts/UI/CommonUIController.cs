using UnityEngine;
using System.Linq;

public class CommonUIController : MonoBehaviour
{
    public static CommonUIController Instance { get; private set; }

    [Header("UI Elements")]
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

    public void SetUIVisible(bool visible, params GameObject[] exceptions)
    {
        SetElementState(hotBar, visible, exceptions, "HotBar");
        SetElementState(itemPopupContainer, visible, exceptions, "ItemPopupContainer");
        SetElementState(commonBar, visible, exceptions, "CommonBar");
        SetElementState(miniMenu, visible, exceptions, "MiniMenu");
        SetElementState(effectGrid, visible, exceptions, "EffectGrid");
        SetElementState(TargetInfoDisplayUI, visible, exceptions, "TargetInfoDisplayUI");
    }

    private void SetElementState(GameObject element, bool targetState, GameObject[] exceptions, string debugName)
    {
        if (element == null)
        {
            return;
        }
        // Kiểm tra ngoại lệ
        if (exceptions != null && exceptions.Contains(element))
        {
            if (!element.activeSelf)
            {
                element.SetActive(true);
            }
            return;
        }

        if (element.activeSelf != targetState)
        {
            element.SetActive(targetState);
        }
    }
}