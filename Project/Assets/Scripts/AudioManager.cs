using UnityEngine;

/// <summary>
/// Reproduce la música de fondo (BGM) y los efectos (SFX).
/// Se crea en MainMenu y sobrevive entre escenas (DontDestroyOnLoad).
/// </summary>
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [SerializeField] private AudioSource musicSource;
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip backgroundMusic;
    [SerializeField, Range(0f, 1f)] private float musicVolume = 0.35f;
    [SerializeField, Range(0f, 1f)] private float sfxVolume = 0.8f;

    private void Awake()
    {
        // Solo puede existir un AudioManager
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (backgroundMusic != null)
        {
            musicSource.clip = backgroundMusic;
            musicSource.loop = true;
            musicSource.volume = musicVolume;
            musicSource.Play();
        }
    }

    /// <summary>Reproduce un efecto. Seguro de llamar aunque no haya AudioManager o clip.</summary>
    public static void PlaySfx(AudioClip clip)
    {
        if (Instance == null || clip == null) return;
        Instance.sfxSource.PlayOneShot(clip, Instance.sfxVolume);
    }
}
