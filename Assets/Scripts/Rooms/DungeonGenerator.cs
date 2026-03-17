using System.Collections.Generic;
using Unity.AI.Navigation;
using UnityEngine;

public class DungeonGenerator : MonoBehaviour
{
    [Header("Data")]
    public RoomCatalogSO catalog;
    public DungeonRulesSO rules;

    [Header("Grid")]
    [Min(1)] public int cellSize = 16; // en unidades Unity (X/Z)
    public bool randomSeed = true;
    public int seed = 0;

    [Header("Generation")]
    public bool awakeDungeonGeneration;
    [Min(1)] public int maxPickAttemptsPerStep = 40; // reintentos para encontrar vecino libre

    [Header("Hierarchy")]
    public Transform dungeonRoot;
    [SerializeField] private NavMeshSurface navMeshSurface;

    private System.Random rng;
    private readonly Dictionary<Vector2Int, Room> placed = new();
    private readonly List<Room> spawned = new();

    // Para ramificaciones
    private readonly List<Vector2Int> mainPathCells = new();


    public void Start()
    {
        if(awakeDungeonGeneration)
        {
            Generate();
            //SpawnMiscelaniaInRooms();
            navMeshSurface?.BuildNavMesh();

            SpawnEnemiesInRooms();
        }
    }

    public void Generate()
    {
        Clear();

        int finalSeed = randomSeed ? UnityEngine.Random.Range(int.MinValue, int.MaxValue) : seed;
        rng = new System.Random(finalSeed);

        if (!dungeonRoot)
        {
            var rootGo = new GameObject("DungeonRoot");
            rootGo.transform.position = transform.position;
            dungeonRoot = rootGo.transform;
        }

        // Start
        Vector2Int cell = Vector2Int.zero;
        Room current = SpawnAt(cell, RoomType.Start);
        placed[cell] = current;

        mainPathCells.Add(cell);

        // Plan del camino principal
        var plan = BuildRoomPlan();

        for (int step = 0; step < plan.Count; step++)
        {
            RoomType nextType = plan[step];

            if (!TryPlaceNext(current, cell, nextType, out Room nextRoom, out Vector2Int nextCell))
            {
                Debug.LogWarning($"No se pudo colocar sala {nextType} en el paso {step}. Abortando main path.");
                break;
            }

            current = nextRoom;
            cell = nextCell;

            mainPathCells.Add(cell);
        }

        // Ramas
        GenerateBranches();

        Debug.Log($"Dungeon generado. Salas: {placed.Count} (seed={finalSeed})");
    }

    private void GenerateBranches()
    {
        int branchesToTry = Mathf.Max(0, rules.branchCount);
        if (branchesToTry == 0) return;

        // Candidatos: salas del camino principal excepto Start y End (y opcionalmente Boss)
        var candidates = new List<Vector2Int>();
        for (int i = 1; i < mainPathCells.Count - 1; i++)
            candidates.Add(mainPathCells[i]);

        Shuffle(candidates);

        int created = 0;

        foreach (var baseCell in candidates)
        {
            if (created >= branchesToTry) break;

            if (!placed.TryGetValue(baseCell, out var baseRoom) || !baseRoom) continue;

            // No ramificar desde Boss si no quieres (opcional)
            if (baseRoom.type == RoomType.Boss) continue;

            // ¿Tiene salidas libres?
            var freeDirs = GetValidExitDirections(baseRoom, baseCell);
            if (freeDirs.Count == 0) continue;

            // Intentar crear una rama
            int len = rng.Next(rules.branchMinLen, rules.branchMaxLen + 1);
            if (TryCreateBranch(baseRoom, baseCell, len))
                created++;
        }
    }

    private bool TryCreateBranch(Room baseRoom, Vector2Int baseCell, int length)
    {
        Room current = baseRoom;
        Vector2Int currentCell = baseCell;

        // Primer paso: salir desde baseRoom por una dirección libre
        var firstDirs = GetValidExitDirections(current, currentCell);
        if (firstDirs.Count == 0) return false;

        DoorDir dir = firstDirs[rng.Next(0, firstDirs.Count)];
        Vector2Int nextCell = currentCell + DirToOffset(dir);

        // El primer tipo de rama (puede ser Normal/Treasure/Secret)
        RoomType firstType = PickBranchType();
        if (!TryPlaceSpecific(current, currentCell, dir, firstType, out Room nextRoom))
            return false;

        current = nextRoom;
        currentCell = nextCell;

        // Continúa la rama (normalmente 0–2 salas)
        for (int i = 1; i < length; i++)
        {
            // Para que se sienta “ramita”, intenta no volver hacia atrás
            var dirs = GetValidExitDirections(current, currentCell);
            if (dirs.Count == 0) return true; // dead end natural

            // Opcional: filtra el opuesto para no volver a la sala anterior (si hay alternativas)
            // (Esto evita que la rama haga “zigzag” raro hacia atrás)
            DoorDir backDir = Opposite(dir); // dir era de la sala anterior hacia esta, así que el backDir te devuelve
            if (dirs.Count > 1)
                dirs.Remove(backDir);

            if (dirs.Count == 0) return true;

            DoorDir stepDir = dirs[rng.Next(0, dirs.Count)];
            Vector2Int stepCell = currentCell + DirToOffset(stepDir);

            RoomType t = PickBranchType();
            if (!TryPlaceSpecific(current, currentCell, stepDir, t, out Room stepRoom))
                return true; // si no puede, la rama termina aquí

            // Avanza
            dir = stepDir;
            current = stepRoom;
            currentCell = stepCell;
        }

        return true;
    }

    // Coloca una sala en una dirección concreta (sin elegir dirección aleatoria)
    private bool TryPlaceSpecific(Room fromRoom, Vector2Int fromCell, DoorDir dir, RoomType nextType, out Room placedRoom)
    {
        placedRoom = null;

        Vector2Int toCell = fromCell + DirToOffset(dir);
        if (placed.ContainsKey(toCell)) return false;

        DoorDir entrySide = Opposite(dir);

        for (int attempt = 0; attempt < maxPickAttemptsPerStep; attempt++)
        {
            var prefab = PickRandomPrefabThatAllows(nextType, entrySide);
            if (!prefab) return false;

            var newRoom = SpawnPrefabAt(toCell, prefab, nextType);
            if (!newRoom) continue;

            if (!newRoom.IsAllowed(entrySide))
            {
                DestroyImmediate(newRoom.gameObject);
                continue;
            }

            ConnectRooms(fromRoom, newRoom, dir);
            placed[toCell] = newRoom;

            placedRoom = newRoom;
            return true;
        }

        return false;
    }

    // Camino principal: elige dirección automáticamente según allowed+libre y prefab compatible
    private bool TryPlaceNext(Room fromRoom, Vector2Int fromCell, RoomType nextType, out Room placedRoom, out Vector2Int placedCell)
    {
        placedRoom = null;
        placedCell = default;

        var validDirs = GetValidExitDirections(fromRoom, fromCell);
        if (validDirs.Count == 0) return false;

        for (int attempt = 0; attempt < maxPickAttemptsPerStep; attempt++)
        {
            DoorDir dir = validDirs[rng.Next(0, validDirs.Count)];
            Vector2Int toCell = fromCell + DirToOffset(dir);
            if (placed.ContainsKey(toCell)) continue;

            DoorDir entrySide = Opposite(dir);

            var prefab = PickRandomPrefabThatAllows(nextType, entrySide);
            if (!prefab) continue;

            var newRoom = SpawnPrefabAt(toCell, prefab, nextType);
            if (!newRoom) continue;

            if (!newRoom.IsAllowed(entrySide))
            {
                DestroyImmediate(newRoom.gameObject);
                continue;
            }

            ConnectRooms(fromRoom, newRoom, dir);
            placed[toCell] = newRoom;

            placedRoom = newRoom;
            placedCell = toCell;
            return true;
        }

        return false;
    }

    private List<DoorDir> GetValidExitDirections(Room room, Vector2Int cell)
    {
        var dirs = new List<DoorDir>(4);

        TryAdd(DoorDir.North);
        TryAdd(DoorDir.South);
        TryAdd(DoorDir.East);
        TryAdd(DoorDir.West);

        return dirs;

        void TryAdd(DoorDir d)
        {
            if (!room.IsAllowed(d)) return;
            if (room.IsConnected(d)) return;
            var nextCell = cell + DirToOffset(d);
            if (placed.ContainsKey(nextCell)) return;
            dirs.Add(d);
        }
    }

    private Room SpawnAt(Vector2Int cell, RoomType type)
    {
        var prefab = PickRandomPrefab(type);
        if (!prefab) return null;
        return SpawnPrefabAt(cell, prefab, type);
    }

    private Room SpawnPrefabAt(Vector2Int cell, GameObject prefab, RoomType type)
    {
        Vector3 origin = transform.position;
        Vector3 pos = origin + new Vector3(cell.x * cellSize, 0f, cell.y * cellSize);

        var go = Instantiate(prefab, pos, Quaternion.identity, dungeonRoot);

        var room = go.GetComponent<Room>();
        if (!room) room = go.AddComponent<Room>();

        room.type = type;
        room.ClearConnections();

        spawned.Add(room);
        return room;
    }

    private void ConnectRooms(Room a, Room b, DoorDir dirFromAToB)
    {
        a.SetConnected(dirFromAToB, true);
        b.SetConnected(Opposite(dirFromAToB), true);
    }

    private GameObject PickRandomPrefab(RoomType type)
    {
        var list = catalog ? catalog.GetPrefabs(type) : null;
        if (list == null || list.Count == 0)
        {
            Debug.LogError($"No hay prefabs para tipo {type} en el catálogo.");
            return null;
        }
        return list[rng.Next(0, list.Count)];
    }

    private GameObject PickRandomPrefabThatAllows(RoomType type, DoorDir requiredSide)
    {
        var list = catalog ? catalog.GetPrefabs(type) : null;
        if (list == null || list.Count == 0) return null;

        var candidates = new List<GameObject>();
        foreach (var p in list)
        {
            if (!p) continue;
            var room = p.GetComponent<Room>();
            if (!room) continue;
            if (room.IsAllowed(requiredSide))
                candidates.Add(p);
        }

        if (candidates.Count == 0) return null;
        return candidates[rng.Next(0, candidates.Count)];
    }

    private RoomType PickBranchType()
    {
        int wN = Mathf.Max(0, rules.branchWeightNormal);
        int wT = Mathf.Max(0, rules.branchWeightTreasure);
        int wS = Mathf.Max(0, rules.branchWeightSecret);

        int sum = wN + wT + wS;
        if (sum <= 0) return RoomType.Normal;

        int roll = rng.Next(0, sum);
        if (roll < wN) return RoomType.Normal;
        roll -= wN;
        if (roll < wT) return RoomType.Treasure;
        return RoomType.Secret;
    }

    private static Vector2Int DirToOffset(DoorDir d) => d switch
    {
        DoorDir.North => Vector2Int.up,
        DoorDir.South => Vector2Int.down,
        DoorDir.East => Vector2Int.right,
        DoorDir.West => Vector2Int.left,
        _ => Vector2Int.zero
    };

    private static DoorDir Opposite(DoorDir d) => d switch
    {
        DoorDir.North => DoorDir.South,
        DoorDir.South => DoorDir.North,
        DoorDir.East => DoorDir.West,
        DoorDir.West => DoorDir.East,
        _ => DoorDir.North
    };

    private void Shuffle<T>(IList<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int j = rng.Next(i, list.Count);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }

    private List<RoomType> BuildRoomPlan()
    {
        int remaining = Mathf.Max(0, rules.totalRooms - 1);
        var plan = new List<RoomType>();

        bool placeEnd = rules.endCount > 0 && remaining > 0;
        if (placeEnd) remaining -= 1;

        int bossToPlace = Mathf.Clamp(rules.bossCount, 0, remaining);
        remaining -= bossToPlace;

        int secretLeft = rules.maxSecret;
        int treasureLeft = rules.maxTreasure;

        for (int i = 0; i < remaining; i++)
            plan.Add(PickFillerType(ref secretLeft, ref treasureLeft));

        for (int i = 0; i < bossToPlace; i++)
            plan.Add(RoomType.Boss);

        if (placeEnd)
            plan.Add(RoomType.End);

        return plan;
    }

    private RoomType PickFillerType(ref int secretLeft, ref int treasureLeft)
    {
        int wN = Mathf.Max(0, rules.weightNormal);
        int wT = treasureLeft > 0 ? Mathf.Max(0, rules.weightTreasure) : 0;
        int wS = secretLeft > 0 ? Mathf.Max(0, rules.weightSecret) : 0;

        int sum = wN + wT + wS;
        if (sum <= 0) return RoomType.Normal;

        int roll = rng.Next(0, sum);
        if (roll < wN) return RoomType.Normal;
        roll -= wN;

        if (roll < wT) { treasureLeft--; return RoomType.Treasure; }

        secretLeft--;
        return RoomType.Secret;
    }

    public void Clear()
    {
        placed.Clear();
        spawned.Clear();
        mainPathCells.Clear();

        if (!dungeonRoot) return;

        for (int i = dungeonRoot.childCount - 1; i >= 0; i--)
            DestroyImmediate(dungeonRoot.GetChild(i).gameObject);
    }

    private void SpawnEnemiesInRooms()
    {
        foreach (var room in spawned)
        {
            if (room == null) continue;

            room.TrySpawnEnemies();
        }
    }
}
