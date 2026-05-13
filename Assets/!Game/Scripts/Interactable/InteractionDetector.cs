using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class InteractionDetector : NetworkBehaviour
{
    public static InteractionDetector Instance { get; private set; }

    private HashSet<FarmPlot> nearbyPlots = new HashSet<FarmPlot>();

    public static event Action<IInteractable> OnTargetChanged;
    PlayerMovement playerMovement => GetComponentInParent<PlayerMovement>();

    [Header("UI Target (Trên đầu đối tượng)")]
    public float targetYOffset = 1.0f;

    private GameObject currentIndicatorInstance;
    private IInteractable currentTarget = null;
    private Camera mainCamera;

    private List<IInteractable> interactablesInRange = new List<IInteractable>();

    [Header("Cài đặt hiệu ứng đung đưa")]
    public float floatAmplitude = 0.02f;
    public float floatSpeed = 5f;

    private bool IsInBattle => PlayerStats.Instance != null && PlayerStats.Instance.netIsOnBattle.Value;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            Instance = this;
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

        HandleIndicatorPosition();
        HandleTargetingLogic();
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

        if (collision.TryGetComponent(out IInteractable interactable))
        {
            if (!interactablesInRange.Contains(interactable))
            {
                interactablesInRange.Add(interactable);
            }
        }

        if (collision.TryGetComponent(out FarmPlot plot))
        {
            nearbyPlots.Add(plot);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsOwner) return;

        if (collision.TryGetComponent(out IInteractable interactable))
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

        if (collision.TryGetComponent(out FarmPlot plot))
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
        if (PauseController.IsGamePause || IsInBattle)
        {
            ClearTarget();
            return;
        }

        if (currentTarget is NPC npc && GameStateManager.IsDialogueActive)
        {
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
                if (hit.collider == null) continue;

                if (hit.collider.CompareTag("Player")) continue;

                if (hit.collider.TryGetComponent(out IInteractable hitTarget) && hitTarget.CanInteract())
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

        interactablesInRange.RemoveAll(item =>
            (item as MonoBehaviour).gameObject.activeInHierarchy == false);

        if (currentTarget != null && currentTarget.CanInteract() && interactablesInRange.Contains(currentTarget))
        {
            return;
        }

        if (currentTarget != null && !currentTarget.CanInteract())
        {
            ClearTarget();
        }

        // --- Tự động tìm mục tiêu gần nhất ---
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
        if (currentIndicatorInstance != null && currentTarget != null)
        {
            Vector3 targetCenter = GetTargetCenterPosition(currentTarget);
            float dynamicYOffset = targetYOffset + (Mathf.Sin(Time.time * floatSpeed) * floatAmplitude);

            Vector3 indicatorPos = targetCenter + new Vector3(0, dynamicYOffset, 0);
            currentIndicatorInstance.transform.position = indicatorPos;
        }
    }

    private void SetTarget(IInteractable newTarget, bool showVisual = true)
    {
        if (currentTarget == newTarget) return;

        if (currentIndicatorInstance != null) Destroy(currentIndicatorInstance);

        currentTarget = newTarget;
        OnTargetChanged?.Invoke(currentTarget);

        if (currentTarget == null) return;

        if (showVisual)
        {
            GameObject indicatorPrefab = LoadResourceManager.Instance.TargetIndicatorPrefab;

            if (indicatorPrefab != null)
            {
                Vector3 targetCenter = GetTargetCenterPosition(currentTarget);
                Vector3 indicatorPos = targetCenter + new Vector3(0, targetYOffset, 0);

                currentIndicatorInstance = Instantiate(indicatorPrefab, indicatorPos, Quaternion.identity);
            }
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
        if (currentIndicatorInstance != null)
        {
            Destroy(currentIndicatorInstance);
            currentIndicatorInstance = null;
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

        Collider2D col = mb.GetComponent<Collider2D>();
        if (col != null)
        {
            Vector3 objectPos = mb.transform.position;
            float offsetY = col.offset.y;
            float halfSizeY = 0f;

            if (col is BoxCollider2D boxCol)
            {
                halfSizeY = boxCol.size.y / 2f;
            }
            else if (col is CapsuleCollider2D capCol)
            {
                halfSizeY = capCol.size.y / 2f;
            }
            else if (col is CircleCollider2D cirCol)
            {
                halfSizeY = cirCol.radius;
            }

            float centerY = objectPos.y + offsetY + halfSizeY;

            return new Vector3(objectPos.x + col.offset.x, centerY, objectPos.z);
        }

        return mb.transform.position;
    }

    private Vector3 GetPlotCenterPosition(FarmPlot plot)
    {
        if (plot == null) return transform.position;

        Collider2D col = plot.GetComponent<Collider2D>();
        if (col != null)
        {
            Vector3 objectPos = plot.transform.position;
            float offsetY = col.offset.y;
            float halfSizeY = 0f;

            if (col is BoxCollider2D boxCol)
            {
                halfSizeY = boxCol.size.y / 2f;
            }

            float centerY = objectPos.y + offsetY + halfSizeY;

            return new Vector3(objectPos.x + col.offset.x, centerY, objectPos.z);
        }

        return plot.transform.position;
    }
}