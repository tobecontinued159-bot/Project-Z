using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor Window สำหรับสร้างฉาก Blockout (Greybox) ของด่าน
/// "ห้องสายพานกระเป๋าใต้ดิน (Baggage Claim Maze)" สำหรับเกม 3D Top-down Zombie Shooter
///
/// สัญลักษณ์ใน ASCII Map:
///   # = กำแพงห้อง (Wall)              สูง 3.0 ม.
///   B = สายพานลำเลียงกระเป๋า (Conveyor) สูง 0.6 ม. สีเทาเข้ม
///   L = กองกระเป๋าเดินทาง (Luggage Pile) สูง 1.5 ม. ใช้เป็นที่กำบัง
///   . = พื้นทางเดินโล่ง (Floor)
///   S = จุดเกิดผู้เล่น (Player Spawn Point) -> สร้าง Object ชื่อ "PlayerSpawn"
///   E = ทางออกไปลานจอดเครื่องบิน (Exit Door) -> สร้าง Object ชื่อ "ExitZone"
///
/// วางไฟล์นี้ในโฟลเดอร์ "Editor" เช่น Assets/Editor/BaggageClaimLevelGenerator.cs
/// </summary>
public class BaggageClaimLevelGenerator : EditorWindow
{
    // ตัวอย่าง ASCII Map ขนาด 15x15 ช่อง (1 ช่อง = 2x2 เมตร)
    // ออกแบบเป็นทางเดินแคบวนคล้ายเขาวงกต มีสายพานอยู่กลางห้อง
    // มีจุดเกิดผู้เล่น 4 จุด (S) อยู่โซนเดียวกันด้านล่าง และทางออก (E) ทะลุกำแพงด้านขวา
    private string asciiMap =
        "###############\n" +
        "#.....#.......#\n" +
        "#.###.#.#####.#\n" +
        "#.#...#.#...#.#\n" +
        "#.#.#####.#.#.#\n" +
        "#.#.#BBBBB#.#.#\n" +
        "#...#BBBBB....#\n" +
        "#.###BBBBB###.#\n" +
        "#.#..L...L..#.#\n" +
        "#.#.#######.#.#\n" +
        "#...#.....#...#\n" +
        "#.###.....###.#\n" +
        "#.#....L....#.#\n" +
        "#S..S...S..S#.E\n" +
        "###############";

    // ---- ขนาดช่องและความสูงวัตถุ (เมตร) ----
    private float cellSize = 2f;
    private float wallHeight = 3f;
    private float conveyorHeight = 0.6f;
    private float luggageHeight = 1.5f;
    private float floorThickness = 0.2f;
    private float exitZoneWidth = 1.5f; // ความกว้างของช่องเปิดทางออก

    // ---- สี ----
    private Color wallColor = new Color(0.55f, 0.55f, 0.58f);       // คอนกรีตเทา
    private Color conveyorColor = new Color(0.25f, 0.25f, 0.27f);   // เทาเข้ม
    private Color luggageColor = new Color(0.45f, 0.3f, 0.15f);     // สีน้ำตาลกระเป๋า
    private Color floorColor = new Color(0.75f, 0.75f, 0.75f);      // พื้นเทาอ่อน
    private Color spawnGizmoColor = new Color(0.2f, 0.6f, 1f);      // ฟ้า (จุดเกิด)
    private Color exitGizmoColor = new Color(1f, 0.6f, 0.1f);       // ส้ม (ทางออก)

    private Vector2 scrollPos;
    private const string ROOT_NAME = "BaggageClaim_Blockout";

    [MenuItem("Tools/Baggage Claim Level Generator")]
    public static void ShowWindow()
    {
        var window = GetWindow<BaggageClaimLevelGenerator>("Baggage Claim Generator");
        window.minSize = new Vector2(380, 560);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("ASCII Map (15x15, 1 ช่อง = 2x2 ม.)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("# กำแพง | B สายพาน | L กองกระเป๋า | . พื้น | S จุดเกิด | E ทางออก", EditorStyles.miniLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(260));
        asciiMap = EditorGUILayout.TextArea(asciiMap, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("ขนาด (เมตร)", EditorStyles.boldLabel);
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
        wallHeight = EditorGUILayout.FloatField("Wall Height", wallHeight);
        conveyorHeight = EditorGUILayout.FloatField("Conveyor Height", conveyorHeight);
        luggageHeight = EditorGUILayout.FloatField("Luggage Height", luggageHeight);
        floorThickness = EditorGUILayout.FloatField("Floor Thickness", floorThickness);
        exitZoneWidth = EditorGUILayout.FloatField("Exit Zone Width", exitZoneWidth);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("สี", EditorStyles.boldLabel);
        wallColor = EditorGUILayout.ColorField("Wall Color", wallColor);
        conveyorColor = EditorGUILayout.ColorField("Conveyor Color", conveyorColor);
        luggageColor = EditorGUILayout.ColorField("Luggage Color", luggageColor);
        floorColor = EditorGUILayout.ColorField("Floor Color", floorColor);
        spawnGizmoColor = EditorGUILayout.ColorField("Spawn Marker Color", spawnGizmoColor);
        exitGizmoColor = EditorGUILayout.ColorField("Exit Marker Color", exitGizmoColor);

        EditorGUILayout.Space(14);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Generate Baggage Level", GUILayout.Height(42)))
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
            EditorUtility.DisplayDialog("Baggage Claim Level Generator", "กรุณาใส่ ASCII Map ก่อน", "OK");
            return;
        }

        ClearLevel();

        GameObject root = new GameObject(ROOT_NAME);
        Undo.RegisterCreatedObjectUndo(root, "Generate Baggage Claim Level");

        Transform wallsParent = CreateChild(root.transform, "Walls");
        Transform conveyorsParent = CreateChild(root.transform, "Conveyors");
        Transform luggageParent = CreateChild(root.transform, "LuggagePiles");
        Transform floorParent = CreateChild(root.transform, "Floor");
        Transform spawnsParent = CreateChild(root.transform, "PlayerSpawns");
        Transform exitParent = CreateChild(root.transform, "ExitZones");

        Material wallMat = CreateMaterial(wallColor);
        Material conveyorMat = CreateMaterial(conveyorColor);
        Material luggageMat = CreateMaterial(luggageColor);
        Material floorMat = CreateMaterial(floorColor);

        string[] lines = asciiMap.Replace("\r", "").Split('\n');
        int rowCount = lines.Length;
        int spawnIndex = 0;
        int exitIndex = 0;

        for (int row = 0; row < rowCount; row++)
        {
            string line = lines[row];

            // แถวบนสุดของข้อความ = ค่า Z มากสุด (อยู่ด้านไกลสุดในโลก 3D)
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

                    case 'L':
                        CreateLuggagePile(basePos, luggageParent, luggageMat);
                        CreateFloorCube(basePos, floorParent, floorMat);
                        break;

                    case '.':
                        CreateFloorCube(basePos, floorParent, floorMat);
                        break;

                    case 'S':
                        spawnIndex++;
                        CreatePlayerSpawn(basePos, spawnsParent, spawnIndex);
                        CreateFloorCube(basePos, floorParent, floorMat);
                        break;

                    case 'E':
                        exitIndex++;
                        CreateExitZone(basePos, exitParent, exitIndex);
                        CreateFloorCube(basePos, floorParent, floorMat);
                        break;

                    default:
                        // อักขระอื่น (เช่นช่องว่าง) ข้ามไป ไม่สร้างอะไร
                        break;
                }
            }
        }

        Selection.activeGameObject = root;
        Debug.Log($"[BaggageClaimLevelGenerator] สร้างฉากเสร็จแล้ว: {rowCount} แถว, " +
                  $"Player Spawn {spawnIndex} จุด, Exit Zone {exitIndex} จุด, cell size {cellSize}m");
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
        cube.name = "ConveyorBelt";
        cube.transform.SetParent(parent);
        cube.transform.localScale = new Vector3(cellSize, conveyorHeight, cellSize);
        cube.transform.position = basePos + new Vector3(0f, conveyorHeight * 0.5f, 0f);
        ApplyMaterial(cube, mat);
        return cube;
    }

    private GameObject CreateLuggagePile(Vector3 basePos, Transform parent, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "LuggagePile";
        cube.transform.SetParent(parent);
        // ลดขนาดกว้าง/ลึกลงเล็กน้อยจากเต็มช่อง ให้ดูเหมือนกองของวางอยู่ ไม่ใช่บล็อกเต็มช่อง
        float footprint = cellSize * 0.85f;
        cube.transform.localScale = new Vector3(footprint, luggageHeight, footprint);
        cube.transform.position = basePos + new Vector3(0f, luggageHeight * 0.5f, 0f);
        ApplyMaterial(cube, mat);

        // ใส่ Collider ไว้แล้ว (มาพร้อม primitive) ใช้เป็นที่กำบังกระสุน/แนวสายตาได้ทันที
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

    private GameObject CreatePlayerSpawn(Vector3 basePos, Transform parent, int index)
    {
        // จุดเกิดผู้เล่น: Empty GameObject ชื่อ "PlayerSpawn" (ต่อท้ายลำดับเพื่อแยกแต่ละจุด)
        // ไม่มี Collider/Renderer เพราะใช้เป็น Transform อ้างอิงสำหรับระบบ spawn เท่านั้น
        GameObject spawn = new GameObject($"PlayerSpawn_{index}");
        spawn.name = "PlayerSpawn"; // คงชื่อหลักตามที่กำหนด ("PlayerSpawn") ตามสเปค
        spawn.transform.SetParent(parent);
        spawn.transform.position = basePos + new Vector3(0f, 0.1f, 0f);

        SpawnPointMarker marker = spawn.AddComponent<SpawnPointMarker>();
        marker.gizmoColor = spawnGizmoColor;
        marker.playerIndex = index;

        return spawn;
    }

    private GameObject CreateExitZone(Vector3 basePos, Transform parent, int index)
    {
        // ทางออกไปลานจอดเครื่องบิน: BoxCollider (isTrigger) ชื่อ "ExitZone"
        GameObject exit = new GameObject("ExitZone");
        exit.transform.SetParent(parent);
        exit.transform.position = basePos + new Vector3(0f, wallHeight * 0.5f, 0f);

        BoxCollider box = exit.AddComponent<BoxCollider>();
        box.isTrigger = true;
        box.size = new Vector3(exitZoneWidth, wallHeight, cellSize);

        ExitZoneMarker marker = exit.AddComponent<ExitZoneMarker>();
        marker.gizmoColor = exitGizmoColor;

        return exit;
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

/// <summary>
/// Component ติดไว้ที่ Object "PlayerSpawn" เพื่อวาด Gizmo ใน Scene View ให้เห็นตำแหน่งชัดเจน
/// (ไม่มีผลใดๆ ตอน Runtime/Build)
/// </summary>
public class SpawnPointMarker : MonoBehaviour
{
    public Color gizmoColor = Color.blue;
    public int playerIndex = 0;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.8f);
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, $"P{playerIndex} Spawn");
#endif
    }
}

/// <summary>
/// Component ติดไว้ที่ Object "ExitZone" เพื่อวาด Gizmo ใน Scene View ให้เห็นตำแหน่งชัดเจน
/// (ไม่มีผลใดๆ ตอน Runtime/Build)
/// </summary>
public class ExitZoneMarker : MonoBehaviour
{
    public Color gizmoColor = new Color(1f, 0.6f, 0.1f);

    private void OnDrawGizmos()
    {
        BoxCollider box = GetComponent<BoxCollider>();
        Gizmos.color = gizmoColor;
        if (box != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(Vector3.zero, box.size);
            Gizmos.matrix = Matrix4x4.identity;
        }
#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 1.5f, "Exit Zone");
#endif
    }
}