using System.Collections.Generic;
using UnityEngine;

public static class PlayerRegistry
{
    private static readonly List<Transform> players = new();
    public static IReadOnlyList<Transform> Players => players;

    public static void Register(Transform t)
    {
        if (!t) return;
        if (!players.Contains(t)) players.Add(t);
    }

    public static void Unregister(Transform t)
    {
        if (!t) return;
        players.Remove(t);
    }
}