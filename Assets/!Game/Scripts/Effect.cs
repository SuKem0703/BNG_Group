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

    // Biến cho DoT (Damage over Time)
    private float tickTimer;
    private float tickInterval = 1.0f; // Gây sát thương mỗi 1 giây

    public void Initialize(GameObject target, float duration, float value)
    {
        this.targetStats = target.GetComponent<PlayerStats>();
        this.duration = duration;
        this.value = value;

        ApplyEffect();

        // Nếu duration <= 0 nghĩa là effect tức thời (bình máu, mana) -> Xóa ngay
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

        // Cập nhật UI cooldown
        if (overlayImage != null)
            overlayImage.fillAmount = 1f - (timer / duration);

        // --- XỬ LÝ DOT (BURN/POISON) ---
        if (effectID == "BURN_FIRE")
        {
            HandleDoT();
        }

        // Hết thời gian
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
            // Gây sát thương mỗi giây
            if (targetStats != null)
            {
                // value ở đây là sát thương mỗi giây (DPS)
                targetStats.TakeDamage(Mathf.RoundToInt(value));
            }
            tickTimer = 0f;
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
                // Logic giảm hồi chiêu swap (nếu có)
                break;

            // --- SỬA LỖI: Dùng hàm ModifyEffectStat thay vì cộng trực tiếp ---
            case "BUFF_STR":
                targetStats.ModifyEffectStat("STR", Mathf.RoundToInt(value));
                break;
            case "BUFF_DEX":
                targetStats.ModifyEffectStat("DEX", Mathf.RoundToInt(value));
                break;
            case "DEBUFF_DEX":
                targetStats.ModifyEffectStat("DEX", -Mathf.RoundToInt(value)); // Trừ DEX
                break;

            case "BURN_FIRE":
                // Burn Fire bây giờ xử lý trong Update (DoT) nên không làm gì ở đây
                // Hoặc có thể gây sát thương ngay tick đầu tiên
                targetStats.TakeDamage(Mathf.RoundToInt(value));
                break;
        }
    }

    private void RemoveEffect()
    {
        if (targetStats == null) return;

        // Hoàn trả lại chỉ số khi hết buff
        switch (effectID)
        {
            // Các effect tức thời không cần hoàn trả
            case "HEAL_INSTANT":
            case "MANA_INSTANT":
            case "BURN_FIRE":
                break;

            case "SWAP_CD":
                break;

            // --- SỬA LỖI: Hoàn trả lại chỉ số ---
            case "BUFF_STR":
                targetStats.ModifyEffectStat("STR", -Mathf.RoundToInt(value));
                break;
            case "BUFF_DEX":
                targetStats.ModifyEffectStat("DEX", -Mathf.RoundToInt(value));
                break;
            case "DEBUFF_DEX":
                targetStats.ModifyEffectStat("DEX", Mathf.RoundToInt(value)); // Cộng lại DEX đã trừ
                break;
        }
        isActive = false;
    }
}