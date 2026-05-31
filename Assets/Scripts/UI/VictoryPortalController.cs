using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Rendering;

public class VictoryPortalController : MonoBehaviour
{
    private readonly HashSet<PlayerInput> playersInside = new();

    private bool victoryTriggered = false;

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
        int requiredPlayers = 0;

        MovementController[] players = FindObjectsByType<MovementController>(FindObjectsSortMode.None);

        foreach (var player in players)
        {
            if (!player.isDead)
                requiredPlayers++;
        }

        return requiredPlayers;
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
