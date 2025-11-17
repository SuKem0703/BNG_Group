using UnityEngine;

public class SlimeSpawner : MonoBehaviour
{
    public GameObject slimePrefab;  // Prefab của slime
    public BoxCollider2D spawnArea;   // BoxCollider làm vùng spawn
    public int maxSlimes = 4;       // Tối đa số slime có thể spawn

    private int currentSlimes = 0;  // Số slime hiện tại trong khu vực

    void Start()
    {
        // Kiểm tra nếu spawnArea chưa được thiết lập
        if (spawnArea == null)
        {
            Debug.LogError("Spawn Area chưa được thiết lập!");
            return;
        }

        SpawnSlimes(); // Gọi để spawn slime khi bắt đầu
    }

    // Hàm spawn slime
    void SpawnSlimes()
    {
        while (currentSlimes < maxSlimes)
        {
            // Tạo vị trí ngẫu nhiên trong vùng spawn (dựa trên BoxCollider)
            Vector3 spawnPosition = new Vector3(
                Random.Range(spawnArea.bounds.min.x, spawnArea.bounds.max.x),
                spawnArea.transform.position.y,  // Giữ vị trí Y của spawnArea
                Random.Range(spawnArea.bounds.min.z, spawnArea.bounds.max.z)
            );

            // Spawn slime tại vị trí đã tính toán
            Instantiate(slimePrefab, spawnPosition, Quaternion.identity);

            // Tăng số lượng slime đã spawn
            currentSlimes++;
        }
    }
}
