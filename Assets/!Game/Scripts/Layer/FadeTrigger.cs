using UnityEngine;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Rigidbody2D))]
public class FadeTrigger : MonoBehaviour
{
    [SerializeField] private DynamicSorting target;

    void Awake()
    {
        if (target == null)
            target = GetComponentInParent<DynamicSorting>();

        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    private void OnTriggerEnter2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
            target.SetFade(true);
    }

    private void OnTriggerExit2D(Collider2D col)
    {
        if (col.CompareTag("Player"))
            target.SetFade(false);
    }
}
