using UnityEngine;
using UnityEditor;

/// <summary>
/// Editor Window สำหรับสร้างฉาก Blockout (Greybox) ของด่าน
/// "ภายในซากเครื่องบิน Boeing 747 (Crashed Airplane)" สำหรับเกม 3D Top-down Zombie Shooter
///
/// สัญลักษณ์ใน ASCII Map:
///   # = ผนังลำตัวเครื่องบินซ้าย-ขวา (Fuselage Wall)         สูง 2.5 ม.
///   C = แถวเก้าอี้ผู้โดยสาร (Seat Row)                       สูง 1.0 ม.
///   . = ทางเดินกลางเครื่องบิน (Narrow Aisle)                 พื้นโล่ง
///   S = จุดเกิดผู้เล่นท้ายเครื่องบิน (Economy Class Spawn)   -> Object ชื่อ "PlayerSpawn"
///   G = พื้นที่ปลอดภัยหน้าห้องนักบิน (Cockpit Safe Zone)      -> Cube สีเขียวโปร่งแสง + Trigger
///
/// วางไฟล์นี้ในโฟลเดอร์ "Editor" เช่น Assets/Editor/AirplaneLevelGenerator.cs
/// </summary>
public class AirplaneLevelGenerator : EditorWindow
{
    // ตัวอย่าง ASCII Map ขนาด 8 (กว้าง) x 20 (ยาว) ช่อง (1 ช่อง = 2x2 เมตร)
    // แถวบนสุดของข้อความ = หัวเครื่องบิน (จมูก/ห้องนักบิน)
    // แถวล่างสุดของข้อความ = ท้ายเครื่องบิน (Economy Class, จุดเกิดผู้เล่น)
    private string asciiMap =
        "########\n" + // จมูกเครื่องบิน (nose cap)
        "#GGGGGG#\n" + // Cockpit Safe Zone
        "#GGGGGG#\n" +
        "#......#\n" + // ประตูทางเข้าห้องนักบิน / จุดพักก่อนเข้าโซนที่นั่ง
        "#CC..CC#\n" +
        "#CC..CC#\n" +
        "#CC..CC#\n" +
        "#CC..CC#\n" +
        "#CC..CC#\n" +
        "#......#\n" + // ทางออกฉุกเฉิน (Emergency Exit Row)
        "#CC..CC#\n" +
        "#CC..CC#\n" +
        "#CC..CC#\n" +
        "#CC..CC#\n" +
        "#......#\n" + // ครัวการบิน / ทางออกฉุกเฉิน (Galley Row)
        "#CC..CC#\n" +
        "#CC..CC#\n" +
        "#CC..CC#\n" +
        "#......#\n" + // พื้นที่ท้ายเครื่องก่อนถึงจุดเกิด
        "#...S..#";     // ท้ายเครื่องบิน (Economy Class Spawn)

    // ---- ขนาดช่องและความสูงวัตถุ (เมตร) ----
    private float cellSize = 2f;
    private float wallHeight = 2.5f;
    private float seatHeight = 1f;
    private float floorThickness = 0.2f;
    private float safeZoneHeight = 2.2f;
    private float safeZoneAlpha = 0.35f;

    // ---- สี ----
    private Color wallColor = new Color(0.55f, 0.55f, 0.58f);      // ผนังลำตัวเครื่องบิน
    private Color seatColor = new Color(0.2f, 0.25f, 0.55f);       // เก้าอี้สีน้ำเงินเข้ม
    private Color floorColor = new Color(0.7f, 0.68f, 0.65f);      // พื้นห้องโดยสาร
    private Color safeZoneColor = new Color(0.2f, 1f, 0.3f);       // เขียวโปร่งแสง
    private Color spawnGizmoColor = new Color(0.2f, 0.6f, 1f);     // ฟ้า

    private Vector2 scrollPos;
    private const string ROOT_NAME = "Airplane747_Blockout";

    [MenuItem("Tools/Airplane Level Generator")]
    public static void ShowWindow()
    {
        var window = GetWindow<AirplaneLevelGenerator>("Airplane 747 Generator");
        window.minSize = new Vector2(380, 560);
    }

    private void OnGUI()
    {
        EditorGUILayout.LabelField("ASCII Map (8 กว้าง x 20 ยาว, 1 ช่อง = 2x2 ม.)", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("# ผนังลำตัว | C เก้าอี้ | . ทางเดิน | S จุดเกิดผู้เล่น | G Cockpit Safe Zone", EditorStyles.miniLabel);
        EditorGUILayout.LabelField("แถวบนสุด = หัวเครื่อง(นักบิน) / แถวล่างสุด = ท้ายเครื่อง(Economy)", EditorStyles.miniLabel);

        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(260));
        asciiMap = EditorGUILayout.TextArea(asciiMap, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("ขนาด (เมตร)", EditorStyles.boldLabel);
        cellSize = EditorGUILayout.FloatField("Cell Size", cellSize);
        wallHeight = EditorGUILayout.FloatField("Fuselage Wall Height", wallHeight);
        seatHeight = EditorGUILayout.FloatField("Seat Height", seatHeight);
        floorThickness = EditorGUILayout.FloatField("Floor Thickness", floorThickness);
        safeZoneHeight = EditorGUILayout.FloatField("Safe Zone Height", safeZoneHeight);
        safeZoneAlpha = EditorGUILayout.Slider("Safe Zone Alpha", safeZoneAlpha, 0.05f, 1f);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("สี", EditorStyles.boldLabel);
        wallColor = EditorGUILayout.ColorField("Fuselage Wall Color", wallColor);
        seatColor = EditorGUILayout.ColorField("Seat Color", seatColor);
        floorColor = EditorGUILayout.ColorField("Floor Color", floorColor);
        safeZoneColor = EditorGUILayout.ColorField("Safe Zone Color", safeZoneColor);
        spawnGizmoColor = EditorGUILayout.ColorField("Spawn Marker Color", spawnGizmoColor);

        EditorGUILayout.Space(14);

        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("Generate Airplane Level", GUILayout.Height(42)))
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
            EditorUtility.DisplayDialog("Airplane Level Generator", "กรุณาใส่ ASCII Map ก่อน", "OK");
            return;
        }

        ClearLevel();

        GameObject root = new GameObject(ROOT_NAME);
        Undo.RegisterCreatedObjectUndo(root, "Generate Airplane 747 Level");

        Transform wallsParent = CreateChild(root.transform, "FuselageWalls");
        Transform seatsParent = CreateChild(root.transform, "SeatRows");
        Transform floorParent = CreateChild(root.transform, "Floor");
        Transform spawnParent = CreateChild(root.transform, "PlayerSpawns");
        Transform safeZoneParent = CreateChild(root.transform, "CockpitSafeZone");

        Material wallMat = CreateOpaqueMaterial(wallColor);
        Material seatMat = CreateOpaqueMaterial(seatColor);
        Material floorMat = CreateOpaqueMaterial(floorColor);
        Material safeZoneMat = CreateTransparentMaterial(safeZoneColor, safeZoneAlpha);

        string[] lines = asciiMap.Replace("\r", "").Split('\n');
        int rowCount = lines.Length;
        int spawnIndex = 0;

        for (int row = 0; row < rowCount; row++)
        {
            string line = lines[row];

            // แถวบนสุดของข้อความ (หัวเครื่อง) = ค่า Z มากสุด, แถวล่างสุด (ท้ายเครื่อง) = ค่า Z น้อยสุด
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

                    case 'C':
                        CreateSeatRow(basePos, seatsParent, seatMat);
                        CreateFloorCube(basePos, floorParent, floorMat);
                        break;

                    case '.':
                        CreateFloorCube(basePos, floorParent, floorMat);
                        break;

                    case 'S':
                        spawnIndex++;
                        CreatePlayerSpawn(basePos, spawnParent, spawnIndex);
                        CreateFloorCube(basePos, floorParent, floorMat);
                        break;

                    case 'G':
                        CreateCockpitSafeZoneCube(basePos, safeZoneParent, safeZoneMat);
                        CreateFloorCube(basePos, floorParent, floorMat);
                        break;

                    default:
                        // อักขระอื่น (เช่นช่องว่าง) ข้ามไป ไม่สร้างอะไร
                        break;
                }
            }
        }

        Selection.activeGameObject = root;
        Debug.Log($"[AirplaneLevelGenerator] สร้างฉากเสร็จแล้ว: {rowCount} แถว, " +
                  $"Player Spawn {spawnIndex} จุด, cell size {cellSize}m");
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
        cube.name = "FuselageWall";
        cube.transform.SetParent(parent);
        cube.transform.localScale = new Vector3(cellSize, wallHeight, cellSize);
        cube.transform.position = basePos + new Vector3(0f, wallHeight * 0.5f, 0f);
        ApplyMaterial(cube, mat);
        return cube;
    }

    private GameObject CreateSeatRow(Vector3 basePos, Transform parent, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "SeatRow";
        cube.transform.SetParent(parent);
        // ลดขนาดหน้าตัดลงเล็กน้อยให้เห็นช่องว่างระหว่างแถวเก้าอี้
        float footprint = cellSize * 0.85f;
        cube.transform.localScale = new Vector3(footprint, seatHeight, footprint);
        cube.transform.position = basePos + new Vector3(0f, seatHeight * 0.5f, 0f);
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

    private GameObject CreatePlayerSpawn(Vector3 basePos, Transform parent, int index)
    {
        // จุดเกิดผู้เล่นท้ายเครื่องบิน: Empty GameObject ชื่อ "PlayerSpawn"
        GameObject spawn = new GameObject("PlayerSpawn");
        spawn.transform.SetParent(parent);
        spawn.transform.position = basePos + new Vector3(0f, 0.1f, 0f);
        // ให้หันหน้าไปทางหัวเครื่องบิน (+Z) ซึ่งเป็นทิศที่ผู้เล่นต้องบุกไป
        spawn.transform.rotation = Quaternion.LookRotation(Vector3.forward, Vector3.up);

        AirplaneSpawnMarker marker = spawn.AddComponent<AirplaneSpawnMarker>();
        marker.gizmoColor = spawnGizmoColor;
        marker.label = index > 1 ? $"Player Spawn {index}" : "Player Spawn";

        return spawn;
    }

    private GameObject CreateCockpitSafeZoneCube(Vector3 basePos, Transform parent, Material mat)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = "CockpitSafeZone";
        cube.transform.SetParent(parent);
        cube.transform.localScale = new Vector3(cellSize, safeZoneHeight, cellSize);
        cube.transform.position = basePos + new Vector3(0f, safeZoneHeight * 0.5f, 0f);
        ApplyMaterial(cube, mat);

        // ตั้งเป็น Trigger แทนของแข็ง เพื่อให้ผู้เล่นเดินเข้าไปยืนในโซนปลอดภัยได้จริง
        Collider col = cube.GetComponent<Collider>();
        if (col != null)
        {
            col.isTrigger = true;
        }

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

    private Material CreateOpaqueMaterial(Color color)
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        if (shader == null) shader = Shader.Find("Diffuse");

        Material mat = new Material(shader);
        mat.color = color;
        return mat;
    }

    /// <summary>
    /// สร้าง Material แบบโปร่งแสง รองรับทั้ง Universal Render Pipeline (Lit) และ Built-in Standard Shader
    /// หมายเหตุ: หากใช้ HDRP หรือ Shader Graph กำหนดเอง อาจต้องปรับ Rendering Mode เป็น Transparent เองใน Inspector
    /// </summary>
    private Material CreateTransparentMaterial(Color color, float alpha)
    {
        Shader urpShader = Shader.Find("Universal Render Pipeline/Lit");
        Color c = color;
        c.a = alpha;

        if (urpShader != null)
        {
            Material urpMat = new Material(urpShader);
            urpMat.color = c;
            urpMat.SetFloat("_Surface", 1f); // 1 = Transparent
            urpMat.SetFloat("_Blend", 0f);   // 0 = Alpha
            urpMat.SetInt("_ZWrite", 0);
            urpMat.SetOverrideTag("RenderType", "Transparent");
            urpMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            urpMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            urpMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            urpMat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            urpMat.DisableKeyword("_ALPHATEST_ON");
            urpMat.EnableKeyword("_ALPHABLEND_ON");
            return urpMat;
        }

        Shader standardShader = Shader.Find("Standard");
        if (standardShader != null)
        {
            Material stdMat = new Material(standardShader);
            stdMat.color = c;
            stdMat.SetFloat("_Mode", 3f); // 3 = Transparent
            stdMat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            stdMat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            stdMat.SetInt("_ZWrite", 0);
            stdMat.DisableKeyword("_ALPHATEST_ON");
            stdMat.EnableKeyword("_ALPHABLEND_ON");
            stdMat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            stdMat.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
            return stdMat;
        }

        // fallback สุดท้าย
        Material fallback = new Material(Shader.Find("Diffuse"));
        fallback.color = c;
        return fallback;
    }
}

/// <summary>
/// Component ติดไว้ที่ Object "PlayerSpawn" เพื่อวาด Gizmo ใน Scene View ให้เห็นตำแหน่งชัดเจน
/// (ไม่มีผลใดๆ ตอน Runtime/Build) ตั้งชื่อไม่ให้ชนกับคลาส marker ของสคริปต์ generator ตัวอื่น
/// </summary>
public class AirplaneSpawnMarker : MonoBehaviour
{
    public Color gizmoColor = Color.blue;
    public string label = "Player Spawn";

    private void OnDrawGizmos()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, 0.5f);
        Gizmos.DrawLine(transform.position, transform.position + Vector3.up * 1.8f);

        Vector3 tip = transform.position + transform.forward * 1f;
        Gizmos.DrawLine(transform.position, tip);

#if UNITY_EDITOR
        UnityEditor.Handles.Label(transform.position + Vector3.up * 2f, label);
#endif
    }
}