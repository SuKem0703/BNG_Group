using UnityEngine;
using DG.Tweening;
using System;
using TMPro;

public class GoldVisualEffect : MonoBehaviour
{
    [SerializeField] private float dropDuration = 0.5f;
    [SerializeField] private float dropRadius = 1.2f;
    [SerializeField] private float flySpeed = 15f;
    [SerializeField] private float jumpPower = 1.5f;
    [SerializeField] private TMP_Text amountText;

    private bool isFlyingToPlayer = false;
    private Transform targetPlayer;

    public Action<GoldVisualEffect> OnGoldCollected;

    public void Spawn(Vector3 spawnPos, int amount)
    {
        transform.position = spawnPos;
        isFlyingToPlayer = false;
        gameObject.SetActive(true);

        if (amountText != null)
        {
            amountText.gameObject.SetActive(amount > 1);
            if (amount > 1)
            {
                amountText.SetText("{0}", amount);
            }
        }

        Vector2 randomCircle = UnityEngine.Random.insideUnitCircle * dropRadius;
        Vector3 dropTarget = spawnPos + new Vector3(randomCircle.x, randomCircle.y, 0);

        transform.DOJump(dropTarget, jumpPower, 1, dropDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(StartFlyingToPlayer);
    }

    private void StartFlyingToPlayer()
    {
        if (PlayerStats.Instance != null)
        {
            targetPlayer = PlayerStats.Instance.transform;
            isFlyingToPlayer = true;
        }
        else
        {
            ReturnToPool();
        }
    }

    private void Update()
    {
        if (isFlyingToPlayer && targetPlayer != null)
        {
            transform.position = Vector3.MoveTowards(transform.position, targetPlayer.position, flySpeed * Time.deltaTime);

            if (Vector3.Distance(transform.position, targetPlayer.position) < 0.2f)
            {
                isFlyingToPlayer = false;
                ReturnToPool();
            }
        }
    }

    private void ReturnToPool()
    {
        OnGoldCollected?.Invoke(this);
        gameObject.SetActive(false);
    }

    private void OnDisable()
    {
        transform.DOKill();
    }
}