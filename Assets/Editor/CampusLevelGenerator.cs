#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class CampusLevelGenerator
{
    private const string RootName = "CampusLevel";

    [MenuItem("Tools/Generate Campus Level")]
    public static void GenerateCampusLevel()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Generate Campus Level");

        CreateMainFloor(root.transform);
        CreateLectureRoom(root.transform);
        CreatePlazaArea(root.transform);
        CreateCoverObjects(root.transform);

        Selection.activeGameObject = root;
        Debug.Log("Campus level generated. Bake NavMesh before Play.");
    }

    private static void CreateMainFloor(Transform root)
    {
        CreateCube(
            "MainFloor",
            root,
            new Vector3(0f, -0.5f, 0f),
            new Vector3(50f, 1f, 50f),
            new Color(0.42f, 0.42f, 0.4f));
    }

    private static void CreateLectureRoom(Transform root)
    {
        Transform room = CreateChild(root, "LectureRoom");
        Color wallColor = new Color(0.55f, 0.54f, 0.5f);

        const float wallHeight = 4f;
        const float wallThickness = 1f;
        const float roomWidth = 16f;
        const float roomDepth = 12f;
        const float doorGap = 3f;
        float wallY = wallHeight * 0.5f;
        float roomCenterZ = -19f;
        float halfWidth = roomWidth * 0.5f;
        float halfDepth = roomDepth * 0.5f;
        float frontZ = roomCenterZ + halfDepth;
        float backZ = roomCenterZ - halfDepth;
        float leftX = -halfWidth;
        float rightX = halfWidth;
        float sideWallLength = roomDepth + wallThickness;
        float wingWidth = (roomWidth - doorGap) * 0.5f;

        CreateCube("LectureRoom_BackWall", room, new Vector3(0f, wallY, backZ), new Vector3(roomWidth + wallThickness, wallHeight, wallThickness), wallColor);
        CreateCube("LectureRoom_LeftWall", room, new Vector3(leftX, wallY, roomCenterZ), new Vector3(wallThickness, wallHeight, sideWallLength), wallColor);
        CreateCube("LectureRoom_RightWall", room, new Vector3(rightX, wallY, roomCenterZ), new Vector3(wallThickness, wallHeight, sideWallLength), wallColor);

        float leftWingX = -((doorGap * 0.5f) + (wingWidth * 0.5f));
        float rightWingX = (doorGap * 0.5f) + (wingWidth * 0.5f);
        CreateCube("LectureRoom_FrontWall_Left", room, new Vector3(leftWingX, wallY, frontZ), new Vector3(wingWidth, wallHeight, wallThickness), wallColor);
        CreateCube("LectureRoom_FrontWall_Right", room, new Vector3(rightWingX, wallY, frontZ), new Vector3(wingWidth, wallHeight, wallThickness), wallColor);

        GameObject doorGapMarker = new GameObject("LectureRoom_DoorGap");
        doorGapMarker.transform.SetParent(room, false);
        doorGapMarker.transform.position = new Vector3(0f, 1.5f, frontZ);
        Undo.RegisterCreatedObjectUndo(doorGapMarker, "Generate Campus Level");
    }

    private static void CreatePlazaArea(Transform root)
    {
        Transform plaza = CreateChild(root, "PlazaArea");
        Color buildingColor = new Color(0.5f, 0.52f, 0.55f);

        CreateCube("CampusBuilding_TopLeft", plaza, new Vector3(-16f, 6f, 16f), new Vector3(12f, 12f, 12f), buildingColor);
        CreateCube("CampusBuilding_TopRight", plaza, new Vector3(16f, 6f, 16f), new Vector3(12f, 12f, 12f), buildingColor);
        CreateCube("CampusBuilding_BottomLeft", plaza, new Vector3(-16f, 5f, -8f), new Vector3(12f, 10f, 12f), buildingColor);
        CreateCube("CampusBuilding_BottomRight", plaza, new Vector3(16f, 5f, -8f), new Vector3(12f, 10f, 12f), buildingColor);
    }

    private static void CreateCoverObjects(Transform root)
    {
        Transform cover = CreateChild(root, "CoverObjects");
        Color coverColor = new Color(0.62f, 0.62f, 0.6f);
        Vector3[] coverLayouts =
        {
            new Vector3(-3.5f, 0.5f, 2f),
            new Vector3(3.2f, 0.5f, 1.5f),
            new Vector3(0.4f, 0.45f, 4.8f),
            new Vector3(-1.8f, 0.4f, -1.2f),
            new Vector3(2.6f, 0.55f, -2.4f),
            new Vector3(-4.2f, 0.45f, 5.5f)
        };

        Vector3[] coverScales =
        {
            new Vector3(3.2f, 1f, 1.1f),
            new Vector3(2.8f, 1f, 1.2f),
            new Vector3(1.4f, 0.9f, 2.6f),
            new Vector3(2.2f, 0.8f, 1.1f),
            new Vector3(1.3f, 1.1f, 2.2f),
            new Vector3(2.4f, 0.9f, 1.2f)
        };

        for (int i = 0; i < coverLayouts.Length; i++)
        {
            CreateCube($"Cover_Table_{i + 1}", cover, coverLayouts[i], coverScales[i], coverColor);
        }
    }

    private static GameObject CreateCube(string objectName, Transform parent, Vector3 position, Vector3 scale, Color color)
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

        GameObjectUtility.SetStaticEditorFlags(cube, StaticEditorFlags.NavigationStatic);
        Undo.RegisterCreatedObjectUndo(cube, "Generate Campus Level");
        return cube;
    }

    private static Transform CreateChild(Transform parent, string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(child, "Generate Campus Level");
        return child.transform;
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
