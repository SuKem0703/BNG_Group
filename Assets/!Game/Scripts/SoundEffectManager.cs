using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SoundEffectManager : MonoBehaviour
{
    private static SoundEffectManager Instance;

    private static AudioSource audioSource;
    private static AudioSource randomPitchAudioSource;
    private static AudioSource voiceAudioSource;
    private static AudioSource bgmAudioSource;

    private static SoundEffectLibrary soundEffectLibrary;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            AudioSource[] audioSources = GetComponents<AudioSource>();

            if (audioSources.Length < 4)
            {
                Debug.LogError("SoundEffectManager cần ít nhất 4 AudioSource!");
                return;
            }

            audioSource = audioSources[0];
            randomPitchAudioSource = audioSources[1];
            voiceAudioSource = audioSources[2];
            bgmAudioSource = audioSources[3];
            soundEffectLibrary = GetComponent<SoundEffectLibrary>();

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(transform.root.gameObject);
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StopBGM();

        // Việc cần làm khi scene mới được tải
    }

    public static void Play(string soundName, bool randomPitch = false)
    {
        AudioClip audioClip = soundEffectLibrary.GetRandomClip(soundName);
        if (audioClip != null)
        {
            if (randomPitch)
            {
                randomPitchAudioSource.pitch = Random.Range(1f, 1.5f);
                randomPitchAudioSource.PlayOneShot(audioClip);
            }
            else
            {
                audioSource.PlayOneShot(audioClip);
            }
        }
    }

    public static void PlayVoice(AudioClip audioClip, float pitch = 1f)
    {
        voiceAudioSource.pitch = pitch;
        voiceAudioSource.PlayOneShot(audioClip);
    }
    // Phát BGM bằng Library
    public static void PlayBGM(string soundName, bool loop = true)
    {
        if (bgmAudioSource != null)
        {
            AudioClip clip = soundEffectLibrary.GetRandomClip(soundName);
            if (clip != null)
            {
                bgmAudioSource.clip = clip;
                bgmAudioSource.loop = loop;
                bgmAudioSource.Play();

                Debug.Log($"[BGM] Đang phát: {clip.name}");
            }
            else
            {
                Debug.LogWarning($"Không tìm thấy BGM với tên {soundName}");
            }
        }
    }

    // Phát BGM cố định từ AudioClip
    public static void PlayBGM(AudioClip clip, bool loop = true)
    {
        if (bgmAudioSource != null && clip != null)
        {
            bgmAudioSource.clip = clip;
            bgmAudioSource.loop = loop;
            bgmAudioSource.Play();
        }
        else
        {
            Debug.LogWarning("Không thể phát BGM: clip null hoặc audioSource null");
        }
    }

    public static void StopBGM()
    {
        if (bgmAudioSource != null)
            bgmAudioSource.Stop();
    }

    public static void SetSFXVolume(float volume)
    {
        audioSource.volume = volume;
        randomPitchAudioSource.volume = volume;
        voiceAudioSource.volume = volume;
    }
    public static void SetBGMVolume(float volume)
    {
        if (bgmAudioSource != null)
            bgmAudioSource.volume = volume;
    }
}