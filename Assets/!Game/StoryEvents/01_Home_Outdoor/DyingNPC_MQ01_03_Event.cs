using System.Collections;
using UnityEngine;
using Unity.Cinemachine;
using UnityEngine.SceneManagement;
using Unity.Netcode;

namespace Chronicles.StoryEvents.Chapter1
{
    public class DyingNPC_MQ01_03_Event : MonoBehaviour
    {
        [Header("Quest Settings")]
        [SerializeField] private string targetQuestID = "MQ_CH01_PH01_03";
        [SerializeField] private string eventSaveID = "Event_DyingNPC_MQ01_03";

        [Header("References")]
        [SerializeField] private Animator animator;
        [SerializeField] private GameObject npcRootObject;

        [Header("NPC Movement Path")]
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        [SerializeField] private Transform pointC;
        [SerializeField] private float npcWalkSpeed = 2f;
        [SerializeField] private float waitBeforeDie = 0.5f;

        [Header("Player Actions")]
        [SerializeField] private Transform pointD;
        [SerializeField] private float playerWalkSpeed = 3f;

        [Header("Camera Logic")]
        [SerializeField] private float cameraBlendDuration = 2f;

        private bool eventTriggered = false;

        private void Start()
        {
            if (npcRootObject != null) npcRootObject.SetActive(false);

            if (SaveController.IsDataLoaded) CheckSaveState();
            else SaveController.OnDataLoaded += HandleDataLoaded;

            QuestController.OnQuestStatusUpdated += OnQuestUpdated;
        }

        private void OnDestroy()
        {
            SaveController.OnDataLoaded -= HandleDataLoaded;
            QuestController.OnQuestStatusUpdated -= OnQuestUpdated;
        }

        private void HandleDataLoaded()
        {
            SaveController.OnDataLoaded -= HandleDataLoaded;
            CheckSaveState();
        }

        private void CheckSaveState()
        {
            if (SaveController.Instance != null && SaveController.Instance.IsCollected(SceneManager.GetActiveScene().name, eventSaveID))
            {
                eventTriggered = true;
                if (npcRootObject != null && pointC != null)
                {
                    Vector3 endPos = new Vector3(pointC.position.x, pointC.position.y, npcRootObject.transform.position.z);
                    npcRootObject.transform.position = endPos;
                    npcRootObject.SetActive(true);

                    // [Sửa lỗi] Đảm bảo khi load save, NPC nằm gục theo đúng hướng đi cuối cùng (B -> C)
                    if (pointB != null && animator != null)
                    {
                        Vector3 finalDir = (pointC.position - pointB.position).normalized;
                        animator.SetFloat("LastInputX", finalDir.x);
                        animator.SetFloat("LastInputY", finalDir.y);
                    }
                }

                if (animator != null) animator.SetTrigger("Die");
            }
            else
            {
                CheckQuestAndTrigger();
            }
        }

        private void OnQuestUpdated(string questID)
        {
            if (questID == targetQuestID)
            {
                CheckQuestAndTrigger();
            }
        }

        private void CheckQuestAndTrigger()
        {
            if (eventTriggered) return;

            bool isCompleted = QuestController.Instance != null && QuestController.Instance.IsQuestCompleted(targetQuestID);

            if (isCompleted)
            {
                eventTriggered = true;
                StartCoroutine(PlayEventSequence());
            }
        }

        private IEnumerator PlayEventSequence()
        {
            if (npcRootObject == null || pointA == null || pointB == null || pointC == null || animator == null)
            {
                Debug.LogError("[DyingNPC Event] LỖI: Thiếu tham chiếu tới các điểm A, B, C hoặc NPC!");
                yield break;
            }

            GameStateManager.StartLoading();

            CinemachineCamera vCam = null;
            Transform originalTarget = null;
            Transform playerTransform = null;
            PlayerMovement playerMovement = null;
            Animator playerAnimator = null;
            PlayerAnimatorHandler playerAnimHandler = null;

            if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
            {
                var localClient = NetworkManager.Singleton.LocalClient;
                if (localClient != null && localClient.PlayerObject != null)
                {
                    playerTransform = localClient.PlayerObject.transform;
                    playerMovement = playerTransform.GetComponent<PlayerMovement>();
                    playerAnimator = playerTransform.GetComponentInChildren<Animator>();
                    playerAnimHandler = playerTransform.GetComponentInChildren<PlayerAnimatorHandler>();
                    vCam = FindFirstObjectByType<CinemachineCamera>();

                    if (vCam != null)
                    {
                        originalTarget = vCam.Target.TrackingTarget;
                    }
                }
            }

            Vector3 startPos = new Vector3(pointA.position.x, pointA.position.y, npcRootObject.transform.position.z);
            npcRootObject.transform.position = startPos;
            npcRootObject.SetActive(true);

            if (playerMovement != null)
            {
                playerMovement.LookTowards(npcRootObject.transform.position);
            }

            if (vCam != null)
            {
                vCam.Target.TrackingTarget = npcRootObject.transform;
                yield return new WaitForSeconds(cameraBlendDuration);
            }

            yield return StartCoroutine(MoveCharacterToPoint(npcRootObject.transform, animator, pointB.position, npcWalkSpeed));
            yield return StartCoroutine(MoveCharacterToPoint(npcRootObject.transform, animator, pointC.position, npcWalkSpeed));

            yield return new WaitForSeconds(waitBeforeDie);
            animator.SetTrigger("Die");
            yield return new WaitForSeconds(2.5f);

            if (vCam != null)
            {
                vCam.Target.TrackingTarget = originalTarget != null ? originalTarget : playerTransform;
                yield return new WaitForSeconds(cameraBlendDuration);
            }

            if (playerTransform != null && pointD != null && playerAnimator != null)
            {
                if (playerAnimHandler != null) playerAnimHandler.enabled = false;

                // [Sửa lỗi] Lưu hướng đi trước khi coroutine snap tọa độ player vào tâm điểm D
                Vector3 finalDirection = (pointD.position - playerTransform.position).normalized;

                yield return StartCoroutine(MoveCharacterToPoint(playerTransform, playerAnimator, pointD.position, playerWalkSpeed));

                // [Sửa lỗi] Cập nhật lại netLastInput cho PlayerMovement thông qua LookTowards 
                // Sử dụng vị trí ảo (vị trí hiện tại + hướng) để tránh lỗi vector (0,0)
                if (playerMovement != null && finalDirection != Vector3.zero)
                {
                    playerMovement.LookTowards(playerTransform.position + finalDirection);
                }

                if (playerAnimHandler != null) playerAnimHandler.enabled = true;
            }

            GameStateManager.EndLoading();

            if (SaveController.Instance != null)
            {
                SaveController.Instance.MarkCollected(SceneManager.GetActiveScene().name, eventSaveID);
                SaveController.Instance.TriggerAutoSave();
            }
        }

        private IEnumerator MoveCharacterToPoint(Transform charTransform, Animator charAnimator, Vector3 targetPoint, float speed)
        {
            Vector3 destination = new Vector3(targetPoint.x, targetPoint.y, charTransform.position.z);

            charAnimator.SetBool("isWalking", true);

            while (Vector3.Distance(charTransform.position, destination) > 0.05f)
            {
                Vector3 direction = (destination - charTransform.position).normalized;

                charAnimator.SetFloat("InputX", direction.x);
                charAnimator.SetFloat("InputY", direction.y);
                charAnimator.SetFloat("LastInputX", direction.x);
                charAnimator.SetFloat("LastInputY", direction.y);

                charTransform.position = Vector3.MoveTowards(charTransform.position, destination, speed * Time.deltaTime);
                yield return null;
            }

            charTransform.position = destination;

            charAnimator.SetBool("isWalking", false);
            charAnimator.SetFloat("InputX", 0);
            charAnimator.SetFloat("InputY", 0);
        }
    }
}