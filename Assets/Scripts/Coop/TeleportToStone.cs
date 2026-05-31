using UnityEngine;
using UnityEngine.InputSystem;

public class TeleportToStone : MonoBehaviour
{
    private TopDownMainCamera mainCamera;

    private void Awake()
    {
        mainCamera = FindFirstObjectByType<TopDownMainCamera>();
    }

    private void Start()
    {
        var pi = GetComponent<PlayerInput>();
        if (pi == null || pi.playerIndex == 0) return;

        var stone = SummonStone.ActiveStone;
        if (stone == null) return;

        TeleportTo(stone.SpawnPoint);

        mainCamera?.SetTargetB(transform);
    }

    private void TeleportTo(Transform target)
    {
        var cc = GetComponent<CharacterController>();
        if (cc) cc.enabled = false;

        transform.SetPositionAndRotation(target.position, target.rotation);

        if (cc) cc.enabled = true;
    }
}
