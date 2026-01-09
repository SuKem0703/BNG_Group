using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class FadeTrigger : MonoBehaviour
{
    [SerializeField] private DynamicSorting target;

    [SerializeField] private string targetLayerName = "Player";
    private int _targetLayerId;

    void Awake()
    {
        if (target == null)
            target = GetComponentInParent<DynamicSorting>();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;

        _targetLayerId = LayerMask.NameToLayer(targetLayerName);
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (IsCorrectLayerAndTag(col))
        {
            target.SetFade(true);
        }
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (IsCorrectLayerAndTag(col))
        {
            target.SetFade(false);
        }
    }

    // Hàm kiểm tra điều kiện kép
    private bool IsCorrectLayerAndTag(Collider2D col)
    {
        if (!col.CompareTag("Player")) return false;

        if (col.gameObject.layer != _targetLayerId) return false;

        return true;
    }
}