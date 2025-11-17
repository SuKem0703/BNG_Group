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
    private PlayerStats targetStats;
    private bool isActive = false;
    public void Initialize(GameObject target, float duration, float value)
    {
        this.targetStats = target.GetComponent<PlayerStats>();
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

        if (timer <= 0f)
        {
            RemoveEffect();
            Destroy(gameObject);
        }
    }

    private void ApplyEffect()
    {
        if (targetStats == null) return;

        switch (effectID)
        {
            case "HEAL_INSTANT":
                targetStats.HealActiveCharacter(Mathf.RoundToInt(value));
                SoundEffectManager.Play("Use Pot");
                break;
            case "MANA_INSTANT":
                targetStats.RecoverMPActiveCharacter(Mathf.RoundToInt(value));
                break;

            case "SWAP_CD":
                break;
            case "BUFF_STR":
                targetStats.STR += Mathf.RoundToInt(value);
                break;
            case "BUFF_DEX":
                targetStats.DEX += Mathf.RoundToInt(value);
                break;
            case "DEBUFF_DEX":
                targetStats.DEX -= Mathf.RoundToInt(value);
                break;
            case "BURN_FIRE":
                targetStats.TakeDamage(Mathf.RoundToInt(value));
                break;
        }
    }

    private void RemoveEffect()
    {
        if (targetStats == null) return;

        switch (effectID)
        {
            case "HEAL_INSTANT":
                break;
            case "MANA_INSTANT":
                break;

            case "SWAP_CD":
                break;
            case "BUFF_STR":
                targetStats.STR -= Mathf.RoundToInt(value);
                break;
            case "BUFF_DEX":
                targetStats.DEX -= Mathf.RoundToInt(value);
                break;
            case "DEBUFF_DEX":
                targetStats.DEX += Mathf.RoundToInt(value);
                break;
        }
        isActive = false;
    }
}