using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public enum TimePeriod
{
    Morning,    // 06:00 - 11:59
    Afternoon,  // 12:00 - 16:59
    Evening,    // 17:00 - 19:59
    Night       // 20:00 - 05:59
}

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

    public Action<int, int> OnTimeChanged;
    public Action<int> OnDayChanged;
    public Action<TimePeriod> OnPeriodChanged;

    private int lastHour = -1;
    private int lastMinute = -1;
    private TimePeriod lastPeriod;
    private float timeMultiplier;

    public static TimePeriod CurrentPeriod
    {
        get
        {
            if (Instance == null) return TimePeriod.Morning;
            float t = Instance.currentTimeOfDay;
            if (t >= 6f && t < 12f) return TimePeriod.Morning;
            if (t >= 12f && t < 17f) return TimePeriod.Afternoon;
            if (t >= 17f && t < 20f) return TimePeriod.Evening;
            return TimePeriod.Night;
        }
    }

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        timeMultiplier = 24f / (dayDurationInRealMinutes * 60f);
        lastPeriod = CurrentPeriod;
        TryFindGlobalLight();
    }

    private void Update()
    {
        if (PauseController.IsGamePause) return;

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

        TimePeriod current = CurrentPeriod;
        if (current != lastPeriod)
        {
            lastPeriod = current;
            OnPeriodChanged?.Invoke(lastPeriod);
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