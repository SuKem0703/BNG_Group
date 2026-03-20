using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[RequireComponent(typeof(BoxCollider2D))]
public class BedInteractable : MonoBehaviour, IInteractable, ITargetableInfo
{
    [Header("Bed Info")]
    public string bedName = "Giường ngủ";
    public Sprite bedIcon;
    public float wakeUpHour = 6f;

    // [Thêm mới] Các thiết lập về thời gian được phép ngủ
    [Header("Sleep Conditions")]
    [Tooltip("Giờ bắt đầu có thể đi ngủ (VD: 20 là 8h tối)")]
    public float sleepStartHour = 20f;
    [Tooltip("Giờ kết thúc khoảng thời gian ngủ (VD: 6 là 6h sáng)")]
    public float sleepEndHour = 6f;

    [Header("Checkpoint Settings")]
    [Tooltip("Vị trí nhân vật sẽ hồi sinh nếu load lại game. Tránh để ngay giữa giường để không bị kẹt collider.")]
    public Transform spawnPoint;

    [Header("Effects")]
    [Tooltip("Một Image UI màu đen phủ toàn màn hình để làm hiệu ứng")]
    public Image fadeOverlay;

    private bool isSleeping = false;

    // [Thêm mới] Component để hiển thị thoại khi chưa đến giờ ngủ
    private Monologue monologueComponent;

    // [Cập nhật] Lấy component Monologue
    private void Awake()
    {
        monologueComponent = GetComponent<Monologue>();
    }

    private void Start()
    {
        GetComponent<BoxCollider2D>().isTrigger = true;

        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(false);
            Color c = fadeOverlay.color;
            c.a = 0;
            fadeOverlay.color = c;
        }

        if (spawnPoint == null)
        {
            spawnPoint = transform;
        }
    }

    // ==================================================
    // IInteractable Implementation
    // ==================================================
    public bool CanInteract()
    {
        // Điều kiện: Chưa ngủ, game không khóa và dữ liệu đã load xong
        return !isSleeping && GameStateManager.CanProcessInput() && SaveController.IsDataLoaded;
    }

    // [Cập nhật] Xử lý logic kiểm tra giờ giấc trước khi gọi bảng hỏi
    public void Interact()
    {
        if (TimeManager.Instance == null) return;

        float currentHour = TimeManager.Instance.currentTimeOfDay;
        bool canSleep = false;

        // Xử lý logic thời gian vắt ngang qua đêm (VD: từ 20h tối đến 6h sáng hôm sau)
        if (sleepStartHour > sleepEndHour)
        {
            canSleep = currentHour >= sleepStartHour || currentHour <= sleepEndHour;
        }
        else // Logic thời gian trong ngày (VD: ngủ trưa từ 12h đến 14h)
        {
            canSleep = currentHour >= sleepStartHour && currentHour <= sleepEndHour;
        }

        // Nếu trong giờ ngủ -> Mở UI xác nhận
        if (canSleep)
        {
            ShowSleepConfirm();
        }
        // Nếu chưa tới giờ ngủ -> Hiển thị độc thoại
        else
        {
            if (monologueComponent != null)
            {
                monologueComponent.OpenDialogOnTrigger();
            }
            else
            {
                Debug.LogWarning("[Bed] Không có component Monologue để hiển thị thoại!");
            }
        }
    }

    // [Thêm mới] Tách logic mở ConfirmUI ra thành hàm riêng để hàm Interact gọn gàng hơn
    private void ShowSleepConfirm()
    {
        if (LoadResourceManager.Instance != null && LoadResourceManager.Instance.ConfirmUIPrefab != null)
        {
            GameObject confirmObj = Instantiate(LoadResourceManager.Instance.ConfirmUIPrefab);
            ConfirmUIController confirmUI = confirmObj.GetComponent<ConfirmUIController>();

            if (confirmUI != null)
            {
                GameStateManager.IsDialogueActive = true;
                PauseController.SetPause(true);

                confirmUI.Show("Bạn có muốn đi ngủ đến sáng mai không?", ConfirmSleep);
                confirmUI.noButton.onClick.AddListener(CancelSleep);
            }
        }
        else
        {
            Debug.LogWarning("[Bed] Không tìm thấy ConfirmUIPrefab trong LoadResourceManager!");
        }
    }

    // ==================================================
    // UI Button Callbacks
    // ==================================================
    private void ConfirmSleep()
    {
        // ConfirmUIController đã tự Destroy nó, ta chỉ việc chạy Coroutine ngủ
        StartCoroutine(SleepSequenceRoutine());
    }

    private void CancelSleep()
    {
        // Trả lại quyền điều khiển nếu người chơi ấn Không
        GameStateManager.IsDialogueActive = false;
        PauseController.SetPause(false);
    }

    // ==================================================
    // Sleep Sequence & Checkpoint Logic
    // ==================================================
    private IEnumerator SleepSequenceRoutine()
    {
        isSleeping = true;

        // 1. Màn hình từ từ tối đi
        if (fadeOverlay != null)
        {
            fadeOverlay.gameObject.SetActive(true);
            float elapsed = 0f;
            Color c = fadeOverlay.color;
            while (elapsed < 1.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(elapsed / 1.5f);
                fadeOverlay.color = c;
                yield return null;
            }
        }

        // 2. Thực hiện lưu Checkpoint
        SaveCheckpoint();

        // 3. Thực hiện logic chuyển thời gian và hồi thể lực
        if (TimeManager.Instance != null)
        {
            TimeManager.Instance.SleepUntilMorning(wakeUpHour);
        }

        Debug.Log("[Bed] Đang ngủ... Zzz");

        // Đợi 2 giây ngoài đời thực để người chơi cảm nhận thời gian qua đi
        yield return new WaitForSecondsRealtime(2f);

        // 4. Màn hình từ từ sáng lên
        if (fadeOverlay != null)
        {
            float elapsed = 0f;
            Color c = fadeOverlay.color;
            while (elapsed < 1.5f)
            {
                elapsed += Time.unscaledDeltaTime;
                c.a = Mathf.Clamp01(1f - (elapsed / 1.5f));
                fadeOverlay.color = c;
                yield return null;
            }
            fadeOverlay.gameObject.SetActive(false);
        }

        // 5. Mở khóa nhân vật và hoàn tất
        GameStateManager.IsDialogueActive = false;
        PauseController.SetPause(false);
        isSleeping = false;
    }

    private void SaveCheckpoint()
    {
        if (!SaveController.IsDataLoaded || SaveController.IsSaving) return;

        Vector3 savePos = spawnPoint != null ? spawnPoint.position : transform.position;
        string sceneName = SceneManager.GetActiveScene().name;

        // Đặt tọa độ hồi sinh
        SaveController.nextSpawnPosition = savePos;
        SaveController.pendingSceneName = sceneName;

        SaveController.Instance.SetCheckpoint(sceneName, savePos);

        // Thực hiện ghi file save
        SaveController.Instance.SaveGame(SaveReason.Checkpoint, (isSuccess) =>
        {
            Debug.Log("[Bed] Lưu game sau khi ngủ hoàn tất!");
        });
    }

    // ==================================================
    // ITargetableInfo Implementation
    // ==================================================
    public TargetInfoData GetInfo()
    {
        return new TargetInfoData(bedName, bedIcon, "Ngủ qua đêm", TargetType.NPC);
    }
}