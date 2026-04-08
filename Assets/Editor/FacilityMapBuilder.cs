#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Mirror;
using MimicFacility.Facility;

public class FacilityMapBuilder
{
    static readonly Color COL_FLOOR      = new Color(0.22f, 0.22f, 0.24f);
    static readonly Color COL_WALL       = new Color(0.32f, 0.32f, 0.35f);
    static readonly Color COL_CEILING    = new Color(0.18f, 0.18f, 0.20f);
    static readonly Color COL_DOOR       = new Color(0.38f, 0.28f, 0.18f);
    static readonly Color COL_TERMINAL   = new Color(0.08f, 0.08f, 0.08f);
    static readonly Color COL_VENT       = new Color(0.15f, 0.20f, 0.10f);
    static readonly Color COL_PIPE       = new Color(0.35f, 0.20f, 0.12f);
    static readonly Color COL_METAL      = new Color(0.30f, 0.30f, 0.35f);
    static readonly Color COL_BLOOD      = new Color(0.25f, 0.05f, 0.03f);
    static readonly Color COL_EXTRACT    = new Color(0.15f, 0.15f, 0.18f);

    static Transform root;
    static float H = 4f;

    [MenuItem("MimicFacility/Build Facility Map")]
    public static void Build()
    {
        if (!EditorUtility.DisplayDialog("Build Facility",
            "This builds a full research facility with 12 rooms.\nContinue?", "Build", "Cancel"))
            return;

        var existing = GameObject.Find("Facility");
        if (existing != null) Undo.DestroyObjectImmediate(existing);

        root = new GameObject("Facility").transform;
        Undo.RegisterCreatedObjectUndo(root.gameObject, "Build Facility");

        // Fog + ambient
        RenderSettings.ambientLight = new Color(0.03f, 0.03f, 0.05f);
        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.025f;
        RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.03f);

        // ═══════════════════════════════════════════════════════════════
        // LAYOUT — 12 rooms connected by corridors
        //
        //  [Reception]---[Hub]---[Lab A]---[Lab B]
        //                  |                  |
        //              [Security]         [Storage]
        //                  |                  |
        //  [Medbay]---[Corridor]---[Server]---[Containment]
        //                  |
        //              [Director]
        //                  |
        //              [Extract]
        // ═══════════════════════════════════════════════════════════════

        // Row 1
        Room("Reception",   -32, 0,  12, 8,  true, false, true, false);
        Room("MainHub",     -16, 0,  14, 14, true, true,  true, true);
        Room("LabA",          2, 0,  10, 10, true, false, true, true);
        Room("LabB",         16, 0,  10, 10, false, false, true, true);

        // Corridors Row 1
        Corridor(-26, 0, 4, 3, true);   // Reception → Hub
        Corridor(-2, 0, 4, 3, true);    // Hub → Lab A
        Corridor(12, 0, 4, 3, true);    // Lab A → Lab B

        // Column from Hub down
        Room("Security",    -16, -16, 10, 8, true, true, false, false);
        Corridor(-16, -7, 3, 2, false); // Hub → Security

        // Column from LabB down
        Room("Storage",      16, -16, 8, 8, false, false, true, false);
        Corridor(16, -7, 3, 2, false);  // LabB → Storage

        // Row 2
        Room("Medbay",      -32, -32, 10, 8, true, false, false, true);
        Room("LowerCorridor",-16,-32, 14, 6, true, true, false, false);
        Room("ServerRoom",    2, -32, 10, 10, true, false, true, false);
        Room("Containment",  16, -32, 12, 10, false, false, false, true);

        // Corridors Row 2
        Corridor(-26, -32, 4, 3, true);   // Medbay → LowerCorridor
        Corridor(-2, -32, 4, 3, true);    // LowerCorridor → ServerRoom
        Corridor(12, -32, 4, 3, true);    // ServerRoom → Containment

        // Vertical connections
        Corridor(-16, -23, 3, 2, false);  // Security → LowerCorridor
        Corridor(16, -23, 3, 2, false);   // Storage → Containment

        // Director's Chamber (below LowerCorridor)
        Room("DirectorChamber", -16, -48, 12, 10, false, true, false, false);
        Corridor(-16, -38, 3, 4, false);  // LowerCorridor → Director

        // Extraction (below Director)
        BuildExtraction(-16, -62);
        Corridor(-16, -53, 3, 4, false);  // Director → Extraction

        // ═══════════════════════════════════════════════════════════════
        // ROOM-SPECIFIC DETAILS
        // ═══════════════════════════════════════════════════════════════

        // Reception — front desk, broken chairs
        Detail("FrontDesk", -32, 0.5f, -1, 3, 1, 1.5f, COL_METAL);
        Detail("Chair1", -34, 0.3f, 2, 0.5f, 0.6f, 0.5f, COL_METAL);
        Detail("Chair2", -30, 0.3f, 2, 0.5f, 0.6f, 0.5f, COL_METAL);

        // Main Hub — central pillar, overhead catwalk supports
        Detail("CentralPillar", -16, 2, 0, 1, 4, 1, COL_WALL);
        Detail("Catwalk1", -16, 3.5f, -3, 14, 0.1f, 2, COL_METAL);
        Detail("Catwalk2", -16, 3.5f, 3, 14, 0.1f, 2, COL_METAL);

        // Lab A — tables, equipment
        Detail("LabTable1", 2, 0.5f, -2, 3, 1, 1.5f, COL_METAL);
        Detail("LabTable2", 2, 0.5f, 2, 3, 1, 1.5f, COL_METAL);
        AddTerminal(root, 5, 1, -4.5f, "LabA");

        // Lab B — containment tanks (cylinders)
        for (int i = 0; i < 3; i++)
        {
            var tank = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            tank.name = $"Tank_{i}";
            tank.transform.SetParent(root);
            tank.transform.position = new Vector3(14 + i * 2, 1.5f, 0);
            tank.transform.localScale = new Vector3(1, 3, 1);
            tank.isStatic = true;
            ApplyColor(tank, new Color(0.1f, 0.15f, 0.1f, 0.8f));
        }

        // Security — desk, monitors
        Detail("SecDesk", -16, 0.5f, -16, 4, 1, 2, COL_METAL);
        AddTerminal(root, -13, 1.2f, -19.5f, "Security");
        AddTerminal(root, -18, 1.2f, -19.5f, "Security");

        // Server Room — server racks
        for (int i = 0; i < 4; i++)
        {
            Detail($"Rack_{i}", 0 + i * 2.5f, 1.5f, -32, 0.8f, 3, 1.2f, new Color(0.08f, 0.08f, 0.1f));
            AddRackLight(root, 0.5f + i * 2.5f, 2f, -32, i % 2 == 0 ? Color.green : Color.red);
        }
        // Server room is cold — blue-ish light
        AddLight(root, 2, 3.5f, -32, 10, new Color(0.4f, 0.5f, 0.9f), 1.5f);

        // Containment — heavy doors, blood stains, broken equipment
        Detail("BrokenTable", 18, 0.3f, -34, 2, 0.6f, 1, COL_METAL);
        Detail("BloodStain1", 15, 0.01f, -30, 1.5f, 0.02f, 1, COL_BLOOD);
        Detail("BloodStain2", 19, 0.01f, -33, 1, 0.02f, 2, COL_BLOOD);

        // Director's Chamber — the throne room
        Detail("DirectorDesk", -16, 0.5f, -48, 5, 1, 2.5f, new Color(0.15f, 0.1f, 0.08f));
        Detail("DirectorChair", -16, 0.6f, -50, 0.8f, 1.2f, 0.8f, new Color(0.2f, 0.05f, 0.05f));
        // Big screen behind desk
        var bigScreen = Prim("DirectorScreen", -16, 2.5f, -52.7f, 8, 3, 0.1f, COL_TERMINAL);
        SetEmission(bigScreen, new Color(0.05f, 0.2f, 0.05f));
        AddTerminal(root, -12, 1, -52.5f, "Director");
        // Red overhead light
        AddLight(root, -16, 3.5f, -48, 12, new Color(0.8f, 0.15f, 0.1f), 2f);

        // Medbay — beds, medical equipment
        for (int i = 0; i < 3; i++)
        {
            Detail($"Bed_{i}", -34 + i * 3, 0.4f, -32, 2, 0.8f, 1, Color.white * 0.3f);
        }

        // Pipes throughout facility
        Pipe(-16, 3.8f, -7, 0.08f, 0.08f, 14);  // Hub ceiling N-S
        Pipe(-16, 3.8f, -23, 0.08f, 0.08f, 10);  // Hub→Security ceiling
        Pipe(-2, 3.8f, 0, 4, 0.08f, 0.08f);      // Hub→LabA ceiling
        Pipe(2, 3.8f, -32, 10, 0.08f, 0.08f);    // ServerRoom ceiling

        // Spore vents in key locations
        AddSporeVent(root, -16, 0, 0, "MainHub");         // Hub center
        AddSporeVent(root, 2, 0, -2, "LabA");             // Lab A
        AddSporeVent(root, 16, 0, -32, "Containment");    // Containment
        AddSporeVent(root, -16, 0, -48, "Director");      // Director's chamber

        // NavMesh
        var surface = root.gameObject.AddComponent<NavMeshSurface>();
        surface.collectObjects = CollectObjects.Children;
        surface.BuildNavMesh();

        EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        Debug.Log("[FacilityMapBuilder] Built 12-room facility with corridors, details, and NavMesh.");
        Debug.Log("  Rooms: Reception, MainHub, LabA, LabB, Security, Storage, Medbay, LowerCorridor, ServerRoom, Containment, DirectorChamber, Extraction");
    }

    // ═══════════════════════════════════════════════════════════════════
    // ROOM BUILDER
    // ═══════════════════════════════════════════════════════════════════

    static void Room(string name, float cx, float cz, float w, float d,
        bool doorE, bool doorS, bool doorW, bool doorN)
    {
        var room = new GameObject(name);
        room.transform.SetParent(root);
        room.transform.position = new Vector3(cx, 0, cz);

        // Floor + ceiling
        Prim(room.transform, "Floor", cx, -0.05f, cz, w, 0.1f, d, COL_FLOOR);
        Prim(room.transform, "Ceiling", cx, H, cz, w, 0.1f, d, COL_CEILING);

        float hw = w / 2f, hd = d / 2f;

        // Walls with door gaps
        if (!doorN) Wall(room.transform, cx, cz + hd, w, 0.3f);
        else WallWithGap(room.transform, cx, cz + hd, w, 0.3f, true);

        if (!doorS) Wall(room.transform, cx, cz - hd, w, 0.3f);
        else WallWithGap(room.transform, cx, cz - hd, w, 0.3f, true);

        if (!doorE) WallZ(room.transform, cx + hw, cz, d, 0.3f);
        else WallZWithGap(room.transform, cx + hw, cz, d, 0.3f);

        if (!doorW) WallZ(room.transform, cx - hw, cz, d, 0.3f);
        else WallZWithGap(room.transform, cx - hw, cz, d, 0.3f);

        // Room light
        AddLight(room.transform, cx, H - 0.5f, cz, Mathf.Max(w, d) * 1.2f,
            new Color(0.7f, 0.8f, 0.9f), 1.0f);

        // Door objects at openings
        if (doorE) AddDoor(room.transform, cx + hw, cz, false, name);
        if (doorW) AddDoor(room.transform, cx - hw, cz, false, name);
        if (doorN) AddDoor(room.transform, cx, cz + hd, true, name);
        if (doorS) AddDoor(room.transform, cx, cz - hd, true, name);
    }

    static void Corridor(float cx, float cz, float length, float width, bool horizontal)
    {
        var corr = new GameObject("Corridor");
        corr.transform.SetParent(root);

        float w = horizontal ? length : width;
        float d = horizontal ? width : length;

        Prim(corr.transform, "Floor", cx, -0.05f, cz, w, 0.1f, d, COL_FLOOR);
        Prim(corr.transform, "Ceiling", cx, H, cz, w, 0.1f, d, COL_CEILING);

        if (horizontal)
        {
            Wall(corr.transform, cx, cz + width / 2f, length, 0.3f);
            Wall(corr.transform, cx, cz - width / 2f, length, 0.3f);
        }
        else
        {
            WallZ(corr.transform, cx + width / 2f, cz, length, 0.3f);
            WallZ(corr.transform, cx - width / 2f, cz, length, 0.3f);
        }

        // Dim corridor light
        AddLight(corr.transform, cx, H - 0.5f, cz, Mathf.Max(w, d),
            new Color(0.5f, 0.5f, 0.6f), 0.5f);
    }

    static void BuildExtraction(float cx, float cz)
    {
        var zone = new GameObject("Extraction");
        zone.transform.SetParent(root);

        Prim(zone.transform, "Floor", cx, -0.05f, cz, 10, 0.1f, 10, COL_EXTRACT);
        Prim(zone.transform, "Ceiling", cx, H, cz, 10, 0.1f, 10, COL_CEILING);

        Wall(zone.transform, cx, cz + 5, 10, 0.3f);
        Wall(zone.transform, cx, cz - 5, 10, 0.3f);
        WallZ(zone.transform, cx + 5, cz, 10, 0.3f);
        WallZWithGap(zone.transform, cx - 5, cz, 10, 0.3f);

        // Green extraction light
        AddLight(zone.transform, cx, H - 0.3f, cz, 12, Color.green, 3f);

        // Exit sign
        var sign = Prim("ExitSign", cx, H - 0.5f, cz + 4.7f, 2, 0.5f, 0.1f, Color.green);
        sign.transform.SetParent(zone.transform);
        SetEmission(sign, Color.green * 0.5f);

        // Extraction trigger
        var trigger = new GameObject("ExtractionZone");
        trigger.transform.SetParent(zone.transform);
        trigger.transform.position = new Vector3(cx, 1.5f, cz);
        var col = trigger.AddComponent<BoxCollider>();
        col.size = new Vector3(8, 3, 8);
        col.isTrigger = true;
    }

    // ═══════════════════════════════════════════════════════════════════
    // PRIMITIVES
    // ═══════════════════════════════════════════════════════════════════

    static GameObject Prim(string name, float x, float y, float z, float sx, float sy, float sz, Color color)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(root);
        obj.transform.position = new Vector3(x, y, z);
        obj.transform.localScale = new Vector3(sx, sy, sz);
        obj.isStatic = true;
        ApplyColor(obj, color);
        return obj;
    }

    static void Prim(Transform parent, string name, float x, float y, float z, float sx, float sy, float sz, Color color)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position = new Vector3(x, y, z);
        obj.transform.localScale = new Vector3(sx, sy, sz);
        obj.isStatic = true;
        ApplyColor(obj, color);
    }

    static void Detail(string name, float x, float y, float z, float sx, float sy, float sz, Color color)
    {
        Prim(name, x, y, z, sx, sy, sz, color);
    }

    static void Wall(Transform parent, float cx, float cz, float length, float thickness)
    {
        Prim(parent, "Wall", cx, H / 2f, cz, length, H, thickness, COL_WALL);
    }

    static void WallWithGap(Transform parent, float cx, float cz, float length, float thickness, bool xAxis)
    {
        float gapWidth = 2f;
        float sideLen = (length - gapWidth) / 2f;
        Prim(parent, "WallL", cx - sideLen / 2f - gapWidth / 2f, H / 2f, cz, sideLen, H, thickness, COL_WALL);
        Prim(parent, "WallR", cx + sideLen / 2f + gapWidth / 2f, H / 2f, cz, sideLen, H, thickness, COL_WALL);
        // Lintel above door
        Prim(parent, "Lintel", cx, H - 0.3f, cz, gapWidth + 0.2f, 0.6f, thickness, COL_WALL);
    }

    static void WallZ(Transform parent, float cx, float cz, float length, float thickness)
    {
        Prim(parent, "Wall", cx, H / 2f, cz, thickness, H, length, COL_WALL);
    }

    static void WallZWithGap(Transform parent, float cx, float cz, float length, float thickness)
    {
        float gapWidth = 2f;
        float sideLen = (length - gapWidth) / 2f;
        Prim(parent, "WallL", cx, H / 2f, cz - sideLen / 2f - gapWidth / 2f, thickness, H, sideLen, COL_WALL);
        Prim(parent, "WallR", cx, H / 2f, cz + sideLen / 2f + gapWidth / 2f, thickness, H, sideLen, COL_WALL);
        Prim(parent, "Lintel", cx, H - 0.3f, cz, thickness, 0.6f, gapWidth + 0.2f, COL_WALL);
    }

    static void Pipe(float x, float y, float z, float sx, float sy, float sz)
    {
        Prim("Pipe", x, y, z, sx, sy, sz, COL_PIPE);
    }

    // ═══════════════════════════════════════════════════════════════════
    // FACILITY OBJECTS
    // ═══════════════════════════════════════════════════════════════════

    static void AddDoor(Transform parent, float x, float z, bool xAligned, string zone)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = $"Door_{zone}";
        obj.transform.SetParent(parent);
        obj.transform.position = new Vector3(x, H / 2f, z);
        obj.transform.localScale = xAligned ? new Vector3(2, H - 0.6f, 0.15f) : new Vector3(0.15f, H - 0.6f, 2);
        ApplyColor(obj, COL_DOOR);
        obj.AddComponent<NetworkIdentity>();
        obj.AddComponent<AudioSource>();
        obj.AddComponent<FacilityDoor>();
    }

    static void AddLight(Transform parent, float x, float y, float z, float range, Color color, float intensity)
    {
        var obj = new GameObject("Light");
        obj.transform.SetParent(parent);
        obj.transform.position = new Vector3(x, y, z);
        var l = obj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = intensity;
        l.range = range;
        obj.AddComponent<NetworkIdentity>();
        obj.AddComponent<FacilityLight>();

        // Fixture visual
        Prim(obj.transform, "Fixture", x, y + 0.15f, z, 0.4f, 0.05f, 0.4f, COL_METAL);
    }

    static void AddRackLight(Transform parent, float x, float y, float z, Color color)
    {
        var obj = new GameObject("RackLED");
        obj.transform.SetParent(parent);
        obj.transform.position = new Vector3(x, y, z);
        var l = obj.AddComponent<Light>();
        l.type = LightType.Point;
        l.color = color;
        l.intensity = 0.3f;
        l.range = 1f;
    }

    static void AddTerminal(Transform parent, float x, float y, float z, string zone)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = $"Terminal_{zone}";
        obj.transform.SetParent(parent);
        obj.transform.position = new Vector3(x, y, z);
        obj.transform.localScale = new Vector3(0.8f, 1.2f, 0.2f);
        obj.isStatic = true;
        ApplyColor(obj, COL_TERMINAL);
        SetEmission(obj, new Color(0.03f, 0.15f, 0.03f));
        obj.AddComponent<NetworkIdentity>();
        obj.AddComponent<AudioSource>();
        obj.AddComponent<ResearchTerminal>();
    }

    static void AddSporeVent(Transform parent, float x, float y, float z, string zone)
    {
        var obj = new GameObject($"SporeVent_{zone}");
        obj.transform.SetParent(parent);
        obj.transform.position = new Vector3(x, y + 0.1f, z);
        var sc = obj.AddComponent<SphereCollider>();
        sc.radius = 4f;
        sc.isTrigger = true;
        obj.AddComponent<ParticleSystem>();
        obj.AddComponent<NetworkIdentity>();
        obj.AddComponent<AudioSource>();
        obj.AddComponent<SporeVent>();

        // Vent grate visual
        var grate = GameObject.CreatePrimitive(PrimitiveType.Cube);
        grate.name = "VentGrate";
        grate.transform.SetParent(obj.transform);
        grate.transform.localPosition = Vector3.zero;
        grate.transform.localScale = new Vector3(1, 0.05f, 1);
        ApplyColor(grate, COL_VENT);
    }

    // ═══════════════════════════════════════════════════════════════════
    // HELPERS
    // ═══════════════════════════════════════════════════════════════════

    static void ApplyColor(GameObject obj, Color color)
    {
        var r = obj.GetComponent<Renderer>();
        if (r == null) return;
        var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
        r.material = new Material(shader) { color = color };
    }

    static void SetEmission(GameObject obj, Color emissionColor)
    {
        var r = obj.GetComponent<Renderer>();
        if (r == null) return;
        r.sharedMaterial.EnableKeyword("_EMISSION");
        r.sharedMaterial.SetColor("_EmissionColor", emissionColor);
    }
}
#endif
