using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class VictoryPortalController : MonoBehaviour
{
    private readonly HashSet<PlayerInput> playersInside = new();

    private PlayerInputManager playerInputManager;
    private bool victoryTriggered = false;

    private void Awake()
    {
        playerInputManager = FindFirstObjectByType<PlayerInputManager>();

        if (playerInputManager == null)
        {
            Debug.LogWarning("VictoryPortalController: No se encontró un PlayerInputManager en la escena.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (victoryTriggered) return;

        PlayerInput playerInput = other.GetComponentInParent<PlayerInput>();
        if (playerInput == null) return;

        playersInside.Add(playerInput);
        TryTriggerVictory();
    }

    private void OnTriggerExit(Collider other)
    {
        if (victoryTriggered) return;

        PlayerInput playerInput = other.GetComponentInParent<PlayerInput>();
        if (playerInput == null) return;

        playersInside.Remove(playerInput);
    }

    private void TryTriggerVictory()
    {
        int requiredPlayers = GetRequiredPlayersCount();
        if (requiredPlayers <= 0) return;

        if (playersInside.Count >= requiredPlayers)
        {
            TriggerVictory();
        }
    }

    private int GetRequiredPlayersCount()
    {
        if (playerInputManager == null) return 0;

        int count = playerInputManager.playerCount;
        // TO DO: count = jugadores unidos y vivos

        return count;
    }
    private void TriggerVictory()
    {
        if (victoryTriggered) return;
        victoryTriggered = true;

        if (GUIManager.Instance != null)
        {
            GUIManager.Instance.ShowVictoryPopup();
        }
        else
        {
            Debug.LogWarning("VictoryPortalController: No se encontró GUIManager.Instance.");
        }
    }
}
