using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AreaAudioTrigger2D : MonoBehaviour
{
    public AudioClip bgmClip;
    public bool loop = true;

    void Start()
    {
        Collider2D col = GetComponent<Collider2D>();
        if (!col.isTrigger)
        {
            col.isTrigger = true;
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("PlayerController"))
        {
            if (bgmClip != null)
            {
                SoundEffectManager.PlayBGM(bgmClip, loop);
            }
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("PlayerController"))
        {
            SoundEffectManager.StopBGM();
        }
    }
}
