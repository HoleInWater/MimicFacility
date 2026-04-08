#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using MimicFacility.UI;

public class IntroSceneBuilder
{
    static readonly Color COL_CONCRETE   = new Color(0.28f, 0.28f, 0.30f);
    static readonly Color COL_CONCRETE_D = new Color(0.18f, 0.18f, 0.20f);
    static readonly Color COL_METAL      = new Color(0.35f, 0.35f, 0.40f);
    static readonly Color COL_RUST       = new Color(0.40f, 0.22f, 0.12f);
    static readonly Color COL_GROUND     = new Color(0.12f, 0.11f, 0.10f);
    static readonly Color COL_SPORE      = new Color(0.30f, 0.45f, 0.20f, 0.6f);
    static readonly Color COL_SCREEN     = new Color(0.05f, 0.20f, 0.05f);
    static readonly Color COL_TITLE      = new Color(0.8f, 0.1f, 0.1f, 0.9f);
    static readonly Color COL_WIRE       = new Color(0.10f, 0.10f, 0.12f);

    [MenuItem("MimicFacility/Scenes/Build Intro Sequence Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!EditorUtility.DisplayDialog("Build Intro Scene",
            "This creates the full cinematic intro scene.\nContinue?", "Build", "Cancel"))
            return;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.02f;
        RenderSettings.fogColor = new Color(0.03f, 0.03f, 0.04f);
        RenderSettings.ambientLight = new Color(0.03f, 0.03f, 0.05f);

        // ── Camera ───────────────────────────────────────────────────────
        var camObj = new GameObject("IntroCamera");
        var cam = camObj.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = Color.black;
        cam.fieldOfView = 65f;
        cam.farClipPlane = 500f;
        camObj.AddComponent<AudioListener>();
        var camCtrl = camObj.AddComponent<IntroCameraController>();

        // ── Audio ────────────────────────────────────────────────────────
        var audioObj = new GameObject("MusicSource");
        var musicSource = audioObj.AddComponent<AudioSource>();
        musicSource.playOnAwake = false;
        musicSource.loop = false;

        // ════════════════════════════════════════════════════════════════
        // PHASE 1: FACILITY EXTERIOR — abandoned brutalist building at night
        // ════════════════════════════════════════════════════════════════
        var exterior = new GameObject("FacilityExterior");

        // Ground
        Prim(exterior.transform, "Ground", Vector3.zero, new Vector3(80, 0.1f, 80), COL_GROUND);

        // Main building facade
        Prim(exterior.transform, "Facade", new Vector3(0, 6, 20), new Vector3(24, 12, 1), COL_CONCRETE);
        Prim(exterior.transform, "FacadeLeft", new Vector3(-12, 6, 15), new Vector3(1, 12, 10), COL_CONCRETE_D);
        Prim(exterior.transform, "FacadeRight", new Vector3(12, 6, 15), new Vector3(1, 12, 10), COL_CONCRETE_D);

        // Entrance — dark opening
        Prim(exterior.transform, "EntranceDoor", new Vector3(0, 1.5f, 19.4f), new Vector3(3, 3, 0.5f), Color.black);

        // Broken windows (dark rectangles on facade)
        for (int i = -2; i <= 2; i++)
        {
            if (i == 0) continue;
            Prim(exterior.transform, "Window", new Vector3(i * 4, 8, 19.4f), new Vector3(1.5f, 2, 0.3f),
                new Color(0.02f, 0.02f, 0.04f));
        }

        // Perimeter fence posts
        for (int i = -5; i <= 5; i++)
        {
            Prim(exterior.transform, "FencePost", new Vector3(i * 3, 1, 5), new Vector3(0.1f, 2, 0.1f), COL_METAL);
        }
        // Chain link (flat stretched cube)
        Prim(exterior.transform, "ChainLink", new Vector3(0, 1, 5), new Vector3(30, 1.8f, 0.05f), new Color(0.3f, 0.3f, 0.3f, 0.5f));

        // Warning signs (small cubes with color)
        Prim(exterior.transform, "Sign1", new Vector3(-6, 1.5f, 4.8f), new Vector3(0.8f, 0.6f, 0.05f), Color.yellow);
        Prim(exterior.transform, "Sign2", new Vector3(6, 1.5f, 4.8f), new Vector3(0.8f, 0.6f, 0.05f), Color.red);

        // Dead trees
        Stump(exterior.transform, new Vector3(-10, 0, 8), 3f);
        Stump(exterior.transform, new Vector3(8, 0, 3), 2.5f);
        Stump(exterior.transform, new Vector3(-14, 0, 2), 4f);

        // Searchlight (broken, tilted)
        var searchlight = new GameObject("BrokenSearchlight");
        searchlight.transform.SetParent(exterior.transform);
        searchlight.transform.position = new Vector3(8, 5, 10);
        var sl = searchlight.AddComponent<Light>();
        sl.type = LightType.Spot;
        sl.color = new Color(0.9f, 0.8f, 0.6f);
        sl.intensity = 5f;
        sl.range = 30f;
        sl.spotAngle = 25f;
        searchlight.transform.rotation = Quaternion.Euler(45, -30, 15);

        // Spore particles
        var sporePS = CreateSporeParticles(exterior.transform, new Vector3(0, 3, 12));

        // Fog particles
        var fogPS = CreateFogParticles(exterior.transform, new Vector3(0, 0.5f, 10));

        // ════════════════════════════════════════════════════════════════
        // PHASE 3: CORRIDOR — brutalist hallway with flickering lights
        // ════════════════════════════════════════════════════════════════
        var corridor = new GameObject("CorridorScene");

        Prim(corridor.transform, "Floor", Vector3.zero, new Vector3(4, 0.1f, 40), COL_CONCRETE_D);
        Prim(corridor.transform, "Ceiling", Vector3.up * 3.5f, new Vector3(4, 0.1f, 40), COL_CONCRETE_D);
        Prim(corridor.transform, "WallL", new Vector3(-2, 1.75f, 0), new Vector3(0.3f, 3.5f, 40), COL_CONCRETE);
        Prim(corridor.transform, "WallR", new Vector3(2, 1.75f, 0), new Vector3(0.3f, 3.5f, 40), COL_CONCRETE);

        // Flickering ceiling lights
        for (int i = 0; i < 8; i++)
        {
            var lightObj = new GameObject($"CeilingLight_{i}");
            lightObj.transform.SetParent(corridor.transform);
            lightObj.transform.position = new Vector3(0, 3.2f, i * 5 - 5);
            var cLight = lightObj.AddComponent<Light>();
            cLight.type = LightType.Point;
            cLight.color = new Color(0.7f, 0.8f, 0.9f);
            cLight.intensity = i % 3 == 0 ? 0.3f : 1.0f;
            cLight.range = 6f;

            // Light fixture visual
            Prim(corridor.transform, "Fixture", lightObj.transform.position + Vector3.up * 0.2f,
                new Vector3(0.4f, 0.05f, 0.4f), COL_METAL);
        }

        // Pipes along ceiling
        Prim(corridor.transform, "PipeL", new Vector3(-1.5f, 3.3f, 0), new Vector3(0.1f, 0.1f, 40), COL_RUST);
        Prim(corridor.transform, "PipeR", new Vector3(1.5f, 3.3f, 0), new Vector3(0.1f, 0.1f, 40), COL_RUST);

        // Spore stains on walls
        for (int i = 0; i < 5; i++)
        {
            float z = Random.Range(-15f, 15f);
            float side = Random.value > 0.5f ? -1.8f : 1.8f;
            Prim(corridor.transform, "SporeStain", new Vector3(side, Random.Range(0.5f, 2f), z),
                new Vector3(0.02f, Random.Range(0.3f, 1f), Random.Range(0.3f, 0.8f)),
                new Color(0.15f, 0.25f, 0.10f, 0.7f));
        }

        // ════════════════════════════════════════════════════════════════
        // PHASE 4: CONTROL ROOM — the Director's domain
        // ════════════════════════════════════════════════════════════════
        var controlRoom = new GameObject("ControlRoomScene");

        // Room shell
        Prim(controlRoom.transform, "Floor", Vector3.zero, new Vector3(12, 0.1f, 12), COL_CONCRETE_D);
        Prim(controlRoom.transform, "Ceiling", Vector3.up * 4, new Vector3(12, 0.1f, 12), COL_CONCRETE_D);
        Prim(controlRoom.transform, "WallN", new Vector3(0, 2, 6), new Vector3(12, 4, 0.3f), COL_CONCRETE);
        Prim(controlRoom.transform, "WallS", new Vector3(0, 2, -6), new Vector3(12, 4, 0.3f), COL_CONCRETE);
        Prim(controlRoom.transform, "WallE", new Vector3(6, 2, 0), new Vector3(0.3f, 4, 12), COL_CONCRETE);
        Prim(controlRoom.transform, "WallW", new Vector3(-6, 2, 0), new Vector3(0.3f, 4, 12), COL_CONCRETE);

        // Main monitor bank (the Director watches from here)
        for (int i = -2; i <= 2; i++)
        {
            var screen = Prim(controlRoom.transform, $"Screen_{i}",
                new Vector3(i * 1.5f, 2.5f, 5.7f), new Vector3(1.2f, 0.8f, 0.1f), COL_SCREEN);
            SetEmission(screen, new Color(0.05f, 0.3f, 0.05f));
        }

        // Console desk
        Prim(controlRoom.transform, "Console", new Vector3(0, 1, 4.5f), new Vector3(8, 0.1f, 2), COL_METAL);
        // Console legs
        Prim(controlRoom.transform, "Leg1", new Vector3(-3.5f, 0.5f, 4.5f), new Vector3(0.1f, 1, 0.1f), COL_METAL);
        Prim(controlRoom.transform, "Leg2", new Vector3(3.5f, 0.5f, 4.5f), new Vector3(0.1f, 1, 0.1f), COL_METAL);

        // Server racks along walls
        for (int i = 0; i < 4; i++)
        {
            Prim(controlRoom.transform, $"Rack_{i}", new Vector3(-5.5f, 1.5f, -4 + i * 2.5f),
                new Vector3(0.6f, 3, 1), COL_WIRE);
            // Blinking lights on racks
            var rackLight = new GameObject($"RackLight_{i}");
            rackLight.transform.SetParent(controlRoom.transform);
            rackLight.transform.position = new Vector3(-5.2f, 2f, -4 + i * 2.5f);
            var rl = rackLight.AddComponent<Light>();
            rl.type = LightType.Point;
            rl.color = i % 2 == 0 ? Color.green : Color.red;
            rl.intensity = 0.5f;
            rl.range = 1.5f;
        }

        // Cables on floor
        for (int i = 0; i < 6; i++)
        {
            Prim(controlRoom.transform, $"Cable_{i}",
                new Vector3(Random.Range(-4f, 4f), 0.05f, Random.Range(-4f, 4f)),
                new Vector3(0.03f, 0.03f, Random.Range(2f, 5f)), COL_WIRE);
        }

        // Central overhead light (dim, red tint)
        var centralLight = new GameObject("CentralLight");
        centralLight.transform.SetParent(controlRoom.transform);
        centralLight.transform.position = Vector3.up * 3.5f;
        var cl = centralLight.AddComponent<Light>();
        cl.type = LightType.Point;
        cl.color = new Color(0.9f, 0.3f, 0.2f);
        cl.intensity = 1.5f;
        cl.range = 10f;

        // ════════════════════════════════════════════════════════════════
        // UI CANVAS
        // ════════════════════════════════════════════════════════════════
        var canvasObj = new GameObject("IntroCanvas");
        var canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // Black overlay
        var blackObj = CreateUIPanel(canvasObj.transform, "BlackOverlay", Color.black);
        var blackGroup = blackObj.AddComponent<CanvasGroup>();
        blackGroup.alpha = 1f;

        // Studio logo
        var logoObj = CreateUIPanel(canvasObj.transform, "StudioLogo", Color.clear);
        var logoGroup = logoObj.AddComponent<CanvasGroup>();
        logoGroup.alpha = 0f;
        var logoText = CreateTMPText(logoObj.transform, "LogoText", "HOLEINWATER", 48,
            new Color(0.9f, 0.9f, 0.9f), TextAlignmentOptions.Center);

        // Credit text
        var creditObj = CreateUIPanel(canvasObj.transform, "CreditPanel", Color.clear);
        var creditGroup = creditObj.AddComponent<CanvasGroup>();
        creditGroup.alpha = 0f;
        var credText = CreateTMPText(creditObj.transform, "CreditText", "", 28,
            new Color(0.8f, 0.8f, 0.8f, 0.9f), TextAlignmentOptions.Center);

        // Title group
        var titleObj = CreateUIPanel(canvasObj.transform, "TitlePanel", Color.clear);
        var titleGroupCG = titleObj.AddComponent<CanvasGroup>();
        titleGroupCG.alpha = 0f;
        var titleTMP = CreateTMPText(titleObj.transform, "TitleText", "MIMIC", 120,
            COL_TITLE, TextAlignmentOptions.Center);

        // Subtitle
        CreateTMPText(titleObj.transform, "Subtitle",
            "The facility is listening.", 24,
            new Color(0.7f, 0.7f, 0.7f, 0.8f), TextAlignmentOptions.Center,
            new Vector2(0, -80));

        // Skip hint
        CreateTMPText(canvasObj.transform, "SkipHint",
            "Press ESC or SPACE to skip", 14,
            new Color(0.5f, 0.5f, 0.5f, 0.4f), TextAlignmentOptions.BottomRight,
            new Vector2(-20, 20));

        // Glitch wipe panel
        var wipeObj = CreateUIPanel(canvasObj.transform, "GlitchWipe", new Color(0.02f, 0.02f, 0.02f));
        wipeObj.SetActive(false);
        var wipeRT = wipeObj.GetComponent<RectTransform>();

        // ════════════════════════════════════════════════════════════════
        // SEQUENCE CONTROLLER
        // ════════════════════════════════════════════════════════════════
        var controllerObj = new GameObject("IntroSequenceController");
        var tsc = controllerObj.AddComponent<IntroSequenceController>();

        tsc.musicSource = musicSource;
        tsc.blackOverlay = blackGroup;
        tsc.facilityExteriorScene = exterior;
        tsc.sporeParticles = sporePS;
        tsc.fogParticles = fogPS;
        tsc.studioLogoGroup = logoGroup;
        tsc.corridorScene = corridor;
        tsc.controlRoomScene = controlRoom;
        tsc.cameraController = camCtrl;
        tsc.titleGroup = titleGroupCG;
        tsc.titleText = titleTMP;
        tsc.creditText = credText;
        tsc.creditTextGroup = creditGroup;
        tsc.glitchWipePanel = wipeRT;

        // ── Mark scene dirty ─────────────────────────────────────────────
        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log("[IntroSceneBuilder] Intro scene built successfully.");
        Debug.Log("  Phase 1: Abandoned facility exterior with spores and fog");
        Debug.Log("  Phase 3: Brutalist corridor with flickering lights");
        Debug.Log("  Phase 4: Director's control room with monitors and server racks");
        Debug.Log("  Phase 5: MIMIC title with glitch wipe transition");
        Debug.Log("  Save the scene to Assets/Scenes/IntroScene.unity");
    }

    // ── Helpers ───────────────────────────────────────────────────────────

    static GameObject Prim(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
    {
        var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        obj.name = name;
        obj.transform.SetParent(parent);
        obj.transform.position = pos;
        obj.transform.localScale = scale;
        obj.isStatic = true;
        var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
        obj.GetComponent<Renderer>().material = new Material(shader) { color = color };
        return obj;
    }

    static void Stump(Transform parent, Vector3 pos, float height)
    {
        var trunk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        trunk.name = "DeadTree";
        trunk.transform.SetParent(parent);
        trunk.transform.position = pos + Vector3.up * height / 2f;
        trunk.transform.localScale = new Vector3(0.15f, height / 2f, 0.15f);
        trunk.transform.rotation = Quaternion.Euler(0, 0, Random.Range(-5f, 5f));
        trunk.isStatic = true;
        var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
        trunk.GetComponent<Renderer>().material = new Material(shader) { color = new Color(0.15f, 0.12f, 0.08f) };
    }

    static void SetEmission(GameObject obj, Color emissionColor)
    {
        var r = obj.GetComponent<Renderer>();
        if (r == null) return;
        r.sharedMaterial.EnableKeyword("_EMISSION");
        r.sharedMaterial.SetColor("_EmissionColor", emissionColor);
    }

    static ParticleSystem CreateSporeParticles(Transform parent, Vector3 position)
    {
        var obj = new GameObject("SporeParticles");
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        var ps = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = COL_SPORE;
        main.startSize = 0.1f;
        main.startLifetime = 8f;
        main.startSpeed = 0.3f;
        main.maxParticles = 200;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        var emission = ps.emission;
        emission.rateOverTime = 20f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(20, 8, 20);

        return ps;
    }

    static ParticleSystem CreateFogParticles(Transform parent, Vector3 position)
    {
        var obj = new GameObject("FogParticles");
        obj.transform.SetParent(parent);
        obj.transform.position = position;
        var ps = obj.AddComponent<ParticleSystem>();
        var main = ps.main;
        main.startColor = new Color(0.1f, 0.1f, 0.12f, 0.15f);
        main.startSize = 5f;
        main.startLifetime = 12f;
        main.startSpeed = 0.2f;
        main.maxParticles = 50;

        var emission = ps.emission;
        emission.rateOverTime = 4f;

        var shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(30, 1, 30);

        return ps;
    }

    static GameObject CreateUIPanel(Transform parent, string name, Color color)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = Vector2.zero;

        var img = obj.AddComponent<Image>();
        img.color = color;
        img.raycastTarget = false;

        return obj;
    }

    static TextMeshProUGUI CreateTMPText(Transform parent, string name, string text,
        int fontSize, Color color, TextAlignmentOptions alignment, Vector2? offset = null)
    {
        var obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        var rt = obj.AddComponent<RectTransform>();
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.sizeDelta = Vector2.zero;
        rt.anchoredPosition = offset ?? Vector2.zero;

        var tmp = obj.AddComponent<TextMeshProUGUI>();
        tmp.text = text;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;

        return tmp;
    }
}
#endif
