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

        // Quay mặt về phía đối tượng
        if (playerMovement != null)
        {
            Transform interactableTransform = (currentTarget as MonoBehaviour)?.transform;
            if (interactableTransform != null)
            {
                playerMovement.LookTowards(interactableTransform.position);
            }
        }

        // Thực hiện tương tác
        currentTarget.Interact();

        // Sau khi tương tác xong, kiểm tra lại trạng thái
        if (currentTarget != null)
        {
            MonoBehaviour mb = currentTarget as MonoBehaviour;

            if (mb != null && mb.TryGetComponent<NPC>(out NPC npc))
            {
                if (npc.IsDialogueActive) return;
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
        return nearbyPlots.OrderBy(p => Vector2.Distance(transform.position, p.transform.position)).FirstOrDefault();
    }

    private void HandleTargetingLogic()
    {
        if (PauseController.IsGamePause || PlayerStats.IsOnBattle)
            return;

        if (currentTarget is NPC npc && npc.IsDialogueActive)
        {
            return;
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
                .Where(i => i.CanInteract()) // Quan trọng: Chỉ chọn cái nào đang sẵn sàng
                .OrderBy(i => Vector2.Distance(transform.position, (i as MonoBehaviour).transform.position))
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
            Transform targetTransform = (currentTarget as MonoBehaviour).transform;
            float dynamicYOffset = targetYOffset + (Mathf.Sin(Time.time * floatSpeed) * floatAmplitude);
            Vector3 indicatorPos = targetTransform.position + new Vector3(0, dynamicYOffset, 0);
            currentIndicatorInstance.transform.position = indicatorPos;
        }
    }

    private void SetTarget(IInteractable newTarget)
    {
        if (currentTarget == newTarget) return;

        if (currentIndicatorInstance != null) Destroy(currentIndicatorInstance);

        currentTarget = newTarget;

        GameObject indicatorPrefab = LoadResourceManager.Instance.TargetIndicatorPrefab;

        // Kiểm tra null và có target
        if (indicatorPrefab != null && currentTarget != null)
        {
            Transform targetTransform = (currentTarget as MonoBehaviour).transform;
            Vector3 indicatorPos = targetTransform.position + new Vector3(0, targetYOffset, 0);

            currentIndicatorInstance = Instantiate(indicatorPrefab, indicatorPos, Quaternion.identity);
        }

        OnTargetChanged?.Invoke(currentTarget);
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
}