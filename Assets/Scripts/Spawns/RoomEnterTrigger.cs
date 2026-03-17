using UnityEngine;

public class RoomEnterTrigger : MonoBehaviour
{
    [Tooltip("If assigned, enemies spawn when Player enter into the room.")]
    [SerializeField] private RoomEnemySpawner spawner;

    private void OnTriggerEnter(Collider other)
    {
        if (!spawner) return;
        if (!other.CompareTag("Player")) return;

        spawner.TrySpawnEnemies();
        enabled = false;
    }
}
