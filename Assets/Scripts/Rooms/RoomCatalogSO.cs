using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Dungeon/Room Catalog")]
public class RoomCatalogSO : ScriptableObject
{
    [Serializable]
    public class RoomEntry
    {
        public RoomType type;
        public List<GameObject> prefabs = new();
    }

    public List<RoomEntry> rooms = new();

    public List<GameObject> GetPrefabs(RoomType type)
    {
        var entry = rooms.Find(r => r.type == type);
        return entry != null ? entry.prefabs : null;
    }
}
