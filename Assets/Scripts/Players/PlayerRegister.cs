using UnityEngine;

public class PlayerRegister : MonoBehaviour
{
    private void OnEnable() => PlayerRegistry.Register(transform);
    private void OnDisable() => PlayerRegistry.Unregister(transform);
}