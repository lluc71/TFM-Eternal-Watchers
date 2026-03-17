using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Room Rules")]
public class DungeonRulesSO : ScriptableObject
{
    [Header("Size")]
    [Min(3)] public int totalRooms = 8;

    [Header("Fixed counts")]
    [Min(1)] public int startCount = 1;
    [Min(1)] public int endCount = 1;
    [Min(0)] public int bossCount = 1;

    [Header("Optional caps")]
    [Min(0)] public int maxSecret = 1;
    [Min(0)] public int maxTreasure = 1;

    [Header("Weights for filler rooms")]
    [Min(0)] public int weightNormal = 80;
    [Min(0)] public int weightTreasure = 15;
    [Min(0)] public int weightSecret = 5;

    [Header("Branches (optional)")]
    [Min(0)] public int branchCount = 2;
    [Min(1)] public int branchMinLen = 1;
    [Min(1)] public int branchMaxLen = 2;

    [Header("Branch room weights")]
    [Min(0)] public int branchWeightNormal = 70;
    [Min(0)] public int branchWeightTreasure = 20;
    [Min(0)] public int branchWeightSecret = 10;
}
