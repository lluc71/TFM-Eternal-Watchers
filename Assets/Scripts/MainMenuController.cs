using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    [Header("First Selected")]
    [SerializeField] private GameObject selectedButton;

    private void Start()
    {
        if (selectedButton == null) return;

        EventSystem.current?.SetSelectedGameObject(selectedButton.gameObject);
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
}
