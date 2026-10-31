using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "RandomAnimatedRuleTile", menuName = "2D/Tiles/Random Animated Rule Tile")]
public class RandomAnimatedRuleTile : RuleTile
{
    public override bool GetTileAnimationData(Vector3Int position, ITilemap tilemap, ref TileAnimationData tileAnimationData)
    {
        // Gọi logic gốc của RuleTile để xác định rule nào đang khớp với vị trí ô tile hiện tại
        // Sau đó kiểm tra xem rule đó có đang bật Output: Animation hay không
        foreach (var rule in m_TilingRules)
        {
            // Kiểm tra nếu rule này khớp với tile xung quanh (dùng lại hàm check sẵn của Unity thông qua RuleTile)
            // Tuy nhiên, cách an toàn nhất là ta để Unity tự trả về animation data mặc định trước:
        }

        bool hasAnimation = base.GetTileAnimationData(position, tilemap, ref tileAnimationData);

        if (hasAnimation && tileAnimationData.animatedSprites != null && tileAnimationData.animatedSprites.Length > 0)
        {
            // Dùng tọa độ (x, y) của từng ô tile làm seed độc lập để tạo độ trễ ngẫu nhiên (start time)
            int seed = position.x * 73856093 ^ position.y * 19349663;
            Random.InitState(seed);

            tileAnimationData.animationStartTime = Random.Range(0f, 10f);
            return true;
        }

        return hasAnimation;
    }
}