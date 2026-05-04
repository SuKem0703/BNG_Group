using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class CombatTargetSelector : MonoBehaviour
{
    public static CombatTargetSelector Instance { get; private set; }

    [Header("Settings")]
    public LayerMask enemyLayer;
    public float targetYOffset = 1.2f;

    [Header("Visual")]
    public GameObject indicatorPrefab;

    private GameObject indicatorInstance;
    private Enemy currentTarget;
    public Enemy CurrentTarget => currentTarget;

    private List<Enemy> enemiesInRange = new List<Enemy>();

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        HandleTargetingLogic();
        UpdateIndicatorPosition();
    }

    private void HandleTargetingLogic()
    {
        enemiesInRange.RemoveAll(e => e == null || e.IsDead || !e.gameObject.activeInHierarchy);

        if (enemiesInRange.Count == 0)
        {
            ClearTarget();
            return;
        }

        Enemy closest = enemiesInRange
            .OrderBy(e => Vector2.Distance(transform.position, e.transform.position))
            .FirstOrDefault();

        if (closest != null && closest != currentTarget)
        {
            SetTarget(closest);
        }
    }

    private void SetTarget(Enemy newTarget)
    {
        ClearTarget();
        currentTarget = newTarget;

        if (indicatorPrefab != null)
        {
            indicatorInstance = Instantiate(indicatorPrefab);
        }
    }

    private void ClearTarget()
    {
        currentTarget = null;
        if (indicatorInstance != null)
        {
            Destroy(indicatorInstance);
            indicatorInstance = null;
        }
    }

    private void UpdateIndicatorPosition()
    {
        if (indicatorInstance != null && currentTarget != null)
        {
            float floatEffect = Mathf.Sin(Time.time * 5f) * 0.05f;
            indicatorInstance.transform.position = currentTarget.transform.position + new Vector3(0, targetYOffset + floatEffect, 0);
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Enemy enemy))
        {
            if (!enemiesInRange.Contains(enemy)) enemiesInRange.Add(enemy);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.TryGetComponent(out Enemy enemy))
        {
            enemiesInRange.Remove(enemy);
            if (currentTarget == enemy) ClearTarget();
        }
    }

    public Vector2 GetAimDirection(Vector2 basePosition, Vector2 fallbackDirection)
    {
        if (currentTarget != null)
        {
            return ((Vector2)currentTarget.transform.position - basePosition).normalized;
        }
        return fallbackDirection;
    }
}