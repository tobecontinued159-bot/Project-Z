#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class LevelGenerator
{
    private const string RootName = "GeneratedLevel";

    [MenuItem("Tools/Generate Level Blockout")]
    public static void GenerateLevelBlockout()
    {
        GameObject existing = GameObject.Find(RootName);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Generate Level Blockout");

        CreateFloor(root.transform);
        CreateMainBuildings(root.transform);
        CreateRuinedBuildings(root.transform);
        CreateCoverZones(root.transform);
        CreateSpawnMarkers(root.transform);

        Selection.activeGameObject = root;
        Debug.Log("Level blockout generated. Bake NavMesh before Play.");
    }

    private static void CreateFloor(Transform root)
    {
        GameObject floor = CreateCube(
            "Floor",
            root,
            new Vector3(0f, -0.5f, 0f),
            new Vector3(50f, 1f, 50f),
            new Color(0.35f, 0.35f, 0.36f),
            markNavigationStatic: true);
        floor.isStatic = true;
    }

    private static void CreateMainBuildings(Transform root)
    {
        Transform buildings = CreateChild(root, "Buildings");

        CreateCube(
            "Building_B1_TopLeft",
            buildings,
            new Vector3(-16f, 4f, 16f),
            new Vector3(12f, 8f, 12f),
            new Color(0.45f, 0.45f, 0.48f),
            markNavigationStatic: true);

        CreateCube(
            "Building_B1_BottomLeft",
            buildings,
            new Vector3(-16f, 4f, -16f),
            new Vector3(12f, 8f, 12f),
            new Color(0.45f, 0.45f, 0.48f),
            markNavigationStatic: true);

        CreateCube(
            "Building_B2_TopRight",
            buildings,
            new Vector3(16f, 6f, 16f),
            new Vector3(12f, 12f, 12f),
            new Color(0.32f, 0.28f, 0.28f),
            markNavigationStatic: false);

        CreateCube(
            "Building_B2_BottomRight",
            buildings,
            new Vector3(16f, 6f, -16f),
            new Vector3(12f, 12f, 12f),
            new Color(0.32f, 0.28f, 0.28f),
            markNavigationStatic: false);
    }

    private static void CreateRuinedBuildings(Transform root)
    {
        Transform ruins = CreateChild(root, "Ruined Buildings");

        CreateCube("Ruin_West_A", ruins, new Vector3(-6f, 1.5f, 8f), new Vector3(4f, 3f, 5f), new Color(0.4f, 0.36f, 0.32f), true);
        CreateCube("Ruin_West_B", ruins, new Vector3(-7f, 1f, -8f), new Vector3(5f, 2f, 4f), new Color(0.38f, 0.34f, 0.3f), true);
        CreateCube("Ruin_East_A", ruins, new Vector3(6f, 1.75f, 7f), new Vector3(4f, 3.5f, 4f), new Color(0.4f, 0.36f, 0.32f), true);
        CreateCube("Ruin_East_B", ruins, new Vector3(7f, 1.25f, -7f), new Vector3(5f, 2.5f, 4f), new Color(0.38f, 0.34f, 0.3f), true);
        CreateCube("Ruin_North", ruins, new Vector3(0f, 1.25f, 11f), new Vector3(6f, 2.5f, 3f), new Color(0.42f, 0.38f, 0.34f), true);
        CreateCube("Ruin_South", ruins, new Vector3(0f, 1.25f, -11f), new Vector3(6f, 2.5f, 3f), new Color(0.42f, 0.38f, 0.34f), true);
    }

    private static void CreateCoverZones(Transform root)
    {
        Transform cover = CreateChild(root, "Cover Zones");

        CreateCube("Cover_North", cover, new Vector3(0f, 0.6f, 3.5f), new Vector3(4f, 1.2f, 1.2f), new Color(0.28f, 0.3f, 0.26f), true);
        CreateCube("Cover_South", cover, new Vector3(0f, 0.6f, -3.5f), new Vector3(4f, 1.2f, 1.2f), new Color(0.28f, 0.3f, 0.26f), true);
        CreateCube("Cover_East", cover, new Vector3(3.5f, 0.7f, 0f), new Vector3(1.2f, 1.4f, 3.5f), new Color(0.26f, 0.28f, 0.24f), true);
        CreateCube("Cover_West", cover, new Vector3(-3.5f, 0.7f, 0f), new Vector3(1.2f, 1.4f, 3.5f), new Color(0.26f, 0.28f, 0.24f), true);
        CreateCube("Cover_Crate_A", cover, new Vector3(2f, 0.5f, 2f), new Vector3(1.4f, 1f, 1.4f), new Color(0.34f, 0.28f, 0.18f), true);
        CreateCube("Cover_Crate_B", cover, new Vector3(-2f, 0.5f, -2f), new Vector3(1.4f, 1f, 1.4f), new Color(0.34f, 0.28f, 0.18f), true);
    }

    private static void CreateSpawnMarkers(Transform root)
    {
        Transform playerSpawns = CreateChild(root, "PlayerSpawns");
        CreateEmptyMarker(playerSpawns, "PlayerSpawn", new Vector3(0f, 1f, 0f));

        Transform zombieSpawns = CreateChild(root, "ZombieSpawns");
        CreateEmptyMarker(zombieSpawns, "ZombieSpawn_B2_TopRight", new Vector3(16f, 1f, 9f));
        CreateEmptyMarker(zombieSpawns, "ZombieSpawn_B2_BottomRight", new Vector3(16f, 1f, -9f));
        CreateEmptyMarker(zombieSpawns, "ZombieSpawn_North", new Vector3(0f, 1f, 20f));
        CreateEmptyMarker(zombieSpawns, "ZombieSpawn_South", new Vector3(0f, 1f, -20f));
    }

    private static GameObject CreateCube(
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

        Undo.RegisterCreatedObjectUndo(cube, "Generate Level Blockout");
        return cube;
    }

    private static Transform CreateChild(Transform parent, string childName)
    {
        GameObject child = new GameObject(childName);
        child.transform.SetParent(parent, false);
        Undo.RegisterCreatedObjectUndo(child, "Generate Level Blockout");
        return child.transform;
    }

    private static void CreateEmptyMarker(Transform parent, string markerName, Vector3 position)
    {
        GameObject marker = new GameObject(markerName);
        marker.transform.SetParent(parent, false);
        marker.transform.position = position;
        Undo.RegisterCreatedObjectUndo(marker, "Generate Level Blockout");
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
