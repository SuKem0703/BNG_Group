using UnityEngine;

public class EnemyDetection : MonoBehaviour
{
    public Enemy enemyChase;
    private Transform player;
    private void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats.IsOnBattle = true;
            enemyChase.OnPlayerDetected(player);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            PlayerStats.IsOnBattle = false;
            enemyChase.OnPlayerLost();
        }
    }
}
