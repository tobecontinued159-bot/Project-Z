#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class FullLevelGenerator
{
    private const string RootName = "ProjectZ_FullLevel";
    private const float WallHeight = 4f;
    private const float WallThickness = 0.6f;

    private const float Z1Width = 28f;
    private const float Z1Depth = 18f;
    private const float Z1EastX = 14f;
    private const float Z2EntryX = 14.4f;
    private const float Z3CenterX = 52f;
    private const float Z3CenterZ = 28f;
    private const float Z3Width = 24f;
    private const float Z3Depth = 20f;
    private const float RoofY = 8f;

    [MenuItem("Project Z/Generate Full Level (Zones 1-4)")]
    public static void GenerateFullLevel()
    {
        DestroyIfExists(RootName);

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Generate Full Level");

        GenerateZone1(root.transform);
        GenerateZone2(root.transform);
        GenerateZone3(root.transform);
        GenerateZone4(root.transform);

        Selection.activeGameObject = root;
        Debug.Log("Full level generated (Zones 1-4). Bake NavMesh, then wire doors, wall buys, and WaveManager spawns.");
    }

    private static void GenerateZone1(Transform root)
    {
        Transform environment = CreateChild(root, "Zone1_Environment");
        Transform logic = CreateChild(root, "Zone1_Logic");
        Color wall = new Color(0.58f, 0.56f, 0.52f);
        float wallY = WallHeight * 0.5f;
        float halfW = Z1Width * 0.5f;
        float halfD = Z1Depth * 0.5f;

        CreateCube("Z1_Floor", environment, new Vector3(0f, -0.5f, 0f), new Vector3(Z1Width, 1f, Z1Depth), new Color(0.36f, 0.35f, 0.33f));
        CreateCube("Z1_Wall_North", environment, new Vector3(0f, wallY, halfD), new Vector3(Z1Width + WallThickness, WallHeight, WallThickness), wall);
        CreateCube("Z1_Wall_South", environment, new Vector3(0f, wallY, -halfD), new Vector3(Z1Width + WallThickness, WallHeight, WallThickness), wall);
        CreateCube("Z1_Wall_West", environment, new Vector3(-halfW, wallY, 0f), new Vector3(WallThickness, WallHeight, Z1Depth + WallThickness), wall);

        const float eastGap = 3.2f;
        float eastWing = (Z1Depth - eastGap) * 0.5f;
        CreateCube("Z1_Wall_East_North", environment, new Vector3(halfW, wallY, (eastGap * 0.5f) + (eastWing * 0.5f)), new Vector3(WallThickness, WallHeight, eastWing), wall);
        CreateCube("Z1_Wall_East_South", environment, new Vector3(halfW, wallY, -((eastGap * 0.5f) + (eastWing * 0.5f))), new Vector3(WallThickness, WallHeight, eastWing), wall);
        CreateCube("Z1_Door_ToZone2", environment, new Vector3(halfW, wallY, 0f), new Vector3(0.5f, WallHeight, eastGap), new Color(0.72f, 0.18f, 0.16f));
        CreateMarker(logic, "Z1_ExitDoor_750", new Vector3(halfW - 0.8f, 1.5f, 0f), new Color(1f, 0.5f, 0.1f), "Exit Door to Zone 2 (750 Pts)");

        float northZ = halfD - 0.45f;
        Color glass = new Color(0.45f, 0.62f, 0.78f);
        CreateCube("Z1_Window_1", environment, new Vector3(-10.5f, 2.1f, northZ), new Vector3(2.2f, 1.8f, 0.12f), glass);
        CreateCube("Z1_Window_2", environment, new Vector3(-7.4f, 2.1f, northZ), new Vector3(2.2f, 1.8f, 0.12f), glass);
        CreateMarker(logic, "Z1_ZombieSpawn_Window1", new Vector3(-10.5f, 1f, northZ + 0.7f), Red, "Zombie Spawn Window 1");
        CreateMarker(logic, "Z1_ZombieSpawn_Window2", new Vector3(-7.4f, 1f, northZ + 0.7f), Red, "Zombie Spawn Window 2");

        CreateCube("Z1_WallBuy_Pistol", environment, new Vector3(-3.6f, 1.4f, northZ), new Vector3(1.1f, 1.4f, 0.18f), new Color(0.85f, 0.75f, 0.2f));
        CreateMarker(logic, "Z1_WallBuy_Pistol", new Vector3(-3.6f, 1.4f, northZ - 0.6f), Yellow, "Pistol Wall Buy");
        CreateVending(environment, new Vector3(-0.4f, 1.2f, northZ));
        CreateMarker(logic, "Z1_QuickRevive", new Vector3(-0.4f, 1.4f, northZ - 0.8f), Blue, "Quick Revive");
        CreateCube("Z1_WallBuy_Shotgun", environment, new Vector3(3.1f, 1.4f, northZ), new Vector3(1.3f, 1.4f, 0.18f), new Color(0.85f, 0.55f, 0.15f));
        CreateMarker(logic, "Z1_WallBuy_Shotgun", new Vector3(3.1f, 1.4f, northZ - 0.6f), Yellow, "Shotgun Wall Buy");

        CreateLectureDesks(environment);
        CreateCube("Z1_Chalkboard", environment, new Vector3(-4.5f, 2.2f, -halfD + 0.4f), new Vector3(10f, 2.4f, 0.16f), new Color(0.12f, 0.22f, 0.16f));
        CreateCube("Z1_TeachersDesk", environment, new Vector3(-4.2f, 0.55f, -halfD + 1.7f), new Vector3(3.6f, 1.1f, 1.4f), new Color(0.42f, 0.28f, 0.16f));
        CreateMarker(logic, "PlayerSpawnPoint", new Vector3(0f, 1f, -5.5f), new Color(0.2f, 0.9f, 0.35f), "Player Spawn Point");
    }

    private static void CreateLectureDesks(Transform parent)
    {
        Transform desks = CreateChild(parent, "LectureDesks");
        Color desk = new Color(0.38f, 0.28f, 0.18f);
        Vector3[] centers = { new Vector3(-4.2f, 0.55f, 1.6f), new Vector3(1.1f, 0.5f, 2f), new Vector3(4.6f, 0.55f, 1.2f), new Vector3(-2.8f, 0.5f, -0.8f), new Vector3(2.4f, 0.5f, -1.1f), new Vector3(-0.4f, 0.55f, -2.6f) };
        Vector3[] scales = { new Vector3(5f, 1.1f, 1.25f), new Vector3(4.5f, 1f, 1.2f), new Vector3(4.2f, 1.1f, 1.15f), new Vector3(4.6f, 1f, 1.2f), new Vector3(4.3f, 1f, 1.2f), new Vector3(4.8f, 1.1f, 1.15f) };
        float[] yaws = { 8f, -7f, 12f, -14f, 6f, -10f };
        for (int i = 0; i < centers.Length; i++)
        {
            GameObject cube = CreateCube($"LectureDesk_{i + 1}", desks, centers[i], scales[i], desk);
            cube.transform.rotation = Quaternion.Euler(0f, yaws[i], 0f);
        }
    }

    private static void GenerateZone2(Transform root)
    {
        Transform environment = CreateChild(root, "Zone2_Environment");
        Transform logic = CreateChild(root, "Zone2_Logic");
        Color wall = new Color(0.54f, 0.52f, 0.48f);
        float wallY = WallHeight * 0.5f;

        CreateCube("Z2_Floor_WestHall", environment, new Vector3(20.2f, -0.5f, 0f), new Vector3(12f, 1f, 4.4f), Floor);
        CreateCube("Z2_Floor_NorthHall", environment, new Vector3(26.2f, -0.5f, 8.2f), new Vector3(4.6f, 1f, 13.2f), Floor);
        CreateCube("Z2_Floor_EastHall", environment, new Vector3(35.4f, -0.5f, 14.6f), new Vector3(15.4f, 1f, 5f), Floor);
        CreateCube("Z2_Floor_LabA", environment, new Vector3(18.4f, -0.5f, 12.4f), new Vector3(10.2f, 1f, 10.2f), Floor);
        CreateCube("Z2_Floor_LabB", environment, new Vector3(41.2f, -0.5f, 9.4f), new Vector3(10.2f, 1f, 10.2f), Floor);

        CreateCube("Z2_Hall_SouthWall", environment, new Vector3(20.4f, wallY, -2.1f), new Vector3(12.2f, WallHeight, WallThickness), wall);
        CreateCube("Z2_Hall_NorthWall_West", environment, new Vector3(18.6f, wallY, 2.1f), new Vector3(8.6f, WallHeight, WallThickness), wall);
        CreateCube("Z2_Hall_WestWall_North", environment, new Vector3(24f, wallY, 8.4f), new Vector3(WallThickness, WallHeight, 12.6f), wall);
        CreateCube("Z2_Hall_EastWall_North", environment, new Vector3(28.4f, wallY, 6.6f), new Vector3(WallThickness, WallHeight, 9.4f), wall);
        CreateCube("Z2_Hall_East_SouthWall", environment, new Vector3(35.2f, wallY, 12.2f), new Vector3(14.2f, WallHeight, WallThickness), wall);
        CreateCube("Z2_Hall_East_NorthWall", environment, new Vector3(33.8f, wallY, 17f), new Vector3(16.6f, WallHeight, WallThickness), wall);

        GameObject lockers = CreateCube("Z2_FallenLockers", environment, new Vector3(18.6f, 0.55f, 0.4f), new Vector3(2.6f, 1.1f, 0.9f), new Color(0.28f, 0.34f, 0.42f));
        lockers.transform.rotation = Quaternion.Euler(0f, 24f, 76f);
        GameObject rubble = CreateCube("Z2_Rubble", environment, new Vector3(26.4f, 0.35f, 6.2f), new Vector3(1.7f, 0.7f, 1.6f), new Color(0.33f, 0.31f, 0.29f));
        rubble.transform.rotation = Quaternion.Euler(0f, 18f, 0f);
        GameObject desk = CreateCube("Z2_BrokenDesk", environment, new Vector3(31.4f, 0.5f, 14.2f), new Vector3(2.3f, 1f, 1.1f), new Color(0.36f, 0.26f, 0.16f));
        desk.transform.rotation = Quaternion.Euler(0f, -28f, 0f);

        CreateLabRoom(environment, logic, "LabA", new Vector3(18.4f, 0f, 12.4f), true);
        CreateLabRoom(environment, logic, "LabB", new Vector3(41.2f, 0f, 9.4f), false);

        CreateCube("Z2_WallBuy_AR", environment, new Vector3(42.8f, 1.45f, 16.4f), new Vector3(1.4f, 1.5f, 0.18f), new Color(0.85f, 0.75f, 0.2f));
        CreateMarker(logic, "Z2_WallBuy_AssaultRifle", new Vector3(42f, 1.45f, 16.4f), Yellow, "Assault Rifle Wall Buy");
        CreateStreetCart(environment, new Vector3(39.6f, 0.7f, 15.8f));
        CreateMarker(logic, "Z2_SpeedCola", new Vector3(39.6f, 1.4f, 14.8f), Blue, "Speed Cola Perk");
        CreateMarker(logic, "Z2_ZombieSpawn_Window7", new Vector3(45.4f, 1f, 9.4f), Red, "Zombie Spawn Window 7");

        CreateCube("Z2_Barricade_ToZone3", environment, new Vector3(41.8f, 2f, 17f), new Vector3(3.6f, 4f, 0.55f), new Color(0.85f, 0.4f, 0.12f));
        CreateMarker(logic, "Z2_ExitBarricade_1250", new Vector3(41.8f, 1.5f, 16.1f), Orange, "Barricade Exit to Zone 3 (1250 Pts)");
        CreateMarker(logic, "Z2_EntryFromZone1", new Vector3(Z2EntryX, 1f, 0f), Orange, "Entry from Zone 1");
    }

    private static void CreateLabRoom(Transform environment, Transform logic, string labName, Vector3 center, bool isLabA)
    {
        Color wall = new Color(0.5f, 0.49f, 0.46f);
        float wallY = WallHeight * 0.5f;
        Transform lab = CreateChild(environment, labName);
        CreateCube($"{labName}_West", lab, center + new Vector3(-5f, wallY, 0f), new Vector3(WallThickness, WallHeight, 10.2f), wall);
        CreateCube($"{labName}_East", lab, center + new Vector3(5f, wallY, 0f), new Vector3(WallThickness, WallHeight, 10.2f), wall);
        CreateCube($"{labName}_North", lab, center + new Vector3(0f, wallY, 5f), new Vector3(10.2f, WallHeight, WallThickness), wall);
        CreateCube($"{labName}_South_Left", lab, center + new Vector3(-3.2f, wallY, -5f), new Vector3(3.8f, WallHeight, WallThickness), wall);
        CreateCube($"{labName}_South_Right", lab, center + new Vector3(3.2f, wallY, -5f), new Vector3(3.8f, WallHeight, WallThickness), wall);

        Color desk = new Color(0.3f, 0.3f, 0.32f);
        for (int row = 0; row < 3; row++)
        {
            for (int col = 0; col < 3; col++)
            {
                Vector3 pos = center + new Vector3((col - 1) * 2.3f, 0.5f, (1 - row) * 2.2f);
                CreateCube($"{labName}_Station_{row}_{col}", lab, pos, new Vector3(1.7f, 1f, 0.85f), desk);
            }
        }

        if (isLabA)
        {
            CreateCube("Z2_Window5", lab, center + new Vector3(-2f, 2.1f, 4.65f), new Vector3(2.1f, 1.7f, 0.12f), new Color(0.55f, 0.72f, 0.85f));
            CreateCube("Z2_Window6", lab, center + new Vector3(-4.65f, 2.1f, 1.4f), new Vector3(0.12f, 1.7f, 2.1f), new Color(0.55f, 0.72f, 0.85f));
            CreateMarker(logic, "Z2_ZombieSpawn_Window5", center + new Vector3(-2f, 1f, 5.4f), Red, "Zombie Spawn Window 5");
            CreateMarker(logic, "Z2_ZombieSpawn_Window6", center + new Vector3(-5.4f, 1f, 1.4f), Red, "Zombie Spawn Window 6");
        }
    }

    private static void GenerateZone3(Transform root)
    {
        Transform environment = CreateChild(root, "Zone3_Environment");
        Transform logic = CreateChild(root, "Zone3_Logic");
        Color wall = new Color(0.56f, 0.55f, 0.52f);
        float wallY = 2.5f;
        float halfW = Z3Width * 0.5f;
        float halfD = Z3Depth * 0.5f;

        CreateCube("Z3_Floor", environment, new Vector3(Z3CenterX, -0.5f, Z3CenterZ), new Vector3(Z3Width, 1f, Z3Depth), Floor);
        CreateCube("Z3_Wall_West", environment, new Vector3(Z3CenterX - halfW, wallY, Z3CenterZ), new Vector3(WallThickness, 5f, Z3Depth + WallThickness), wall);
        CreateCube("Z3_Wall_East", environment, new Vector3(Z3CenterX + halfW, wallY, Z3CenterZ - 1.1f), new Vector3(WallThickness, 5f, Z3Depth - 2.2f), wall);
        CreateCube("Z3_Wall_South_West", environment, new Vector3(Z3CenterX - 5.2f, wallY, Z3CenterZ - halfD), new Vector3(13.6f, 5f, WallThickness), wall);
        CreateCube("Z3_Wall_South_East", environment, new Vector3(Z3CenterX + 7.4f, wallY, Z3CenterZ - halfD), new Vector3(9.2f, 5f, WallThickness), wall);
        CreateCube("Z3_Wall_North_West", environment, new Vector3(Z3CenterX - 3.4f, wallY, Z3CenterZ + halfD), new Vector3(17.2f, 5f, WallThickness), wall);

        CreateCube("Z3_EntryGate_FromZone2", environment, new Vector3(41.8f, 2f, Z3CenterZ - halfD), new Vector3(3.6f, 4f, 0.55f), new Color(0.85f, 0.4f, 0.12f));
        CreateMarker(logic, "Z3_Entry_1250", new Vector3(41.8f, 1.5f, Z3CenterZ - halfD + 0.9f), Orange, "Entry from Zone 2 (1250 Pts)");

        Color concrete = new Color(0.48f, 0.47f, 0.45f);
        CreateCube("Z3_Pillar_NW", environment, new Vector3(Z3CenterX - 4.4f, 2.4f, Z3CenterZ + 3.6f), new Vector3(1.8f, 4.8f, 1.8f), concrete);
        CreateCube("Z3_Pillar_NE", environment, new Vector3(Z3CenterX + 4.4f, 2.4f, Z3CenterZ + 3.6f), new Vector3(1.8f, 4.8f, 1.8f), concrete);
        CreateCube("Z3_Pillar_SW", environment, new Vector3(Z3CenterX - 4.4f, 2.4f, Z3CenterZ - 3.6f), new Vector3(1.8f, 4.8f, 1.8f), concrete);
        CreateCube("Z3_Pillar_SE", environment, new Vector3(Z3CenterX + 4.4f, 2.4f, Z3CenterZ - 3.6f), new Vector3(1.8f, 4.8f, 1.8f), concrete);

        GameObject manhole = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        manhole.name = "Z3_DrainageManhole";
        manhole.transform.SetParent(environment, false);
        manhole.transform.position = new Vector3(Z3CenterX, 0.04f, Z3CenterZ);
        manhole.transform.localScale = new Vector3(2.4f, 0.04f, 2.4f);
        MakeStatic(manhole);
        ApplyColor(manhole, new Color(0.18f, 0.18f, 0.19f));
        Undo.RegisterCreatedObjectUndo(manhole, "Generate Full Level");
        CreateMarker(logic, "Z3_ZombieSpawn_Manhole", new Vector3(Z3CenterX, 0.4f, Z3CenterZ), Red, "Drainage Manhole Spawn");

        float westX = Z3CenterX - halfW + 0.55f;
        CreateCube("Z3_MysteryBox", environment, new Vector3(westX + 0.7f, 0.7f, Z3CenterZ + 1.6f), new Vector3(1.8f, 1.2f, 1.1f), new Color(0.12f, 0.45f, 0.18f));
        CreateMarker(logic, "Z3_MysteryBox", new Vector3(westX + 1.5f, 1.3f, Z3CenterZ + 1.6f), new Color(0.25f, 0.9f, 0.3f), "Mystery Weapon Box");
        CreateCube("Z3_WallBuy_HMG", environment, new Vector3(westX, 1.5f, Z3CenterZ - 3.4f), new Vector3(0.2f, 1.6f, 1.6f), new Color(0.75f, 0.55f, 0.15f));
        CreateMarker(logic, "Z3_WallBuy_HMG", new Vector3(westX + 1.2f, 1.4f, Z3CenterZ - 3.4f), Yellow, "Heavy Machine Gun Wall Buy");

        CreateCube("Z3_Window9", environment, new Vector3(Z3CenterX - 2.4f, 2.3f, Z3CenterZ + halfD - 0.45f), new Vector3(2.4f, 1.8f, 0.14f), new Color(0.5f, 0.7f, 0.85f));
        CreateMarker(logic, "Z3_ZombieSpawn_Window9", new Vector3(Z3CenterX - 2.4f, 1f, Z3CenterZ + halfD + 0.4f), Red, "Zombie Spawn Window 9");
        CreateCube("Z3_Window10", environment, new Vector3(Z3CenterX + halfW - 0.45f, 2.3f, Z3CenterZ + 1.2f), new Vector3(0.14f, 1.8f, 2.4f), new Color(0.5f, 0.7f, 0.85f));
        CreateMarker(logic, "Z3_ZombieSpawn_Window10", new Vector3(Z3CenterX + halfW + 0.4f, 1f, Z3CenterZ + 1.2f), Red, "Zombie Spawn Window 10");

        float stairX = Z3CenterX + 8.4f;
        float stairZ = Z3CenterZ + 8.2f;
        CreateCube("Z3_StairCore", environment, new Vector3(stairX, 1.6f, stairZ), new Vector3(1.4f, 3.2f, 1.4f), concrete);
        CreateCube("Z3_StairGate_Zone4", environment, new Vector3(stairX + 0.2f, 2f, stairZ + 2.1f), new Vector3(3.8f, 4f, 0.45f), new Color(0.85f, 0.4f, 0.12f));
        CreateMarker(logic, "Z3_StairGate_2000", new Vector3(stairX, 1.6f, stairZ + 1.1f), Orange, "Gated Spiral Staircase -> Zone 4 (2000 Pts)");
    }

    private static void GenerateZone4(Transform root)
    {
        Transform environment = CreateChild(root, "Zone4_Environment");
        Transform logic = CreateChild(root, "Zone4_Logic");
        float y = RoofY;
        float halfW = Z3Width * 0.5f;
        float halfD = Z3Depth * 0.5f;
        Color ledge = new Color(0.46f, 0.45f, 0.43f);

        CreateCube("Z4_RooftopFloor", environment, new Vector3(Z3CenterX, y - 0.5f, Z3CenterZ), new Vector3(Z3Width, 1f, Z3Depth), new Color(0.38f, 0.38f, 0.37f));
        CreateCube("Z4_Ledge_North", environment, new Vector3(Z3CenterX, y + 0.28f, Z3CenterZ + halfD), new Vector3(Z3Width + 0.5f, 0.55f, 0.5f), ledge);
        CreateCube("Z4_Ledge_South", environment, new Vector3(Z3CenterX + 2.1f, y + 0.28f, Z3CenterZ - halfD), new Vector3(Z3Width - 3.8f, 0.55f, 0.5f), ledge);
        CreateCube("Z4_Ledge_West", environment, new Vector3(Z3CenterX - halfW, y + 0.28f, Z3CenterZ), new Vector3(0.5f, 0.55f, Z3Depth + 0.5f), ledge);
        CreateCube("Z4_Ledge_East", environment, new Vector3(Z3CenterX + halfW, y + 0.28f, Z3CenterZ), new Vector3(0.5f, 0.55f, Z3Depth + 0.5f), ledge);

        float entryX = Z3CenterX + 8.4f;
        float entryZ = Z3CenterZ + 8.2f;
        CreateCube("Z4_EntryGate_FromZone3", environment, new Vector3(entryX, y + 1.2f, entryZ + 2.1f), new Vector3(3.8f, 2.4f, 0.4f), new Color(0.85f, 0.4f, 0.12f));
        CreateMarker(logic, "Z4_Entrance_2000", new Vector3(entryX, y + 1.4f, entryZ + 1.1f), Orange, "Entrance from Zone 3 (2000 Pts)");

        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        pad.name = "Z4_Helipad";
        pad.transform.SetParent(environment, false);
        pad.transform.position = new Vector3(Z3CenterX, y + 0.06f, Z3CenterZ);
        pad.transform.localScale = new Vector3(9.5f, 0.06f, 9.5f);
        MakeStatic(pad);
        ApplyColor(pad, new Color(0.22f, 0.22f, 0.23f));
        Undo.RegisterCreatedObjectUndo(pad, "Generate Full Level");
        CreateCube("Z4_Helipad_H", environment, new Vector3(Z3CenterX, y + 0.13f, Z3CenterZ), new Vector3(3.4f, 0.05f, 0.45f), Color.white);

        CreateCube("Z4_PowerBox", environment, new Vector3(Z3CenterX - 8.6f, y + 1.1f, Z3CenterZ + 7.2f), new Vector3(1.1f, 1.6f, 0.7f), new Color(0.18f, 0.18f, 0.2f));
        CreateMarker(logic, "Z4_MainPowerSwitch", new Vector3(Z3CenterX - 8.6f, y + 1.3f, Z3CenterZ + 6.3f), new Color(0.25f, 0.9f, 0.3f), "Main Power Switch");

        CreateCube("Z4_PackAPunch_Copier", environment, new Vector3(Z3CenterX + 8.2f, y + 0.7f, Z3CenterZ + 6.6f), new Vector3(1.8f, 1.4f, 1.3f), new Color(0.55f, 0.18f, 0.2f));
        CreateMarker(logic, "Z4_PackAPunch_5000", new Vector3(Z3CenterX + 8.2f, y + 1.5f, Z3CenterZ + 5.4f), new Color(0.65f, 0.2f, 0.85f), "Pack-a-Punch Spot: 5000 pts");

        CreateCube("Z4_OrangeCooler", environment, new Vector3(Z3CenterX + 7.6f, y + 0.7f, Z3CenterZ - 6.6f), new Vector3(1.5f, 1.2f, 1.1f), new Color(0.95f, 0.45f, 0.08f));
        CreateMarker(logic, "Z4_HealthBoost_2500", new Vector3(Z3CenterX + 7.6f, y + 1.4f, Z3CenterZ - 7.6f), Blue, "Health Boost Perk Spot: 2500 pts");

        CreateMarker(logic, "Z4_ZombieSpawn_AtticDoor", new Vector3(Z3CenterX - 2.2f, y + 1f, Z3CenterZ + halfD - 1.1f), Red, "Roof Door / Attic Spawn");
        CreateMarker(logic, "Z4_ZombieSpawn_LedgeClimbers", new Vector3(Z3CenterX + 3.8f, y + 1f, Z3CenterZ - halfD + 1.1f), Red, "Ledge Climbers Spawn");
        CreateMarker(logic, "Z4_ZombieSpawn_FenceBreakers", new Vector3(Z3CenterX + halfW - 1.1f, y + 1f, Z3CenterZ), Red, "Fence Breakers Spawn");
    }

    private static void CreateVending(Transform parent, Vector3 position)
    {
        CreateCube("Z1_QuickRevive_Body", parent, position, new Vector3(1.1f, 2.4f, 0.9f), new Color(0.18f, 0.35f, 0.85f));
        CreateCube("Z1_QuickRevive_Screen", parent, position + new Vector3(0f, 0.45f, -0.48f), new Vector3(0.7f, 0.55f, 0.05f), new Color(0.4f, 0.9f, 1f));
    }

    private static void CreateStreetCart(Transform parent, Vector3 position)
    {
        CreateCube("Z2_Cart_Body", parent, position, new Vector3(2.2f, 1.1f, 1.2f), new Color(0.82f, 0.22f, 0.18f));
        CreateCube("Z2_Cart_Canopy", parent, position + new Vector3(0f, 1.15f, 0f), new Vector3(2.4f, 0.12f, 1.4f), new Color(0.95f, 0.85f, 0.2f));
    }

    private static readonly Color Floor = new Color(0.32f, 0.32f, 0.31f);
    private static readonly Color Red = new Color(0.95f, 0.15f, 0.12f);
    private static readonly Color Yellow = new Color(1f, 0.85f, 0.1f);
    private static readonly Color Blue = new Color(0.2f, 0.45f, 1f);
    private static readonly Color Orange = new Color(1f, 0.5f, 0.1f);

    private static GameObject CreateCube(string objectName, Transform parent, Vector3 position, Vector3 scale, Color color)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(parent, false);
        cube.transform.position = position;
        cube.transform.localScale = scale;
        MakeStatic(cube);
        ApplyColor(cube, color);
        Undo.RegisterCreatedObjectUndo(cube, "Generate Full Level");
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
        Undo.RegisterCreatedObjectUndo(child, "Generate Full Level");
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
        Undo.RegisterCreatedObjectUndo(marker, "Generate Full Level");
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
