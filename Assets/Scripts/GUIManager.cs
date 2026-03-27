using UnityEngine;
using UnityEngine.InputSystem;

public class GUIManager : MonoBehaviour
{
    public static GUIManager Instance;

    [Header("UI Panels")]
    public GameObject player1Panel;
    public GameObject player2Panel;

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
}
