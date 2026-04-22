using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
public class TimeManager : MonoBehaviour
{
    public static TimeManager Instance { get; private set; }

    [Header("Time Settings")]
    public float dayDurationInRealMinutes = 48f;
    public float currentTimeOfDay = 6f;
    public int currentDay = 1;

    [Header("Lighting Settings")]
    [SerializeField] private Light2D globalLight;
    [SerializeField] private Gradient lightColorGradient;
    [SerializeField] private AnimationCurve lightIntensityCurve;

    // Các sự kiện
    public Action<int, int> OnTimeChanged;
    public Action<int> OnDayChanged;

    private int lastHour = -1;
    private int lastMinute = -1;
    private float timeMultiplier;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        timeMultiplier = 24f / (dayDurationInRealMinutes * 60f);

        TryFindGlobalLight();
    }

    private void Update()
    {
        currentTimeOfDay += Time.deltaTime * timeMultiplier;

        if (currentTimeOfDay >= 24f)
        {
            currentTimeOfDay -= 24f;
            currentDay++;
            OnDayChanged?.Invoke(currentDay);
        }

        CalculateTime();

        UpdateLighting();
    }

    private void CalculateTime()
    {
        int currentHour = Mathf.FloorToInt(currentTimeOfDay);
        int currentMinute = Mathf.FloorToInt((currentTimeOfDay - currentHour) * 60f);

        if (currentHour != lastHour || currentMinute != lastMinute)
        {
            lastHour = currentHour;
            lastMinute = currentMinute;

            OnTimeChanged?.Invoke(currentHour, currentMinute);
        }
    }

    private void UpdateLighting()
    {
        if (globalLight == null)
        {
            TryFindGlobalLight();
            if (globalLight == null) return;
        }

        float timePercent = currentTimeOfDay / 24f;

        globalLight.color = lightColorGradient.Evaluate(timePercent);
        globalLight.intensity = lightIntensityCurve.Evaluate(timePercent);
    }

    private void TryFindGlobalLight()
    {
        Light2D[] lights = FindObjectsByType<Light2D>(FindObjectsSortMode.None);
        foreach (Light2D light in lights)
        {
            if (light.lightType == Light2D.LightType.Global)
            {
                globalLight = light;
                return;
            }
        }
    }

    // Gọi hàm này khi người chơi tương tác với giường ngủ
    public void SleepUntilMorning(float wakeUpHour = 6f)
    {
        if (currentTimeOfDay > wakeUpHour)
        {
            currentDay++;
            OnDayChanged?.Invoke(currentDay);
        }

        currentTimeOfDay = wakeUpHour;
        CalculateTime();
        UpdateLighting();

        PlayerStats stats = FindFirstObjectByType<PlayerStats>();
        if (stats != null)
        {
            stats.RefreshStats();
        }
    }
}