#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class FullVerticalMapGenerator
{
    private const string RootName = "ProjectZ_Level";
    private const float WallHeight = 4f;
    private const float WallThickness = 0.6f;
    private const float GroundY = -8f;
    private const float RoofY = 8f;

    private static readonly Color FloorColor = new Color(0.34f, 0.34f, 0.33f);
    private static readonly Color WallColor = new Color(0.56f, 0.54f, 0.5f);
    private static readonly Color Red = new Color(0.95f, 0.15f, 0.12f);
    private static readonly Color Yellow = new Color(1f, 0.85f, 0.1f);
    private static readonly Color Blue = new Color(0.2f, 0.45f, 1f);
    private static readonly Color Orange = new Color(1f, 0.5f, 0.1f);
    private static readonly Color Green = new Color(0.25f, 0.9f, 0.3f);
    private static readonly Color Purple = new Color(0.65f, 0.2f, 0.85f);

    [MenuItem("Project Z/Generate Full Vertical Map (Zones 1-4)")]
    public static void GenerateFullVerticalMap()
    {
        DestroyIfExists(RootName);

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Generate Full Vertical Map");

        Transform floor2 = CreateChild(root.transform, "Floor2_Middle_Y0");
        Transform floor1 = CreateChild(root.transform, "Floor1_Ground_Y-8");
        Transform floor3 = CreateChild(root.transform, "Floor3_Rooftop_Y+8");

        GenerateZone1(floor2);
        GenerateZone2(floor2);
        GenerateDownStairs(floor2, floor1);
        GenerateUpStairs(floor2, floor3);
        GenerateZone3(floor1);
        GenerateZone4(floor3);

        Selection.activeGameObject = root;
        Debug.Log("Vertical 3-story map generated. Bake NavMesh per floor, then wire BuyableDoor on the 1250/2000 stair gates.");
    }

    private static void GenerateZone1(Transform floor)
    {
        Transform environment = CreateChild(floor, "Zone1_Environment");
        Transform logic = CreateChild(floor, "Zone1_Logic");
        const float width = 28f;
        const float depth = 18f;
        float wallY = WallHeight * 0.5f;
        float halfW = width * 0.5f;
        float halfD = depth * 0.5f;

        CreateCube("Z1_Floor", environment, new Vector3(0f, -0.5f, 0f), new Vector3(width, 1f, depth), FloorColor);
        CreateCube("Z1_Wall_North", environment, new Vector3(0f, wallY, halfD), new Vector3(width + WallThickness, WallHeight, WallThickness), WallColor);
        CreateCube("Z1_Wall_South", environment, new Vector3(0f, wallY, -halfD), new Vector3(width + WallThickness, WallHeight, WallThickness), WallColor);
        CreateCube("Z1_Wall_West", environment, new Vector3(-halfW, wallY, 0f), new Vector3(WallThickness, WallHeight, depth + WallThickness), WallColor);

        const float doorGap = 3.2f;
        float wing = (depth - doorGap) * 0.5f;
        CreateCube("Z1_Wall_East_North", environment, new Vector3(halfW, wallY, (doorGap * 0.5f) + (wing * 0.5f)), new Vector3(WallThickness, WallHeight, wing), WallColor);
        CreateCube("Z1_Wall_East_South", environment, new Vector3(halfW, wallY, -((doorGap * 0.5f) + (wing * 0.5f))), new Vector3(WallThickness, WallHeight, wing), WallColor);
        CreateCube("Z1_Door_ToZone2", environment, new Vector3(halfW, wallY, 0f), new Vector3(0.5f, WallHeight, doorGap), new Color(0.72f, 0.18f, 0.16f));
        CreateMarker(logic, "Z1_Exit_ToZone2", new Vector3(halfW - 0.8f, 1.5f, 0f), Orange, "East Door to Zone 2");

        float northZ = halfD - 0.45f;
        Color glass = new Color(0.45f, 0.62f, 0.78f);
        CreateCube("Z1_Window_1", environment, new Vector3(-10.4f, 2.1f, northZ), new Vector3(2.2f, 1.8f, 0.12f), glass);
        CreateCube("Z1_Window_2", environment, new Vector3(-7.4f, 2.1f, northZ), new Vector3(2.2f, 1.8f, 0.12f), glass);
        CreateMarker(logic, "Z1_Spawn_Window1", new Vector3(-10.4f, 1f, northZ + 0.7f), Red, "Zombie Spawn Window 1");
        CreateMarker(logic, "Z1_Spawn_Window2", new Vector3(-7.4f, 1f, northZ + 0.7f), Red, "Zombie Spawn Window 2");

        CreateCube("Z1_WallBuy_Pistol", environment, new Vector3(-3.5f, 1.4f, northZ), new Vector3(1.1f, 1.4f, 0.18f), new Color(0.85f, 0.75f, 0.2f));
        CreateMarker(logic, "Z1_WallBuy_Pistol", new Vector3(-3.5f, 1.4f, northZ - 0.6f), Yellow, "Pistol Wall Buy");
        CreateCube("Z1_QuickRevive", environment, new Vector3(-0.3f, 1.2f, northZ), new Vector3(1.1f, 2.4f, 0.9f), new Color(0.18f, 0.35f, 0.85f));
        CreateMarker(logic, "Z1_QuickRevive", new Vector3(-0.3f, 1.4f, northZ - 0.8f), Blue, "Quick Revive");
        CreateCube("Z1_WallBuy_Shotgun", environment, new Vector3(3f, 1.4f, northZ), new Vector3(1.3f, 1.4f, 0.18f), new Color(0.85f, 0.55f, 0.15f));
        CreateMarker(logic, "Z1_WallBuy_Shotgun", new Vector3(3f, 1.4f, northZ - 0.6f), Yellow, "Shotgun Wall Buy");

        Transform desks = CreateChild(environment, "BrokenDesks");
        Vector3[] deskPos = { new Vector3(-4.2f, 0.55f, 1.5f), new Vector3(1.2f, 0.5f, 2f), new Vector3(4.5f, 0.55f, 1.1f), new Vector3(-2.6f, 0.5f, -0.9f), new Vector3(2.5f, 0.5f, -1.2f), new Vector3(-0.2f, 0.55f, -2.6f) };
        float[] yaws = { 8f, -8f, 12f, -14f, 6f, -10f };
        for (int i = 0; i < deskPos.Length; i++)
        {
            GameObject desk = CreateCube($"Desk_{i + 1}", desks, deskPos[i], new Vector3(4.6f, 1.05f, 1.2f), new Color(0.38f, 0.28f, 0.18f));
            desk.transform.rotation = Quaternion.Euler(0f, yaws[i], 0f);
        }

        CreateCube("Z1_Chalkboard", environment, new Vector3(-4.4f, 2.2f, -halfD + 0.4f), new Vector3(10f, 2.4f, 0.16f), new Color(0.12f, 0.22f, 0.16f));
        CreateMarker(logic, "PlayerSpawnPoint", new Vector3(0f, 1f, -5.5f), Green, "Player Spawn Point");
    }

    private static void GenerateZone2(Transform floor)
    {
        Transform environment = CreateChild(floor, "Zone2_Environment");
        Transform logic = CreateChild(floor, "Zone2_Logic");
        float wallY = WallHeight * 0.5f;

        CreateCube("Z2_Floor_EastHall", environment, new Vector3(20.4f, -0.5f, 0f), new Vector3(12.4f, 1f, 4.4f), FloorColor);
        CreateCube("Z2_Floor_NorthHall", environment, new Vector3(26.6f, -0.5f, 8f), new Vector3(4.8f, 1f, 12.8f), FloorColor);
        CreateCube("Z2_Floor_Hub", environment, new Vector3(32.8f, -0.5f, 14.2f), new Vector3(9.2f, 1f, 5.2f), FloorColor);
        CreateCube("Z2_Floor_LabA", environment, new Vector3(18.6f, -0.5f, 12.2f), new Vector3(10f, 1f, 10f), FloorColor);
        CreateCube("Z2_Floor_LabB", environment, new Vector3(36.8f, -0.5f, 8.4f), new Vector3(10f, 1f, 10f), FloorColor);

        CreateCube("Z2_Hall_South", environment, new Vector3(20.6f, wallY, -2.1f), new Vector3(12.6f, WallHeight, WallThickness), WallColor);
        CreateCube("Z2_Hall_NorthWest", environment, new Vector3(18.8f, wallY, 2.1f), new Vector3(8.8f, WallHeight, WallThickness), WallColor);
        CreateCube("Z2_Hall_West", environment, new Vector3(24.3f, wallY, 8.2f), new Vector3(WallThickness, WallHeight, 12.2f), WallColor);
        CreateCube("Z2_Hall_East", environment, new Vector3(28.9f, wallY, 7.2f), new Vector3(WallThickness, WallHeight, 10.4f), WallColor);

        GameObject lockers = CreateCube("Z2_FallenLockers", environment, new Vector3(18.8f, 0.55f, 0.35f), new Vector3(2.6f, 1.1f, 0.9f), new Color(0.28f, 0.34f, 0.42f));
        lockers.transform.rotation = Quaternion.Euler(0f, 26f, 74f);
        CreateCube("Z2_Rubble", environment, new Vector3(26.8f, 0.35f, 6.4f), new Vector3(1.7f, 0.7f, 1.6f), new Color(0.33f, 0.31f, 0.29f));

        CreateLab(environment, logic, "LabA", new Vector3(18.6f, 0f, 12.2f), true);
        CreateLab(environment, logic, "LabB", new Vector3(36.8f, 0f, 8.4f), false);
        CreateMarker(logic, "Z2_Spawn_Window7", new Vector3(41.4f, 1f, 8.4f), Red, "Zombie Spawn Window 7");

        CreateCube("Z2_WallBuy_AR", environment, new Vector3(32.4f, 1.45f, 16.2f), new Vector3(1.4f, 1.5f, 0.18f), new Color(0.85f, 0.75f, 0.2f));
        CreateMarker(logic, "Z2_WallBuy_AssaultRifle", new Vector3(32.4f, 1.45f, 15.4f), Yellow, "Assault Rifle Wall Buy");
        CreateCube("Z2_SpeedColaCart", environment, new Vector3(35.6f, 0.7f, 15.6f), new Vector3(2.1f, 1.1f, 1.15f), new Color(0.82f, 0.22f, 0.18f));
        CreateMarker(logic, "Z2_SpeedCola", new Vector3(35.6f, 1.4f, 14.6f), Blue, "Speed Cola Perk");
    }

    private static void CreateLab(Transform environment, Transform logic, string name, Vector3 center, bool labA)
    {
        Transform lab = CreateChild(environment, name);
        float wallY = WallHeight * 0.5f;
        CreateCube($"{name}_West", lab, center + new Vector3(-5f, wallY, 0f), new Vector3(WallThickness, WallHeight, 10f), WallColor);
        CreateCube($"{name}_East", lab, center + new Vector3(5f, wallY, 0f), new Vector3(WallThickness, WallHeight, 10f), WallColor);
        CreateCube($"{name}_North", lab, center + new Vector3(0f, wallY, 5f), new Vector3(10f, WallHeight, WallThickness), WallColor);
        CreateCube($"{name}_SouthL", lab, center + new Vector3(-3.1f, wallY, -5f), new Vector3(3.8f, WallHeight, WallThickness), WallColor);
        CreateCube($"{name}_SouthR", lab, center + new Vector3(3.1f, wallY, -5f), new Vector3(3.8f, WallHeight, WallThickness), WallColor);

        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                CreateCube($"{name}_PC_{row}_{col}", lab, center + new Vector3((col - 1) * 2.3f, 0.5f, (1 - row) * 2.2f), new Vector3(1.7f, 1f, 0.85f), new Color(0.3f, 0.3f, 0.32f));
            }
        }

        if (labA)
        {
            CreateCube("Z2_Window5", lab, center + new Vector3(-2f, 2.1f, 4.65f), new Vector3(2.1f, 1.7f, 0.12f), new Color(0.55f, 0.72f, 0.85f));
            CreateCube("Z2_Window6", lab, center + new Vector3(-4.65f, 2.1f, 1.2f), new Vector3(0.12f, 1.7f, 2.1f), new Color(0.55f, 0.72f, 0.85f));
            CreateMarker(logic, "Z2_Spawn_Window5", center + new Vector3(-2f, 1f, 5.4f), Red, "Zombie Spawn Window 5");
            CreateMarker(logic, "Z2_Spawn_Window6", center + new Vector3(-5.3f, 1f, 1.2f), Red, "Zombie Spawn Window 6");
        }
    }

    private static void GenerateDownStairs(Transform floor2, Transform floor1)
    {
        Transform stairs = CreateChild(floor2, "Stairs_Down_ToFloor1");
        Vector3 start = new Vector3(30.4f, 0f, 14.2f);
        CreateStairRamp(stairs, start, Vector3.forward, GroundY, "Down");
        CreateCube("Gate_Down_1250", stairs, start + new Vector3(0f, 2f, -0.4f), new Vector3(3.4f, 4f, 0.4f), new Color(0.85f, 0.4f, 0.12f));
        CreateMarker(CreateChild(floor2, "Zone2_VerticalLogic"), "StairsDown_1250", start + new Vector3(0f, 1.5f, 0.8f), Orange, "Main Stairs DOWN to Floor 1 (1250 Pts)");
    }

    private static void GenerateUpStairs(Transform floor2, Transform floor3)
    {
        Transform stairs = CreateChild(floor2, "Stairs_Up_ToFloor3");
        Vector3 start = new Vector3(34.8f, 0f, 16.6f);
        CreateStairRamp(stairs, start, Vector3.right, RoofY, "Up");
        CreateCube("Gate_Up_2000", stairs, start + new Vector3(-0.4f, 2f, 0f), new Vector3(0.4f, 4f, 3.4f), new Color(0.85f, 0.4f, 0.12f));
        CreateCube("SpiralCore", stairs, start + new Vector3(1.2f, 4f, 0f), new Vector3(1.1f, 8f, 1.1f), new Color(0.35f, 0.36f, 0.38f));
        CreateMarker(CreateChild(floor2, "Zone2_FireEscapeLogic"), "FireEscapeUp_2000", start + new Vector3(0.8f, 1.5f, 0f), Orange, "Iron Spiral Fire Escape UP to Floor 3 (2000 Pts)");
    }

    private static void CreateStairRamp(Transform parent, Vector3 start, Vector3 direction, float endY, string prefix)
    {
        direction.Normalize();
        int steps = 10;
        float rise = (endY - start.y) / steps;
        for (int i = 0; i <= steps; i++)
        {
            Vector3 pos = start + direction * (1.05f * i) + new Vector3(0f, rise * i + 0.15f, 0f);
            CreateCube($"{prefix}_Step_{i}", parent, pos, new Vector3(3.2f, 0.3f, 1.1f), new Color(0.42f, 0.41f, 0.39f));
        }

        Vector3 rampMid = start + direction * 5.2f + new Vector3(0f, (endY - start.y) * 0.5f, 0f);
        GameObject ramp = CreateCube($"{prefix}_Ramp", parent, rampMid, new Vector3(3f, 0.25f, 11.2f), new Color(0.4f, 0.39f, 0.37f));
        ramp.transform.rotation = Quaternion.LookRotation(new Vector3(direction.x, (endY - start.y) / 10.5f, direction.z));
    }

    private static void GenerateZone3(Transform floor)
    {
        Transform environment = CreateChild(floor, "Zone3_Environment");
        Transform logic = CreateChild(floor, "Zone3_Logic");
        Vector3 center = new Vector3(30.4f, GroundY, 26.5f);
        const float width = 24f;
        const float depth = 18f;
        float halfW = width * 0.5f;
        float halfD = depth * 0.5f;

        CreateCube("Z3_Floor", environment, center + new Vector3(0f, -0.5f, 0f), new Vector3(width, 1f, depth), FloorColor);
        CreateCube("Z3_Wall_West", environment, center + new Vector3(-halfW, 2.5f, 0f), new Vector3(WallThickness, 5f, depth), WallColor);
        CreateCube("Z3_Wall_East", environment, center + new Vector3(halfW, 2.5f, 0f), new Vector3(WallThickness, 5f, depth), WallColor);
        CreateCube("Z3_Wall_North", environment, center + new Vector3(0f, 2.5f, halfD), new Vector3(width, 5f, WallThickness), WallColor);
        CreateCube("Z3_Wall_SouthL", environment, center + new Vector3(-5.4f, 2.5f, -halfD), new Vector3(13.2f, 5f, WallThickness), WallColor);
        CreateCube("Z3_Wall_SouthR", environment, center + new Vector3(7.2f, 2.5f, -halfD), new Vector3(9.4f, 5f, WallThickness), WallColor);

        Color concrete = new Color(0.48f, 0.47f, 0.45f);
        CreateCube("Z3_Pillar_NW", environment, center + new Vector3(-4.2f, 2.4f, 3.4f), new Vector3(1.8f, 4.8f, 1.8f), concrete);
        CreateCube("Z3_Pillar_NE", environment, center + new Vector3(4.2f, 2.4f, 3.4f), new Vector3(1.8f, 4.8f, 1.8f), concrete);
        CreateCube("Z3_Pillar_SW", environment, center + new Vector3(-4.2f, 2.4f, -3.4f), new Vector3(1.8f, 4.8f, 1.8f), concrete);
        CreateCube("Z3_Pillar_SE", environment, center + new Vector3(4.2f, 2.4f, -3.4f), new Vector3(1.8f, 4.8f, 1.8f), concrete);

        GameObject manhole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        manhole.name = "Z3_DrainageManhole";
        manhole.transform.SetParent(environment, false);
        manhole.transform.position = center + new Vector3(0f, 0.04f, 0f);
        manhole.transform.localScale = new Vector3(2.4f, 0.04f, 2.4f);
        MakeStatic(manhole);
        ApplyColor(manhole, new Color(0.18f, 0.18f, 0.19f));
        Undo.RegisterCreatedObjectUndo(manhole, "Generate Full Vertical Map");
        CreateMarker(logic, "Z3_Spawn_Manhole", center + new Vector3(0f, 0.4f, 0f), Red, "Drainage Manhole Spawn");

        CreateCube("Z3_MysteryBox", environment, center + new Vector3(-10.4f, 0.7f, 1.4f), new Vector3(1.8f, 1.2f, 1.1f), new Color(0.12f, 0.45f, 0.18f));
        CreateMarker(logic, "Z3_MysteryBox", center + new Vector3(-9.4f, 1.3f, 1.4f), Green, "Mystery Box");
        CreateCube("Z3_WallBuy_HMG", environment, center + new Vector3(-11.6f, 1.5f, -3.2f), new Vector3(0.2f, 1.6f, 1.6f), new Color(0.75f, 0.55f, 0.15f));
        CreateMarker(logic, "Z3_WallBuy_HMG", center + new Vector3(-10.4f, 1.4f, -3.2f), Yellow, "Heavy Machine Gun Wall Buy");
        CreateMarker(logic, "Z3_EntryFromStairs", center + new Vector3(0f, 1.5f, -halfD + 1.4f), Orange, "Entry UP to Zone 2");
    }

    private static void GenerateZone4(Transform floor)
    {
        Transform environment = CreateChild(floor, "Zone4_Environment");
        Transform logic = CreateChild(floor, "Zone4_Logic");
        Vector3 center = new Vector3(38.5f, RoofY, 16.6f);
        const float width = 22f;
        const float depth = 18f;
        float halfW = width * 0.5f;
        float halfD = depth * 0.5f;
        Color ledge = new Color(0.46f, 0.45f, 0.43f);

        CreateCube("Z4_Floor", environment, center + new Vector3(0f, -0.5f, 0f), new Vector3(width, 1f, depth), new Color(0.38f, 0.38f, 0.37f));
        CreateCube("Z4_Ledge_N", environment, center + new Vector3(0f, 0.28f, halfD), new Vector3(width + 0.5f, 0.55f, 0.5f), ledge);
        CreateCube("Z4_Ledge_S", environment, center + new Vector3(1.8f, 0.28f, -halfD), new Vector3(width - 3.6f, 0.55f, 0.5f), ledge);
        CreateCube("Z4_Ledge_W", environment, center + new Vector3(-halfW, 0.28f, 0f), new Vector3(0.5f, 0.55f, depth + 0.5f), ledge);
        CreateCube("Z4_Ledge_E", environment, center + new Vector3(halfW, 0.28f, 0f), new Vector3(0.5f, 0.55f, depth + 0.5f), ledge);

        CreateCube("Z4_EntryGate", environment, center + new Vector3(-halfW + 3f, 1.2f, -halfD), new Vector3(3.6f, 2.4f, 0.4f), new Color(0.85f, 0.4f, 0.12f));
        CreateMarker(logic, "Z4_EntryFromFireEscape", center + new Vector3(-halfW + 3f, 1.4f, -halfD + 1.1f), Orange, "Entry DOWN to Zone 2");

        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "Z4_Helipad";
        pad.transform.SetParent(environment, false);
        pad.transform.position = center + new Vector3(0f, 0.06f, 0f);
        pad.transform.localScale = new Vector3(8.8f, 0.06f, 8.8f);
        MakeStatic(pad);
        ApplyColor(pad, new Color(0.22f, 0.22f, 0.23f));
        Undo.RegisterCreatedObjectUndo(pad, "Generate Full Vertical Map");
        CreateCube("Z4_Helipad_H", environment, center + new Vector3(0f, 0.13f, 0f), new Vector3(3.2f, 0.05f, 0.4f), Color.white);

        CreateCube("Z4_PowerSwitch", environment, center + new Vector3(-8f, 1.1f, 6.4f), new Vector3(1.1f, 1.6f, 0.7f), new Color(0.18f, 0.18f, 0.2f));
        CreateMarker(logic, "Z4_MainPowerSwitch", center + new Vector3(-8f, 1.3f, 5.5f), Green, "Main Power Switch");
        CreateCube("Z4_PackAPunch", environment, center + new Vector3(7.6f, 0.7f, 6.2f), new Vector3(1.8f, 1.4f, 1.3f), new Color(0.55f, 0.18f, 0.2f));
        CreateMarker(logic, "Z4_PackAPunch_5000", center + new Vector3(7.6f, 1.5f, 5.1f), Purple, "Pack-a-Punch Spot");
        CreateCube("Z4_OrangeCooler", environment, center + new Vector3(7.2f, 0.7f, -6.2f), new Vector3(1.5f, 1.2f, 1.1f), new Color(0.95f, 0.45f, 0.08f));
        CreateMarker(logic, "Z4_HealthBoost_2500", center + new Vector3(7.2f, 1.4f, -7.2f), Blue, "Health Boost Perk Spot");

        CreateMarker(logic, "Z4_Spawn_RoofDoor", center + new Vector3(-2.2f, 1f, halfD - 1f), Red, "Rooftop Door Spawn");
        CreateMarker(logic, "Z4_Spawn_LedgeClimbers", center + new Vector3(3.4f, 1f, -halfD + 1f), Red, "Ledge Climbers Spawn");
        CreateMarker(logic, "Z4_Spawn_FenceBreakers", center + new Vector3(halfW - 1f, 1f, 0.4f), Red, "Fence Breakers Spawn");
    }

    private static GameObject CreateCube(string objectName, Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(parent, false);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        MakeStatic(cube);
        ApplyColor(cube, color);
        Undo.RegisterCreatedObjectUndo(cube, "Generate Full Vertical Map");
        return cube;
    }

    private static void MakeStatic(GameObject go)
    {
        go.isStatic = true;
        GameObjectUtility.SetStaticEditorFlags(go, StaticEditorFlags.NavigationStatic | StaticEditorFlags.BatchingStatic);
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
        Undo.RegisterCreatedObjectUndo(child, "Generate Full Vertical Map");
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
        Undo.RegisterCreatedObjectUndo(marker, "Generate Full Vertical Map");
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
