using UnityEngine;
using System.Collections;
using Unity.Cinemachine;

public class CinemachineShaker : MonoBehaviour
{
    public static CinemachineShaker Instance { get; private set; }

    private CinemachineBasicMultiChannelPerlin activePerlinNoise;
    private Coroutine shakeCoroutine;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // Ensure no perlin component is actively shaking by default
        DisableAllPerlinAtStartup();
    }

    private void DisableAllPerlinAtStartup()
    {
        // Use the newer API that avoids the obsolete FindObjectsOfType(bool) overload
        var perlins = Object.FindObjectsByType<CinemachineBasicMultiChannelPerlin>(FindObjectsSortMode.None);
        foreach (var p in perlins)
        {
            // Force gains to zero so Perlin won't cause passive shaking
            p.AmplitudeGain = 0f;
            p.FrequencyGain = 0f;
        }
    }

    // Try to find the active virtual camera's Noise component
    private void EnsureActivePerlin()
    {
        if (activePerlinNoise != null) return;

        // Iterate over Cinemachine registered virtual cameras and find a live one
        for (int i = 0; i < CinemachineCore.VirtualCameraCount; i++)
        {
            var vcam = CinemachineCore.GetVirtualCamera(i) as CinemachineVirtualCameraBase;
            if (vcam == null) continue;

            // Check if this vcam is live
            if (CinemachineCore.IsLive(vcam))
            {
                var comp = vcam.GetCinemachineComponent(CinemachineCore.Stage.Noise) as CinemachineBasicMultiChannelPerlin;
                if (comp != null)
                {
                    activePerlinNoise = comp;
                    return;
                }
            }
        }

        // Fallback: try to query the Brain's active virtual camera if any camera has a brain
        var brain = Camera.main != null ? Camera.main.GetComponent<CinemachineBrain>() : null;
        if (brain != null)
        {
            var active = brain.ActiveVirtualCamera as CinemachineVirtualCameraBase;
            if (active != null)
            {
                var comp = active.GetCinemachineComponent(CinemachineCore.Stage.Noise) as CinemachineBasicMultiChannelPerlin;
                if (comp != null)
                    activePerlinNoise = comp;
            }
        }
    }

    public void TriggerShake(float intensity, float frequency, float duration)
    {
        EnsureActivePerlin();

        if (activePerlinNoise == null)
        {
            Debug.LogWarning("Không thể rung: không tìm thấy thành phần Noise trên VCam đang hoạt động.");
            return;
        }

        if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            // ensure previous shake stopped and gains cleared
            activePerlinNoise.AmplitudeGain = 0f;
            activePerlinNoise.FrequencyGain = 0f;
        }

        shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, frequency, duration));
    }

    private IEnumerator ShakeRoutine(float intensity, float frequency, float duration)
    {
        if (activePerlinNoise == null) yield break;

        // Apply shake
        activePerlinNoise.AmplitudeGain = intensity;
        activePerlinNoise.FrequencyGain = frequency;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        // When shake ends, explicitly set gains to zero
        activePerlinNoise.AmplitudeGain = 0f;
        activePerlinNoise.FrequencyGain = 0f;
        shakeCoroutine = null;
    }
}