using System;
using UnityEngine;

[ExecuteInEditMode]
public class SnapToGrid : MonoBehaviour
{
    public float gridSize = 1f;

    private void Update()
    {
        if (!Application.isPlaying)
        {
            if (gridSize <= 0f) gridSize = 1f;

            Vector3 pos = transform.position;
            transform.position = new Vector3
                (
                    Mathf.Round(pos.x / gridSize) * gridSize,
                    Mathf.Round(pos.y / gridSize) * gridSize,
                    Mathf.Round(pos.z / gridSize) * gridSize
                );
        }
    }
}
