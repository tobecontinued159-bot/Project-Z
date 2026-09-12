#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class IndoorLevelGenerator
{
    private const string RootName = "IndoorLevel";
    private const float WallHeight = 4f;
    private const float WallThickness = 1f;

    [MenuItem("Tools/Generate Indoor Level")]
    public static void GenerateIndoorLevel()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Generate Indoor Level");

        CreateFloor(root.transform);
        CreateStartRoom(root.transform);
        CreateHallway(root.transform);
        CreateSideRooms(root.transform);
        CreateWeaponWallBuy(root.transform);

        Selection.activeGameObject = root;
        Debug.Log("Indoor level generated. Place BuyableDoor on Door_Blocker, then bake NavMesh.");
    }

    private static void CreateFloor(Transform root)
    {
        CreateSolid(
            "Floor",
            root,
            new Vector3(0f, -0.5f, 0f),
            new Vector3(40f, 1f, 40f),
            new Color(0.38f, 0.38f, 0.37f),
            markNavigationStatic: true);
    }

    private static void CreateStartRoom(Transform root)
    {
        Transform room = CreateChild(root, "StartRoom");
        Color wallColor = new Color(0.52f, 0.51f, 0.48f);

        const float roomSize = 10f;
        const float doorGap = 3f;
        float wallY = WallHeight * 0.5f;
        float roomCenterZ = -12f;
        float half = roomSize * 0.5f;
        float backZ = roomCenterZ - half;
        float frontZ = roomCenterZ + half;
        float leftX = -half;
        float rightX = half;
        float wingWidth = (roomSize - doorGap) * 0.5f;

        CreateSolid("StartRoom_BackWall", room, new Vector3(0f, wallY, backZ), new Vector3(roomSize + WallThickness, WallHeight, WallThickness), wallColor, true);
        CreateSolid("StartRoom_LeftWall", room, new Vector3(leftX, wallY, roomCenterZ), new Vector3(WallThickness, WallHeight, roomSize + WallThickness), wallColor, true);
        CreateSolid("StartRoom_RightWall", room, new Vector3(rightX, wallY, roomCenterZ), new Vector3(WallThickness, WallHeight, roomSize + WallThickness), wallColor, true);

        float leftWingX = -((doorGap * 0.5f) + (wingWidth * 0.5f));
        float rightWingX = (doorGap * 0.5f) + (wingWidth * 0.5f);
        CreateSolid("StartRoom_FrontWall_Left", room, new Vector3(leftWingX, wallY, frontZ), new Vector3(wingWidth, WallHeight, WallThickness), wallColor, true);
        CreateSolid("StartRoom_FrontWall_Right", room, new Vector3(rightWingX, wallY, frontZ), new Vector3(wingWidth, WallHeight, WallThickness), wallColor, true);

        CreateSolid(
            "Door_Blocker",
            room,
            new Vector3(0f, wallY, frontZ),
            new Vector3(doorGap, WallHeight, WallThickness * 0.9f),
            new Color(0.85f, 0.18f, 0.16f),
            markNavigationStatic: false);
    }

    private static void CreateHallway(Transform root)
    {
        Transform hallway = CreateChild(root, "Hallway");
        Color wallColor = new Color(0.48f, 0.47f, 0.45f);

        float wallY = WallHeight * 0.5f;
        const float hallStartZ = -6.5f;
        const float hallEndZ = 12f;
        float hallLength = hallEndZ - hallStartZ;
        float hallCenterZ = (hallStartZ + hallEndZ) * 0.5f;

        CreateSolid("Hallway_LeftWall", hallway, new Vector3(-3.5f, wallY, hallCenterZ), new Vector3(WallThickness, WallHeight, hallLength), wallColor, true);
        CreateSolid("Hallway_RightWall", hallway, new Vector3(3.5f, wallY, hallCenterZ), new Vector3(WallThickness, WallHeight, hallLength), wallColor, true);
        CreateSolid("Hallway_EndWall_Left", hallway, new Vector3(-2.25f, wallY, hallEndZ), new Vector3(3.5f, WallHeight, WallThickness), wallColor, true);
        CreateSolid("Hallway_EndWall_Right", hallway, new Vector3(2.25f, wallY, hallEndZ), new Vector3(3.5f, WallHeight, WallThickness), wallColor, true);
    }

    private static void CreateSideRooms(Transform root)
    {
        Transform rooms = CreateChild(root, "SideRooms");
        Color wallColor = new Color(0.5f, 0.49f, 0.46f);
        float wallY = WallHeight * 0.5f;

        CreateWestClassroom(rooms, wallColor, wallY);
        CreateEastBathroom(rooms, wallColor, wallY);
        CreateNorthLab(rooms, wallColor, wallY);
    }

    private static void CreateWestClassroom(Transform parent, Color wallColor, float wallY)
    {
        Transform room = CreateChild(parent, "Classroom_West");
        const float doorGap = 2.5f;
        float roomCenterX = -9f;
        float roomCenterZ = -1f;
        const float roomWidth = 10f;
        const float roomDepth = 8f;
        float halfW = roomWidth * 0.5f;
        float halfD = roomDepth * 0.5f;

        CreateSolid("Classroom_West_BackWall", room, new Vector3(roomCenterX - halfW, wallY, roomCenterZ), new Vector3(WallThickness, WallHeight, roomDepth + WallThickness), wallColor, true);
        CreateSolid("Classroom_West_NorthWall", room, new Vector3(roomCenterX, wallY, roomCenterZ + halfD), new Vector3(roomWidth + WallThickness, WallHeight, WallThickness), wallColor, true);
        CreateSolid("Classroom_West_SouthWall", room, new Vector3(roomCenterX, wallY, roomCenterZ - halfD), new Vector3(roomWidth + WallThickness, WallHeight, WallThickness), wallColor, true);

        float wingDepth = (roomDepth - doorGap) * 0.5f;
        CreateSolid("Classroom_West_HallWall_North", room, new Vector3(roomCenterX + halfW, wallY, roomCenterZ + ((doorGap * 0.5f) + (wingDepth * 0.5f))), new Vector3(WallThickness, WallHeight, wingDepth), wallColor, true);
        CreateSolid("Classroom_West_HallWall_South", room, new Vector3(roomCenterX + halfW, wallY, roomCenterZ - ((doorGap * 0.5f) + (wingDepth * 0.5f))), new Vector3(WallThickness, WallHeight, wingDepth), wallColor, true);

        CreateEmptyMarker(room, "ZombieSpawn_Classroom", new Vector3(roomCenterX, 1f, roomCenterZ));
    }

    private static void CreateEastBathroom(Transform parent, Color wallColor, float wallY)
    {
        Transform room = CreateChild(parent, "Bathroom_East");
        const float doorGap = 2.5f;
        float roomCenterX = 8.5f;
        float roomCenterZ = 4f;
        const float roomWidth = 8f;
        const float roomDepth = 8f;
        float halfW = roomWidth * 0.5f;
        float halfD = roomDepth * 0.5f;

        CreateSolid("Bathroom_East_BackWall", room, new Vector3(roomCenterX + halfW, wallY, roomCenterZ), new Vector3(WallThickness, WallHeight, roomDepth + WallThickness), wallColor, true);
        CreateSolid("Bathroom_East_NorthWall", room, new Vector3(roomCenterX, wallY, roomCenterZ + halfD), new Vector3(roomWidth + WallThickness, WallHeight, WallThickness), wallColor, true);
        CreateSolid("Bathroom_East_SouthWall", room, new Vector3(roomCenterX, wallY, roomCenterZ - halfD), new Vector3(roomWidth + WallThickness, WallHeight, WallThickness), wallColor, true);

        float wingDepth = (roomDepth - doorGap) * 0.5f;
        CreateSolid("Bathroom_East_HallWall_North", room, new Vector3(roomCenterX - halfW, wallY, roomCenterZ + ((doorGap * 0.5f) + (wingDepth * 0.5f))), new Vector3(WallThickness, WallHeight, wingDepth), wallColor, true);
        CreateSolid("Bathroom_East_HallWall_South", room, new Vector3(roomCenterX - halfW, wallY, roomCenterZ - ((doorGap * 0.5f) + (wingDepth * 0.5f))), new Vector3(WallThickness, WallHeight, wingDepth), wallColor, true);

        CreateEmptyMarker(room, "ZombieSpawn_Bathroom", new Vector3(roomCenterX, 1f, roomCenterZ));
    }

    private static void CreateNorthLab(Transform parent, Color wallColor, float wallY)
    {
        Transform room = CreateChild(parent, "Lab_North");
        const float doorGap = 3f;
        float roomCenterZ = 16.5f;
        const float roomWidth = 12f;
        const float roomDepth = 8f;
        float halfW = roomWidth * 0.5f;
        float halfD = roomDepth * 0.5f;
        float frontZ = roomCenterZ - halfD;

        CreateSolid("Lab_North_BackWall", room, new Vector3(0f, wallY, roomCenterZ + halfD), new Vector3(roomWidth + WallThickness, WallHeight, WallThickness), wallColor, true);
        CreateSolid("Lab_North_LeftWall", room, new Vector3(-halfW, wallY, roomCenterZ), new Vector3(WallThickness, WallHeight, roomDepth + WallThickness), wallColor, true);
        CreateSolid("Lab_North_RightWall", room, new Vector3(halfW, wallY, roomCenterZ), new Vector3(WallThickness, WallHeight, roomDepth + WallThickness), wallColor, true);

        float wingWidth = (roomWidth - doorGap) * 0.5f;
        CreateSolid("Lab_North_FrontWall_Left", room, new Vector3(-((doorGap * 0.5f) + (wingWidth * 0.5f)), wallY, frontZ), new Vector3(wingWidth, WallHeight, WallThickness), wallColor, true);
        CreateSolid("Lab_North_FrontWall_Right", room, new Vector3((doorGap * 0.5f) + (wingWidth * 0.5f), wallY, frontZ), new Vector3(wingWidth, WallHeight, WallThickness), wallColor, true);

        CreateEmptyMarker(room, "ZombieSpawn_Lab", new Vector3(0f, 1f, roomCenterZ));
    }

    private static void CreateWeaponWallBuy(Transform root)
    {
        CreateSolid(
            "Weapon_WallBuy",
            root,
            new Vector3(3.15f, 1.4f, 1.5f),
            new Vector3(0.2f, 1.8f, 1.6f),
            new Color(0.2f, 0.45f, 0.85f),
            markNavigationStatic: false);
    }

    private static GameObject CreateSolid(
        string objectName,
        Transform parent,
        Vector3 position,
        Vector3 scale,
        Color color,
        bool markNavigationStatic)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = objectName;
        cube.transform.SetParent(parent, false);
        cube.transform.position = position;
        cube.transform.localScale = scale;

        if (cube.GetComponent<BoxCollider>() == null)
        {
            cube.AddComponent<BoxCollider>();
        }

        Renderer renderer = cube.GetComponent<Renderer>();
        if (renderer != null)
        {
            renderer.sharedMaterial = CreateMaterial(color);
        }

        if (markNavigationStatic)
        {
            GameObjectUtility.SetStaticEditorFlags(cube, StaticEditorFlags.NavigationStatic);
        }

        Undo.RegisterCreatedObjectUndo(cube, "Generate Indoor Level");
        return cube;
    }

    private static Transform CreateChild(Transform parent, string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(child, "Generate Indoor Level");
        return child.transform;
    }

    private static void CreateEmptyMarker(Transform parent, string markerName, Vector3 position)
    {
        GameObject marker = new GameObject(markerName);
        marker.transform.SetParent(parent, false);
        marker.transform.position = position;
        Undo.RegisterCreatedObjectUndo(marker, "Generate Indoor Level");
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
