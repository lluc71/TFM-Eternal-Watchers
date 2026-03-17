using UnityEngine;

public class Doorway : MonoBehaviour
{
    public string socketTag = "Any"; // "Normal", "Boss", "Secret", etc.
    public bool used;

    //TODO: Eliminar en la version final
    private void OnDrawGizmos()
    {
        Gizmos.color = used ? Color.red : Color.green;
        Gizmos.DrawSphere(transform.position, 0.2f);
        Gizmos.DrawRay(transform.position, transform.forward * 0.6f);
    }
}
