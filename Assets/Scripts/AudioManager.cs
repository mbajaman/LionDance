using UnityEngine;

/// <summary>
/// Plays and controls the game's background music.
/// Add this to a scene GameObject, assign a music clip, and leave playOnStart
/// enabled to begin playback automatically.
/// </summary>
[DisallowMultipleComponent]
[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance { get; private set; }

    [Header("Music")]
    [Tooltip("Song to use as background music.")]
    [SerializeField] private AudioClip backgroundMusic;

    [Tooltip("Start playing the background music when the scene starts.")]
    [SerializeField] private bool playOnStart = true;

    [Tooltip("Loop the background music continuously.")]
    [SerializeField] private bool loop = true;

    [Tooltip("Playback volume for the background music.")]
    [SerializeField, Range(0f, 1f)] private float volume = 0.75f;

    [Header("Behaviour")]
    [Tooltip("If true, this manager persists across scene loads. Leave off if each scene has its own music.")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    private AudioSource _audioSource;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning($"[AudioManager] Duplicate instance on '{name}' destroyed; using '{Instance.name}'.", this);
            Destroy(gameObject);
            return;
        }

        Instance = this;
        if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);

        _audioSource = GetComponent<AudioSource>();
        ConfigureAudioSource(backgroundMusic);
    }

    private void Start()
    {
        if (playOnStart) PlayMusic();
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void OnValidate()
    {
        volume = Mathf.Clamp01(volume);

        if (_audioSource == null) _audioSource = GetComponent<AudioSource>();
        if (_audioSource == null) return;

        _audioSource.loop = loop;
        _audioSource.volume = volume;

        if (backgroundMusic != null) _audioSource.clip = backgroundMusic;
    }

    public void PlayMusic()
    {
        PlayMusic(backgroundMusic);
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null)
        {
            Debug.LogWarning("[AudioManager] No background music clip assigned.", this);
            return;
        }

        ConfigureAudioSource(clip);

        if (!_audioSource.isPlaying)
        {
            _audioSource.Play();
        }
    }

    public void StopMusic()
    {
        if (_audioSource == null) return;
        _audioSource.Stop();
    }

    public void PauseMusic()
    {
        if (_audioSource == null) return;
        _audioSource.Pause();
    }

    public void ResumeMusic()
    {
        if (_audioSource == null || _audioSource.clip == null || _audioSource.isPlaying) return;
        _audioSource.UnPause();
    }

    public void SetVolume(float value)
    {
        volume = Mathf.Clamp01(value);
        if (_audioSource != null) _audioSource.volume = volume;
    }

    private void ConfigureAudioSource(AudioClip clip)
    {
        if (_audioSource == null) return;

        _audioSource.clip = clip;
        _audioSource.loop = loop;
        _audioSource.volume = volume;
        _audioSource.playOnAwake = false;
    }
}
