using UnityEngine;
using UnityEngine.UI;

public class Effect : MonoBehaviour
{
    [Header("UI")]
    public Image iconImage;
    public Image overlayImage;

    [Header("Effect Settings")]
    public string effectID;

    public float duration;
    public float value;

    private float timer;
    private PlayerVitals targetVitals;
    private PlayerStats targetStats;
    private bool isActive = false;

    // Biến cho DoT (Damage over Time)
    private float tickTimer;
    private float tickInterval = 1.0f;

    public void Initialize(GameObject target, float duration, float value)
    {
        this.targetVitals = target.GetComponent<PlayerVitals>();
        this.duration = duration;
        this.value = value;

        ApplyEffect();

        if (duration <= 0f)
        {
            isActive = false;
            Destroy(gameObject);
        }
        else
        {
            timer = duration;
            isActive = true;
        }
    }

    private void Update()
    {
        if (!isActive) return;

        timer -= Time.deltaTime;

        if (overlayImage != null)
            overlayImage.fillAmount = 1f - (timer / duration);

        if (effectID == "BURN_FIRE")
        {
            HandleDoT();
        }

        if (timer <= 0f)
        {
            RemoveEffect();
            Destroy(gameObject);
        }
    }

    private void HandleDoT()
    {
        tickTimer += Time.deltaTime;
        if (tickTimer >= tickInterval)
        {
            if (targetVitals != null)
            {
                targetVitals.TakeDamage(Mathf.RoundToInt(value));
            }
            tickTimer = 0f;
        }
    }

    private void ApplyEffect()
    {
        if (targetVitals == null) return;

        switch (effectID)
        {
            case "HEAL_INSTANT":
                targetVitals.HealActiveCharacter(Mathf.RoundToInt(value));
                SoundEffectManager.Play("Use Pot");
                break;
            case "MANA_INSTANT":
                targetVitals.RecoverMPActiveCharacter(Mathf.RoundToInt(value));
                break;

            case "SWAP_CD":
                break;

            case "BUFF_STR":
                targetStats.ModifyEffectStat("STR", Mathf.RoundToInt(value));
                break;
            case "BUFF_DEX":
                targetStats.ModifyEffectStat("DEX", Mathf.RoundToInt(value));
                break;
            case "DEBUFF_DEX":
                targetStats.ModifyEffectStat("DEX", -Mathf.RoundToInt(value));
                break;

            case "BURN_FIRE":
                targetVitals.TakeDamage(Mathf.RoundToInt(value));
                break;
        }
    }

    private void RemoveEffect()
    {
        if (targetVitals == null) return;

        switch (effectID)
        {
            case "HEAL_INSTANT":
            case "MANA_INSTANT":
            case "BURN_FIRE":
                break;

            case "SWAP_CD":
                break;

            case "BUFF_STR":
                targetStats.ModifyEffectStat("STR", -Mathf.RoundToInt(value));
                break;
            case "BUFF_DEX":
                targetStats.ModifyEffectStat("DEX", -Mathf.RoundToInt(value));
                break;
            case "DEBUFF_DEX":
                targetStats.ModifyEffectStat("DEX", Mathf.RoundToInt(value));
                break;
        }
        isActive = false;
    }
}