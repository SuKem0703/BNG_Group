using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public static MenuController Instance;

    public GameObject menuCanvas;
    public CanvasGroup canvasGroup;
    public GameObject itemPopupContainer;

    [Header("References")]
    [SerializeField] private GameObject commonUI;
    private EquipmentScrollViewController equipmentScrollView;

    [Header("Audio")]
    [SerializeField] private AudioClip openMenuSound;
    [SerializeField] private AudioClip closeMenuSound;

    private bool isTransitioning = false;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (menuCanvas == null) menuCanvas = GameObject.Find("Menu");
        if (itemPopupContainer == null) itemPopupContainer = GameObject.Find("ItemPopupContainer");
        if (commonUI == null) commonUI = GameObject.Find("CommonUI");

        if (menuCanvas != null)
        {
            canvasGroup = menuCanvas.GetComponentInChildren<CanvasGroup>();
            if (canvasGroup == null) canvasGroup = menuCanvas.AddComponent<CanvasGroup>();
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        SaveController.OnDataLoaded += EnableMenuOpening;
    }

    private void OnDisable()
    {
        SaveController.OnDataLoaded -= EnableMenuOpening;
    }

    private void EnableMenuOpening()
    {
        GameStateManager.CanOpenMenu = true;
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0f;
        }
    }

    void Start()
    {
        if (equipmentScrollView == null)
            equipmentScrollView = FindFirstObjectByType<EquipmentScrollViewController>();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isTransitioning)
        {
            if (!menuCanvas.activeSelf)
            {
                if (GameStateManager.CanProcessInput() && GameStateManager.CanOpenMenu)
                {
                    ToggleMenuWithState(true);
                }
            }
            else
            {
                if (!GameStateManager.IsLoading)
                {
                    ToggleMenuWithState(false);
                }
            }
        }
    }

    private void ToggleMenuWithState(bool open)
    {
        GameStateManager.IsMenuOpen = open;
        StartCoroutine(ToggleMenu(open));
    }

    private IEnumerator ToggleMenu(bool open)
    {
        isTransitioning = true;

        if (open)
        {
            SoundEffectManager.PlayVoice(openMenuSound);
            MenuStateManager.Instance.OpenMenu(null, true);

            menuCanvas.SetActive(true);
            canvasGroup.alpha = 1f;

            equipmentScrollView?.ShowEquipmentItems();
        }
        else
        {
            SoundEffectManager.PlayVoice(closeMenuSound);
            MenuStateManager.Instance.CloseCurrentMenu();

            EquipTooltip.Instance?.Hide();
            ConsumableTooltip.Instance?.Hide();
            NecklaceEquipTooltip.Instance?.Hide();

            menuCanvas.SetActive(false);
        }

        PauseController.SetPause(open);
        CommonUIController.Instance?.SetUIVisible(!open);

        if (commonUI != null)
            yield return StartCoroutine(SetCommonUIVisible(!open));

        isTransitioning = false;
    }

    private IEnumerator SetCommonUIVisible(bool visible)
    {
        if (commonUI == null) yield break;

        foreach (Transform child in commonUI.transform)
        {
            if (child.name.Equals("Hotbar", System.StringComparison.OrdinalIgnoreCase))
            {
                child.gameObject.SetActive(true);
            }
            else
            {
                child.gameObject.SetActive(visible);
            }
            yield return null;
        }
    }
}