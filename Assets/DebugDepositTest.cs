using UnityEngine;
using UnityEngine.UI;

public class DebugDepositTest : MonoBehaviour
{
    [Header("Kéo Item đang bị lỗi vào đây")]
    public Item targetItem;

    [Header("Thông tin giả lập")]
    public string targetChestID = "1.0_Trong nhà_-11_7"; // Copy ChestID từ Inspector của rương
    public int targetSlotIndex = 0;

    // Gắn hàm này vào Button OnClick
    public void TestDeposit()
    {
        if (targetItem == null)
        {
            Debug.LogError("[Debug] Chưa gán Target Item!");
            return;
        }

        Debug.Log($"[Debug] Bắt đầu test cất đồ...");
        Debug.Log($"[Debug] Item Name: {targetItem.Name}");
        Debug.Log($"[Debug] Item DB ID (Client đang giữ): {targetItem.dbID}"); // <--- Quan trọng nhất là số này
        Debug.Log($"[Debug] Chest ID gửi đi: {targetChestID}");

        if (targetItem.dbID == 0)
        {
            Debug.LogError("[Debug] LỖI: Item này có DB ID = 0. Server chắc chắn sẽ trả về 404!");
            return;
        }

        // Gọi API trực tiếp, bỏ qua mọi logic UI
        InventoryService.Instance.RequestDeposit(
            targetItem.dbID,
            targetChestID,
            targetSlotIndex,
            (success) =>
            {
                if (success)
                {
                    Debug.Log("<color=green>[Debug] Cất đồ THÀNH CÔNG! API hoạt động tốt.</color>");
                }
                else
                {
                    Debug.LogError("<color=red>[Debug] Cất đồ THẤT BẠI! Server không tìm thấy ID này.</color>");
                }
            }
        );
    }
}