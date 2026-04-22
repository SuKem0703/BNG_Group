using UnityEngine;

[ExecuteInEditMode] // Chạy ngay khi không Play game
public class WorldGridLayout : MonoBehaviour
{
    [Header("Cấu hình Lưới")]
    [Tooltip("Số lượng cột tối đa trước khi xuống dòng")]
    [SerializeField] private int columnCount = 5;

    [Tooltip("Kích thước mỗi ô (khoảng cách X, Y)")]
    [SerializeField] private Vector2 cellSize = new Vector2(1f, 1f);

    [Tooltip("Khoảng cách thêm giữa các ô")]
    [SerializeField] private Vector2 spacing = Vector2.zero;

    [Header("Căn chỉnh")]
    [Tooltip("Tâm của lưới bắt đầu từ đâu")]
    [SerializeField] private Vector3 startOffset = Vector3.zero;

    // Nút bấm thủ công nếu không muốn update liên tục
    public bool updateLayout = true;

#if UNITY_EDITOR
    void Update()
    {
        // Chỉ chạy trong Editor để sắp xếp
        if (!Application.isPlaying && updateLayout)
        {
            RepositionChildren();
        }
    }
#endif

    // Hàm gọi khi thêm/bớt con hoặc thay đổi thông số
    [ContextMenu("Force Update Layout")]
    public void RepositionChildren()
    {
        // Duyệt qua tất cả object con
        for (int i = 0; i < transform.childCount; i++)
        {
            Transform child = transform.GetChild(i);

            // Tính toán cột và hàng hiện tại
            int row = i / columnCount;
            int col = i % columnCount;

            // Tính toán vị trí X và Y
            // X tăng dần theo cột
            float posX = col * (cellSize.x + spacing.x);

            // Y giảm dần theo hàng (vì top-down thường xếp từ trên xuống dưới)
            // Hoặc tăng dần tùy game, ở đây mình để giảm dần (xuống dòng)
            float posY = row * -(cellSize.y + spacing.y);

            // Áp dụng vị trí (cộng thêm vị trí gốc của cha)
            // Dùng localPosition để nó đi theo cha
            child.localPosition = startOffset + new Vector3(posX, posY, 0);
        }
    }

    // Vẽ Gizmos để dễ nhìn ô lưới
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 origin = transform.position + startOffset;

        for (int i = 0; i < transform.childCount; i++)
        {
            int row = i / columnCount;
            int col = i % columnCount;
            float posX = col * (cellSize.x + spacing.x);
            float posY = row * -(cellSize.y + spacing.y);

            Vector3 center = origin + new Vector3(posX, posY, 0);
            Gizmos.DrawWireCube(center, new Vector3(cellSize.x, cellSize.y, 0.1f));
        }
    }
}