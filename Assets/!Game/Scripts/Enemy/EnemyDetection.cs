using UnityEngine;
using Unity.Netcode;

public class EnemyDetection : MonoBehaviour
{
    public Enemy enemyChase;

    [Header("Leash Settings")]
    public float maxLeashDistance = 15f;
    private Vector2 spawnOrigin;
    private bool isLeashActive = false;

    public void SetSpawnOrigin(Vector2 origin)
    {
        spawnOrigin = origin;
        isLeashActive = true;
    }

    private void Update()
    {
        if (!isLeashActive || enemyChase == null || !enemyChase.IsServer || enemyChase.isDead) return;

        if (Vector2.Distance(transform.position, spawnOrigin) > maxLeashDistance)
        {
            BreakLeash();
        }
    }

    private void BreakLeash()
    {
        isLeashActive = false;

        var ai = enemyChase.GetComponent<EnemyCombatAI>();
        if (ai != null && ai.player != null)
        {
            ai.OnPlayerLost(ai.player);
            ai.StopMovement();
        }

        if (enemyChase.IsServer)
        {
            enemyChase.netHealth.Value = 0;
            enemyChase.Die(false);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerController"))
        {
            enemyChase.OnPlayerDetected(other.transform);
        }
    }

/*
    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("PlayerController"))
        {
            enemyChase.OnPlayerLost(other.transform);
        }
    }
*/

}