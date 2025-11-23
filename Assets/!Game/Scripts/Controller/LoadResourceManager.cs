using UnityEngine;

// Class này chịu trách nhiệm giữ các prefab UI dùng chung
public class LoadResourceManager : MonoBehaviour
{
    public static LoadResourceManager Instance { get; private set; }

    [Header("Shared UI Prefabs")]
    [SerializeField] private GameObject confirmUIPrefab;
    [SerializeField] private GameObject notifyUIPrefab;
    [SerializeField] private GameObject selectionBoxPrefab;

    public GameObject ConfirmUIPrefab => confirmUIPrefab;
    public GameObject NotifyUIPrefab => notifyUIPrefab;
    public GameObject SelectionBoxPrefab => selectionBoxPrefab;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (confirmUIPrefab == null) confirmUIPrefab = Resources.Load<GameObject>("UI/ConfirmUICanvas");
        if (notifyUIPrefab == null) notifyUIPrefab = Resources.Load<GameObject>("UI/NotifyUICanvas");
        if (selectionBoxPrefab == null) selectionBoxPrefab = Resources.Load<GameObject>("UI/SelectionBox");
    }
    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}