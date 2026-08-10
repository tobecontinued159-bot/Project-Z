using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor Window สำหรับสร้างฉาก Blockout ด่านสายพานกระเป๋าสนามบิน จาก ASCII Map
/// #  = กำแพง (Wall Cube)
/// B  = สายพานลำเลียง (Conveyor Cube สีเทา)
/// .  = พื้นโล่ง (Floor)
/// วางไฟล์นี้ในโฟลเดอร์ "Editor" เช่น Assets/Editor/AirportLevelGenerator.cs
/// </summary>
public class AirportLevelGenerator : EditorWindow
{
    private string asciiMap =
        "########\n" +
        "#......#\n" +
        "#.BBBB.#\n" +
        "#......#\n" +
        "########";

    private float cellSize = 2f;       // ขนาดช่องละ 2x2 เมตร ตามที่กำหนด
    private float wallHeight = 3f;
    private float conveyorHeight = 0.5f;
    private float floorThickness = 0.2f;

    private Color wallColor = new Color(0.45f, 0.45f, 0.5f);
    private Color conveyorColor = new Color(0.5f, 0.5f, 0.5f); // สายพานสีเทา
    private Color floorColor = new Color(0.8f, 0.8f, 0.8f);

    private Vector2 scrollPos;
    private const string ROOT_NAME = "GeneratedLevel";

    [MenuItem("Tools/Airport Level Generator")]
    public static void ShowWindow()
    {
        var window = GetWindow<AirportLevelGenerator>("Airport Level Generator");
        window.minSize = new Vector2(360, 480);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("ASCII Map", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("# = กำแพง   B = สายพาน   . = พื้นโล่ง", EditorStyles.miniLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(200));
        asciiMap = EditorGUILayout.TextArea(asciiMap, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("ค่าขนาด (เมตร)", EditorStyles.boldLabel);
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
        wallHeight = EditorGUILayout.FloatField("Wall Height", wallHeight);
        conveyorHeight = EditorGUILayout.FloatField("Conveyor Height", conveyorHeight);
        floorThickness = EditorGUILayout.FloatField("Floor Thickness", floorThickness);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("สี", EditorStyles.boldLabel);
        wallColor = EditorGUILayout.ColorField("Wall Color", wallColor);
        conveyorColor = EditorGUILayout.ColorField("Conveyor Color", conveyorColor);
        floorColor = EditorGUILayout.ColorField("Floor Color", floorColor);

        EditorGUILayout.Space(12);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Generate Level", GUILayout.Height(40)))
        {
            GenerateLevel();
        }
        GUI.backgroundColor = Color.white;

        if (GUILayout.Button("Clear Level", GUILayout.Height(24)))
        {
            ClearLevel();
        }
    }

    private void ClearLevel()
    {
        GameObject existing = GameObject.Find(ROOT_NAME);
        if (existing != null)
        {
            Undo.DestroyObjectImmediate(existing);
        }
    }

    private void GenerateLevel()
    {
        if (string.IsNullOrEmpty(asciiMap))
        {
            EditorUtility.DisplayDialog("Airport Level Generator", "กรุณาใส่ ASCII Map ก่อน", "OK");
            return;
        }

        ClearLevel();

        GameObject root = new GameObject(ROOT_NAME);
        Undo.RegisterCreatedObjectUndo(root, "Generate Airport Level");

        Transform wallsParent = CreateChild(root.transform, "Walls");
        Transform conveyorsParent = CreateChild(root.transform, "Conveyors");
        Transform floorParent = CreateChild(root.transform, "Floor");

        Material wallMat = CreateMaterial(wallColor);
        Material conveyorMat = CreateMaterial(conveyorColor);
        Material floorMat = CreateMaterial(floorColor);

        string[] lines = asciiMap.Replace("\r", "").Split('\n');
        int rowCount = lines.Length;

        for (int row = 0; row < rowCount; row++)
        {
            string line = lines[row];

            // แถวบนสุดของข้อความ = ค่า Z มากสุด (อยู่ด้านหน้า/ด้านไกลสุดในโลก 3D)
            float zPos = (rowCount - 1 - row) * cellSize;

            for (int col = 0; col < line.Length; col++)
            {
                char c = line[col];
                Vector3 basePos = new Vector3(col * cellSize, 0f, zPos);

                switch (c)
                {
                    case '#':
                        CreateWallCube(basePos, wallsParent, wallMat);
                        break;

                    case 'B':
                        CreateConveyorCube(basePos, conveyorsParent, conveyorMat);
                        CreateFloorCube(basePos, floorParent, floorMat);
                        break;

                    case '.':
                        CreateFloorCube(basePos, floorParent, floorMat);
                        break;

                    default:
                        // ตัวอักษรที่ไม่รู้จัก (เช่น space) จะข้ามไป ไม่สร้างอะไร
                        break;
                }
            }
        }

        Selection.activeGameObject = root;
        Debug.Log($"[AirportLevelGenerator] สร้างฉากเสร็จแล้ว: {rowCount} แถว, cell size {cellSize}m");
    }

    private Transform CreateChild(Transform parent, string name)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent);
        return go.transform;
    }

    private GameObject CreateWallCube(Vector3 basePos, Transform parent, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Wall";
        cube.transform.SetParent(parent);
        cube.transform.localScale = new Vector3(cellSize, wallHeight, cellSize);
        cube.transform.position = basePos + new Vector3(0f, wallHeight * 0.5f, 0f);
        ApplyMaterial(cube, mat);
        return cube;
    }

    private GameObject CreateConveyorCube(Vector3 basePos, Transform parent, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Conveyor";
        cube.transform.SetParent(parent);
        cube.transform.localScale = new Vector3(cellSize, conveyorHeight, cellSize);
        cube.transform.position = basePos + new Vector3(0f, conveyorHeight * 0.5f, 0f);
        ApplyMaterial(cube, mat);
        return cube;
    }

    private GameObject CreateFloorCube(Vector3 basePos, Transform parent, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Floor";
        cube.transform.SetParent(parent);
        cube.transform.localScale = new Vector3(cellSize, floorThickness, cellSize);
        cube.transform.position = basePos + new Vector3(0f, -floorThickness * 0.5f, 0f);
        ApplyMaterial(cube, mat);
        return cube;
    }

    private void ApplyMaterial(GameObject go, Material mat)
    {
        Renderer rend = go.GetComponent<Renderer>();
        if (rend != null && mat != null)
        {
            rend.sharedMaterial = mat;
        }
    }

    private Material CreateMaterial(Color color)
    {
        // รองรับทั้ง URP และ Built-in Render Pipeline
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");

        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }
}