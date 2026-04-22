using UnityEngine;
using Unity.Cinemachine;
using System;

public class MapTransition : MonoBehaviour
{
    [SerializeField] PolygonCollider2D mapBoundary;
    CinemachineConfiner2D confiner;
    [SerializeField] Transform teleportTargetPosition;

    void Awake()
    {
        if (mapBoundary == null) {
            mapBoundary = GetComponent<PolygonCollider2D>();
        }

        if (confiner == null)
            confiner = FindFirstObjectByType<CinemachineConfiner2D>();
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.gameObject.CompareTag("Player"))
        {
            if (confiner != null && mapBoundary != null)
            {
                confiner.BoundingShape2D = mapBoundary;
                UpdatePlayerPosition();
            }
        }
    }

    void UpdatePlayerPosition()
    {
        if (teleportTargetPosition != null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                player.transform.position = teleportTargetPosition.position;
            }
        }
    }
}
