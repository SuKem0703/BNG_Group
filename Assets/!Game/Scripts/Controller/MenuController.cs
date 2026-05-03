using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;

public class MenuController : MonoBehaviour
{
    public static MenuController Instance;

    public GameObject menuCanvas;
    public CanvasGroup canvasGroup;
    public GameObject itemPopupContainer;

    [Header("References")]
    private EquipmentScrollViewController equipmentScrollView;

    [Header("Input Actions (Cross-Platform)")]
    [SerializeField] private InputActionReference toggleMenuAction;

    [Header("Audio")]
    [SerializeField] private AudioClip openMenuSound;
    [SerializeField] private AudioClip closeMenuSound;

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

        if (toggleMenuAction != null)
            toggleMenuAction.action.performed += OnShortcutPressed;
    }

    private void OnDisable()
    {
        SaveController.OnDataLoaded -= EnableMenuOpening;

        if (toggleMenuAction != null)
            toggleMenuAction.action.performed -= OnShortcutPressed;
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

    private void OnShortcutPressed(InputAction.CallbackContext context)
    {
        if (!menuCanvas.activeSelf)
        {
            if (GameStateManager.CanProcessInput() && GameStateManager.CanOpenMenu)
                ToggleMenu(true);
        }
        else
        {
            if (!GameStateManager.IsLoading)
                ToggleMenu(false);
        }
    }

    public void Button_CloseMenu()
    {
        if (!GameStateManager.IsLoading)
            ToggleMenu(false);
    }

    public void Button_OpenMenu()
    {
        if (GameStateManager.CanProcessInput() && GameStateManager.CanOpenMenu)
            ToggleMenu(true);
    }

    private void ToggleMenu(bool open)
    {
        if (menuCanvas == null) return;

        if (open)
        {
            SoundEffectManager.PlayVoice(openMenuSound);

            MenuStateManager.Instance.OpenMenu(null, true);

            menuCanvas.SetActive(true);
            if (canvasGroup != null) canvasGroup.alpha = 1f;

            equipmentScrollView?.ShowEquipmentItems();

            if (CommonUIController.Instance != null)
            {
                CommonUIController.Instance.SetUIVisible(false, CommonUIController.Instance.hotBar);
            }
        }
        else
        {
            SoundEffectManager.PlayVoice(closeMenuSound);

            menuCanvas.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            MenuStateManager.Instance.CloseCurrentMenu();

            EquipTooltip.Instance?.Hide();
            ConsumableTooltip.Instance?.Hide();
            NecklaceEquipTooltip.Instance?.Hide();
        }
    }
}