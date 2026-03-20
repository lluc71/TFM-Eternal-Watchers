using UnityEngine;
using UnityEngine.AI;

public class SpawnPointController : MonoBehaviour
{
    [Header("Rule")]
    [SerializeField] private SpawnPointRule rule;

    [Header("Optional")]
    [SerializeField] private bool snapToNavMesh = true;
    [SerializeField] private float navSampleRadius = 1f;
    [SerializeField] private GameObject spawnVFX;

    private bool hasSpawned;

    public bool TrySpawn()
    {
        if (hasSpawned) return false;

        hasSpawned = true;

        if (!ShouldSpawnEnemy()) return false;

        GameObject prefabToSpawn = GetRandomEnemyPrefab();
        if (prefabToSpawn == null) return false;

        Vector3 spawnPosition = transform.position;
        Quaternion spawnRotation = transform.rotation;

        if (snapToNavMesh && NavMesh.SamplePosition(spawnPosition, out NavMeshHit hit, navSampleRadius, NavMesh.AllAreas))
        {
            spawnPosition = hit.position;
        }

        GameObject enemyGO = Instantiate(prefabToSpawn, spawnPosition, spawnRotation);

        EnemyBasic enemy = enemyGO.GetComponent<EnemyBasic>();
        if (enemy != null)
        {
            bool usePatrol = rule.isPatrolState();
            Transform[] patrolPoints = usePatrol ? GetPatrolPoints() : null;
            enemy.SpawnInit(usePatrol, patrolPoints);
        }

        if (spawnVFX != null)
        {
            Instantiate(spawnVFX, spawnPosition, Quaternion.Euler(-90f, 0f, 0f));
        }

        return true;
    }

    private bool ShouldSpawnEnemy()
    {
        return Random.value <= rule.chance;
    }

    private GameObject GetRandomEnemyPrefab()
    {
        if (rule.enemies == null || rule.enemies.Length == 0) return null;

        float totalWeight = 0f;

        foreach (var entry in rule.enemies)
        {
            if (entry != null && entry.enemyPrefab != null && entry.weight > 0f)
                totalWeight += entry.weight;
        }

        if (totalWeight <= 0f) return null;

        float randomValue = Random.value * totalWeight;

        foreach (var entry in rule.enemies)
        {
            if (entry == null || entry?.enemyPrefab == null || entry.weight <= 0f)
                continue;

            randomValue -= entry.weight;

            if (randomValue <= 0f)
                return entry.enemyPrefab;
        }

        return null;
    }

    private Transform[] GetPatrolPoints()
    {
        if (rule.patrolPathRoot == null) return null;

        int childCount = rule.patrolPathRoot.childCount;
        if (childCount == 0) return null;

        Transform[] points = new Transform[childCount];
        for (int i = 0; i < childCount; i++)
        {
            points[i] = rule.patrolPathRoot.GetChild(i);
        }

        return points;
    }

    public void ResetSpawnState()
    {
        hasSpawned = false;
    }
}
