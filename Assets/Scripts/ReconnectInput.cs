using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;

[RequireComponent(typeof(PlayerInput))]

public class ReconnectInput : MonoBehaviour
{
    private PlayerInput pi;

    private void Awake()
    {
        pi = GetComponent<PlayerInput>();
    }

    private void OnEnable()
    {
        InputSystem.onDeviceChange += OnDeviceChange;
    }

    private void OnDisable()
    {
        InputSystem.onDeviceChange -= OnDeviceChange;
    }

    private void OnDeviceChange(InputDevice device, InputDeviceChange change)
    {
        // Nos interesan reconexiones / añadidos
        if (change != InputDeviceChange.Reconnected && change != InputDeviceChange.Added)
            return;

        // Solo nos interesa mando (puedes ampliar a Keyboard/Mouse si quieres)
        if (device is not Gamepad gp)
            return;

        // Si ESTE jugador ya tiene un gamepad, no hacemos nada
        if (HasPairedGamepad(pi.user))
            return;

        // Si este gamepad ya está emparejado a otro jugador, no lo robamos
        if (IsPairedToAnotherUser(gp, pi.user))
            return;

        // Emparejar el mando reconectado a ESTE jugador
        InputUser.PerformPairingWithDevice(gp, pi.user);

        // Actualizar scheme (si usas control schemes)
        pi.SwitchCurrentControlScheme(gp);
    }

    private static bool HasPairedGamepad(InputUser user)
    {
        foreach (var d in user.pairedDevices)
            if (d is Gamepad) return true;
        return false;
    }

    private static bool IsPairedToAnotherUser(Gamepad gp, InputUser myUser)
    {
        foreach (var u in InputUser.all)
        {
            if (u == myUser) continue;
            if (u.pairedDevices.Contains(gp)) return true;
        }
        return false;
    }
}
