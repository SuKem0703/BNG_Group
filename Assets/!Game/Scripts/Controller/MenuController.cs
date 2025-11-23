using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class MenuController : MonoBehaviour
{
    public static MenuController Instance;

    public GameObject menuCanvas;
    public GameObject itemPopupContainer;
    public CanvasGroup canvasGroup;
    private EquipmentScrollViewController equipmentScrollView;

    public float transitionDuration = 0.4f;
    public Ease scaleEaseOpen = Ease.OutBack;
    public Ease scaleEaseClose = Ease.InSine;

    [SerializeField] private List<RectTransform> menuElements;
    public float staggerDelay = 0.01f;
    public float staggerElementDuration = 0.02f;

    private bool isTransitioning = false;

    [SerializeField] private GameObject commonUI;

    public static bool CanOpenMenu = false;
    public static bool IsMenuOpen = false;

    [Header("Âm thanh")]
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

        if (menuCanvas == null)
            menuCanvas = GameObject.Find("Menu");

        if (canvasGroup == null && menuCanvas != null)
        {
            canvasGroup = menuCanvas.GetComponentInChildren<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = menuCanvas.AddComponent<CanvasGroup>();
        }

        if (itemPopupContainer == null)
            itemPopupContainer = GameObject.Find("ItemPopupContainer");

        if (commonUI == null)
        {
            commonUI = GameObject.Find("CommonUI");
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
        CanOpenMenu = true;

        if (menuCanvas != null)
        {
            menuCanvas.SetActive(false);
            if (canvasGroup != null)
                canvasGroup.alpha = 0f;
        }
    }
    void Start()
    {
        if (equipmentScrollView == null)
        {
            equipmentScrollView = FindFirstObjectByType<EquipmentScrollViewController>();
            if (equipmentScrollView == null)
                Debug.LogError("Không tìm thấy EquipmentScrollViewController trong scene!");
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.E) && !isTransitioning && CanOpenMenu)
        {
            bool isOpening = !menuCanvas.activeSelf;
            IsMenuOpen = isOpening;
            StartCoroutine(ToggleMenu(isOpening));
        }
    }

    private IEnumerator ToggleMenu(bool open)
    {
        if (isTransitioning) yield break;
        isTransitioning = true;

        RectTransform menuRect = menuCanvas.GetComponent<RectTransform>();
        if (menuRect == null || canvasGroup == null)
        {
            Debug.LogError("Thiếu RectTransform hoặc CanvasGroup trên Menu Canvas!");
            menuCanvas.SetActive(open);
            isTransitioning = false;
            yield break;
        }

        if (open)
        {
            SoundEffectManager.PlayVoice(openMenuSound);

            menuCanvas.SetActive(true);
            canvasGroup.alpha = 0f;
            menuRect.localScale = Vector3.one * 0.85f;

            foreach (var element in menuElements)
            {
                if (element != null)
                {
                    element.gameObject.SetActive(true);
                    element.localScale = Vector3.zero;
                }
            }
        }
        else
        {
            SoundEffectManager.PlayVoice(closeMenuSound);
        }

        PauseController.SetPause(open);
        //itemPopupContainer.SetActive(!open);
        CommonUIController.Instance?.SetUIVisible(!open);
        if (commonUI != null)
            yield return StartCoroutine(SetCommonUIVisible(!open));

        if (!open)
        {
            EquipTooltip.Instance?.Hide();
            ConsumableTooltip.Instance?.Hide();
            NecklaceEquipTooltip.Instance?.Hide();
        }

        if (open)
        {
            //foreach (Transform child in itemPopupContainer.transform)
            //    Destroy(child.gameObject);

            //ItemPickupUIController.Instance?.ClearAllPopups();
            equipmentScrollView?.ShowEquipmentItems();
        }

        Sequence seq = DOTween.Sequence();

        if (open)
        {
            seq.Append(canvasGroup.DOFade(1, transitionDuration));
            seq.Join(menuRect.DOScale(1, transitionDuration).SetEase(scaleEaseOpen));
            seq.AppendInterval(0.1f);

            for (int i = 0; i < menuElements.Count; i++)
            {
                RectTransform element = menuElements[i];
                if (element != null)
                {
                    seq.Append(element.DOScale(1, staggerElementDuration)
                        .SetEase(Ease.OutBack)
                        .SetDelay(i * staggerDelay));
                }
            }
        }
        else
        {
            seq.Append(canvasGroup.DOFade(0, transitionDuration * 0.8f));
            seq.Join(menuRect.DOScale(0.85f, transitionDuration * 0.8f).SetEase(scaleEaseClose));
        }

        yield return seq.WaitForCompletion(true);

        if (!open)
            menuCanvas.SetActive(false);
        else
        {
            menuRect.localScale = Vector3.one;
            canvasGroup.alpha = 1f;
        }

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
    public void SaveGame()
    {
        SaveController saveController = FindFirstObjectByType<SaveController>();
        if (saveController != null)
            saveController.SaveGame();
        else
            Debug.LogWarning("SaveController not found in the scene.");
    }
}
