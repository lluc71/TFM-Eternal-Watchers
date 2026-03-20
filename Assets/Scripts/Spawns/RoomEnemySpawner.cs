using UnityEngine;

public class RoomEnemySpawner : MonoBehaviour
{
    [Header("Room Points")]
    [SerializeField] private SpawnPointController[] spawnPoints;

    [SerializeField] private bool spawnOnLoad;

    private bool spawned;

    private void Awake()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            spawnPoints = GetComponentsInChildren<SpawnPointController>();
        }
    }

    public void TrySpawnEnemies()
    {
        if (spawned) return;

        spawned = true;

        if (spawnPoints == null || spawnPoints.Length == 0) return;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (spawnPoints[i] == null) continue;

            spawnPoints[i].TrySpawn();
        }
    }

    public void ResetSpawner()
    {
        spawned = false;

        if (spawnPoints == null) return;

        foreach (var spawnPoint in spawnPoints)
        {
            if (spawnPoint != null)
            {
                spawnPoint.ResetSpawnState();
            }
        }
    }

    public bool isSpawnOnLoad()
    {
        return spawnOnLoad;
    }

}
