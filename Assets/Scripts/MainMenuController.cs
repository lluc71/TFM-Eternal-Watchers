using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Localization.Settings;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("First Selected")]
    [SerializeField] private GameObject selectedButton;

    private void Start()
    {
        if (selectedButton == null) return;

        EventSystem.current?.SetSelectedGameObject(selectedButton.gameObject);

        SetLocale(PlayerPrefs.GetString("lang", "es"));
    }

    public void OpenScene(string name)
    {
        SceneManager.LoadScene(name);
    }

    public void QuitGame()
    {
        Application.Quit();

        #if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
        #endif
    }

    /**
     * Modifica el idioma del juego y lo guarda en las preferencias
     */
    public void SetLocale(string code)
    {
        var locale = LocalizationSettings.AvailableLocales.GetLocale(code);
        if (locale == null) return;
         
        LocalizationSettings.SelectedLocale = locale;
        PlayerPrefs.SetString("lang", code);
        PlayerPrefs.Save();
    }
}
