using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using System;

public class InteractionDetector : MonoBehaviour
{
    public static InteractionDetector Instance { get; private set; }

    // Quét Farm Plot gần đó để ưu tiên tương tác
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

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
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
        HandleIndicatorPosition();
        HandleTargetingLogic();
    }

    public void OnInteract(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
        if (PlayerStats.IsOnBattle) return;

        if (currentTarget == null) return;

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
        if (PlayerStats.IsOnBattle) return;

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
        if (PauseController.IsGamePause || PlayerStats.IsOnBattle)
            return;

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

        // Loại bỏ object bị disable (SetActive false)
        interactablesInRange.RemoveAll(item =>
            (item as MonoBehaviour).gameObject.activeInHierarchy == false);

        // Nếu target hiện tại vẫn hợp lệ thì giữ nguyên
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
                .OrderBy(i => Vector2.Distance(transform.position, GetTargetCenterPosition(i)))
                .FirstOrDefault();

            if (closest != null)
            {
                SetTarget(closest);
            }
            else
            {
                ClearTarget(); // Không có gì sẵn sàng thì bỏ target
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

    // Dùng để ép set target từ bên ngoài (bỏ qua các kiểm tra)
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

            // BoxCollider2D có thuộc tính size
            if (col is BoxCollider2D boxCol)
            {
                halfSizeY = boxCol.size.y / 2f;
            }
            // CapsuleCollider2D cũng có thuộc tính size
            else if (col is CapsuleCollider2D capCol)
            {
                halfSizeY = capCol.size.y / 2f;
            }
            // CircleCollider2D có thuộc tính radius
            else if (col is CircleCollider2D cirCol)
            {
                halfSizeY = cirCol.radius;
            }

            // Công thức: Tọa độ gốc + Offset Y + Một nửa chiều cao Collider
            float centerY = objectPos.y + offsetY + halfSizeY;

            return new Vector3(objectPos.x + col.offset.x, centerY, objectPos.z);
        }

        // Nếu không có collider, fallback về vị trí transform
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

            // Tâm Plot: Tọa độ gốc + Offset Y + Nửa chiều cao
            float centerY = objectPos.y + offsetY + halfSizeY;

            return new Vector3(objectPos.x + col.offset.x, centerY, objectPos.z);
        }

        return plot.transform.position;
    }
}