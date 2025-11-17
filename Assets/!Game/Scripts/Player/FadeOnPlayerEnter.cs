using UnityEngine;
using UnityEngine.Tilemaps;

public class FadeTilemapOnPlayerEnter : MonoBehaviour
{
    public float fadeAlpha = 0.4f;
    private Tilemap tilemap;
    private Color originalColor;

    private int playerInsideCount = 0; // Đếm số collider Player đang ở trong vùng

    void Start()
    {
        tilemap = GetComponent<Tilemap>();
        originalColor = tilemap.color;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInsideCount++;

            if (playerInsideCount == 1) // Chỉ khi Player mới vừa vào
            {
                Color faded = originalColor;
                faded.a = fadeAlpha;
                tilemap.color = faded;
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInsideCount--;

            if (playerInsideCount <= 0)
            {
                playerInsideCount = 0; // tránh giá trị âm
                tilemap.color = originalColor;
            }
        }
    }
}
