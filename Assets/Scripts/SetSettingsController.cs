using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SetSettingsController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Toggle randomSeedToggle;
    [SerializeField] private TMP_InputField seedInputField;
    [SerializeField] private GameObject spanishFocus;
    [SerializeField] private GameObject englishFocus;
    [SerializeField] private Slider musicVolumeSlider;

    public void Start()
    {
        LoadSettings();
    }

    public void SetRandomSeed(bool random)
    {
        PlayerPrefs.SetInt(PlayerPrefsKeys.RandomSeed, random ? 1 : 0);
        PlayerPrefs.Save();
    }

    public void SetSeed(string value)
    {
        int seed = 0;

        if (!string.IsNullOrWhiteSpace(value))
        {
            int.TryParse(value, out seed);
        }

        PlayerPrefs.SetInt(PlayerPrefsKeys.Seed, seed);
        PlayerPrefs.Save();
    }

    public void SetMusicVolume(float volume)
    {
        MusicManager.Instance.SetVolume(volume);
    }

    private void LoadSettings()
    {
        // Seed
        int savedSeed = PlayerPrefs.GetInt(PlayerPrefsKeys.Seed, 0);
        seedInputField.text = (savedSeed == 0) ? "" : savedSeed.ToString();

        // Random Seed
        bool randomSeed = PlayerPrefs.GetInt(PlayerPrefsKeys.RandomSeed, 1) == 1;
        randomSeedToggle.isOn = randomSeed;

        //Language Focus
        bool isSpanish = PlayerPrefs.GetString(PlayerPrefsKeys.Language, "es") == "es";
        spanishFocus.SetActive(isSpanish);
        englishFocus.SetActive(!isSpanish);

        // Music Volume
        float musicVolume = PlayerPrefs.GetFloat(PlayerPrefsKeys.MusicVolume, 1f);
        musicVolumeSlider.value = musicVolume;
    }
}
