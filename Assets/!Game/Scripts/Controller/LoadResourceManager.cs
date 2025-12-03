using UnityEngine;

// Class này chịu trách nhiệm giữ các prefab UI dùng chung
public class LoadResourceManager : MonoBehaviour
{
    public static LoadResourceManager Instance { get; private set; }

    [Header("Shared UI Prefabs")]
    [SerializeField] private GameObject confirmUIPrefab;
    [SerializeField] private GameObject notifyUIPrefab;
    [SerializeField] private GameObject selectionBoxPrefab;
    [SerializeField] private GameObject damagePopupPrefab;
    [SerializeField] private GameObject targetIndicatorPrefab;
    [SerializeField] private GameObject mainLoadingCanvasPrefab;
    [SerializeField] private GameObject miniLoadingScreenPrefab;
    [SerializeField] private GameObject mapInfoUIPrefab;
    public GameObject ConfirmUIPrefab => confirmUIPrefab;
    public GameObject NotifyUIPrefab => notifyUIPrefab;
    public GameObject SelectionBoxPrefab => selectionBoxPrefab;
    public GameObject DamagePopupPrefab => damagePopupPrefab;
    public GameObject TargetIndicatorPrefab => targetIndicatorPrefab;
    public GameObject MainLoadingCanvasPrefab => mainLoadingCanvasPrefab;
    public GameObject MiniLoadingScreenPrefab => miniLoadingScreenPrefab;
    public GameObject MapInfoUIPrefab => mapInfoUIPrefab;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadResources();
        CheckLoadResources();
    }
    void LoadResources()
    {
        if (confirmUIPrefab == null) confirmUIPrefab = Resources.Load<GameObject>("UI/ConfirmUICanvas");
        if (notifyUIPrefab == null) notifyUIPrefab = Resources.Load<GameObject>("UI/NotifyUICanvas");
        if (selectionBoxPrefab == null) selectionBoxPrefab = Resources.Load<GameObject>("UI/SelectionBox");
        if (damagePopupPrefab == null) damagePopupPrefab = Resources.Load<GameObject>("DamagePopup");
        if (targetIndicatorPrefab == null) targetIndicatorPrefab = Resources.Load<GameObject>("UI/TargetIndicator_Prefab");
        if (miniLoadingScreenPrefab == null) miniLoadingScreenPrefab = Resources.Load<GameObject>("UI/MiniLoadingScreen");
        if (mainLoadingCanvasPrefab == null) mainLoadingCanvasPrefab = Resources.Load<GameObject>("UI/LoadingCanvas");
        if (mapInfoUIPrefab == null) mapInfoUIPrefab = Resources.Load<GameObject>("UI/MapInfoUICanvas");
    }

    void CheckLoadResources()
    {
        if (confirmUIPrefab == null)
        {
            Debug.LogWarning("ConfirmUIPrefab chưa được gán.");
        }
        if (notifyUIPrefab == null)
        {
            Debug.LogWarning("NotifyUIPrefab chưa được gán.");
        }
        if (selectionBoxPrefab == null)
        {
            Debug.LogWarning("SelectionBoxPrefab chưa được gán.");
        }
        if (damagePopupPrefab == null)
        {
            Debug.LogWarning("DamagePopupPrefab chưa được gán.");
        }
        if (targetIndicatorPrefab == null)
        {
            Debug.LogWarning("TargetIndicatorPrefab chưa được gán.");
        }
        if (miniLoadingScreenPrefab == null)
        {
            Debug.LogWarning("MiniLoadingScreenPrefab chưa được gán.");
        }
        if (mainLoadingCanvasPrefab == null)
        {
            Debug.LogWarning("MainLoadingCanvasPrefab chưa được gán.");
        }
        if (mapInfoUIPrefab == null)
        {
            Debug.LogWarning("MapInfoUIPrefab chưa được gán.");
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}