using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class SortingOrderByY : MonoBehaviour
{
    private SpriteRenderer sr;
    public float offset = 0f;
    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        sr.sortingLayerName = "Player";
        sr.sortingOrder = -(int)(transform.position.y * 100);
    }
}
