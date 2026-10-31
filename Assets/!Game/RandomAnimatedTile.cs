using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "RandomAnimatedTile", menuName = "2D/Tiles/Random Animated Tile")]
public class RandomAnimatedTile : TileBase
{
    public Sprite[] m_AnimatedSprites;
    public float m_MinSpeed = 1f;
    public float m_MaxSpeed = 1.5f;

    public override void GetTileData(Vector3Int position, ITilemap tilemap, ref TileData tileData)
    {
        tileData.transform = Matrix4x4.identity;
        tileData.color = Color.white;

        if (m_AnimatedSprites != null && m_AnimatedSprites.Length > 0)
        {
            // Hiển thị frame đầu tiên làm hình đại diện
            tileData.sprite = m_AnimatedSprites[0];
        }
    }

    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        if (m_AnimatedSprites == null || m_AnimatedSprites.Length == 0)
            return false;

        tileAnimationData.animatedSprites = m_AnimatedSprites;
        tileAnimationData.animationSpeed = Random.Range(m_MinSpeed, m_MaxSpeed);

        // Dùng tọa độ (x, y) của từng ô tile làm seed để tạo độ trễ (start time) ngẫu nhiên riêng biệt
        int seed = position.x * 73856093 ^ position.y * 19349663;
        Random.InitState(seed);
        tileAnimationData.animationStartTime = Random.Range(0f, 10f);

        return true; // Phải trả về true để Unity biết đây là Tile động và tự động chạy animation!
    }
}