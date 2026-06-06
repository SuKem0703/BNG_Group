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
        [SerializeField] private GameObject npcActor;
        [SerializeField] private GameObject npcObject;
        [SerializeField] private bool hideActorAtEnd = true; 

        [Header("NPC Movement Path")]
        [SerializeField] private Transform pointA;
        [SerializeField] private Transform pointB;
        [SerializeField] private Transform pointC;
        [SerializeField] private float npcWalkSpeed = 2f;
        [SerializeField] private float waitBeforeDie = 0.5f;

        [Header("Player Actions")]
        [SerializeField] private Transform pointD;
        [SerializeField] private float playerRunSpeed = 5f; 

        [Header("Camera Logic")]
        [SerializeField] private float cameraBlendDuration = 2f;

        private bool eventTriggered = false;

        private void Start()
        {
            if (npcActor != null) npcActor.SetActive(false);

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
                
                if (npcActor != null && pointC != null)
                {
                    if (hideActorAtEnd)
                    {
                        npcActor.SetActive(false);
                    }
                    else
                    {
                        Vector3 endPos = new Vector3(pointC.position.x, pointC.position.y, npcActor.transform.position.z);
                        npcActor.transform.position = endPos;
                        npcActor.SetActive(true);

                        if (pointB != null && animator != null)
                        {
                            Vector3 finalDir = (pointC.position - pointB.position).normalized;
                            animator.SetFloat("LastInputX", finalDir.x);
                            animator.SetFloat("LastInputY", finalDir.y);
                        }
                    }
                }

                if (animator != null && !hideActorAtEnd) animator.SetTrigger("Die");

                if (npcObject != null && pointC != null)
                {
                    Vector3 npcEndPos = new Vector3(pointC.position.x, pointC.position.y, npcObject.transform.position.z);
                    npcObject.transform.position = npcEndPos;
                    npcObject.SetActive(true);
                }
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
            if (npcActor == null || pointA == null || pointB == null || pointC == null || animator == null)
            {
                Debug.LogError("[DyingNPC Event] LỖI: Thiếu tham chiếu tới các điểm A, B, C hoặc NPC Actor!");
                yield break;
            }

            GameStateManager.StartLoading();

            if (npcObject != null)
            {
                npcObject.SetActive(false);
            }

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

            Vector3 startPos = new Vector3(pointA.position.x, pointA.position.y, npcActor.transform.position.z);
            npcActor.transform.position = startPos;
            npcActor.SetActive(true);

            if (playerMovement != null)
            {
                playerMovement.LookTowards(npcActor.transform.position);
            }

            if (vCam != null)
            {
                vCam.Target.TrackingTarget = npcActor.transform;
                yield return new WaitForSeconds(cameraBlendDuration);
            }

            yield return StartCoroutine(MoveCharacterToPoint(npcActor.transform, animator, pointB.position, npcWalkSpeed));
            yield return StartCoroutine(MoveCharacterToPoint(npcActor.transform, animator, pointC.position, npcWalkSpeed));

            yield return new WaitForSeconds(waitBeforeDie);
            animator.SetTrigger("Die");
            yield return new WaitForSeconds(2.5f);

            if (vCam != null)
            {
                vCam.Target.TrackingTarget = originalTarget != null ? originalTarget : playerTransform;
                yield return new WaitForSeconds(cameraBlendDuration);
            }

            if (hideActorAtEnd)
            {
                npcActor.SetActive(false);
            }

            if (npcObject != null)
            {
                Vector3 realNpcEndPos = new Vector3(pointC.position.x, pointC.position.y, npcObject.transform.position.z);
                npcObject.transform.position = realNpcEndPos;
                npcObject.SetActive(true);
            }

            if (playerTransform != null && pointD != null && playerAnimator != null)
            {
                if (playerAnimHandler != null) playerAnimHandler.enabled = false;

                Vector3 finalDirection = (pointD.position - playerTransform.position).normalized;
                
                yield return StartCoroutine(MoveCharacterToPoint(playerTransform, playerAnimator, pointD.position, playerRunSpeed, true));

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

        private IEnumerator MoveCharacterToPoint(Transform charTransform, Animator charAnimator, Vector3 targetPoint, float speed, bool isRunningAnim = false)
        {
            Vector3 destination = new Vector3(targetPoint.x, targetPoint.y, charTransform.position.z);

            string currentAnimParam = isRunningAnim ? "isRunning" : "isWalking";
            charAnimator.SetBool(currentAnimParam, true);

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

            charAnimator.SetBool(currentAnimParam, false);
            charAnimator.SetFloat("InputX", 0);
            charAnimator.SetFloat("InputY", 0);
        }
    }
}