using System;
using UnityEngine;

public enum EnemyStartState
{
    Idle,
    Patrol
}

[Serializable]
public class EnemySpawnRule
{
    public GameObject enemyPrefab;
    [Min(0)] public int min = 0;
    [Min(1)] public int max = 1;
    public EnemyStartState startState;
    [Range(0f, 1f)] public float chance = 1f;
}
