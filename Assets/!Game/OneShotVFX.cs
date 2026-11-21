using UnityEngine;

public class OneShotVFX : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField, Min(0.1f)] private float lifeTime = 1f;

    private void Awake()
    {
        if (animator == null) animator = GetComponent<Animator>();
        if (animator == null) animator = GetComponentInChildren<Animator>(true);
    }

    public void Initialize()
    {
        Destroy(gameObject, lifeTime);
    }
}