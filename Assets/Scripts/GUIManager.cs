using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class GUIManager : MonoBehaviour
{
    public static GUIManager Instance;

    [Header("UI Panels")]
    public GameObject player1Panel;
    public GameObject player2Panel;

    [Header("UI Popups")]
    [SerializeField] private GameObject joinPlayerPopup;
    [SerializeField] private GameObject victoryPopup;
    [SerializeField] private GameObject failurePopup;

    private void Awake()
    {
        Instance = this;
    }

    /*
     * Le asigna al jugador su Panel de Info (IGU)
     */
    public void RegisterPlayer(MovementController player, PlayerInput input)
    {
        int index = input.playerIndex;

        if (index == 0)
        {
            SetupPlayerInfo(player, player1Panel);
        }
        else if (index > 0)
        {
            SetupPlayerInfo(player, player2Panel);
        }
    }

    private void SetupPlayerInfo(MovementController player, GameObject panel)
    {
        if (panel == null) return;

        panel.SetActive(true);
        player.SetPlayerInfoGUI(panel.GetComponent<GUIPlayerInfo>());
    }

    public void ToggleJoinPlayerPopup(bool showPanel)
    {
        if (joinPlayerPopup == null) return;

        joinPlayerPopup.SetActive(showPanel);
    }

    public void ShowVictoryPopup()
    {
        if (victoryPopup == null) return;

        Time.timeScale = 0f;
        victoryPopup.SetActive(true);

        SelectFirstButton(victoryPopup);
    }

    public bool isVictoryPopupActive()
    {
        return (victoryPopup != null) ? victoryPopup.activeSelf : false;
    }
    public bool isFailurePopupActive()
    {
        return (failurePopup != null) ? failurePopup.activeSelf : false;
    }

    private void SelectFirstButton(GameObject popup)
    {
        var button = popup.GetComponentInChildren<Button>();
        if (button == null) return;

        EventSystem.current?.SetSelectedGameObject(button.gameObject);
    }

    public void ShowFailurePopup()
    {
        if (failurePopup == null) return;

        Time.timeScale = 0f;
        failurePopup.SetActive(true);

        SelectFirstButton(failurePopup);
    }
}
