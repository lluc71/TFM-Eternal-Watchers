using UnityEngine;

public class MusicManager : MonoBehaviour
{
    public static MusicManager Instance { get; private set; }

    [Header("Audio")]
    [SerializeField] private AudioSource musicSource;

    private AudioClip currentClip;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        LoadVolume();
    }

    private void LoadVolume()
    {
        musicSource.volume = PlayerPrefs.GetFloat(PlayerPrefsKeys.MusicVolume, 1f);
    }

    public void SetVolume(float volume)
    {
        musicSource.volume = Mathf.Clamp01(volume);
        PlayerPrefs.SetFloat(PlayerPrefsKeys.MusicVolume, musicSource.volume);
        PlayerPrefs.Save();
    }

    public void PlayMusic(AudioClip clip)
    {
        if (clip == null) return;
        if (currentClip == clip) return;

        currentClip = clip;
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void StopMusic()
    {
        musicSource.Stop();
        currentClip = null;
    }
}
