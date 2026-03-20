using System;
using UnityEngine;

[Serializable]
public class EnemySpawnEntry
{
    public GameObject enemyPrefab;
    [Range(0f, 100f)] public float weight = 100f;
}
