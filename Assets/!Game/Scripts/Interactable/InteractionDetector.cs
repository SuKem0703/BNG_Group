using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.Tilemaps;

public class InteractionDetector : NetworkBehaviour
{
    public static InteractionDetector Instance { get; private set; }

    public static GameObject SharedIndicator { get; private set; }
    public static Renderer SharedIndicatorRenderer { get; private set; }

    private HashSet<FarmPlot> nearbyPlots = new HashSet<FarmPlot>();

    public static event Action<IInteractable> OnTargetChanged;
    PlayerMovement playerMovement => GetComponentInParent<PlayerMovement>();

    [Header("UI Target (Trên đầu đối tượng)")]
    public float targetYOffset = 1.0f;

    private IInteractable currentTarget = null;
    private Renderer currentTargetRenderer = null;
    private Camera mainCamera;

    private List<IInteractable> interactablesInRange = new List<IInteractable>();

    [Header("Cài đặt hiệu ứng đung đưa")]
    public float floatAmplitude = 0.02f;
    public float floatSpeed = 5f;

    private bool IsInBattle => PlayerStats.Instance != null && PlayerStats.Instance.netIsOnBattle.Value;

    public static void InitSharedIndicator(GameObject prefab)
    {
        if (SharedIndicator == null && prefab != null)
        {
            SharedIndicator = Instantiate(prefab);
            DontDestroyOnLoad(SharedIndicator);
            SharedIndicator.SetActive(false);

            SharedIndicatorRenderer = SharedIndicator.GetComponentInChildren<Renderer>();
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Instance = this;
            InitSharedIndicator(LoadResourceManager.Instance.TargetIndicatorPrefab);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    void Start()
    {
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (!IsOwner) return;

        HandleTargetingLogic();
        HandleIndicatorPosition();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;

        if (!context.performed) return;

        if (IsInBattle) return;

        if (currentTarget == null) return;

        if (currentTarget is Item itemTarget && !itemTarget.IsOwnedByLocalPlayer())
        {
            GameNotify.Show("Vật phẩm này thuộc về người khác.");
            return;
        }

        if (playerMovement != null)
        {
            Vector3 targetCenter = GetTargetCenterPosition(currentTarget);
            playerMovement.LookTowards(targetCenter);
        }

        currentTarget.Interact();

        if (currentTarget != null)
        {
            if (GameStateManager.IsDialogueActive)
            {
                return;
            }

            if (!currentTarget.CanInteract())
            {
                ClearTarget();
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;

        IInteractable interactable = collision.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            if (!interactablesInRange.Contains(interactable))
            {
                interactablesInRange.Add(interactable);
            }
        }

        FarmPlot plot = collision.GetComponentInParent<FarmPlot>();
        if (plot != null)
        {
            nearbyPlots.Add(plot);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsOwner) return;

        IInteractable interactable = collision.GetComponentInParent<IInteractable>();
        if (interactable != null)
        {
            if (interactablesInRange.Contains(interactable))
            {
                interactablesInRange.Remove(interactable);
            }

            if (interactable == currentTarget)
            {
                ClearTarget();
            }
        }

        FarmPlot plot = collision.GetComponentInParent<FarmPlot>();
        if (plot != null)
        {
            nearbyPlots.Remove(plot);
        }
    }

    public bool IsPlotInRange(FarmPlot plot)
    {
        return plot != null && nearbyPlots.Contains(plot);
    }

    public FarmPlot GetNearestPlotInRange()
    {
        if (nearbyPlots.Count == 0) return null;

        return nearbyPlots.OrderBy(p => Vector2.Distance(transform.position, GetPlotCenterPosition(p))).FirstOrDefault();
    }

    private void HandleTargetingLogic()
    {
        if (GameStateManager.IsDialogueActive)
        {
            return;
        }

        if (PauseController.IsGamePause || IsInBattle)
        {
            ClearTarget();
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
            if (mainCamera == null) return;
        }

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D[] hits = Physics2D.RaycastAll(mousePosition, Vector2.zero);

            foreach (var hit in hits)
            {
                if (hit.collider == null || hit.collider.CompareTag("Player")) continue;

                IInteractable hitTarget = hit.collider.GetComponentInParent<IInteractable>();
                if (hitTarget != null && hitTarget.CanInteract())
                {
                    if (interactablesInRange.Contains(hitTarget))
                    {
                        SetTarget(hitTarget);
                        return;
                    }
                }
            }
        }

        interactablesInRange.RemoveAll(item => item == null || item.Equals(null));
        interactablesInRange.RemoveAll(item => (item as MonoBehaviour).gameObject.activeInHierarchy == false);

        if (currentTarget != null && currentTarget.CanInteract() && interactablesInRange.Contains(currentTarget))
        {
            return;
        }

        if (currentTarget != null && !currentTarget.CanInteract())
        {
            ClearTarget();
        }

        if (interactablesInRange.Count > 0)
        {
            IInteractable closest = interactablesInRange
                .Where(i => i.CanInteract())
                .Where(i => !(i is Item) || (i is Item item && item.IsOwnedByLocalPlayer()))
                .OrderBy(i => Vector2.Distance(transform.position, GetTargetCenterPosition(i)))
                .FirstOrDefault();

            if (closest != null)
            {
                SetTarget(closest);
            }
            else
            {
                ClearTarget();
            }
        }
        else
        {
            ClearTarget();
        }
    }

    private void HandleIndicatorPosition()
    {
        if (SharedIndicator != null && SharedIndicator.activeSelf && currentTarget != null)
        {
            Vector3 targetCenter = GetTargetCenterPosition(currentTarget);
            float dynamicYOffset = targetYOffset + (Mathf.Sin(Time.time * floatSpeed) * floatAmplitude);

            SharedIndicator.transform.position = targetCenter + new Vector3(0, dynamicYOffset, 0);

            if (SharedIndicatorRenderer != null && currentTargetRenderer != null)
            {
                SharedIndicatorRenderer.sortingOrder = currentTargetRenderer.sortingOrder + 1;
            }
        }
    }

    private void SetTarget(IInteractable newTarget, bool showVisual = true)
    {
        if (currentTarget == newTarget) return;

        currentTarget = newTarget;

        if (currentTarget != null)
        {
            currentTargetRenderer = ((MonoBehaviour)currentTarget).GetComponentInChildren<Renderer>();
        }

        OnTargetChanged?.Invoke(currentTarget);

        if (currentTarget == null)
        {
            if (SharedIndicator != null) SharedIndicator.SetActive(false);
            return;
        }

        if (showVisual && SharedIndicator != null)
        {
            SharedIndicator.SetActive(true);
        }
    }

    public void ForceSetTarget(IInteractable target, bool showVisual = true)
    {
        SetTarget(target, showVisual);
    }

    private void ClearTarget()
    {
        if (currentTarget == null) return;

        currentTarget = null;
        currentTargetRenderer = null;

        if (SharedIndicator != null)
        {
            SharedIndicator.SetActive(false);
        }

        OnTargetChanged?.Invoke(null);
    }

    private void OnDisable()
    {
        interactablesInRange.Clear();
        nearbyPlots.Clear();
        ClearTarget();
    }

    private Vector3 GetTargetCenterPosition(IInteractable target)
    {
        MonoBehaviour mb = target as MonoBehaviour;
        if (mb == null) return transform.position;

        Transform indicatorPoint = mb.transform.Find("IndicatorPoint");
        if (indicatorPoint != null)
        {
            return indicatorPoint.position;
        }

        Collider2D[] colliders = mb.GetComponentsInChildren<Collider2D>();
        if (colliders.Length > 0)
        {
            Collider2D targetCol = colliders.FirstOrDefault(c => c.isTrigger);

            if (targetCol == null)
            {
                targetCol = colliders.FirstOrDefault(c => !c.isTrigger && c is TilemapCollider2D);
            }

            if (targetCol == null) targetCol = colliders[0];

            return new Vector3(targetCol.bounds.center.x, targetCol.bounds.max.y, mb.transform.position.z);
        }

        return mb.transform.position;
    }

    private Vector3 GetPlotCenterPosition(FarmPlot plot)
    {
        if (plot == null) return transform.position;

        Transform indicatorPoint = plot.transform.Find("IndicatorPoint");
        if (indicatorPoint != null)
        {
            return indicatorPoint.position;
        }

        Collider2D[] colliders = plot.GetComponentsInChildren<Collider2D>();
        if (colliders.Length > 0)
        {
            Collider2D targetCol = colliders.FirstOrDefault(c => c.isTrigger);

            if (targetCol == null)
            {
                targetCol = colliders.FirstOrDefault(c => !c.isTrigger && c is TilemapCollider2D);
            }

            if (targetCol == null) targetCol = colliders[0];

            return new Vector3(targetCol.bounds.center.x, targetCol.bounds.max.y, plot.transform.position.z);
        }

        return plot.transform.position;
    }
}