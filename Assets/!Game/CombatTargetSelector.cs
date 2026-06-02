using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class CombatTargetSelector : NetworkBehaviour
{
    [Header("Settings")]
    public LayerMask enemyLayer;
    public float targetYOffset = 1.2f;
    public float floatAmplitude = 0.05f;
    public float floatSpeed = 5f;

    [Header("Visual")]
    public GameObject indicatorPrefab;

    private Enemy currentTarget;
    private Renderer currentTargetRenderer;
    public Enemy CurrentTarget => currentTarget;

    private List<Enemy> enemiesInRange = new List<Enemy>();

    public static event Action<Enemy> OnEnemyTargetChanged;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            InteractionDetector.InitSharedIndicator(indicatorPrefab);
        }
    }

    private void Update()
    {
        if (!IsOwner) return;

        HandleTargetingLogic();
        UpdateIndicatorPosition();
    }

    private void HandleTargetingLogic()
    {
        enemiesInRange.RemoveAll(e => e == null || e.IsDead || !e.gameObject.activeInHierarchy);

        if (enemiesInRange.Count == 0)
        {
            if (currentTarget != null)
            {
                ClearTarget();
            }
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
        if (currentTarget == newTarget) return;

        currentTarget = newTarget;

        if (currentTarget != null)
        {
            currentTargetRenderer = currentTarget.GetComponentInChildren<Renderer>();
        }

        if (InteractionDetector.SharedIndicator != null)
        {
            InteractionDetector.SharedIndicator.SetActive(true);
        }

        OnEnemyTargetChanged?.Invoke(currentTarget);
    }

    private void ClearTarget()
    {
        if (currentTarget == null) return;

        currentTarget = null;
        currentTargetRenderer = null;

        if (InteractionDetector.SharedIndicator != null)
        {
            InteractionDetector.SharedIndicator.SetActive(false);
        }

        OnEnemyTargetChanged?.Invoke(null);
    }

    private void UpdateIndicatorPosition()
    {
        if (InteractionDetector.SharedIndicator != null && InteractionDetector.SharedIndicator.activeSelf && currentTarget != null)
        {
            Vector3 targetCenter = GetEnemyCenterPosition(currentTarget);
            float dynamicYOffset = targetYOffset + (Mathf.Sin(Time.time * floatSpeed) * floatAmplitude);

            InteractionDetector.SharedIndicator.transform.position = targetCenter + new Vector3(0, dynamicYOffset, 0);

            if (InteractionDetector.SharedIndicatorRenderer != null && currentTargetRenderer != null)
            {
                InteractionDetector.SharedIndicatorRenderer.sortingOrder = currentTargetRenderer.sortingOrder + 1;
            }
        }
    }

    private Vector3 GetEnemyCenterPosition(Enemy target)
    {
        Collider2D[] colliders = target.GetComponentsInChildren<Collider2D>();
        if (colliders.Length > 0)
        {
            Collider2D targetCol = colliders.FirstOrDefault(c => !c.isTrigger);
            if (targetCol == null) targetCol = colliders[0];

            return new Vector3(targetCol.bounds.center.x, targetCol.bounds.max.y, target.transform.position.z);
        }
        return target.transform.position;
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (!IsOwner) return;

        if (collision.TryGetComponent(out Enemy enemy))
        {
            if (!enemiesInRange.Contains(enemy)) enemiesInRange.Add(enemy);
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!IsOwner) return;

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