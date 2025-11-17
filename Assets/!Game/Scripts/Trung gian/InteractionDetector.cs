using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using System.Linq;
using System;

public class InteractionDetector : MonoBehaviour
{
    public static event Action<IInteractable> OnTargetChanged;
    PlayerMovement playerMovement => GetComponentInParent<PlayerMovement>();

    [Header("UI Target (Trên đầu đối tượng)")]
    public GameObject targetIndicatorPrefab;
    public float targetYOffset = 1.0f;

    private GameObject currentIndicatorInstance;
    private IInteractable currentTarget = null;
    private Camera mainCamera;

    private List<IInteractable> interactablesInRange = new List<IInteractable>();

    [Header("Cài đặt hiệu ứng đung đưa")]
    [Tooltip("Biên độ (khoảng cách) di chuyển lên xuống")]
    public float floatAmplitude = 0.02f;
    [Tooltip("Tốc độ di chuyển lên xuống")]
    public float floatSpeed = 5f;
    private void Awake()
    {
        if (targetIndicatorPrefab == null)
        {
            targetIndicatorPrefab = Resources.Load<GameObject>("UI/TargetIndicator_Prefab");
        }
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
            Transform interactableTransform = (currentTarget as MonoBehaviour)?.transform;
            if (interactableTransform != null)
            {
                playerMovement.LookTowards(interactableTransform.position);
            }
        }

        currentTarget.Interact();

        if (currentTarget != null)
        {
            MonoBehaviour mb = currentTarget as MonoBehaviour;

            if (mb != null && mb.TryGetComponent<NPC>(out NPC npc))
            {
                if (npc.IsDialogueActive)
                    return;
            }

            if (!currentTarget.CanInteract())
            {
                interactablesInRange.Remove(currentTarget);
                ClearTarget();
            }
        }

    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (PlayerStats.IsOnBattle) return;

        if (collision.TryGetComponent(out IInteractable interactable) && interactable.CanInteract())
        {
            if (!interactablesInRange.Contains(interactable))
            {
                interactablesInRange.Add(interactable);
            }
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
    }

    private void HandleTargetingLogic()
    {
        // Không xử lý click nếu game paused hoặc đang battle
        if (PauseController.IsGamePause || PlayerStats.IsOnBattle)
            return;

        // Nếu target hiện tại là NPC đang mở hội thoại thì giữ nguyên, không auto-switch
        if (currentTarget is NPC npc && npc.IsDialogueActive)
        {
            return;
        }

        if (Input.GetMouseButtonDown(0) && !EventSystem.current.IsPointerOverGameObject())
        {
            Vector2 mousePosition = mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);

            if (hit.collider != null)
            {
                if (hit.collider.TryGetComponent(out IInteractable hitTarget) && hitTarget.CanInteract())
                {
                    if (interactablesInRange.Contains(hitTarget))
                    {
                        SetTarget(hitTarget);
                        return;
                    }
                }
            }

            ClearTarget();
            return;
        }

        interactablesInRange.RemoveAll(item => item == null || !(item as MonoBehaviour) || !(item as MonoBehaviour).gameObject.activeInHierarchy || !item.CanInteract());

        if (currentTarget != null && interactablesInRange.Contains(currentTarget))
        {
            return;
        }

        if (interactablesInRange.Count > 0)
        {
            IInteractable closest = interactablesInRange
                .OrderBy(i => Vector2.Distance(transform.position, (i as MonoBehaviour).transform.position))
                .FirstOrDefault();

            SetTarget(closest);
        }
        else
        {
            // Không còn gì trong tầm
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

        // Xóa indicator cũ
        if (currentIndicatorInstance != null)
            Destroy(currentIndicatorInstance);

        currentTarget = newTarget;

        if (targetIndicatorPrefab != null && currentTarget != null)
        {
            Transform targetTransform = (currentTarget as MonoBehaviour).transform;
            Vector3 indicatorPos = targetTransform.position + new Vector3(0, targetYOffset, 0);

            currentIndicatorInstance = Instantiate(targetIndicatorPrefab, indicatorPos, Quaternion.identity);

            // (Tùy chọn: Làm indicator con của target để di chuyển theo nếu target là NPC động)
            // currentIndicatorInstance.transform.SetParent(targetTransform); 
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
        ClearTarget();
    }
}