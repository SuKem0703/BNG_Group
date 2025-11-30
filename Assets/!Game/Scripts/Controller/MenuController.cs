using UnityEngine;
using UnityEngine.UI;

public class MenuController : MonoBehaviour
{
    public static MenuController Instance;

    public GameObject menuCanvas;
    public CanvasGroup canvasGroup;
    public GameObject itemPopupContainer;

    [Header("References")]
    private EquipmentScrollViewController equipmentScrollView;

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
        if (Input.GetKeyDown(KeyCode.E))
        {
            if (!menuCanvas.activeSelf)
            {
                if (GameStateManager.CanProcessInput() && GameStateManager.CanOpenMenu)
                {
                    ToggleMenu(true);
                }
            }
            else
            {
                if (!GameStateManager.IsLoading)
                {
                    ToggleMenu(false);
                }
            }
        }
    }

    private void ToggleMenu(bool open)
    {
        if (menuCanvas == null) return;

        if (open)
        {
            SoundEffectManager.PlayVoice(openMenuSound);

            // Gọi Manager để quản lý trạng thái chung
            MenuStateManager.Instance.OpenMenu(null, true);

            // Bật UI riêng của Menu
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

            // Tắt UI
            menuCanvas.SetActive(false);
            if (canvasGroup != null) canvasGroup.alpha = 0f;

            MenuStateManager.Instance.CloseCurrentMenu();

            EquipTooltip.Instance?.Hide();
            ConsumableTooltip.Instance?.Hide();
            NecklaceEquipTooltip.Instance?.Hide();
        }
    }
}