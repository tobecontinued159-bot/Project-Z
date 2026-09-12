#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class Zone2LabsGenerator
{
    private const string EnvironmentRootName = "Zone2_Environment";
    private const string LogicRootName = "Zone2_Logic";
    private const float WallHeight = 4f;
    private const float WallThickness = 0.6f;

    // Aligns with Starting Room steel shutter at x = 3.8, z = 9.
    private const float EntryX = 3.8f;
    private const float EntryZ = 9.3f;

    [MenuItem("Project Z/Generate Zone 2 (Labs)")]
    public static void GenerateZone2()
    {
        DestroyIfExists(EnvironmentRootName);
        DestroyIfExists(LogicRootName);

        GameObject environmentRoot = new GameObject(EnvironmentRootName);
        GameObject logicRoot = new GameObject(LogicRootName);
        Undo.RegisterCreatedObjectUndo(environmentRoot, "Generate Zone 2");
        Undo.RegisterCreatedObjectUndo(logicRoot, "Generate Zone 2");

        Transform environment = environmentRoot.transform;
        Transform logic = logicRoot.transform;

        CreateFloors(environment);
        CreateHallway(environment, logic);
        CreateComputerLabA(environment, logic);
        CreateDeadEndAndLabB(environment, logic);
        CreateZone3Exit(environment, logic);

        Selection.activeGameObject = environmentRoot;
        Debug.Log("Zone 2 generated adjacent to the Starting Room north door. Bake NavMesh after placing BuyableDoor on the 1250 barricade.");
    }

    private static void CreateFloors(Transform parent)
    {
        Color floorColor = new Color(0.32f, 0.32f, 0.31f);
        CreateCube("Floor_EntryHall", parent, new Vector3(EntryX, -0.5f, 12.2f), new Vector3(5.2f, 1f, 6.2f), floorColor);
        CreateCube("Floor_WestHall", parent, new Vector3(-4f, -0.5f, 15.4f), new Vector3(16.5f, 1f, 4.4f), floorColor);
        CreateCube("Floor_NorthHall", parent, new Vector3(-10.4f, -0.5f, 20.6f), new Vector3(5f, 1f, 8.2f), floorColor);
        CreateCube("Floor_EastHall", parent, new Vector3(4.5f, -0.5f, 24.6f), new Vector3(25f, 1f, 5.2f), floorColor);
        CreateCube("Floor_LabA", parent, new Vector3(-17.5f, -0.5f, 21.5f), new Vector3(10.5f, 1f, 10.5f), floorColor);
        CreateCube("Floor_LabB", parent, new Vector3(16.8f, -0.5f, 19.2f), new Vector3(10.2f, 1f, 10.2f), floorColor);
        CreateCube("Floor_DeadEnd", parent, new Vector3(20.5f, -0.5f, 24.6f), new Vector3(8f, 1f, 5.2f), floorColor);
    }

    private static void CreateHallway(Transform environment, Transform logic)
    {
        Transform hallway = CreateChild(environment, "MainHallway");
        Transform zombies = CreateChild(logic, "ZombieSpawns");
        Color wall = new Color(0.54f, 0.52f, 0.48f);
        float wallY = WallHeight * 0.5f;

        CreateCube("Hall_Entry_WestWall", hallway, new Vector3(EntryX - 2.4f, wallY, 12.4f), new Vector3(WallThickness, WallHeight, 6.6f), wall);
        CreateCube("Hall_Entry_EastWall", hallway, new Vector3(EntryX + 2.4f, wallY, 12.4f), new Vector3(WallThickness, WallHeight, 6.6f), wall);

        CreateCube("Hall_West_SouthWall", hallway, new Vector3(-3.2f, wallY, 13.3f), new Vector3(14.8f, WallHeight, WallThickness), wall);
        CreateCube("Hall_West_NorthWall_Left", hallway, new Vector3(-8.6f, wallY, 17.5f), new Vector3(6.2f, WallHeight, WallThickness), wall);
        CreateCube("Hall_West_NorthWall_Right", hallway, new Vector3(1.6f, wallY, 17.5f), new Vector3(8.4f, WallHeight, WallThickness), wall);

        CreateCube("Hall_North_WestWall_Lower", hallway, new Vector3(-12.8f, wallY, 19.2f), new Vector3(WallThickness, WallHeight, 3.8f), wall);
        CreateCube("Hall_North_EastWall", hallway, new Vector3(-8f, wallY, 21.1f), new Vector3(WallThickness, WallHeight, 7.6f), wall);

        CreateCube("Hall_East_SouthWall_Left", hallway, new Vector3(-2.2f, wallY, 22.1f), new Vector3(11.2f, WallHeight, WallThickness), wall);
        CreateCube("Hall_East_SouthWall_Right", hallway, new Vector3(11.4f, wallY, 22.1f), new Vector3(8.8f, WallHeight, WallThickness), wall);
        CreateCube("Hall_East_NorthWall", hallway, new Vector3(2.2f, wallY, 27.1f), new Vector3(21.5f, WallHeight, WallThickness), wall);

        CreateHallwayObstacles(hallway);
        CreateBrokenWindow(hallway, zombies, "Window_BrokenGlass_5", "ZombieSpawn_Window5", new Vector3(-1.4f, 2.1f, 17.15f));
        CreateMarker(zombies, "ZombieSpawn_Hallway", new Vector3(-8.8f, 1f, 15.4f), new Color(0.95f, 0.15f, 0.12f), "Hallway Spawn");
        CreateMarker(logic, "Zone1_Entry", new Vector3(EntryX, 1f, EntryZ), new Color(0.2f, 0.9f, 0.35f), "Zone 1 Entry");
    }

    private static void CreateHallwayObstacles(Transform hallway)
    {
        Color locker = new Color(0.28f, 0.34f, 0.42f);
        Color desk = new Color(0.36f, 0.26f, 0.16f);
        Color rubble = new Color(0.33f, 0.31f, 0.29f);

        GameObject lockers = CreateCube("FallenLockers", hallway, new Vector3(2.1f, 0.55f, 12.8f), new Vector3(2.8f, 1.1f, 0.9f), locker);
        lockers.transform.rotation = Quaternion.Euler(0f, 28f, 78f);

        GameObject brokenDesk = CreateCube("BrokenDesk_Hall", hallway, new Vector3(-3.6f, 0.5f, 14.7f), new Vector3(2.4f, 1f, 1.1f), desk);
        brokenDesk.transform.rotation = Quaternion.Euler(0f, -22f, 0f);

        CreateCube("Rubble_Hall_A", hallway, new Vector3(-9.4f, 0.35f, 16.1f), new Vector3(1.8f, 0.7f, 1.5f), rubble);
        CreateCube("Rubble_Hall_B", hallway, new Vector3(-10.8f, 0.28f, 21.4f), new Vector3(1.4f, 0.55f, 1.8f), rubble);

        GameObject locker2 = CreateCube("FallenLockers_North", hallway, new Vector3(-9.2f, 0.5f, 23.6f), new Vector3(2.4f, 1f, 0.8f), locker);
        locker2.transform.rotation = Quaternion.Euler(0f, 16f, -70f);

        GameObject desk2 = CreateCube("BrokenDesk_EastHall", hallway, new Vector3(5.4f, 0.5f, 24.1f), new Vector3(2.2f, 1f, 1.15f), desk);
        desk2.transform.rotation = Quaternion.Euler(0f, 34f, 0f);
    }

    private static void CreateComputerLabA(Transform environment, Transform logic)
    {
        Transform lab = CreateChild(environment, "ComputerLabA");
        Transform zombies = logic.Find("ZombieSpawns");
        if (zombies == null)
        {
            zombies = CreateChild(logic, "ZombieSpawns");
        }

        Color wall = new Color(0.5f, 0.49f, 0.46f);
        float wallY = WallHeight * 0.5f;
        Vector3 roomCenter = new Vector3(-17.5f, 0f, 21.5f);

        CreateCube("LabA_WestWall", lab, roomCenter + new Vector3(-5.1f, wallY, 0f), new Vector3(WallThickness, WallHeight, 10.6f), wall);
        CreateCube("LabA_NorthWall", lab, roomCenter + new Vector3(0f, wallY, 5.1f), new Vector3(10.6f, WallHeight, WallThickness), wall);
        CreateCube("LabA_SouthWall", lab, roomCenter + new Vector3(0f, wallY, -5.1f), new Vector3(10.6f, WallHeight, WallThickness), wall);
        CreateCube("LabA_EastWall_North", lab, roomCenter + new Vector3(5.1f, wallY, 2.4f), new Vector3(WallThickness, WallHeight, 5.4f), wall);
        CreateCube("LabA_EastWall_South", lab, roomCenter + new Vector3(5.1f, wallY, -3.3f), new Vector3(WallThickness, WallHeight, 3.4f), wall);

        GameObject brokenDoor = CreateCube("BrokenDoor_LabA", lab, roomCenter + new Vector3(5.1f, 1.6f, -0.2f), new Vector3(0.18f, 3.2f, 1.4f), new Color(0.34f, 0.22f, 0.12f));
        brokenDoor.transform.rotation = Quaternion.Euler(0f, 0f, -18f);

        CreateComputerStations(lab, roomCenter + new Vector3(-1.2f, 0f, 0.4f));
        CreateBrokenWindow(lab, zombies, "Window_LabA_6", "ZombieSpawn_Window6", roomCenter + new Vector3(-2.4f, 2.1f, 4.75f));
        CreateBrokenWindow(lab, zombies, "Window_LabA_7", "ZombieSpawn_Window7", roomCenter + new Vector3(-5f, 2.1f, 1.6f));
    }

    private static void CreateComputerStations(Transform lab, Vector3 origin)
    {
        Color desk = new Color(0.3f, 0.3f, 0.32f);
        Color monitor = new Color(0.12f, 0.14f, 0.18f);

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                Vector3 pos = origin + new Vector3((col - 1) * 2.4f, 0.5f, (1 - row) * 2.3f);
                float yaw = (row == 1 && col == 2) ? 16f : (row == 2 && col == 0) ? -12f : 4f * (col - 1);
                GameObject table = CreateCube($"ComputerStation_{row + 1}_{col + 1}", lab, pos, new Vector3(1.8f, 1f, 0.9f), desk);
                table.transform.rotation = Quaternion.Euler(0f, yaw, 0f);

                Vector3 monitorPos = pos + Quaternion.Euler(0f, yaw, 0f) * new Vector3(0f, 0.7f, -0.15f);
                CreateCube($"Monitor_{row + 1}_{col + 1}", lab, monitorPos, new Vector3(0.7f, 0.5f, 0.12f), monitor);
            }
        }
    }

    private static void CreateDeadEndAndLabB(Transform environment, Transform logic)
    {
        Transform pocket = CreateChild(environment, "DeadEndPocket_LabB");
        Color wall = new Color(0.5f, 0.48f, 0.45f);
        float wallY = WallHeight * 0.5f;

        CreateCube("DeadEnd_EastWall", pocket, new Vector3(24.4f, wallY, 24.6f), new Vector3(WallThickness, WallHeight, 5.4f), wall);
        CreateCube("DeadEnd_SouthWall", pocket, new Vector3(20.8f, wallY, 22.1f), new Vector3(7.4f, WallHeight, WallThickness), wall);

        CreateStreetFoodCart(pocket, new Vector3(21.6f, 0.7f, 24.4f));
        CreateMarker(logic, "SpeedCola_PerkMachine", new Vector3(21.6f, 1.4f, 23.5f), new Color(0.2f, 0.45f, 1f), "Speed Cola Perk Machine");

        CreateCube("WallBuy_AssaultRifle", pocket, new Vector3(23.9f, 1.45f, 25.6f), new Vector3(0.18f, 1.5f, 1.4f), new Color(0.85f, 0.75f, 0.2f));
        CreateMarker(logic, "WallBuy_AssaultRifle_Marker", new Vector3(23.1f, 1.45f, 25.6f), new Color(1f, 0.85f, 0.1f), "Wall-Buy Weapon: Assault Rifle");

        Transform labB = CreateChild(pocket, "ComputerLabB");
        Vector3 labCenter = new Vector3(16.8f, 0f, 18.4f);
        CreateCube("LabB_EastWall", labB, labCenter + new Vector3(5f, wallY, 0f), new Vector3(WallThickness, WallHeight, 10.2f), wall);
        CreateCube("LabB_SouthWall", labB, labCenter + new Vector3(0f, wallY, -5.1f), new Vector3(10.2f, WallHeight, WallThickness), wall);
        CreateCube("LabB_WestWall_South", labB, labCenter + new Vector3(-5f, wallY, -2.4f), new Vector3(WallThickness, WallHeight, 5.4f), wall);
        CreateCube("LabB_WestWall_North", labB, labCenter + new Vector3(-5f, wallY, 3.4f), new Vector3(WallThickness, WallHeight, 3.2f), wall);
        CreateComputerStations(labB, labCenter + new Vector3(0.2f, 0f, -0.4f));
        CreateMarker(logic.Find("ZombieSpawns") ?? CreateChild(logic, "ZombieSpawns"), "ZombieSpawn_LabB", labCenter + new Vector3(2.4f, 1f, -3.2f), new Color(0.95f, 0.15f, 0.12f), "Lab B Spawn");
    }

    private static void CreateStreetFoodCart(Transform parent, Vector3 position)
    {
        Transform cart = CreateChild(parent, "SpeedCola_StreetFoodCart");
        CreateCube("Cart_Body", cart, position, new Vector3(2.2f, 1.1f, 1.2f), new Color(0.82f, 0.22f, 0.18f));
        CreateCube("Cart_Canopy", cart, position + new Vector3(0f, 1.15f, 0f), new Vector3(2.4f, 0.12f, 1.4f), new Color(0.95f, 0.85f, 0.2f));
        CreateCube("Cart_Wheel_L", cart, position + new Vector3(-0.7f, -0.45f, 0.55f), new Vector3(0.25f, 0.5f, 0.25f), new Color(0.12f, 0.12f, 0.12f));
        CreateCube("Cart_Wheel_R", cart, position + new Vector3(0.7f, -0.45f, 0.55f), new Vector3(0.25f, 0.5f, 0.25f), new Color(0.12f, 0.12f, 0.12f));
        CreateCube("Cart_Sign", cart, position + new Vector3(0f, 0.35f, -0.62f), new Vector3(1.4f, 0.45f, 0.08f), new Color(0.15f, 0.45f, 0.95f));
    }

    private static void CreateZone3Exit(Transform environment, Transform logic)
    {
        Vector3 barricadePos = new Vector3(12.6f, 2f, 27.1f);
        CreateCube("Barricade_Exit_Zone3", environment, barricadePos, new Vector3(3.6f, 4f, 0.55f), new Color(0.85f, 0.4f, 0.12f));
        CreateMarker(logic, "BarricadeExit_Zone3", barricadePos + new Vector3(0f, -0.4f, -0.8f), new Color(1f, 0.5f, 0.1f), "Barricade Exit -> Zone 3 (1250 Pts)");
    }

    private static void CreateBrokenWindow(Transform parent, Transform spawnParent, string windowName, string spawnName, Vector3 windowPosition)
    {
        CreateCube(windowName, parent, windowPosition, new Vector3(2.1f, 1.7f, 0.12f), new Color(0.55f, 0.72f, 0.85f));
        CreateCube(windowName + "_Shard", parent, windowPosition + new Vector3(0.4f, -0.9f, -0.35f), new Vector3(0.7f, 0.12f, 0.5f), new Color(0.7f, 0.82f, 0.9f));
        CreateMarker(spawnParent, spawnName, windowPosition + new Vector3(0f, -1.1f, 0.7f), new Color(0.95f, 0.15f, 0.12f), spawnName);
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

        Undo.RegisterCreatedObjectUndo(cube, "Generate Zone 2");
        return cube;
    }

    private static Transform CreateChild(Transform parent, string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(child, "Generate Zone 2");
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

        Undo.RegisterCreatedObjectUndo(marker, "Generate Zone 2");
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
