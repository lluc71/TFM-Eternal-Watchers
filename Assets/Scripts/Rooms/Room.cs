using UnityEngine;

public class Room : MonoBehaviour
{
    [Header("Room type")]
    public RoomType type;

    [Header("Direcciones permitidas (puertas)")]
    public bool allowNorth = true;
    public bool allowSouth = true;
    public bool allowEast = true;
    public bool allowWest = true;

    [Header("Connections (runtime)")]
    public bool north;
    public bool south;
    public bool east;
    public bool west;

    [Header("Prefabs de las puertas")]
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

    /**
     * Activa o desactiva los prefabs de las puertas de la habitacion
     */
    public void RefreshDoorVisuals()
    {
        if (doorNorthVisual) doorNorthVisual.SetActive(!north);
        if (doorSouthVisual) doorSouthVisual.SetActive(!south);
        if (doorEastVisual) doorEastVisual.SetActive(!east);
        if (doorWestVisual) doorWestVisual.SetActive(!west);
    }

    /**
     * Si el flag isSpawnOnLoad esta activado, intenta Spawnear a los enemigos de la sala
     */
    public void TrySpawnEnemiesOnLoad()
    {
        if (!enemySpawner) return;
        if (!enemySpawner.isSpawnOnLoad()) return;

        enemySpawner.TrySpawnEnemies();
    }
}
