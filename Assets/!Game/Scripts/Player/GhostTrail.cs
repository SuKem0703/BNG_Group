using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class GhostTrail : MonoBehaviour
{
    public SpriteRenderer targetRenderer;
    public Material ghostMaterial;
    public float fadeDuration = 0.5f;
    public float spawnInterval = 0.05f;
    public int ghostCount = 5;

    private Queue<SpriteRenderer> ghostPool = new Queue<SpriteRenderer>();
    private GameObject poolContainer;

    private bool isInitialized = false;

    private void InitializePool()
    {
        if (isInitialized) return;

        poolContainer = new GameObject(gameObject.name + "_GhostPool");
        poolContainer.transform.SetParent(null);

        DontDestroyOnLoad(poolContainer);

        for (int i = 0; i < ghostCount; i++)
        {
            GameObject ghost = new GameObject("Ghost_" + i);

            ghost.transform.SetParent(poolContainer.transform);

            SpriteRenderer sr = ghost.AddComponent<SpriteRenderer>();
            sr.material = ghostMaterial;
            ghost.SetActive(false);
            ghostPool.Enqueue(sr);
        }

        isInitialized = true;
    }

    public void CreateTrail()
    {
        InitializePool();

        StartCoroutine(SpawnGhosts());
    }

    private IEnumerator SpawnGhosts()
    {
        for (int i = 0; i < ghostCount; i++)
        {
            SpawnGhost();
            yield return new WaitForSeconds(spawnInterval);
        }
    }

    private void SpawnGhost()
    {
        if (ghostPool.Count == 0) return;

        SpriteRenderer sr = ghostPool.Dequeue();
        sr.gameObject.SetActive(true);

        sr.sprite = targetRenderer.sprite;
        sr.transform.position = targetRenderer.transform.position;
        sr.transform.rotation = targetRenderer.transform.rotation;
        sr.transform.localScale = targetRenderer.transform.lossyScale;
        sr.flipX = targetRenderer.flipX;
        sr.sortingLayerID = targetRenderer.sortingLayerID;
        sr.sortingOrder = targetRenderer.sortingOrder - 1;

        sr.color = new Color(1f, 1f, 1f, 1f);

        StartCoroutine(FadeOut(sr));
    }

    private IEnumerator FadeOut(SpriteRenderer sr)
    {
        float elapsed = 0f;
        Color startColor = sr.color;

        while (elapsed < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        sr.gameObject.SetActive(false);
        ghostPool.Enqueue(sr);
    }

    private void OnDestroy()
    {
        if (poolContainer != null)
        {
            Destroy(poolContainer);
        }

        if (ghostPool != null)
        {
            ghostPool.Clear();
        }
    }
}