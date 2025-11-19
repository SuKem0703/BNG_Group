using System.Collections.Generic;
using UnityEngine;

public class QuestItemManager : MonoBehaviour
{
    // List này tự động quản lý các item con
    [SerializeField] private List<QuestDependentItem> managedItems;

    // ----- THÊM DÒNG NÀY -----
    // List tạm để chứa các item đã bị phá hủy và chờ xóa
    [SerializeField] private List<QuestDependentItem> itemsToRemove = new List<QuestDependentItem>();
    // -------------------------

    void Awake()
    {
        // Tự động tìm tất cả các script QuestDependentItem 
        // trong các object con và nạp vào List
        managedItems = new List<QuestDependentItem>(
            GetComponentsInChildren<QuestDependentItem>(true) // true = bao gồm cả các object đang bị tắt
        );
    }

    void Start()
    {
        // Chờ SaveController load xong
        if (SaveController.IsDataLoaded)
        {
            UpdateAllItems();
        }
        else
        {
            SaveController.OnDataLoaded += HandleDataLoaded;
        }
    }

    private void HandleDataLoaded()
    {
        UpdateAllItems();
        SaveController.OnDataLoaded -= HandleDataLoaded;
    }

    private void OnEnable()
    {
        // Manager lắng nghe sự kiện
        QuestController.OnQuestStatusUpdated += HandleQuestUpdate;
    }

    private void OnDisable()
    {
        QuestController.OnQuestStatusUpdated -= HandleQuestUpdate;
    }

    /// <summary>
    /// Khi 1 quest thay đổi, Manager "can thiệp"
    /// </summary>
    private void HandleQuestUpdate(string updatedQuestID)
    {
        // ----- THÊM DÒNG NÀY -----
        // Dọn dẹp list tạm trước mỗi lần chạy
        itemsToRemove.Clear();
        // -------------------------

        // Duyệt qua List
        foreach (var item in managedItems)
        {
            // ----- BẮT ĐẦU SỬA LỖI -----
            // Kiểm tra xem item có "chết" (bị null) không
            if (item == null)
            {
                // Nếu item đã bị phá hủy, đánh dấu nó để xóa sau
                itemsToRemove.Add(item);
                continue; // Bỏ qua, không chạy code bên dưới
            }
            // ----- KẾT THÚC SỬA LỖI -----

            // Chỉ gọi hàm update của những item nào liên quan đến quest này
            if (item.requiredQuestID == updatedQuestID)
            {
                item.UpdateVisibility();
            }
        }

        // ----- THÊM KHỐI NÀY -----
        // Bây giờ, dọn dẹp các item "chết" ra khỏi List chính
        foreach (var item in itemsToRemove)
        {
            managedItems.Remove(item);
        }
        // -------------------------
    }

    /// <summary>
    /// Chạy 1 lần lúc ban đầu để cập nhật tất cả
    /// </summary>
    private void UpdateAllItems()
    {
        // (Chúng ta cũng nên dọn dẹp ở đây để cho chắc)
        itemsToRemove.Clear();

        foreach (var item in managedItems)
        {
            if (item == null)
            {
                itemsToRemove.Add(item);
                continue;
            }
            item.UpdateVisibility();
        }

        foreach (var item in itemsToRemove)
        {
            managedItems.Remove(item);
        }
    }
}