using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Random = UnityEngine.Random;

public class RoomEnemySpawner : MonoBehaviour
{
    [Header("Rules")]
    public EnemySpawnRule[] rules;

    [Header("Room Points")]
    [SerializeField] Transform spawnPointsRoot; // hijos = spawn points
    [SerializeField] Transform patrolPathsRoot; // hijos = paths; path hijos = patrol points

    [Header("Optional")]
    public bool snapToNavMesh = true;
    public float navSampleRadius = 1f;
    public GameObject spawnVFX;

    private bool spawned;
    private int nextPatrolPathIndex = 0;

    Transform GetSpawnPoint(int[] spIdx, int used) => spawnPointsRoot.GetChild(spIdx[used]);

    public void TrySpawnEnemies()
    {
        if (spawned) return;
        spawned = true;

        if (rules == null || rules.Length == 0) return;
        if (!spawnPointsRoot || spawnPointsRoot.childCount == 0) return;

        //Desordenamos los spawnPoints (con Shuffle de sus indices)
        int spCount = spawnPointsRoot.childCount;
        int[] spIdx = BuildShuffledIndices(spCount);
        //TODO: Hacer que puedan no desordenarse para algunas salas => booleano

        int used = 0;
        int[] spawnedPerRule = new int[rules.Length];

        //Spawneamos los mínimos (si hay de cada enemigo)
        for (int r = 0; r < rules.Length && used < spCount; r++)
        {
            var rule = rules[r];
            int targetMin = Mathf.Min(rule.min, rule.max);
            while (spawnedPerRule[r] < targetMin && used < spCount)
            {
                bool didSpawn = TrySpawnOne(rule, GetSpawnPoint(spIdx, used));
                if (didSpawn) { used++; spawnedPerRule[r]++; }
            }
        }

        //Luego rellenamos hasta maximos (mientras queden spawnPoints)
        var candidates = new List<int>(rules.Length);
        while (used < spCount)
        {
            BuildCandidates(candidates, rules, spawnedPerRule);
            if (candidates.Count == 0) break;

            int pick = candidates[Random.Range(0, candidates.Count)];
            bool didSpawn = TrySpawnOne(rules[pick], GetSpawnPoint(spIdx, used));
            if (didSpawn) { used++; spawnedPerRule[pick]++; }
        }
    }

    bool TrySpawnOne(EnemySpawnRule rule, Transform spawnPoint)
    {
        if (rule.chance < 1f && Random.value > rule.chance) 
            return false;

        Vector3 pos = spawnPoint.position;
        Quaternion rot = spawnPoint.rotation;

        if (snapToNavMesh && NavMesh.SamplePosition(pos, out var hit, navSampleRadius, NavMesh.AllAreas))
            pos = hit.position;

        var go = Instantiate(rule.enemyPrefab, pos, rot, transform);

        var enemy = go.GetComponent<EnemyBasic>();
        if (!enemy) return true;

        if (rule.startState == EnemyStartState.Patrol)
        {
            enemy.SpawnInit(true, GetNextPatrolPoints());
        } else
        {
            enemy.SpawnInit(false, null);
        }

        if(spawnVFX != null)
        Instantiate(spawnVFX, pos, Quaternion.Euler(-90f, 0f, 0f), transform);

        return true;
    }

    private Transform[] GetNextPatrolPoints()
    {
        if (!patrolPathsRoot || patrolPathsRoot.childCount == 0) return null;

        //Si nos faltan mas paths, volvemos al principio
        if (nextPatrolPathIndex >= patrolPathsRoot.childCount)
            nextPatrolPathIndex = 0;

        var path = patrolPathsRoot.GetChild(nextPatrolPathIndex++);
        int n = path.childCount;
        if (n == 0) return null;

        var pts = new Transform[n];
        for (int i = 0; i < n; i++) pts[i] = path.GetChild(i);
        return pts;
    }

    private void BuildCandidates(List<int> candidates, EnemySpawnRule[] rules, int[] spawnedPerRule)
    {
        candidates.Clear();
        for (int r = 0; r < rules.Length; r++)
        {
            var rule = rules[r];
            if (spawnedPerRule[r] >= rule.max) continue;
            candidates.Add(r);
        }
    }

    private int[] BuildShuffledIndices(int count)
    {
        int[] idx = new int[count];
        for (int i = 0; i < count; i++) idx[i] = i;

        for (int i = 0; i < idx.Length; i++)
        {
            int j = Random.Range(i, idx.Length);
            (idx[i], idx[j]) = (idx[j], idx[i]);
        }

        return idx;
    }

}
