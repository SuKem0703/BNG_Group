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

    public NetworkVariable<bool> netIsKnightActive = new NetworkVariable<bool>(true, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);

    private GameObject currentClass;

    [SerializeField] private float swapCooldown = 2.0f;
    private bool canSwap = true;

    private PlayerStats stats => GetComponent<PlayerStats>();
    private PlayerMovement playerMovement => GetComponent<PlayerMovement>();

    [SerializeField] private Animator knightAnimator;
    [SerializeField] private Animator mageAnimator;

    public event Action<string> OnClassSwapped;

    public bool IsKnightActive => netIsKnightActive.Value;

    private void Awake()
    {
        if (knightObject == null) knightObject = transform.FindDeepChild("Knight").gameObject;
        if (mageObject == null) mageObject = transform.FindDeepChild("Mage").gameObject;

        if (knightObject != null) knightAnimator = knightObject.GetComponentInChildren<Animator>(true);
        if (mageObject != null) mageAnimator = mageObject.GetComponentInChildren<Animator>(true);
    }

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

        netIsKnightActive.OnValueChanged += OnClassStateSynced;

        if (IsOwner)
        {
            Instance = this;
            currentClass = knightObject;
            netIsKnightActive.Value = true;

            EnableLocalInput();
            OnClassSwapped?.Invoke("Knight");
        }
        else
        {
            DisableInputForOthers();
        }

        ApplyClassVisibility(netIsKnightActive.Value);
    }

    private void OnClassStateSynced(bool previous, bool current)
    {
        ApplyClassVisibility(current);
    }

    private void ApplyClassVisibility(bool isKnight)
    {
        ToggleClassVisuals(knightObject, isKnight);
        ToggleClassVisuals(mageObject, !isKnight);
    }

    private void ToggleClassVisuals(GameObject classObj, bool isVisible)
    {
        if (classObj == null) return;
        var sprites = classObj.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var s in sprites) s.enabled = isVisible;
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
        return netIsKnightActive.Value ? "Knight" : "Mage";
    }

    private void TrySwapClass()
    {
        if (!canSwap) return;

        if (stats != null && (stats.IsProcessingDeath || stats.isGameOver)) return;

        if (playerMovement != null && playerMovement.isAttacking && !playerMovement.canMoveWhileAttacking) return;

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
        }

        currentClass = newClass;
        currentClass.tag = "Player";

        if (IsOwner)
        {
            netIsKnightActive.Value = (currentClass == knightObject);
        }

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

        if (stats != null) stats.ApplyEquippedItems();

        if (playerMovement != null)
        {
            playerMovement.ghostTrail = currentClass.GetComponentInChildren<GhostTrail>();
        }

        if (IsOwner)
        {
            OnClassSwapped?.Invoke(GetCurrentClassName());
        }
    }
}