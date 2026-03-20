using System;
using UnityEngine;

public enum EnemyStartState
{
    Idle,
    Patrol
}

[Serializable]
public class SpawnPointRule
{
    [Header("Spawn")]
    [Range(0f, 1f)] public float chance = 1f;

    [Header("Initial State")]
    public EnemyStartState startState = EnemyStartState.Idle;
    public Transform patrolPathRoot;

    [Header("Enemy Types")]
    public EnemySpawnEntry[] enemies;

    public bool isPatrolState()
    {
        return startState == EnemyStartState.Patrol;
    }
}
