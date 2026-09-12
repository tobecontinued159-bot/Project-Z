#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class Zone3PlazaGenerator
{
    private const string EnvironmentRootName = "Zone3_Environment";
    private const string LogicRootName = "Zone3_Logic";
    private const float WallHeight = 5f;
    private const float WallThickness = 0.7f;

    // Aligns with Zone 2 barricade at x = 12.6, z = 27.1
    private const float EntryX = 12.6f;
    private const float EntryZ = 27.4f;
    private const float PlazaWidth = 24f;
    private const float PlazaDepth = 20f;
    private const float CenterX = 16.5f;
    private const float CenterZ = 38.5f;

    [MenuItem("Project Z/Generate Zone 3 (Plaza)")]
    public static void GenerateZone3()
    {
        DestroyIfExists(EnvironmentRootName);
        DestroyIfExists(LogicRootName);

        GameObject environmentRoot = new GameObject(EnvironmentRootName);
        GameObject logicRoot = new GameObject(LogicRootName);
        Undo.RegisterCreatedObjectUndo(environmentRoot, "Generate Zone 3");
        Undo.RegisterCreatedObjectUndo(logicRoot, "Generate Zone 3");

        Transform environment = environmentRoot.transform;
        Transform logic = logicRoot.transform;

        CreateFloor(environment);
        CreateWalls(environment);
        CreateEntryFromZone2(environment, logic);
        CreateCenterObstacles(environment, logic);
        CreateWestWallBuys(environment, logic);
        CreateZombieWindows(environment, logic);
        CreateZone4Staircase(environment, logic);

        Selection.activeGameObject = environmentRoot;
        Debug.Log("Zone 3 plaza generated north of the Zone 2 barricade. Bake NavMesh, then wire BuyableDoor (1250 entry / 2000 stairs).");
    }

    private static void CreateFloor(Transform parent)
    {
        CreateCube(
            "Floor_Plaza",
            parent,
            new Vector3(CenterX, -0.5f, CenterZ),
            new Vector3(PlazaWidth, 1f, PlazaDepth),
            new Color(0.34f, 0.34f, 0.33f));
    }

    private static void CreateWalls(Transform parent)
    {
        Color wall = new Color(0.56f, 0.55f, 0.52f);
        float wallY = WallHeight * 0.5f;
        float halfW = PlazaWidth * 0.5f;
        float halfD = PlazaDepth * 0.5f;

        const float southDoorGap = 3.6f;
        const float northEastStairGap = 3.8f;

        float southWing = (PlazaWidth - southDoorGap) * 0.5f;
        CreateCube("Wall_South_WestWing", parent, new Vector3(CenterX - ((southDoorGap * 0.5f) + (southWing * 0.5f)), wallY, CenterZ - halfD), new Vector3(southWing, WallHeight, WallThickness), wall);
        CreateCube("Wall_South_EastWing", parent, new Vector3(CenterX + ((southDoorGap * 0.5f) + (southWing * 0.5f)), wallY, CenterZ - halfD), new Vector3(southWing, WallHeight, WallThickness), wall);

        CreateCube("Wall_West", parent, new Vector3(CenterX - halfW, wallY, CenterZ), new Vector3(WallThickness, WallHeight, PlazaDepth + WallThickness), wall);
        CreateCube("Wall_East", parent, new Vector3(CenterX + halfW, wallY, CenterZ - 1.1f), new Vector3(WallThickness, WallHeight, PlazaDepth - 2.2f), wall);

        float northStairOffset = 6.4f;
        CreateCube("Wall_North_West", parent, new Vector3(CenterX - 4.2f, wallY, CenterZ + halfD), new Vector3(PlazaWidth - northStairOffset - 2.2f, WallHeight, WallThickness), wall);
        CreateCube("Wall_North_EastStub", parent, new Vector3(CenterX + halfW - 1.1f, wallY, CenterZ + halfD), new Vector3(2.2f, WallHeight, WallThickness), wall);
    }

    private static void CreateEntryFromZone2(Transform environment, Transform logic)
    {
        Vector3 gatePos = new Vector3(EntryX, WallHeight * 0.5f, CenterZ - (PlazaDepth * 0.5f));
        CreateCube("EntryGate_FromZone2", environment, gatePos, new Vector3(3.6f, WallHeight, 0.55f), new Color(0.85f, 0.4f, 0.12f));
        CreateMarker(logic, "Entry_FromZone2_1250", new Vector3(EntryX, 1.5f, gatePos.z + 0.9f), new Color(1f, 0.5f, 0.1f), "Entry from Zone 2 (1250 Pts)");
    }

    private static void CreateCenterObstacles(Transform environment, Transform logic)
    {
        Transform pillars = CreateChild(environment, "ConcretePillars");
        Color concrete = new Color(0.48f, 0.47f, 0.45f);
        float offsetX = 4.4f;
        float offsetZ = 3.6f;

        CreateCube("Pillar_NW", pillars, new Vector3(CenterX - offsetX, 2.4f, CenterZ + offsetZ), new Vector3(1.8f, 4.8f, 1.8f), concrete);
        CreateCube("Pillar_NE", pillars, new Vector3(CenterX + offsetX, 2.4f, CenterZ + offsetZ), new Vector3(1.8f, 4.8f, 1.8f), concrete);
        CreateCube("Pillar_SW", pillars, new Vector3(CenterX - offsetX, 2.4f, CenterZ - offsetZ), new Vector3(1.8f, 4.8f, 1.8f), concrete);
        CreateCube("Pillar_SE", pillars, new Vector3(CenterX + offsetX, 2.4f, CenterZ - offsetZ), new Vector3(1.8f, 4.8f, 1.8f), concrete);

        GameObject manhole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        manhole.name = "DrainageManhole";
        manhole.transform.SetParent(environment, false);
        manhole.transform.position = new Vector3(CenterX, 0.04f, CenterZ);
        manhole.transform.localScale = new Vector3(2.4f, 0.04f, 2.4f);
        manhole.isStatic = true;
        GameObjectUtility.SetStaticEditorFlags(manhole, StaticEditorFlags.NavigationStatic | StaticEditorFlags.BatchingStatic);
        ApplyColor(manhole, new Color(0.18f, 0.18f, 0.19f));
        Undo.RegisterCreatedObjectUndo(manhole, "Generate Zone 3");

        CreateMarker(logic, "ZombieSpawn_DrainageManhole", new Vector3(CenterX, 0.4f, CenterZ), new Color(0.95f, 0.15f, 0.12f), "Zombie Spawn - Drainage Manhole");
    }

    private static void CreateWestWallBuys(Transform environment, Transform logic)
    {
        float westX = CenterX - (PlazaWidth * 0.5f) + 0.55f;

        CreateCube("MysteryWeaponBox", environment, new Vector3(westX + 0.7f, 0.7f, CenterZ + 1.6f), new Vector3(1.8f, 1.2f, 1.1f), new Color(0.12f, 0.45f, 0.18f));
        CreateMarker(logic, "MysteryWeaponBox_Marker", new Vector3(westX + 1.5f, 1.3f, CenterZ + 1.6f), new Color(0.25f, 0.9f, 0.3f), "Mystery Weapon Box");

        CreateStreetFoodCart(environment, new Vector3(westX + 1.1f, 0.7f, CenterZ - 2.8f));
        CreateCube("WallBuy_HeavyMachineGun", environment, new Vector3(westX, 1.5f, CenterZ - 4.6f), new Vector3(0.2f, 1.6f, 1.6f), new Color(0.75f, 0.55f, 0.15f));
        CreateMarker(logic, "SpeedCola_HMG_Marker", new Vector3(westX + 1.4f, 1.4f, CenterZ - 3.4f), new Color(0.2f, 0.45f, 1f), "Speed Cola / Heavy Machine Gun Wall Buy");
    }

    private static void CreateStreetFoodCart(Transform parent, Vector3 position)
    {
        Transform cart = CreateChild(parent, "SpeedCola_StreetFoodCart");
        CreateCube("Cart_Body", cart, position, new Vector3(2.1f, 1.1f, 1.15f), new Color(0.82f, 0.22f, 0.18f));
        CreateCube("Cart_Canopy", cart, position + new Vector3(0f, 1.15f, 0f), new Vector3(2.3f, 0.12f, 1.35f), new Color(0.95f, 0.85f, 0.2f));
        CreateCube("Cart_Sign", cart, position + new Vector3(0.7f, 0.35f, 0f), new Vector3(0.08f, 0.45f, 1.1f), new Color(0.15f, 0.45f, 0.95f));
    }

    private static void CreateZombieWindows(Transform environment, Transform logic)
    {
        Transform windows = CreateChild(environment, "SpawnWindows");
        Color glass = new Color(0.5f, 0.7f, 0.85f);
        float northZ = CenterZ + (PlazaDepth * 0.5f) - 0.45f;
        float eastX = CenterX + (PlazaWidth * 0.5f) - 0.45f;

        CreateCube("Window_9_North", windows, new Vector3(CenterX - 2.4f, 2.3f, northZ), new Vector3(2.4f, 1.8f, 0.14f), glass);
        CreateMarker(logic, "ZombieSpawn_Window9", new Vector3(CenterX - 2.4f, 1f, northZ + 0.8f), new Color(0.95f, 0.15f, 0.12f), "Zombie Spawn - Window 9");

        CreateCube("Window_10_East", windows, new Vector3(eastX, 2.3f, CenterZ + 1.2f), new Vector3(0.14f, 1.8f, 2.4f), glass);
        CreateMarker(logic, "ZombieSpawn_Window10", new Vector3(eastX + 0.8f, 1f, CenterZ + 1.2f), new Color(0.95f, 0.15f, 0.12f), "Zombie Spawn - Window 10");
    }

    private static void CreateZone4Staircase(Transform environment, Transform logic)
    {
        Transform stairs = CreateChild(environment, "GatedSpiralStaircase");
        float stairX = CenterX + 8.4f;
        float stairZ = CenterZ + 8.2f;
        Color concrete = new Color(0.42f, 0.41f, 0.39f);

        CreateCube("StairCore", stairs, new Vector3(stairX, 1.6f, stairZ), new Vector3(1.4f, 3.2f, 1.4f), concrete);
        CreateCube("StairStep_1", stairs, new Vector3(stairX + 1.3f, 0.35f, stairZ), new Vector3(1.6f, 0.35f, 1.1f), concrete);
        CreateCube("StairStep_2", stairs, new Vector3(stairX, 0.9f, stairZ + 1.3f), new Vector3(1.1f, 0.35f, 1.6f), concrete);
        CreateCube("StairStep_3", stairs, new Vector3(stairX - 1.3f, 1.45f, stairZ), new Vector3(1.6f, 0.35f, 1.1f), concrete);
        CreateCube("StairStep_4", stairs, new Vector3(stairX, 2f, stairZ - 1.3f), new Vector3(1.1f, 0.35f, 1.6f), concrete);

        CreateCube("StairGate_Zone4", stairs, new Vector3(stairX + 0.2f, 2f, stairZ + 2.15f), new Vector3(3.8f, 4f, 0.45f), new Color(0.85f, 0.4f, 0.12f));
        CreateMarker(logic, "GatedSpiralStaircase_Zone4_2000", new Vector3(stairX, 1.6f, stairZ + 1.2f), new Color(1f, 0.5f, 0.1f), "Gated Spiral Staircase -> Zone 4 (2000 Pts)");
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
        ApplyColor(cube, color);
        Undo.RegisterCreatedObjectUndo(cube, "Generate Zone 3");
        return cube;
    }

    private static void ApplyColor(GameObject go, Color color)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer == null)
        {
            return;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.color = color;
        renderer.sharedMaterial = material;
    }

    private static Transform CreateChild(Transform parent, string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(child, "Generate Zone 3");
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

        Undo.RegisterCreatedObjectUndo(marker, "Generate Zone 3");
    }

    private static void DestroyIfExists(string objectName)
    {
        GameObject existing = GameObject.Find(objectName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }
    }
}
#endif
