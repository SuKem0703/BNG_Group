using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(PolygonCollider2D))]
public class EnemyHurtboxHandler : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private SpriteRenderer targetSpriteRenderer;

    private PolygonCollider2D polyCollider;
    private Sprite lastUpdatedSprite = null;

    private void Awake()
    {
        polyCollider = GetComponent<PolygonCollider2D>();
        polyCollider.isTrigger = true; // Bắt buộc là Trigger để nhận đạn và kiếm chém

        if (targetSpriteRenderer == null)
        {
            targetSpriteRenderer = GetComponentInParent<SpriteRenderer>();
        }
    }

    private void Update()
    {
        // Sửa lỗi: so sánh với null thay vì long.MinValue
        if (targetSpriteRenderer != null && targetSpriteRenderer.sprite != lastUpdatedSprite)
        {
            UpdateColliderToCurrentSprite();
        }
    }

    private void UpdateColliderToCurrentSprite()
    {
        Sprite currentSprite = targetSpriteRenderer.sprite;
        if (currentSprite == null) return;

        lastUpdatedSprite = currentSprite;

        int shapeCount = currentSprite.GetPhysicsShapeCount();
        polyCollider.pathCount = shapeCount;

        List<Vector2> pathPoints = new List<Vector2>();
        for (int i = 0; i < shapeCount; i++)
        {
            pathPoints.Clear();
            currentSprite.GetPhysicsShape(i, pathPoints);
            polyCollider.SetPath(i, pathPoints);
        }
    }
}