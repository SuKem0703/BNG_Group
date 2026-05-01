using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClassController : MonoBehaviour
{
    public static ClassController Instance { get; private set; }

    [Header("Knight")]
    public GameObject knightObject;
    public GameObject knightHealthBar;
    public GameObject knightHealthText;

    [Header("Mage")]
    public GameObject mageObject;
    public GameObject mageHealthBar;
    public GameObject mageHealthText;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference swapAction;

    private GameObject currentClass;

    [SerializeField] private float swapCooldown = 2.0f;
    private bool canSwap = true;

    private PlayerStats stats;

    [SerializeField] private Animator knightAnimator;
    [SerializeField] private Animator mageAnimator;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (knightObject == null) 
        {
            knightObject = transform.FindDeepChild("Knight").gameObject;
        }
        if (knightHealthBar == null)
        {
            knightHealthBar = GameObject.Find("GameUI/CommonUI/StatusUI/KnightHealthBarFill");
        }
        if (knightHealthText == null)
        {
            knightHealthText = GameObject.Find("GameUI/CommonUI/StatusUI/KnightHealthText");
        }
        if (mageObject == null)
        {
            mageObject = transform.FindDeepChild("Mage").gameObject;
        }

        if (mageHealthBar != null)
        {
            mageHealthBar = GameObject.Find("GameUI/CommonUI/StatusUI/MageHealthBarFill");
        }
        if (mageHealthText != null)
        {
            mageHealthText = GameObject.Find("GameUI/CommonUI/StatusUI/MageHealthText");
        }

        if (knightObject != null)
            knightAnimator = knightObject.GetComponentInChildren<Animator>(true);
        if (mageObject != null)
            mageAnimator = mageObject.GetComponentInChildren<Animator>(true);

        if (knightAnimator == null) Debug.LogError("ClassController: Không tìm thấy Knight Animator!");
        if (mageAnimator == null) Debug.LogError("ClassController: Không tìm thấy Mage Animator!");

        stats = GetComponent<PlayerStats>();
    }
    private void Start()
    {
        knightObject.SetActive(true);
        mageObject.SetActive(false);
        currentClass = knightObject;

        if (knightHealthBar != null && knightHealthText != null)
        {
            knightHealthBar.SetActive(true);
            knightHealthText.SetActive(true);
        }
        if (mageHealthBar != null && mageHealthText != null)
        {
            mageHealthBar.SetActive(false);
            mageHealthText.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;

    }
    private void OnEnable()
    {
        if (swapAction != null)
            swapAction.action.performed += OnSwapPerformed;
    }
    private void OnDisable()
    {
        if (swapAction != null)
            swapAction.action.performed -= OnSwapPerformed;
    }
    private void OnSwapPerformed(InputAction.CallbackContext ctx)
    {
        TrySwapClass();
    }
    public string GetCurrentClassName()
    {
        if (currentClass == knightObject) return "Knight";
        if (currentClass == mageObject) return "Mage";
        return null;
    }
    private void TrySwapClass()
    {
        if (!canSwap) return;
        if (PauseController.IsGamePause || !GameFlags.HasRecruitedLyria()) return;

        GameObject target = (currentClass == knightObject) ? mageObject : knightObject;

        if (CanSwap(target))
        {
            SwitchClass(target);
            StartCoroutine(SwapDelay());
            if (EffectService.Instance != null)
                EffectService.Instance.AddEffect(gameObject, "SWAP_CD", swapCooldown, 0);
        }
        else
        {
            Debug.Log("❌ Không thể swap: nhân vật này đã mất khả năng chiến đấu!");
        }
    }
    private IEnumerator SwapDelay()
    {
        canSwap = false;

        float elapsed = 0f;
        while (elapsed < swapCooldown)
        {
            if (!PauseController.IsGamePause)
            {
                elapsed += Time.deltaTime;
            }
            yield return null;
        }

        canSwap = true;
    }

    private bool CanSwap(GameObject targetClass)
    {
        if (stats == null) return true;

        if (targetClass == knightObject)
            return stats.knightHealth > 0;
        else if (targetClass == mageObject)
            return stats.mageHealth > 0;

        return true;
    }
    public void SwitchClass(GameObject newClass)
    {
        if (currentClass == newClass) return;

        float lastX = 0;
        float lastY = -1;

        Animator oldAnimator = (currentClass == knightObject) ? knightAnimator : mageAnimator;

        if (oldAnimator != null)
        {
            lastX = oldAnimator.GetFloat("LastInputX");
            lastY = oldAnimator.GetFloat("LastInputY");
        }

        // ⚙️ Xóa tag "Player" khỏi class cũ
        currentClass.tag = "Untagged";

        // Tắt PlayerInput của class hiện tại và ngăn xử lý input
        var oldInput = currentClass.GetComponent<PlayerInput>();
        if (oldInput != null)
        {
            oldInput.DeactivateInput();
            oldInput.enabled = false; // Ngăn xử lý input
            currentClass.SetActive(false);
        }

        // Tắt UI cũ
        if (currentClass == knightObject && knightHealthBar != null && knightHealthText != null)
        {
            knightHealthBar.SetActive(false);
            knightHealthText.SetActive(false);
        }
        else if (currentClass == mageObject && mageHealthBar != null && mageHealthText != null)
        {
            mageHealthBar.SetActive(false);
            mageHealthText.SetActive(false);
        }

        // Kích hoạt class mới
        currentClass = newClass;
        currentClass.SetActive(true);

        // ⚙️ Gắn lại tag "Player" cho class mới
        currentClass.tag = "Player";

        Animator newAnimator = (currentClass == knightObject) ? knightAnimator : mageAnimator;

        if (newAnimator != null)
        {
            // Set cả 4 tham số để đảm bảo Idle và Attack đều đúng hướng
            newAnimator.SetFloat("LastInputX", lastX);
            newAnimator.SetFloat("LastInputY", lastY);
            newAnimator.SetFloat("LookX", lastX);
            newAnimator.SetFloat("LookY", lastY);
        }

        // Bật UI mới
        if (currentClass == knightObject && knightHealthBar != null && knightHealthText != null)
        {
            knightHealthBar.SetActive(true);
            knightHealthText.SetActive(true);
        }
        else if (currentClass == mageObject && mageHealthBar != null && mageHealthText != null)
        {
            mageHealthBar.SetActive(true);
            mageHealthText.SetActive(true);
        }

        // Bật PlayerInput của class mới và cho phép xử lý input
        var newInput = currentClass.GetComponent<PlayerInput>();
        if (newInput != null)
        {
            newInput.enabled = true;
            newInput.ActivateInput();
        }

        // 👉 Gọi hàm tính lại chỉ số sau khi swap class
        var stats = GetComponent<PlayerStats>();
        if (stats != null)
        {
            stats.ApplyEquippedItems();
        }
    }
}
