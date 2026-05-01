using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class ClassController : NetworkBehaviour
{
    public static ClassController Instance { get; private set; }

    [Header("Classes")]
    public GameObject knightObject;
    public GameObject mageObject;

    [Header("Input Actions")]
    [SerializeField] private InputActionReference swapAction;

    private GameObject currentClass;

    [SerializeField] private float swapCooldown = 2.0f;
    private bool canSwap = true;

    private PlayerStats stats;

    [SerializeField] private Animator knightAnimator;
    [SerializeField] private Animator mageAnimator;

    public event Action<string> OnClassSwapped;

    private void Awake()
    {
        if (knightObject == null) knightObject = transform.FindDeepChild("Knight").gameObject;
        if (mageObject == null) mageObject = transform.FindDeepChild("Mage").gameObject;

        if (knightObject != null) knightAnimator = knightObject.GetComponentInChildren<Animator>(true);
        if (mageObject != null) mageAnimator = mageObject.GetComponentInChildren<Animator>(true);

        stats = GetComponent<PlayerStats>();
    }

    //private void Start()
    //{
    //    knightObject.SetActive(true);
    //    mageObject.SetActive(false);
    //    currentClass = knightObject;

    //    OnClassSwapped?.Invoke("Knight");
    //}

    public override void OnDestroy()
    {
        base.OnDestroy();
        if (Instance == this) Instance = null;
    }

    private void OnEnable()
    {
        if (swapAction != null) swapAction.action.performed += OnSwapPerformed;
    }

    private void OnDisable()
    {
        if (swapAction != null) swapAction.action.performed -= OnSwapPerformed;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (IsOwner)
        {
            Instance = this;

            knightObject.SetActive(true);
            mageObject.SetActive(false);
            currentClass = knightObject;

            EnableLocalInput();
            OnClassSwapped?.Invoke("Knight");
        }
        else
        {
            DisableInputForOthers();
        }
    }

    private void EnableLocalInput()
    {
        var activeInput = (currentClass == knightObject) ?
            knightObject.GetComponent<PlayerInput>() :
            mageObject.GetComponent<PlayerInput>();

        if (activeInput != null) activeInput.enabled = true;
    }

    private void DisableInputForOthers()
    {
        if (knightObject != null) knightObject.GetComponent<PlayerInput>().enabled = false;
        if (mageObject != null) mageObject.GetComponent<PlayerInput>().enabled = false;
    }

    private void OnSwapPerformed(InputAction.CallbackContext ctx) => TrySwapClass();

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
    }

    private IEnumerator SwapDelay()
    {
        canSwap = false;
        float elapsed = 0f;
        while (elapsed < swapCooldown)
        {
            if (!PauseController.IsGamePause) elapsed += Time.deltaTime;
            yield return null;
        }
        canSwap = true;
    }

    private bool CanSwap(GameObject targetClass)
    {
        if (stats == null) return true;
        if (targetClass == knightObject) return stats.knightHealth > 0;
        else if (targetClass == mageObject) return stats.mageHealth > 0;
        return true;
    }

    public void SwitchClass(GameObject newClass)
    {
        if (currentClass == newClass) return;

        float lastX = 0, lastY = -1;
        Animator oldAnimator = (currentClass == knightObject) ? knightAnimator : mageAnimator;

        if (oldAnimator != null)
        {
            lastX = oldAnimator.GetFloat("LastInputX");
            lastY = oldAnimator.GetFloat("LastInputY");
        }

        currentClass.tag = "Untagged";

        var oldInput = currentClass.GetComponent<PlayerInput>();
        if (oldInput != null)
        {
            oldInput.DeactivateInput();
            oldInput.enabled = false;
            currentClass.SetActive(false);
        }

        currentClass = newClass;
        currentClass.SetActive(true);
        currentClass.tag = "Player";

        Animator newAnimator = (currentClass == knightObject) ? knightAnimator : mageAnimator;
        if (newAnimator != null)
        {
            newAnimator.SetFloat("LastInputX", lastX);
            newAnimator.SetFloat("LastInputY", lastY);
            newAnimator.SetFloat("LookX", lastX);
            newAnimator.SetFloat("LookY", lastY);
        }

        var newInput = currentClass.GetComponent<PlayerInput>();
        if (newInput != null)
        {
            if (IsOwner)
            {
                newInput.enabled = true;
                newInput.ActivateInput();
            }
            else
            {
                newInput.DeactivateInput();
                newInput.enabled = false;
            }
        }

        var currentStats = GetComponent<PlayerStats>();
        if (currentStats != null) currentStats.ApplyEquippedItems();

        OnClassSwapped?.Invoke(GetCurrentClassName());
    }
}