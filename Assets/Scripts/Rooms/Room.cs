using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("Meta")]
    public RoomType type;

    [Header("Allowed door sides (design-time)")]
    public bool allowNorth = true;
    public bool allowSouth = true;
    public bool allowEast = true;
    public bool allowWest = true;

    [Header("Connections (runtime)")]
    public bool north;
    public bool south;
    public bool east;
    public bool west;

    [Header("Optional visuals (assign in prefab)")]
    public GameObject doorNorthVisual;
    public GameObject doorSouthVisual;
    public GameObject doorEastVisual;
    public GameObject doorWestVisual;

    [Header("Spawns")]
    [SerializeField] private RoomEnemySpawner enemySpawner;

    public bool IsAllowed(DoorDir dir) => dir switch
    {
        DoorDir.North => allowNorth,
        DoorDir.South => allowSouth,
        DoorDir.East => allowEast,
        DoorDir.West => allowWest,
        _ => false
    };

    public bool IsConnected(DoorDir dir) => dir switch
    {
        DoorDir.North => north,
        DoorDir.South => south,
        DoorDir.East => east,
        DoorDir.West => west,
        _ => false
    };

    public void SetConnected(DoorDir dir, bool value)
    {
        switch (dir)
        {
            case DoorDir.North: north = value; break;
            case DoorDir.South: south = value; break;
            case DoorDir.East: east = value; break;
            case DoorDir.West: west = value; break;
        }
        RefreshDoorVisuals();
    }

    public void ClearConnections()
    {
        north = south = east = west = false;
        RefreshDoorVisuals();
    }

    public void RefreshDoorVisuals()
    {
        // Interpretación simple: si está conectado, se muestra.
        if (doorNorthVisual) doorNorthVisual.SetActive(!north);
        if (doorSouthVisual) doorSouthVisual.SetActive(!south);
        if (doorEastVisual) doorEastVisual.SetActive(!east);
        if (doorWestVisual) doorWestVisual.SetActive(!west);
    }

    public void TrySpawnEnemiesOnLoad()
    {
        if (!enemySpawner) return;

        if (enemySpawner.isSpawnOnLoad())
        {
            enemySpawner.TrySpawnEnemies();
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Gizmo pequeñito para ver allowed (verde) / no allowed (rojo)
        DrawDirGizmo(Vector3.forward, allowNorth);
        DrawDirGizmo(Vector3.back, allowSouth);
        DrawDirGizmo(Vector3.right, allowEast);
        DrawDirGizmo(Vector3.left, allowWest);
    }

    private void DrawDirGizmo(Vector3 dir, bool allowed)
    {
        Gizmos.color = allowed ? Color.green : Color.red;
        Gizmos.DrawRay(transform.position, dir * 1.5f);
    }
}
