#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class StartingRoomGenerator
{
    private const string EnvironmentRootName = "StartingRoom_Environment";
    private const string LogicRootName = "StartingRoom_Logic";
    private const float WallHeight = 4f;
    private const float WallThickness = 0.6f;
    private const float RoomWidth = 28f;
    private const float RoomDepth = 18f;

    [MenuItem("Project Z/Generate Starting Room")]
    public static void GenerateStartingRoom()
    {
        DestroyIfExists(EnvironmentRootName);
        DestroyIfExists(LogicRootName);

        GameObject environmentRoot = new GameObject(EnvironmentRootName);
        GameObject logicRoot = new GameObject(LogicRootName);
        Undo.RegisterCreatedObjectUndo(environmentRoot, "Generate Starting Room");
        Undo.RegisterCreatedObjectUndo(logicRoot, "Generate Starting Room");

        CreateFloor(environmentRoot.transform);
        CreatePerimeterWalls(environmentRoot.transform);
        CreateNorthWallFeatures(environmentRoot.transform, logicRoot.transform);
        CreateCenterDesks(environmentRoot.transform);
        CreateSouthWallFeatures(environmentRoot.transform);
        CreateCornerDebris(environmentRoot.transform);
        CreatePlayerSpawn(logicRoot.transform);

        Selection.activeGameObject = environmentRoot;
        Debug.Log("Starting Room generated. Bake NavMesh, then wire BuyableDoor / WeaponWallBuy / WaveManager to the logic markers.");
    }

    private static void CreateFloor(Transform parent)
    {
        CreateCube(
            "Floor",
            parent,
            new Vector3(0f, -0.5f, 0f),
            new Vector3(RoomWidth, 1f, RoomDepth),
            new Color(0.36f, 0.35f, 0.33f));
    }

    private static void CreatePerimeterWalls(Transform parent)
    {
        Color wallColor = new Color(0.58f, 0.56f, 0.52f);
        float wallY = WallHeight * 0.5f;
        float halfW = RoomWidth * 0.5f;
        float halfD = RoomDepth * 0.5f;

        CreateCube("Wall_South", parent, new Vector3(0f, wallY, -halfD), new Vector3(RoomWidth + WallThickness, WallHeight, WallThickness), wallColor);
        CreateCube("Wall_West", parent, new Vector3(-halfW, wallY, 0f), new Vector3(WallThickness, WallHeight, RoomDepth + WallThickness), wallColor);
        CreateCube("Wall_East", parent, new Vector3(halfW, wallY, 0f), new Vector3(WallThickness, WallHeight, RoomDepth + WallThickness), wallColor);

        const float doorGap = 3.2f;
        float northZ = halfD;
        float wingWidth = (RoomWidth - doorGap) * 0.5f;
        CreateCube("Wall_North_WestWing", parent, new Vector3(-((doorGap * 0.5f) + (wingWidth * 0.5f)), wallY, northZ), new Vector3(wingWidth, WallHeight, WallThickness), wallColor);
        CreateCube("Wall_North_EastWing", parent, new Vector3((doorGap * 0.5f) + (wingWidth * 0.5f), wallY, northZ), new Vector3(wingWidth, WallHeight, WallThickness), wallColor);
    }

    private static void CreateNorthWallFeatures(Transform environment, Transform logic)
    {
        Transform windows = CreateChild(environment, "NorthWindows");
        Transform zombieSpawns = CreateChild(logic, "ZombieSpawns");
        Transform wallBuys = CreateChild(logic, "WallBuys");
        Color windowColor = new Color(0.45f, 0.62f, 0.78f, 1f);
        float northInteriorZ = (RoomDepth * 0.5f) - 0.45f;

        CreateWindowWithSpawn(windows, zombieSpawns, "Window_Balcony_1", "ZombieSpawn_Window1", new Vector3(-12.2f, 2.1f, northInteriorZ), windowColor);
        CreateWindowWithSpawn(windows, zombieSpawns, "Window_Balcony_2", "ZombieSpawn_Window2", new Vector3(-9.4f, 2.1f, northInteriorZ), windowColor);

        CreateWallProp("WallBuy_Pistol", environment, new Vector3(-6.2f, 1.4f, northInteriorZ), new Vector3(0.18f, 1.4f, 1.1f), new Color(0.85f, 0.75f, 0.2f));
        CreateMarker(wallBuys, "WallBuy_Pistol_Marker", new Vector3(-6.2f, 1.4f, northInteriorZ - 0.6f), new Color(1f, 0.85f, 0.1f), "Pistol Wall Buy");

        CreateVendingMachine(environment, new Vector3(-3.1f, 1.2f, northInteriorZ));
        CreateMarker(logic, "QuickRevive_Marker", new Vector3(-3.1f, 1.4f, northInteriorZ - 0.8f), new Color(0.2f, 0.45f, 1f), "Quick Revive");

        CreateWallProp("WallBuy_Shotgun", environment, new Vector3(0.1f, 1.4f, northInteriorZ), new Vector3(0.18f, 1.4f, 1.3f), new Color(0.85f, 0.55f, 0.15f));
        CreateMarker(wallBuys, "WallBuy_Shotgun_Marker", new Vector3(0.1f, 1.4f, northInteriorZ - 0.6f), new Color(1f, 0.85f, 0.1f), "Shotgun Wall Buy");

        CreateCube(
            "Door_SteelShutter",
            environment,
            new Vector3(3.8f, WallHeight * 0.5f, RoomDepth * 0.5f),
            new Vector3(3.2f, WallHeight, 0.5f),
            new Color(0.72f, 0.18f, 0.16f));
        CreateMarker(logic, "InteractableDoor_750", new Vector3(3.8f, 1.5f, (RoomDepth * 0.5f) - 0.7f), new Color(1f, 0.85f, 0.1f), "Steel Door 750 Pts");

        CreateWindowWithSpawn(windows, zombieSpawns, "Window_Hallway_3", "ZombieSpawn_Window3", new Vector3(8.4f, 2.1f, northInteriorZ), windowColor);
        CreateWindowWithSpawn(windows, zombieSpawns, "Window_Hallway_4", "ZombieSpawn_Window4", new Vector3(11.4f, 2.1f, northInteriorZ), windowColor);
    }

    private static void CreateCenterDesks(Transform parent)
    {
        Transform desks = CreateChild(parent, "LectureDesks");
        Color deskColor = new Color(0.38f, 0.28f, 0.18f);
        Color chairColor = new Color(0.22f, 0.22f, 0.24f);

        Vector3[] deskCenters =
        {
            new Vector3(-4.5f, 0.55f, 1.8f),
            new Vector3(0.4f, 0.5f, 2.2f),
            new Vector3(4.8f, 0.6f, 1.4f),
            new Vector3(-3.6f, 0.5f, -0.6f),
            new Vector3(1.8f, 0.45f, -0.2f),
            new Vector3(-1.2f, 0.55f, -2.4f),
            new Vector3(3.4f, 0.5f, -2.1f)
        };

        Vector3[] deskScales =
        {
            new Vector3(5.2f, 1.1f, 1.3f),
            new Vector3(4.6f, 1f, 1.2f),
            new Vector3(4.8f, 1.2f, 1.15f),
            new Vector3(4.2f, 1f, 1.25f),
            new Vector3(4.4f, 0.9f, 1.2f),
            new Vector3(5f, 1.1f, 1.15f),
            new Vector3(3.8f, 1f, 1.2f)
        };

        float[] deskYaws = { 8f, -6f, 14f, -12f, 5f, -18f, 10f };

        for (int i = 0; i < deskCenters.Length; i++)
        {
            GameObject desk = CreateCube($"LectureDesk_{i + 1}", desks, deskCenters[i], deskScales[i], deskColor);
            desk.transform.rotation = Quaternion.Euler(0f, deskYaws[i], 0f);

            Vector3 chairPos = deskCenters[i] + Quaternion.Euler(0f, deskYaws[i], 0f) * new Vector3(0f, -0.05f, -1.1f);
            GameObject chair = CreateCube($"LectureChair_{i + 1}", desks, chairPos, new Vector3(0.7f, 0.9f, 0.7f), chairColor);
            chair.transform.rotation = Quaternion.Euler(0f, deskYaws[i] + 12f, 0f);
        }
    }

    private static void CreateSouthWallFeatures(Transform parent)
    {
        float southZ = -(RoomDepth * 0.5f) + 0.4f;
        Color boardColor = new Color(0.12f, 0.22f, 0.16f);
        Color woodColor = new Color(0.42f, 0.28f, 0.16f);

        CreateCube("Chalkboard", parent, new Vector3(-4.5f, 2.2f, southZ), new Vector3(10f, 2.4f, 0.16f), boardColor);
        CreateCube("TeachersDesk", parent, new Vector3(-4.2f, 0.55f, southZ + 1.4f), new Vector3(3.6f, 1.1f, 1.4f), woodColor);
        CreateCube("TeachersDesk_Debris", parent, new Vector3(-2.1f, 0.25f, southZ + 2.1f), new Vector3(1.2f, 0.5f, 0.8f), new Color(0.3f, 0.2f, 0.12f));

        CreateCube("MainDoor_Left", parent, new Vector3(4.3f, 1.8f, southZ), new Vector3(1.5f, 3.6f, 0.22f), woodColor);
        CreateCube("MainDoor_Right", parent, new Vector3(5.9f, 1.8f, southZ), new Vector3(1.5f, 3.6f, 0.22f), woodColor);
        CreateCube("MainDoors_LockBar", parent, new Vector3(5.1f, 1.6f, southZ + 0.18f), new Vector3(3.3f, 0.18f, 0.12f), new Color(0.15f, 0.15f, 0.16f));
    }

    private static void CreateCornerDebris(Transform parent)
    {
        Transform debris = CreateChild(parent, "CornerDebris");
        Color rubble = new Color(0.34f, 0.32f, 0.3f);
        float halfW = RoomWidth * 0.5f - 1.2f;
        float halfD = RoomDepth * 0.5f - 1.2f;

        CreateCube("Debris_NW", debris, new Vector3(-halfW, 0.35f, halfD), new Vector3(2.2f, 0.7f, 1.6f), rubble);
        CreateCube("Debris_NE", debris, new Vector3(halfW, 0.3f, halfD), new Vector3(1.8f, 0.6f, 1.8f), rubble);
        CreateCube("Debris_SW", debris, new Vector3(-halfW, 0.28f, -halfD), new Vector3(2f, 0.55f, 1.5f), rubble);
        CreateCube("Debris_SE", debris, new Vector3(halfW, 0.32f, -halfD), new Vector3(1.7f, 0.65f, 1.7f), rubble);
    }

    private static void CreatePlayerSpawn(Transform logic)
    {
        CreateMarker(logic, "PlayerSpawnPoint", new Vector3(0f, 1f, -5.5f), new Color(0.2f, 0.9f, 0.35f), "Player Spawn");
    }

    private static void CreateWindowWithSpawn(
        Transform windowParent,
        Transform spawnParent,
        string windowName,
        string spawnName,
        Vector3 windowPosition,
        Color windowColor)
    {
        CreateCube(windowName, windowParent, windowPosition, new Vector3(2.2f, 1.8f, 0.12f), windowColor);
        CreateMarker(spawnParent, spawnName, windowPosition + new Vector3(0f, -1.1f, 0.8f), new Color(0.95f, 0.15f, 0.12f), spawnName);
    }

    private static void CreateVendingMachine(Transform parent, Vector3 position)
    {
        Transform machine = CreateChild(parent, "QuickRevive_Vending");
        CreateCube("Vending_Body", machine, position, new Vector3(1.1f, 2.4f, 0.9f), new Color(0.18f, 0.35f, 0.85f));
        CreateCube("Vending_Top", machine, position + new Vector3(0f, 1.15f, 0.05f), new Vector3(1.15f, 0.2f, 0.95f), new Color(0.12f, 0.22f, 0.55f));
        CreateCube("Vending_Screen", machine, position + new Vector3(0f, 0.45f, -0.48f), new Vector3(0.7f, 0.55f, 0.05f), new Color(0.4f, 0.9f, 1f));
    }

    private static void CreateWallProp(string objectName, Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        CreateCube(objectName, parent, position, scale, color);
    }

    private static GameObject CreateCube(string objectName, Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(parent, false);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        cube.isStatic = true;
        GameObjectUtility.SetStaticEditorFlags(cube, StaticEditorFlags.NavigationStatic | StaticEditorFlags.BatchingStatic);

        if (cube.GetComponent<BoxCollider>() == null)
        {
            cube.AddComponent<BoxCollider>();
        }

        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateMaterial(color);
        }

        Undo.RegisterCreatedObjectUndo(cube, "Generate Starting Room");
        return cube;
    }

    private static Transform CreateChild(Transform parent, string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(child, "Generate Starting Room");
        return child.transform;
    }

    private static void CreateMarker(Transform parent, string markerName, Vector3 position, Color color, string label)
    {
        GameObject marker = new GameObject(markerName);
        marker.transform.SetParent(parent, false);
        marker.transform.position = position;

        LevelMarkerGizmo gizmo = marker.AddComponent<LevelMarkerGizmo>();
        gizmo.gizmoColor = color;
        gizmo.gizmoLabel = label;
        gizmo.gizmoRadius = 0.35f;

        Undo.RegisterCreatedObjectUndo(marker, "Generate Starting Room");
    }

    private static void DestroyIfExists(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }
    }

    private static Material CreateMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;
        return material;
    }
}
#endif
