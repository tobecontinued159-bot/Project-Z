using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor Window สำหรับสร้างฉาก Blockout (Greybox) ของด่าน
/// "ลานจอดเครื่องบินด้านนอก (Open Tarmac Arena)" สำหรับเกม 3D Top-down Zombie Shooter
/// เน้นเป็นลานกว้างสำหรับต่อสู้กับฝูงซอมบี้รอบทิศทาง
///
/// สัญลักษณ์ใน ASCII Map:
///   # = รั้ว/กำแพงขอบสนามบิน (Perimeter Wall)     สูง 3.0 ม.
///   O = สิ่งกีดขวางขนาดใหญ่ เช่น ซากรถบัส/ตู้คอนเทนเนอร์ (Obstacle) สูง 2.5 ม.
///   . = พื้นลานบินโล่ง (Open Tarmac Floor)
///   S = จุดเกิดผู้เล่นตรงกลางลาน (Player Spawn)  -> สร้าง Object ชื่อ "PlayerSpawn"
///   Z = จุดเกิดฝูงซอมบี้รอบลาน (Zombie Spawn)     -> สร้าง Object ชื่อ "ZombieSpawn"
///
/// วางไฟล์นี้ในโฟลเดอร์ "Editor" เช่น Assets/Editor/TarmacLevelGenerator.cs
/// </summary>
public class TarmacLevelGenerator : EditorWindow
{
    // ตัวอย่าง ASCII Map ขนาด 18x18 ช่อง (1 ช่อง = 2x2 เมตร)
    // ออกแบบเป็นลานโล่งกว้าง มีสิ่งกีดขวางกระจายเป็นที่กำบัง
    // จุดเกิดผู้เล่นอยู่กึ่งกลางลาน และจุดเกิดซอมบี้ 4 ทิศ (เหนือ-ใต้-ตะวันออก-ตะวันตก)
    private string asciiMap =
        "##################\n" +
        "#........Z.......#\n" +
        "#..O.........O...#\n" +
        "#................#\n" +
        "#.....O....O.....#\n" +
        "#................#\n" +
        "#..O.....O....O..#\n" +
        "#................#\n" +
        "#....O......O....#\n" +
        "#Z.......S......Z#\n" +
        "#................#\n" +
        "#..O.....O....O..#\n" +
        "#................#\n" +
        "#.....O....O.....#\n" +
        "#................#\n" +
        "#..O.........O...#\n" +
        "#........Z.......#\n" +
        "##################";

    // ---- ขนาดช่องและความสูงวัตถุ (เมตร) ----
    private float cellSize = 2f;
    private float wallHeight = 3f;
    private float obstacleHeight = 2.5f;
    private float floorThickness = 0.2f;
    private bool randomizeObstacleRotation = true;
    private bool randomizeObstacleScale = true;

    // ---- สี ----
    private Color wallColor = new Color(0.5f, 0.5f, 0.52f);        // คอนกรีต/รั้วเทา
    private Color obstacleColor = new Color(0.35f, 0.32f, 0.3f);   // ซากรถ/ตู้คอนเทนเนอร์ สีเข้ม
    private Color floorColor = new Color(0.62f, 0.62f, 0.6f);      // ยางมะตอยลานบิน
    private Color playerSpawnGizmoColor = new Color(0.2f, 0.6f, 1f);  // ฟ้า
    private Color zombieSpawnGizmoColor = new Color(0.9f, 0.15f, 0.15f); // แดง

    private Vector2 scrollPos;
    private const string ROOT_NAME = "Tarmac_Blockout";

    [MenuItem("Tools/Tarmac Level Generator")]
    public static void ShowWindow()
    {
        var window = GetWindow<TarmacLevelGenerator>("Tarmac Generator");
        window.minSize = new Vector2(380, 560);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("ASCII Map (18x18, 1 ช่อง = 2x2 ม.)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("# กำแพงขอบสนาม | O สิ่งกีดขวาง | . พื้นโล่ง | S จุดเกิดผู้เล่น | Z จุดเกิดซอมบี้", EditorStyles.miniLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(260));
        asciiMap = EditorGUILayout.TextArea(asciiMap, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("ขนาด (เมตร)", EditorStyles.boldLabel);
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
        wallHeight = EditorGUILayout.FloatField("Wall Height", wallHeight);
        obstacleHeight = EditorGUILayout.FloatField("Obstacle Height", obstacleHeight);
        floorThickness = EditorGUILayout.FloatField("Floor Thickness", floorThickness);

        EditorGUILayout.Space(6);
        randomizeObstacleRotation = EditorGUILayout.Toggle("Randomize Obstacle Rotation", randomizeObstacleRotation);
        randomizeObstacleScale = EditorGUILayout.Toggle("Randomize Obstacle Scale", randomizeObstacleScale);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("สี", EditorStyles.boldLabel);
        wallColor = EditorGUILayout.ColorField("Wall Color", wallColor);
        obstacleColor = EditorGUILayout.ColorField("Obstacle Color", obstacleColor);
        floorColor = EditorGUILayout.ColorField("Floor Color", floorColor);
        playerSpawnGizmoColor = EditorGUILayout.ColorField("Player Spawn Marker Color", playerSpawnGizmoColor);
        zombieSpawnGizmoColor = EditorGUILayout.ColorField("Zombie Spawn Marker Color", zombieSpawnGizmoColor);

        EditorGUILayout.Space(14);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Generate Tarmac Level", GUILayout.Height(42)))
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
            EditorUtility.DisplayDialog("Tarmac Level Generator", "กรุณาใส่ ASCII Map ก่อน", "OK");
            return;
        }

        ClearLevel();

        GameObject root = new GameObject(ROOT_NAME);
        Undo.RegisterCreatedObjectUndo(root, "Generate Tarmac Level");

        Transform wallsParent = CreateChild(root.transform, "PerimeterWalls");
        Transform obstaclesParent = CreateChild(root.transform, "Obstacles");
        Transform floorParent = CreateChild(root.transform, "Floor");
        Transform playerSpawnParent = CreateChild(root.transform, "PlayerSpawns");
        Transform zombieSpawnParent = CreateChild(root.transform, "ZombieSpawns");

        Material wallMat = CreateMaterial(wallColor);
        Material obstacleMat = CreateMaterial(obstacleColor);
        Material floorMat = CreateMaterial(floorColor);

        string[] lines = asciiMap.Replace("\r", "").Split('\n');
        int rowCount = lines.Length;

        // คำนวณจุดกึ่งกลางแผนที่ไว้ล่วงหน้า สำหรับให้ Zombie Spawn หันหน้าเข้าหาศูนย์กลางลาน
        Vector3 mapCenter = ComputeMapCenter(lines, rowCount);

        int playerSpawnIndex = 0;
        int zombieSpawnIndex = 0;

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

                    case 'O':
                        CreateObstacle(basePos, obstaclesParent, obstacleMat);
                        CreateFloorCube(basePos, floorParent, floorMat);
                        break;

                    case '.':
                        CreateFloorCube(basePos, floorParent, floorMat);
                        break;

                    case 'S':
                        playerSpawnIndex++;
                        CreatePlayerSpawn(basePos, playerSpawnParent, playerSpawnIndex);
                        CreateFloorCube(basePos, floorParent, floorMat);
                        break;

                    case 'Z':
                        zombieSpawnIndex++;
                        CreateZombieSpawn(basePos, zombieSpawnParent, zombieSpawnIndex, mapCenter);
                        CreateFloorCube(basePos, floorParent, floorMat);
                        break;

                    default:
                        // อักขระอื่น (เช่นช่องว่าง) ข้ามไป ไม่สร้างอะไร
                        break;
                }
            }
        }

        Selection.activeGameObject = root;
        Debug.Log($"[TarmacLevelGenerator] สร้างฉากเสร็จแล้ว: {rowCount} แถว, " +
                  $"Player Spawn {playerSpawnIndex} จุด, Zombie Spawn {zombieSpawnIndex} จุด, cell size {cellSize}m");
    }

    private Vector3 ComputeMapCenter(string[] lines, int rowCount)
    {
        int maxCols = 0;
        foreach (string l in lines)
        {
            if (l.Length > maxCols) maxCols = l.Length;
        }
        float centerX = (maxCols - 1) * cellSize * 0.5f;
        float centerZ = (rowCount - 1) * cellSize * 0.5f;
        return new Vector3(centerX, 0f, centerZ);
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
        cube.name = "PerimeterWall";
        cube.transform.SetParent(parent);
        cube.transform.localScale = new Vector3(cellSize, wallHeight, cellSize);
        cube.transform.position = basePos + new Vector3(0f, wallHeight * 0.5f, 0f);
        ApplyMaterial(cube, mat);
        return cube;
    }

    private GameObject CreateObstacle(Vector3 basePos, Transform parent, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "Obstacle";
        cube.transform.SetParent(parent);

        float footprintX = cellSize * 0.9f;
        float footprintZ = cellSize * 0.9f;

        if (randomizeObstacleScale)
        {
            // สุ่มขนาดหน้าตัดเล็กน้อย ให้ดูเหมือนซากรถบัส/ตู้คอนเทนเนอร์ที่ไม่เท่ากันทุกชิ้น
            footprintX *= Random.Range(0.85f, 1.15f);
            footprintZ *= Random.Range(0.85f, 1.15f);
        }

        cube.transform.localScale = new Vector3(footprintX, obstacleHeight, footprintZ);
        cube.transform.position = basePos + new Vector3(0f, obstacleHeight * 0.5f, 0f);

        if (randomizeObstacleRotation)
        {
            float randomY = Random.Range(0f, 360f);
            cube.transform.rotation = Quaternion.Euler(0f, randomY, 0f);
        }

        ApplyMaterial(cube, mat);
        // Collider มาพร้อม primitive อยู่แล้ว ใช้เป็นที่กำบังกระสุน/แนวสายตาได้ทันที
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
        // จุดเกิดผู้เล่นตรงกลางลาน: Empty GameObject ชื่อ "PlayerSpawn"
        GameObject spawn = new GameObject("PlayerSpawn");
        spawn.transform.SetParent(parent);
        spawn.transform.position = basePos + new Vector3(0f, 0.1f, 0f);

        TarmacSpawnMarker marker = spawn.AddComponent<TarmacSpawnMarker>();
        marker.gizmoColor = playerSpawnGizmoColor;
        marker.label = index > 1 ? $"Player Spawn {index}" : "Player Spawn";

        return spawn;
    }

    private GameObject CreateZombieSpawn(Vector3 basePos, Transform parent, int index, Vector3 mapCenter)
    {
        // จุดเกิดฝูงซอมบี้รอบลาน: Empty GameObject ชื่อ "ZombieSpawn"
        // หันหน้า (forward) เข้าหาจุดศูนย์กลางลานโดยอัตโนมัติ เพื่อให้ซอมบี้ไล่เข้าหาผู้เล่นได้ทันที
        GameObject spawn = new GameObject("ZombieSpawn");
        spawn.transform.SetParent(parent);
        Vector3 spawnPos = basePos + new Vector3(0f, 0.1f, 0f);
        spawn.transform.position = spawnPos;

        Vector3 dirToCenter = mapCenter - spawnPos;
        dirToCenter.y = 0f;
        if (dirToCenter.sqrMagnitude > 0.001f)
        {
            spawn.transform.rotation = Quaternion.LookRotation(dirToCenter.normalized, Vector3.up);
        }

        TarmacSpawnMarker marker = spawn.AddComponent<TarmacSpawnMarker>();
        marker.gizmoColor = zombieSpawnGizmoColor;
        marker.label = $"Zombie Spawn {index}";
        marker.drawForwardArrow = true;

        return spawn;
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
/// Component ติดไว้ที่ Object จุดเกิด (PlayerSpawn / ZombieSpawn) เพื่อวาด Gizmo ใน Scene View
/// ให้เห็นตำแหน่งและทิศทางชัดเจน (ไม่มีผลใดๆ ตอน Runtime/Build)
/// </summary>
public class TarmacSpawnMarker : MonoBehaviour
{
    public Color gizmoColor = Color.blue;
    public string label = "Spawn";
    public bool drawForwardArrow = false;

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.8f);

        if (drawForwardArrow)
        {
            Vector3 tip = transform.position + transform.forward * 1.5f;
            Gizmos.DrawLine(transform.position, tip);
            Gizmos.DrawLine(tip, tip - transform.forward * 0.4f + transform.right * 0.3f);
            Gizmos.DrawLine(tip, tip - transform.forward * 0.4f - transform.right * 0.3f);
        }

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, label);
#endif
    }
}