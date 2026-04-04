using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PauseManager : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private TMP_Text seedText;
    [SerializeField] private Selectable firstSelectedButton;

    [Header("References")]
    [SerializeField] private DungeonGenerator dungeonGenerator;
    [SerializeField] private InputActionReference pauseAction;

    public bool isPaused { get; private set; }

    private void Awake()
    {
        Time.timeScale = 1f;
        isPaused = false;

        if (dungeonGenerator == null)
            dungeonGenerator = FindFirstObjectByType<DungeonGenerator>();

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (pauseAction != null)
        {
            pauseAction.action.Enable();
            pauseAction.action.performed += OnPausePerformed;
        }
    }

    /*
     * NOTA: Mantener por seguridad (Evita al recargar pantalla bugg)
     */
    private void OnDestroy()
    {
        if (pauseAction != null)
            pauseAction.action.performed -= OnPausePerformed;

        Time.timeScale = 1f;
    }

    private void OnPausePerformed(InputAction.CallbackContext context)
    {
        TogglePause();
    }

    public void TogglePause()
    {
        if (GUIManager.Instance.isVictoryPopupActive()) return;

        if (isPaused)
            ResumeGame();
        else
            PauseGame();
    }

    private void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;

        if (pausePanel != null)
            pausePanel.SetActive(true);

        RefreshUI();

        if (firstSelectedButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstSelectedButton.gameObject);
    }

    private void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;

        if (pausePanel != null)
            pausePanel.SetActive(false);

        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    private void RefreshUI()
    {
        if (seedText != null && dungeonGenerator != null)
            seedText.text = $"Seed: {dungeonGenerator.currentSeed}";
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void ExitToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(0);
    }
}
