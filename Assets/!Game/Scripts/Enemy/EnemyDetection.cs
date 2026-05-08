using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    public Enemy enemyChase;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerController"))
        {
            enemyChase.OnPlayerDetected(other.transform);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("PlayerController"))
        {
            enemyChase.OnPlayerLost(other.transform);
        }
    }
}