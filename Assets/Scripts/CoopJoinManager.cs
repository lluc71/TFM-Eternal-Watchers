using System.Collections;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

[RequireComponent(typeof(PlayerInputManager))]
public class CoopJoinManager : MonoBehaviour
{
    public static CoopJoinManager Instance { get; private set; }

    [Header("Rules")]
    [SerializeField] private bool allowJoin = false;

    [Header("Only allow joining with gamepads")]
    [SerializeField] private bool gamepadOnly = true;

    private PlayerInputManager pim;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        pim = GetComponent<PlayerInputManager>();
    }

    private void OnEnable()
    {
        // Filtra intentos de join: si no te gusta, lo rechazas.
        pim.onPlayerJoined += OnPlayerJoined;
    }

    private void OnDisable()
    {
        pim.onPlayerJoined -= OnPlayerJoined;
    }

    private void Start()
    {
        // Deja entrar al Player 1 siempre
        pim.EnableJoining();

        // Si ya hay Player 1 por escena (no por join), entonces aplica tu estado normal
        if (pim.playerCount > 0)
            ApplyJoinState();
    }

    public void SetAllowJoin(bool value)
    {
        allowJoin = value;
        ApplyJoinState();
    }

    private void ApplyJoinState()
    {
        if (!allowJoin)
        {
            pim.DisableJoining();
            return;
        }

        // Si ya está permitido:
        pim.EnableJoining();
    }

    private void OnPlayerJoined(PlayerInput player)
    {
        // El Player 1 SIEMPRE se permite (aunque sea teclado)
        if (player.playerIndex == 0)
        {
            // Opcional: en cuanto existe P1, cerramos joining hasta la piedra
            if (!allowJoin) pim?.DisableJoining();
            return;
        }

        // A partir del Player 2, sí aplicamos el gate
        if (!allowJoin)
        {
            Destroy(player.gameObject);
            return;
        }

        if (!gamepadOnly) return;

        // Player 2 debe tener gamepad emparejado
        bool hasGamepad = false;
        foreach (var d in player.user.pairedDevices)
        {
            if (d is Gamepad) { hasGamepad = true; break; }
        }

        if (!hasGamepad)
        {
            Destroy(player.gameObject);
            return;
        }
    }
}
