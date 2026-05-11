using UnityEngine;
using System.Linq;

public class CommonUIController : MonoBehaviour
{
    public static CommonUIController Instance { get; private set; }

    [Header("Static UI")]
    public GameObject hotBar;

    [Header("Dynamic UI")]
    public GameObject itemPopupContainer;
    public GameObject statusUI;
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
        SetDynamicUIVisible(visible, exceptions);
        SetStaticUIVisible(visible, exceptions);
    }

    public void SetDynamicUIVisible(bool visible, params GameObject[] exceptions)
    {
        SetElementState(itemPopupContainer, visible, exceptions, "ItemPopupContainer");
        SetElementState(statusUI, visible, exceptions, "StatusUI");
        SetElementState(miniMenu, visible, exceptions, "MiniMenu");
        SetElementState(effectGrid, visible, exceptions, "EffectGrid");
        SetElementState(TargetInfoDisplayUI, visible, exceptions, "TargetInfoDisplayUI");
    }

    public void SetStaticUIVisible(bool visible, params GameObject[] exceptions)
    {
        SetElementState(hotBar, visible, exceptions, "HotBar");
    }

    private void SetElementState(GameObject element, bool targetState, GameObject[] exceptions, string debugName)
    {
        if (element == null)
        {
            return;
        }

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