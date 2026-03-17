using UnityEngine;
using UnityEngine.InputSystem;

public class SummonStone : MonoBehaviour
{
    [Header("Spawn")]
    [SerializeField] private Transform player2SpawnPoint;
    
    public static SummonStone ActiveStone { get; private set; }
    
    public Transform SpawnPoint => player2SpawnPoint;

    private void OnTriggerEnter(Collider other)
    {
        var pi = other.GetComponentInParent<PlayerInput>();
        if (pi == null || pi.playerIndex != 0) return;

        // Esta piedra pasa a ser la activa
        ActiveStone = this;

        CoopJoinManager.Instance?.SetAllowJoin(true);
    }

    private void OnTriggerExit(Collider other)
    {
        var pi = other.GetComponentInParent<PlayerInput>();
        if (pi == null || pi.playerIndex != 0) return;

        // Si esta era la activa, se desactiva
        if (ActiveStone == this)
            ActiveStone = null;

        CoopJoinManager.Instance?.SetAllowJoin(false);
    }
}
