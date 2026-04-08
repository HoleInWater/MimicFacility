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
    static readonly Color COL_PIPE       = new Color(0.30f, 0.18f, 0.10f);
    static readonly Color COL_BLOOD      = new Color(0.25f, 0.05f, 0.03f);
    static readonly Color COL_DOOR       = new Color(0.38f, 0.28f, 0.18f);

    [MenuItem("MimicFacility/Scenes/Build Intro Sequence Scene")]
    public static void Build()
    {
        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
        if (!EditorUtility.DisplayDialog("Build Intro Scene",
            "This creates the full cinematic intro scene.\nContinue?", "Build", "Cancel"))
            return;

        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        RenderSettings.fog = true;
        RenderSettings.fogMode = FogMode.Linear;
        RenderSettings.fogStartDistance = 5f;
        RenderSettings.fogEndDistance = 80f;
        RenderSettings.fogColor = new Color(0.01f, 0.01f, 0.02f);
        RenderSettings.ambientLight = new Color(0.02f, 0.02f, 0.03f);

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
        // Audio corruptor — song degrades over time like a dying machine
        audioObj.AddComponent<AudioCorruptor>();

        // ════════════════════════════════════════════════════════════════
        // PHASE 1: FACILITY EXTERIOR — abandoned brutalist building at night
        // ════════════════════════════════════════════════════════════════
        var exterior = new GameObject("FacilityExterior");
        var ext = exterior.transform;

        // Ground — cracked concrete lot with color variation
        Prim(ext, "Ground", Vector3.zero, new Vector3(100, 0.1f, 100), COL_GROUND);
        Prim(ext, "GroundStain1", new Vector3(-8, 0.01f, 5), new Vector3(6, 0.01f, 4), new Color(0.08f, 0.08f, 0.06f));
        Prim(ext, "GroundStain2", new Vector3(5, 0.01f, -3), new Vector3(4, 0.01f, 3), new Color(0.10f, 0.07f, 0.05f));
        Prim(ext, "GroundCrack1", new Vector3(-3, 0.02f, 2), new Vector3(8, 0.01f, 0.05f), new Color(0.05f, 0.05f, 0.04f));
        Prim(ext, "GroundCrack2", new Vector3(2, 0.02f, -5), new Vector3(0.05f, 0.01f, 10), new Color(0.05f, 0.05f, 0.04f));

        // Main building — multi-section brutalist facade
        Prim(ext, "FacadeMain", new Vector3(0, 7, 20), new Vector3(28, 14, 1.5f), COL_CONCRETE);
        Prim(ext, "FacadeWingL", new Vector3(-14, 5, 15), new Vector3(1.5f, 10, 12), COL_CONCRETE_D);
        Prim(ext, "FacadeWingR", new Vector3(14, 5, 15), new Vector3(1.5f, 10, 12), COL_CONCRETE_D);
        // Upper overhang — brutalist cantilever
        Prim(ext, "Overhang", new Vector3(0, 13, 18), new Vector3(32, 1, 5), COL_CONCRETE_D);
        // Pillars at entrance
        Prim(ext, "PillarL", new Vector3(-4, 5, 18), new Vector3(0.8f, 10, 0.8f), COL_CONCRETE);
        Prim(ext, "PillarR", new Vector3(4, 5, 18), new Vector3(0.8f, 10, 0.8f), COL_CONCRETE);
        // Roof structure
        Prim(ext, "Roof", new Vector3(0, 14, 14), new Vector3(30, 0.5f, 14), COL_CONCRETE_D);
        // Side walls extending back
        Prim(ext, "SideWallL", new Vector3(-14, 5, 10), new Vector3(1, 10, 20), COL_CONCRETE_D);
        Prim(ext, "SideWallR", new Vector3(14, 5, 10), new Vector3(1, 10, 20), COL_CONCRETE_D);

        // Entrance — dark void with steps
        Prim(ext, "EntranceVoid", new Vector3(0, 2, 19.2f), new Vector3(4, 4, 0.5f), new Color(0.01f, 0.01f, 0.01f));
        Prim(ext, "Step1", new Vector3(0, 0.15f, 17), new Vector3(6, 0.3f, 1.5f), COL_CONCRETE);
        Prim(ext, "Step2", new Vector3(0, 0.35f, 17.8f), new Vector3(5, 0.3f, 1), COL_CONCRETE);
        Prim(ext, "Step3", new Vector3(0, 0.55f, 18.4f), new Vector3(4.5f, 0.3f, 0.8f), COL_CONCRETE);

        // Windows — three rows, some dark, some faintly lit
        for (int row = 0; row < 3; row++)
        for (int col = -3; col <= 3; col++)
        {
            if (col == 0 && row == 0) continue; // entrance gap
            float wy = 4 + row * 3.5f;
            bool lit = (row + col) % 5 == 0;
            var win = Prim(ext, $"Window_{row}_{col}", new Vector3(col * 3.5f, wy, 19.3f),
                new Vector3(1.2f, 1.8f, 0.2f), lit ? new Color(0.08f, 0.06f, 0.02f) : new Color(0.02f, 0.02f, 0.03f));
            if (lit) SetEmission(win, new Color(0.04f, 0.03f, 0.01f));
        }

        // Perimeter fence — double row with barbed wire
        for (int i = -8; i <= 8; i++)
        {
            Prim(ext, $"FencePost_{i}", new Vector3(i * 2.5f, 1.2f, 5), new Vector3(0.08f, 2.4f, 0.08f), COL_METAL);
            if (i < 8) // horizontal bars
                Prim(ext, $"FenceBar_{i}", new Vector3(i * 2.5f + 1.25f, 2.2f, 5), new Vector3(2.5f, 0.04f, 0.04f), COL_METAL);
        }
        // Chain link sections
        Prim(ext, "ChainL", new Vector3(-10, 1, 5), new Vector3(20, 2, 0.03f), new Color(0.25f, 0.25f, 0.27f, 0.4f));
        Prim(ext, "ChainR", new Vector3(10, 1, 5), new Vector3(20, 2, 0.03f), new Color(0.25f, 0.25f, 0.27f, 0.4f));
        // Barbed wire on top
        Prim(ext, "BarbedWire", new Vector3(0, 2.5f, 5), new Vector3(40, 0.06f, 0.06f), new Color(0.4f, 0.35f, 0.3f));

        // Warning signs
        Prim(ext, "Sign_Bio", new Vector3(-5, 1.8f, 4.8f), new Vector3(0.8f, 0.6f, 0.05f), Color.yellow);
        Prim(ext, "Sign_NoEntry", new Vector3(5, 1.8f, 4.8f), new Vector3(0.8f, 0.6f, 0.05f), Color.red);
        Prim(ext, "Sign_Danger", new Vector3(0, 1.8f, 4.8f), new Vector3(1, 0.4f, 0.05f), new Color(0.8f, 0.4f, 0f));

        // Dead trees — gnarled, different heights
        Stump(ext, new Vector3(-12, 0, 8), 4f);
        Stump(ext, new Vector3(-15, 0, 3), 2.5f);
        Stump(ext, new Vector3(9, 0, 2), 3.5f);
        Stump(ext, new Vector3(16, 0, 7), 5f);
        Stump(ext, new Vector3(-8, 0, -3), 2f);
        Stump(ext, new Vector3(3, 0, -6), 1.5f);

        // Debris — fallen concrete chunks
        Prim(ext, "Debris1", new Vector3(-7, 0.3f, 12), new Vector3(1.5f, 0.6f, 1), COL_CONCRETE);
        Prim(ext, "Debris2", new Vector3(9, 0.2f, 14), new Vector3(0.8f, 0.4f, 1.2f), COL_CONCRETE_D);
        Prim(ext, "Debris3", new Vector3(-3, 0.15f, 8), new Vector3(0.5f, 0.3f, 0.5f), COL_CONCRETE);
        Prim(ext, "Debris4", new Vector3(6, 0.25f, 10), new Vector3(1, 0.5f, 0.7f), COL_CONCRETE_D);

        // Overturned vehicle (simplified)
        Prim(ext, "CarBody", new Vector3(-18, 0.8f, 8), new Vector3(4, 1.5f, 2), COL_METAL);
        Prim(ext, "CarRoof", new Vector3(-18, 1.8f, 8), new Vector3(3, 0.3f, 1.8f), COL_METAL);
        Prim(ext, "CarWheel1", new Vector3(-19.5f, 0.3f, 7), new Vector3(0.3f, 0.6f, 0.6f), new Color(0.1f, 0.1f, 0.1f));
        Prim(ext, "CarWheel2", new Vector3(-16.5f, 0.3f, 7), new Vector3(0.3f, 0.6f, 0.6f), new Color(0.1f, 0.1f, 0.1f));

        // Guard booth (abandoned)
        Prim(ext, "BoothWalls", new Vector3(12, 1.2f, 3), new Vector3(2.5f, 2.4f, 2.5f), COL_CONCRETE);
        Prim(ext, "BoothRoof", new Vector3(12, 2.5f, 3), new Vector3(3, 0.15f, 3), COL_METAL);
        Prim(ext, "BoothWindow", new Vector3(11.8f, 1.8f, 1.8f), new Vector3(1.5f, 0.8f, 0.1f), new Color(0.03f, 0.03f, 0.05f));

        // Utility poles + dangling wires
        Prim(ext, "Pole1", new Vector3(-20, 4, 0), new Vector3(0.15f, 8, 0.15f), COL_METAL);
        Prim(ext, "Pole2", new Vector3(20, 4, 0), new Vector3(0.15f, 8, 0.15f), COL_METAL);
        Prim(ext, "Wire1", new Vector3(0, 7.5f, 0), new Vector3(40, 0.02f, 0.02f), COL_WIRE);
        Prim(ext, "Wire2", new Vector3(0, 7.2f, 0), new Vector3(40, 0.02f, 0.02f), COL_WIRE);

        // Searchlights — one broken, one still on
        var searchlight = new GameObject("BrokenSearchlight");
        searchlight.transform.SetParent(ext);
        searchlight.transform.position = new Vector3(8, 6, 10);
        var sl = searchlight.AddComponent<Light>();
        sl.type = LightType.Spot;
        sl.color = new Color(0.9f, 0.8f, 0.6f);
        sl.intensity = 5f;
        sl.range = 40f;
        sl.spotAngle = 20f;
        searchlight.transform.rotation = Quaternion.Euler(45, -30, 15);
        Prim(ext, "SearchlightPole", new Vector3(8, 3, 10), new Vector3(0.15f, 6, 0.15f), COL_METAL);

        // Second searchlight — working, slowly sweeping (simulated with angled spot)
        var searchlight2 = new GameObject("WorkingSearchlight");
        searchlight2.transform.SetParent(ext);
        searchlight2.transform.position = new Vector3(-10, 8, 12);
        var sl2 = searchlight2.AddComponent<Light>();
        sl2.type = LightType.Spot;
        sl2.color = new Color(0.7f, 0.7f, 0.8f);
        sl2.intensity = 8f;
        sl2.range = 60f;
        sl2.spotAngle = 15f;
        searchlight2.transform.rotation = Quaternion.Euler(50, 20, 0);
        searchlight2.AddComponent<IntroSearchlightSweep>();
        Prim(ext, "SearchlightPole2", new Vector3(-10, 4, 12), new Vector3(0.15f, 8, 0.15f), COL_METAL);

        // Distant background buildings (silhouettes)
        Prim(ext, "BgBuilding1", new Vector3(-35, 8, 60), new Vector3(12, 16, 8), new Color(0.04f, 0.04f, 0.05f));
        Prim(ext, "BgBuilding2", new Vector3(30, 6, 70), new Vector3(10, 12, 6), new Color(0.03f, 0.03f, 0.04f));
        Prim(ext, "BgBuilding3", new Vector3(-10, 10, 80), new Vector3(15, 20, 10), new Color(0.025f, 0.025f, 0.035f));
        Prim(ext, "BgBuilding4", new Vector3(20, 5, 55), new Vector3(8, 10, 5), new Color(0.04f, 0.04f, 0.05f));

        // Red warning light on building — slow blink (simulated)
        var warningLight = new GameObject("WarningLight");
        warningLight.transform.SetParent(ext);
        warningLight.transform.position = new Vector3(0, 14.5f, 20);
        var wl = warningLight.AddComponent<Light>();
        wl.type = LightType.Point;
        wl.color = Color.red;
        wl.intensity = 2f;
        wl.range = 8f;
        warningLight.AddComponent<IntroWarningLightBlink>();

        var sporePS = CreateSporeParticles(exterior.transform, new Vector3(0, 3, 12));
        var fogPS = CreateFogParticles(exterior.transform, new Vector3(0, 0.5f, 10));

        // ════════════════════════════════════════════════════════════════
        // PHASE 3: CORRIDOR — long brutalist hallway, oppressive
        // ════════════════════════════════════════════════════════════════
        var corridor = new GameObject("CorridorScene");
        var cor = corridor.transform;

        // Main structure
        Prim(cor, "Floor", Vector3.zero, new Vector3(5, 0.1f, 50), COL_CONCRETE_D);
        Prim(cor, "Ceiling", Vector3.up * 3.5f, new Vector3(5, 0.15f, 50), COL_CONCRETE_D);
        Prim(cor, "WallL", new Vector3(-2.5f, 1.75f, 0), new Vector3(0.4f, 3.5f, 50), COL_CONCRETE);
        Prim(cor, "WallR", new Vector3(2.5f, 1.75f, 0), new Vector3(0.4f, 3.5f, 50), COL_CONCRETE);

        // Floor tile lines
        for (int i = -5; i <= 10; i++)
            Prim(cor, $"FloorLine_{i}", new Vector3(0, 0.01f, i * 4), new Vector3(5, 0.005f, 0.02f), new Color(0.15f, 0.15f, 0.17f));

        // Wall trim / baseboard
        Prim(cor, "BaseboardL", new Vector3(-2.3f, 0.1f, 0), new Vector3(0.05f, 0.2f, 50), new Color(0.2f, 0.2f, 0.22f));
        Prim(cor, "BaseboardR", new Vector3(2.3f, 0.1f, 0), new Vector3(0.05f, 0.2f, 50), new Color(0.2f, 0.2f, 0.22f));

        // Ceiling lights — alternating working/broken/missing
        for (int i = 0; i < 12; i++)
        {
            float z = i * 4 - 10;
            bool broken = i % 4 == 2;
            bool missing = i % 7 == 0;

            // Fixture housing always present
            Prim(cor, $"Fixture_{i}", new Vector3(0, 3.4f, z), new Vector3(0.5f, 0.06f, 0.3f), COL_METAL);

            if (!missing)
            {
                var lightObj = new GameObject($"CeilingLight_{i}");
                lightObj.transform.SetParent(cor);
                lightObj.transform.position = new Vector3(0, 3.2f, z);
                var cLight = lightObj.AddComponent<Light>();
                cLight.type = LightType.Point;
                cLight.color = broken ? new Color(0.5f, 0.4f, 0.3f) : new Color(0.7f, 0.8f, 0.9f);
                cLight.intensity = broken ? 0.2f : 0.8f;
                cLight.range = 5f;
                var flicker = lightObj.AddComponent<IntroLightFlicker>();
                flicker.isBroken = broken;
            }
        }

        // Pipes — multiple running along ceiling and walls
        Prim(cor, "PipeL1", new Vector3(-2.1f, 3.3f, 0), new Vector3(0.08f, 0.08f, 50), COL_RUST);
        Prim(cor, "PipeL2", new Vector3(-2.1f, 3.0f, 0), new Vector3(0.06f, 0.06f, 50), COL_PIPE);
        Prim(cor, "PipeR1", new Vector3(2.1f, 3.3f, 0), new Vector3(0.08f, 0.08f, 50), COL_RUST);
        Prim(cor, "PipeCeiling", new Vector3(0, 3.45f, 0), new Vector3(0.1f, 0.05f, 50), COL_METAL);
        // Pipe joints / brackets
        for (int i = 0; i < 6; i++)
        {
            Prim(cor, $"PipeBracketL_{i}", new Vector3(-2.1f, 3.3f, i * 8 - 15), new Vector3(0.15f, 0.15f, 0.15f), COL_METAL);
            Prim(cor, $"PipeBracketR_{i}", new Vector3(2.1f, 3.3f, i * 8 - 15), new Vector3(0.15f, 0.15f, 0.15f), COL_METAL);
        }

        // Spore stains — more of them, varied sizes
        for (int i = 0; i < 12; i++)
        {
            float z = Random.Range(-18f, 30f);
            float side = Random.value > 0.5f ? -2.3f : 2.3f;
            float h = Random.Range(0.3f, 2.5f);
            Prim(cor, $"SporeStain_{i}", new Vector3(side, h, z),
                new Vector3(0.02f, Random.Range(0.2f, 1.2f), Random.Range(0.2f, 1f)),
                new Color(0.12f + Random.Range(0f, 0.08f), 0.2f + Random.Range(0f, 0.1f), 0.08f, 0.6f));
        }

        // Blood smear on floor
        Prim(cor, "BloodSmear", new Vector3(0.5f, 0.01f, 8), new Vector3(1.5f, 0.01f, 3), COL_BLOOD);
        Prim(cor, "BloodDrip", new Vector3(1.2f, 0.01f, 12), new Vector3(0.3f, 0.01f, 0.5f), COL_BLOOD);

        // Doors along corridor (some open, some shut)
        for (int i = 0; i < 4; i++)
        {
            float z = 5 + i * 8;
            float side = i % 2 == 0 ? -2.3f : 2.3f;
            float doorAngle = i == 1 ? 0.3f : 0f; // one slightly ajar
            Prim(cor, $"DoorFrame_{i}", new Vector3(side, 1.5f, z),
                new Vector3(i % 2 == 0 ? 0.1f : 0.1f, 3, 1.2f), COL_CONCRETE_D);
            var doorObj = Prim(cor, $"Door_{i}", new Vector3(side + (i % 2 == 0 ? 0.15f : -0.15f) + doorAngle, 1.5f, z),
                new Vector3(0.08f, 2.8f, 1), COL_DOOR);
            if (i == 1) doorObj.AddComponent<IntroDoorCreak>();
        }

        // Wheelchair (toppled, rocking)
        var wheelchair = Prim(cor, "Wheelchair", new Vector3(-1, 0.3f, 15), new Vector3(0.6f, 0.8f, 0.5f), COL_METAL);
        wheelchair.AddComponent<IntroWheelchairRock>();
        Prim(cor, "WheelchairWheel", new Vector3(-1.3f, 0.3f, 15), new Vector3(0.05f, 0.5f, 0.5f), new Color(0.1f, 0.1f, 0.1f));

        // Paper / debris on floor
        for (int i = 0; i < 8; i++)
        {
            var paper = Prim(cor, $"Paper_{i}",
                new Vector3(Random.Range(-1.5f, 1.5f), 0.01f, Random.Range(-5f, 25f)),
                new Vector3(Random.Range(0.1f, 0.3f), 0.005f, Random.Range(0.1f, 0.2f)),
                new Color(0.35f, 0.33f, 0.28f));
            paper.AddComponent<IntroPaperFloat>();
        }

        // Vent grate on ceiling (something could be up there)
        var ventGrate = Prim(cor, "VentGrate", new Vector3(0, 3.48f, 20), new Vector3(0.8f, 0.02f, 0.8f), COL_METAL);
        ventGrate.AddComponent<IntroVentBreathe>();
        // Dark void behind the grate
        Prim(cor, "VentVoid", new Vector3(0, 3.55f, 20), new Vector3(0.7f, 0.3f, 0.7f), new Color(0.01f, 0.01f, 0.01f));

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
            screen.AddComponent<IntroScreenGlitch>();
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
        var logoText = CreateTMPText(logoObj.transform, "LogoText", "CRIMSON BLADE INTERACTIVE", 48,
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
        // Custom rendered INTAKE title (procedural horror font)
        var titleRendererObj = new GameObject("IntakeTitle");
        titleRendererObj.transform.SetParent(titleObj.transform, false);
        var titleRT = titleRendererObj.AddComponent<RectTransform>();
        titleRT.anchorMin = new Vector2(0.1f, 0.3f);
        titleRT.anchorMax = new Vector2(0.9f, 0.7f);
        titleRT.sizeDelta = Vector2.zero;
        var titleRenderer = titleRendererObj.AddComponent<MimicTitleRenderer>();

        // TMPro fallback for subtitle
        var titleTMP = CreateTMPText(titleObj.transform, "TitleText", "", 1,
            Color.clear, TextAlignmentOptions.Center);

        // Subtitle
        CreateTMPText(titleObj.transform, "Subtitle",
            "Anything you say can and will be used against you.", 22,
            new Color(0.7f, 0.7f, 0.7f, 0.8f), TextAlignmentOptions.Center,
            new Vector2(0, -100));

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

        // Wire audio — Daisy Bell (first computer to sing, 1961)
        var introClip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Music/DaisyBell.mp3");
        if (introClip == null)
            introClip = FindAsset<AudioClip>("DaisyBell");
        if (introClip == null)
            introClip = FindAsset<AudioClip>("First computer to sing");
        if (introClip == null)
            introClip = FindAsset<AudioClip>("Daisy Bell");
        if (introClip != null)
        {
            tsc.mainThemeClip = introClip;
            Debug.Log($"[IntroSceneBuilder] Wired audio: {introClip.name} ({introClip.length:F1}s)");
        }
        else
        {
            Debug.LogWarning("[IntroSceneBuilder] Could not find 'WhispersInTheLoadingScreen' audio clip. " +
                "Place it at Assets/Audio/Music/WhispersInTheLoadingScreen.mp3");
        }

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
        Debug.Log("  Phase 5: INTAKE title with glitch wipe transition");
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

    static Material GetParticleMaterial(Color color)
    {
        // Save a material asset to disk so it persists and doesn't go pink
        string matDir = "Assets/Materials/Particles";
        if (!AssetDatabase.IsValidFolder("Assets/Materials"))
            AssetDatabase.CreateFolder("Assets", "Materials");
        if (!AssetDatabase.IsValidFolder(matDir))
            AssetDatabase.CreateFolder("Assets/Materials", "Particles");

        string safeName = $"Particle_{ColorToHex(color)}";
        string matPath = $"{matDir}/{safeName}.mat";

        // Return existing if already created
        var existing = AssetDatabase.LoadAssetAtPath<Material>(matPath);
        if (existing != null) return existing;

        // Try every possible particle shader name across Unity versions
        string[] shaderNames = new string[]
        {
            "Universal Render Pipeline/Particles/Unlit",
            "Universal Render Pipeline/Particles/Simple Lit",
            "Universal Render Pipeline/Particles/Lit",
            "Particles/Standard Unlit",
            "Particles/Standard Surface",
            "Mobile/Particles/Additive",
            "Mobile/Particles/Alpha Blended",
            "Legacy Shaders/Particles/Alpha Blended",
            "Sprites/Default",
            "UI/Default",
        };

        Shader shader = null;
        foreach (var name in shaderNames)
        {
            shader = Shader.Find(name);
            if (shader != null)
            {
                Debug.Log($"[IntroSceneBuilder] Using particle shader: {name}");
                break;
            }
        }

        if (shader == null)
        {
            // Last resort — use the default sprite material which always exists
            Debug.LogWarning("[IntroSceneBuilder] No particle shader found, using Sprites/Default");
            shader = Shader.Find("Sprites/Default");
        }

        if (shader == null)
        {
            Debug.LogError("[IntroSceneBuilder] Cannot find ANY shader for particles.");
            return new Material(Shader.Find("Hidden/InternalErrorShader"));
        }

        var mat = new Material(shader);
        mat.color = color;

        // URP transparency settings
        if (mat.HasProperty("_Surface"))
            mat.SetFloat("_Surface", 1f);
        if (mat.HasProperty("_Blend"))
            mat.SetFloat("_Blend", 0f);
        if (mat.HasProperty("_Mode"))
            mat.SetFloat("_Mode", 2f);

        // Force alpha blending
        if (mat.HasProperty("_SrcBlend"))
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        if (mat.HasProperty("_DstBlend"))
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        if (mat.HasProperty("_ZWrite"))
            mat.SetInt("_ZWrite", 0);

        mat.renderQueue = 3000;
        mat.EnableKeyword("_ALPHABLEND_ON");
        mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");

        // Save to disk so it survives scene reload
        AssetDatabase.CreateAsset(mat, matPath);
        AssetDatabase.SaveAssets();
        Debug.Log($"[IntroSceneBuilder] Created particle material: {matPath}");

        return mat;
    }

    static string ColorToHex(Color c)
    {
        return $"{(int)(c.r*255):X2}{(int)(c.g*255):X2}{(int)(c.b*255):X2}";
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

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial(COL_SPORE);
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

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

        var renderer = obj.GetComponent<ParticleSystemRenderer>();
        renderer.material = GetParticleMaterial(new Color(0.1f, 0.1f, 0.12f, 0.15f));
        renderer.renderMode = ParticleSystemRenderMode.Billboard;

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

    static T FindAsset<T>(string name) where T : Object
    {
        string[] guids = AssetDatabase.FindAssets(name + " t:" + typeof(T).Name);
        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null) return asset;
        }
        return null;
    }
}
#endif
