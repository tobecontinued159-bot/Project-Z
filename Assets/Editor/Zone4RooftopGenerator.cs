#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class Zone4RooftopGenerator
{
    private const string EnvironmentRootName = "Zone4_Environment";
    private const string LogicRootName = "Zone4_Logic";
    private const float RoofY = 8f;
    private const float LedgeHeight = 0.55f;
    private const float LedgeThickness = 0.5f;
    private const float RoofWidth = 24f;
    private const float RoofDepth = 20f;

    // SW of this rooftop sits on Zone 3's spiral staircase (~24.9, 46.7).
    private const float CenterX = 37f;
    private const float CenterZ = 56.7f;

    [MenuItem("Project Z/Generate Zone 4 (Rooftop)")]
    public static void GenerateZone4()
    {
        DestroyIfExists(EnvironmentRootName);
        DestroyIfExists(LogicRootName);

        GameObject environmentRoot = new GameObject(EnvironmentRootName);
        GameObject logicRoot = new GameObject(LogicRootName);
        Undo.RegisterCreatedObjectUndo(environmentRoot, "Generate Zone 4");
        Undo.RegisterCreatedObjectUndo(logicRoot, "Generate Zone 4");

        Transform environment = environmentRoot.transform;
        Transform logic = logicRoot.transform;

        CreateRoofFloor(environment);
        CreatePerimeterLedges(environment);
        CreateEntryFromZone3(environment, logic);
        CreateHelipad(environment);
        CreatePowerSwitch(environment, logic);
        CreatePackAPunch(environment, logic);
        CreateHealthPerk(environment, logic);
        CreateZombieSpawns(environment, logic);

        Selection.activeGameObject = environmentRoot;
        Debug.Log("Zone 4 rooftop generated at Y=8 above Zone 3 stairs. Bake NavMesh, then wire BuyableDoor (2000) and interactables.");
    }

    private static void CreateRoofFloor(Transform parent)
    {
        CreateCube(
            "RooftopFloor",
            parent,
            new Vector3(CenterX, RoofY - 0.5f, CenterZ),
            new Vector3(RoofWidth, 1f, RoofDepth),
            new Color(0.38f, 0.38f, 0.37f));
    }

    private static void CreatePerimeterLedges(Transform parent)
    {
        Transform ledges = CreateChild(parent, "DeadlyDropLedges");
        Color ledge = new Color(0.46f, 0.45f, 0.43f);
        float y = RoofY + (LedgeHeight * 0.5f);
        float halfW = RoofWidth * 0.5f;
        float halfD = RoofDepth * 0.5f;
        const float southGap = 3.8f;
        float southWing = (RoofWidth - southGap) * 0.5f;

        CreateCube("Ledge_North", ledges, new Vector3(CenterX, y, CenterZ + halfD), new Vector3(RoofWidth + LedgeThickness, LedgeHeight, LedgeThickness), ledge);
        CreateCube("Ledge_West", ledges, new Vector3(CenterX - halfW, y, CenterZ), new Vector3(LedgeThickness, LedgeHeight, RoofDepth + LedgeThickness), ledge);
        CreateCube("Ledge_East", ledges, new Vector3(CenterX + halfW, y, CenterZ), new Vector3(LedgeThickness, LedgeHeight, RoofDepth + LedgeThickness), ledge);
        CreateCube("Ledge_South_WestWing", ledges, new Vector3(CenterX - ((southGap * 0.5f) + (southWing * 0.5f)), y, CenterZ - halfD), new Vector3(southWing, LedgeHeight, LedgeThickness), ledge);
        CreateCube("Ledge_South_EastWing", ledges, new Vector3(CenterX + ((southGap * 0.5f) + (southWing * 0.5f)), y, CenterZ - halfD), new Vector3(southWing, LedgeHeight, LedgeThickness), ledge);
    }

    private static void CreateEntryFromZone3(Transform environment, Transform logic)
    {
        float entryX = CenterX - (RoofWidth * 0.5f) + 3.2f;
        float entryZ = CenterZ - (RoofDepth * 0.5f);
        float y = RoofY + 1.2f;

        CreateCube("StairTop_Landing", environment, new Vector3(entryX, RoofY + 0.15f, entryZ + 1.1f), new Vector3(4.2f, 0.3f, 2.4f), new Color(0.42f, 0.41f, 0.39f));
        CreateCube("EntryGate_FromZone3", environment, new Vector3(entryX, y, entryZ), new Vector3(3.8f, 2.4f, 0.4f), new Color(0.85f, 0.4f, 0.12f));
        CreateCube("StairCore_Top", environment, new Vector3(entryX, RoofY - 1.6f, entryZ + 0.2f), new Vector3(1.4f, 3.2f, 1.4f), new Color(0.42f, 0.41f, 0.39f));
        CreateMarker(logic, "Entrance_FromZone3_2000", new Vector3(entryX, RoofY + 1.4f, entryZ + 1.2f), new Color(1f, 0.5f, 0.1f), "Entrance from Zone 3 (2000 Pts)");
    }

    private static void CreateHelipad(Transform parent)
    {
        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "HelipadFightingArea";
        pad.transform.SetParent(parent, false);
        pad.transform.position = new Vector3(CenterX, RoofY + 0.06f, CenterZ + 0.4f);
        pad.transform.localScale = new Vector3(9.5f, 0.06f, 9.5f);
        pad.isStatic = true;
        GameObjectUtility.SetStaticEditorFlags(pad, StaticEditorFlags.NavigationStatic | StaticEditorFlags.BatchingStatic);
        ApplyColor(pad, new Color(0.22f, 0.22f, 0.23f));
        Undo.RegisterCreatedObjectUndo(pad, "Generate Zone 4");

        CreateCube("Helipad_H_Bar", parent, new Vector3(CenterX, RoofY + 0.13f, CenterZ + 0.4f), new Vector3(3.4f, 0.05f, 0.45f), new Color(0.92f, 0.92f, 0.9f));
        CreateCube("Helipad_H_Left", parent, new Vector3(CenterX - 1.7f, RoofY + 0.13f, CenterZ + 0.4f), new Vector3(0.45f, 0.05f, 3.4f), new Color(0.92f, 0.92f, 0.9f));
        CreateCube("Helipad_H_Right", parent, new Vector3(CenterX + 1.7f, RoofY + 0.13f, CenterZ + 0.4f), new Vector3(0.45f, 0.05f, 3.4f), new Color(0.92f, 0.92f, 0.9f));
    }

    private static void CreatePowerSwitch(Transform environment, Transform logic)
    {
        float x = CenterX - 8.6f;
        float z = CenterZ + 7.4f;
        Transform box = CreateChild(environment, "MainPowerSwitch");
        CreateCube("PowerBox", box, new Vector3(x, RoofY + 1.1f, z), new Vector3(1.1f, 1.6f, 0.7f), new Color(0.18f, 0.18f, 0.2f));
        CreateCube("PowerLever", box, new Vector3(x, RoofY + 1.35f, z - 0.45f), new Vector3(0.12f, 0.7f, 0.12f), new Color(0.85f, 0.15f, 0.12f));
        CreateCube("PowerLight", box, new Vector3(x, RoofY + 1.85f, z - 0.32f), new Vector3(0.25f, 0.18f, 0.18f), new Color(0.2f, 0.85f, 0.25f));
        CreateMarker(logic, "MainPowerSwitch_Marker", new Vector3(x, RoofY + 1.3f, z - 0.9f), new Color(0.25f, 0.9f, 0.3f), "Main Power Switch");
    }

    private static void CreatePackAPunch(Transform environment, Transform logic)
    {
        float x = CenterX + 8.2f;
        float z = CenterZ + 6.8f;
        Transform machine = CreateChild(environment, "PackAPunch_CopyMachine");
        CreateCube("Copier_Body", machine, new Vector3(x, RoofY + 0.7f, z), new Vector3(1.8f, 1.4f, 1.3f), new Color(0.55f, 0.55f, 0.58f));
        CreateCube("Copier_Lid", machine, new Vector3(x, RoofY + 1.45f, z + 0.1f), new Vector3(1.7f, 0.12f, 1.1f), new Color(0.35f, 0.12f, 0.14f));
        CreateCube("Copier_Blood", machine, new Vector3(x + 0.4f, RoofY + 0.15f, z - 0.4f), new Vector3(0.9f, 0.08f, 0.7f), new Color(0.45f, 0.05f, 0.05f));
        CreateMarker(logic, "PackAPunch_5000", new Vector3(x, RoofY + 1.5f, z - 1.1f), new Color(0.65f, 0.2f, 0.85f), "Pack-a-Punch Spot: 5000 pts");
    }

    private static void CreateHealthPerk(Transform environment, Transform logic)
    {
        float x = CenterX + 7.6f;
        float z = CenterZ - 6.6f;
        Transform cooler = CreateChild(environment, "HealthBoost_OrangeIceCooler");
        CreateCube("Cooler_Body", cooler, new Vector3(x, RoofY + 0.7f, z), new Vector3(1.5f, 1.2f, 1.1f), new Color(0.95f, 0.45f, 0.08f));
        CreateCube("Cooler_Lid", cooler, new Vector3(x, RoofY + 1.35f, z), new Vector3(1.55f, 0.12f, 1.15f), new Color(0.85f, 0.35f, 0.05f));
        CreateCube("Cooler_Logo", cooler, new Vector3(x, RoofY + 0.75f, z - 0.56f), new Vector3(0.9f, 0.45f, 0.05f), new Color(1f, 0.85f, 0.2f));
        CreateMarker(logic, "HealthBoostPerk_2500", new Vector3(x, RoofY + 1.4f, z - 1f), new Color(0.2f, 0.45f, 1f), "Health Boost Perk Spot: 2500 pts");
    }

    private static void CreateZombieSpawns(Transform environment, Transform logic)
    {
        Transform props = CreateChild(environment, "SpawnProps");
        Color metal = new Color(0.4f, 0.42f, 0.44f);
        float northZ = CenterZ + (RoofDepth * 0.5f) - 1.2f;
        float eastX = CenterX + (RoofWidth * 0.5f) - 1.1f;
        float southZ = CenterZ - (RoofDepth * 0.5f) + 1.1f;

        CreateCube("AtticDoor_Window12", props, new Vector3(CenterX - 2.2f, RoofY + 1.6f, northZ + 0.7f), new Vector3(1.8f, 2.2f, 0.25f), new Color(0.32f, 0.24f, 0.16f));
        CreateMarker(logic, "ZombieSpawn_Window12_AtticDoor", new Vector3(CenterX - 2.2f, RoofY + 1f, northZ), new Color(0.95f, 0.15f, 0.12f), "Window 12: Attic Door");

        CreateCube("WaterTank_Window13", props, new Vector3(CenterX + 2.6f, RoofY + 1.3f, northZ), new Vector3(2.2f, 2.4f, 2.2f), metal);
        CreateCube("WaterTank_Nook", props, new Vector3(CenterX + 4.2f, RoofY + 0.45f, northZ - 1.4f), new Vector3(1.4f, 0.9f, 1.6f), metal);
        CreateMarker(logic, "ZombieSpawn_Window13_WaterTankNook", new Vector3(CenterX + 2.6f, RoofY + 1f, northZ - 1.6f), new Color(0.95f, 0.15f, 0.12f), "Window 13: Water Tank Nook");

        CreateCube("LedgeClimber_South", props, new Vector3(CenterX + 1.5f, RoofY + 0.2f, southZ), new Vector3(2.4f, 0.4f, 0.8f), metal);
        CreateMarker(logic, "ZombieSpawn_Window14_LedgeClimbers", new Vector3(CenterX + 3.8f, RoofY + 1f, southZ + 0.4f), new Color(0.95f, 0.15f, 0.12f), "Window 14: Ledge Climbers");

        CreateCube("FenceBreak_East", props, new Vector3(eastX, RoofY + 0.7f, CenterZ + 0.6f), new Vector3(0.18f, 1.4f, 3.6f), new Color(0.28f, 0.3f, 0.32f));
        CreateMarker(logic, "ZombieSpawn_Window15_FenceBreakers", new Vector3(eastX - 0.8f, RoofY + 1f, CenterZ + 0.6f), new Color(0.95f, 0.15f, 0.12f), "Window 15: Fence Breakers");
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
        Undo.RegisterCreatedObjectUndo(cube, "Generate Zone 4");
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
        Undo.RegisterCreatedObjectUndo(child, "Generate Zone 4");
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

        Undo.RegisterCreatedObjectUndo(marker, "Generate Zone 4");
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
