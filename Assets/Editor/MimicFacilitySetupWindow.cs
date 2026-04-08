#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Mirror;
using MimicFacility.Core;
using MimicFacility.Characters;
using MimicFacility.Entities;
using MimicFacility.AI.Director;
using MimicFacility.AI.LLM;
using MimicFacility.AI.Persistence;
using MimicFacility.AI.Weapons;
using MimicFacility.AI.Voice;
using MimicFacility.AI.Dialogue;
using MimicFacility.Audio;
using MimicFacility.Facility;
using MimicFacility.Gameplay;
using MimicFacility.Horror;
using MimicFacility.Lore;
using MimicFacility.Testing;

public class MimicFacilitySetupWindow : EditorWindow
{
    // ── Tab state ────────────────────────────────────────────────────────
    private enum Tab { QuickStart, Pipeline, Map, Player, Entities, Systems, Validate }
    private Tab currentTab = Tab.QuickStart;

    // ── Quick Start ──────────────────────────────────────────────────────
    private bool qs_generateMap = true;
    private bool qs_spawnPlayer = true;
    private bool qs_spawnDirector = true;
    private bool qs_spawnEntities = true;
    private bool qs_spawnGear = true;
    private bool qs_addBootstrap = true;

    // ── Map ──────────────────────────────────────────────────────────────
    private enum MapType { Procedural, MarchingCubes, Empty }
    private MapType mapType = MapType.Procedural;
    private int roomCount = 6;
    private float roomSize = 10f;
    private float wallHeight = 4f;
    private float corridorWidth = 3f;
    private int mcGridX = 64;
    private int mcGridY = 16;
    private int mcGridZ = 64;
    private float mcDecay = 0f;

    // ── Player ───────────────────────────────────────────────────────────
    private Vector3 playerSpawnPos = new Vector3(5f, 1f, 5f);

    // ── Pipeline ─────────────────────────────────────────────────────────
    private TextAsset mapJsonAsset;
    private string pipelineLog = "";

    // ── Entities ─────────────────────────────────────────────────────────
    private int mimicCount = 2;
    private int stalkerCount = 1;
    private int fraudCount = 1;
    private int phantomCount = 1;
    private int parasiteCount = 0;
    private int skinwalkerCount = 0;
    private int wardenCount = 0;

    // ── Scroll ───────────────────────────────────────────────────────────
    private Vector2 scrollPos;

    // ── Colors ───────────────────────────────────────────────────────────
    static readonly Color COL_FLOOR   = new Color(0.25f, 0.25f, 0.28f);
    static readonly Color COL_WALL    = new Color(0.35f, 0.35f, 0.38f);
    static readonly Color COL_CEILING = new Color(0.20f, 0.20f, 0.22f);
    static readonly Color COL_DOOR    = new Color(0.40f, 0.30f, 0.20f);
    static readonly Color COL_TERMINAL = new Color(0.10f, 0.10f, 0.10f);

    [MenuItem("MimicFacility/Setup Wizard %#m")]
    public static void ShowWindow()
    {
        var window = GetWindow<MimicFacilitySetupWindow>("MimicFacility Setup");
        window.minSize = new Vector2(420, 500);
    }

    private void OnGUI()
    {
        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("MimicFacility Setup Wizard", EditorStyles.boldLabel);
        EditorGUILayout.Space(2);

        currentTab = (Tab)GUILayout.Toolbar((int)currentTab,
            new[] { "Quick Start", "Pipeline", "Map", "Player", "Entities", "Systems", "Validate" });

        EditorGUILayout.Space(5);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        switch (currentTab)
        {
            case Tab.QuickStart: DrawQuickStart(); break;
            case Tab.Pipeline:   DrawPipeline(); break;
            case Tab.Map:        DrawMap(); break;
            case Tab.Player:     DrawPlayer(); break;
            case Tab.Entities:   DrawEntities(); break;
            case Tab.Systems:    DrawSystems(); break;
            case Tab.Validate:   DrawValidate(); break;
        }

        EditorGUILayout.EndScrollView();
    }

    // ══════════════════════════════════════════════════════════════════════
    // QUICK START — one button does everything
    // ══════════════════════════════════════════════════════════════════════

    private void DrawQuickStart()
    {
        EditorGUILayout.HelpBox(
            "One-click setup: generates a full test scene with map, player, " +
            "Director AI, entities, gear, and all required systems wired together.",
            MessageType.Info);

        EditorGUILayout.Space(5);
        qs_generateMap = EditorGUILayout.Toggle("Generate Map", qs_generateMap);
        qs_spawnPlayer = EditorGUILayout.Toggle("Spawn Player", qs_spawnPlayer);
        qs_spawnDirector = EditorGUILayout.Toggle("Spawn Director AI", qs_spawnDirector);
        qs_spawnEntities = EditorGUILayout.Toggle("Spawn Entities", qs_spawnEntities);
        qs_spawnGear = EditorGUILayout.Toggle("Spawn Gear", qs_spawnGear);
        qs_addBootstrap = EditorGUILayout.Toggle("Add Runtime Bootstrap", qs_addBootstrap);

        EditorGUILayout.Space(10);

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("BUILD EVERYTHING", GUILayout.Height(40)))
        {
            BuildEverything();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);
        if (GUILayout.Button("Clear Scene (keep camera)"))
        {
            ClearScene();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // PIPELINE TAB — JSON-driven import
    // ══════════════════════════════════════════════════════════════════════

    private void DrawPipeline()
    {
        EditorGUILayout.LabelField("Import Pipeline", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Drop a map definition JSON to import a fully wired scene.\n" +
            "The pipeline resolves all dependencies, spawns systems in order, " +
            "generates geometry, places entities and gear, bakes NavMesh.\n\n" +
            "Pipeline: JSON → Systems → Map → Entities → Gear → Lighting → Network → NavMesh",
            MessageType.Info);

        EditorGUILayout.Space(5);
        mapJsonAsset = (TextAsset)EditorGUILayout.ObjectField("Map Definition JSON", mapJsonAsset, typeof(TextAsset), false);

        EditorGUILayout.Space(5);

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("IMPORT FROM JSON", GUILayout.Height(35)))
        {
            if (mapJsonAsset != null)
                RunPipeline(mapJsonAsset.text);
            else
                EditorUtility.DisplayDialog("No JSON", "Drag a map definition JSON into the field above.", "OK");
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space(5);
        if (GUILayout.Button("Import Default Facility"))
        {
            var pipeline = new MimicFacility.Pipeline.ImportPipeline();
            var def = MimicFacility.Pipeline.ImportPipeline.CreateDefaultDefinition();
            RunPipelineWithDef(def);
        }

        EditorGUILayout.Space(5);
        if (GUILayout.Button("Browse for JSON file..."))
        {
            string path = EditorUtility.OpenFilePanel("Select Map Definition", "Assets/Data/Maps", "json");
            if (!string.IsNullOrEmpty(path))
            {
                string json = System.IO.File.ReadAllText(path);
                RunPipeline(json);
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Export", EditorStyles.boldLabel);

        if (GUILayout.Button("Export Default Definition to JSON"))
        {
            var def = MimicFacility.Pipeline.ImportPipeline.CreateDefaultDefinition();
            var pipeline = new MimicFacility.Pipeline.ImportPipeline();
            string json = pipeline.SaveToJson(def);
            string path = EditorUtility.SaveFilePanel("Save Map Definition", "Assets/Data/Maps", "my_map", "json");
            if (!string.IsNullOrEmpty(path))
            {
                System.IO.File.WriteAllText(path, json);
                AssetDatabase.Refresh();
                Debug.Log($"[Pipeline] Exported to {path}");
            }
        }

        if (!string.IsNullOrEmpty(pipelineLog))
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Pipeline Log", EditorStyles.boldLabel);
            EditorGUILayout.TextArea(pipelineLog, GUILayout.Height(150));
        }
    }

    private void RunPipeline(string json)
    {
        var pipeline = new MimicFacility.Pipeline.ImportPipeline();
        var def = pipeline.LoadFromJson(json);
        if (def == null)
        {
            EditorUtility.DisplayDialog("Error", "Failed to parse JSON.", "OK");
            return;
        }
        RunPipelineWithDef(def);
    }

    private void RunPipelineWithDef(MimicFacility.Pipeline.MapDefinition def)
    {
        pipelineLog = "";
        var pipeline = new MimicFacility.Pipeline.ImportPipeline();
        pipeline.OnLog += msg => pipelineLog += msg + "\n";
        pipeline.OnWarning += msg => pipelineLog += "[WARN] " + msg + "\n";
        pipeline.OnError += msg => pipelineLog += "[ERROR] " + msg + "\n";

        Undo.SetCurrentGroupName("Pipeline Import: " + def.mapName);
        pipeline.Execute(def);

        // Also create player
        if (FindObjectOfType<PlayerCharacter>() == null)
            CreatePlayer(FindBestSpawnPoint());

        Repaint();
    }

    // ══════════════════════════════════════════════════════════════════════
    // MAP TAB
    // ══════════════════════════════════════════════════════════════════════

    private void DrawMap()
    {
        EditorGUILayout.LabelField("Map Generation", EditorStyles.boldLabel);
        mapType = (MapType)EditorGUILayout.EnumPopup("Map Type", mapType);

        EditorGUILayout.Space(5);

        if (mapType == MapType.Procedural)
        {
            roomCount = EditorGUILayout.IntSlider("Room Count", roomCount, 2, 12);
            roomSize = EditorGUILayout.Slider("Room Size", roomSize, 6f, 20f);
            wallHeight = EditorGUILayout.Slider("Wall Height", wallHeight, 3f, 8f);
            corridorWidth = EditorGUILayout.Slider("Corridor Width", corridorWidth, 2f, 5f);

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Generate Procedural Map"))
            {
                GenerateProceduralMap();
            }
        }
        else if (mapType == MapType.MarchingCubes)
        {
            EditorGUILayout.HelpBox(
                "Marching Cubes generates smooth slopes using signed distance fields. " +
                "Walls blend into floors with natural curves instead of sharp 90-degree edges.",
                MessageType.Info);

            mcGridX = EditorGUILayout.IntSlider("Grid X", mcGridX, 16, 128);
            mcGridY = EditorGUILayout.IntSlider("Grid Y", mcGridY, 8, 32);
            mcGridZ = EditorGUILayout.IntSlider("Grid Z", mcGridZ, 16, 128);
            mcDecay = EditorGUILayout.Slider("Decay (Corruption)", mcDecay, 0f, 1f);

            EditorGUILayout.Space(5);
            if (GUILayout.Button("Generate Marching Cubes Map"))
            {
                GenerateMarchingCubesMap();
            }
        }
        else
        {
            EditorGUILayout.HelpBox("Empty: just floor + ambient. Add your own geometry.", MessageType.Info);
            if (GUILayout.Button("Generate Empty Room"))
            {
                GenerateEmptyRoom();
            }
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Facility Objects", EditorStyles.boldLabel);

        if (GUILayout.Button("Add Door to Selection"))
            AddFacilityObject<FacilityDoor>("FacilityDoor", PrimitiveType.Cube, new Vector3(0.2f, 3f, 2f), COL_DOOR);

        if (GUILayout.Button("Add Light to Selection"))
            AddFacilityLight();

        if (GUILayout.Button("Add Spore Vent to Selection"))
            AddFacilityObject<SporeVent>("SporeVent", null, Vector3.one, Color.green);

        if (GUILayout.Button("Add Research Terminal to Selection"))
            AddFacilityObject<ResearchTerminal>("Terminal", PrimitiveType.Cube, new Vector3(1f, 1.5f, 0.3f), COL_TERMINAL);

        EditorGUILayout.Space(5);
        if (GUILayout.Button("Bake NavMesh"))
        {
            BakeNavMesh();
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // PLAYER TAB
    // ══════════════════════════════════════════════════════════════════════

    private void DrawPlayer()
    {
        EditorGUILayout.LabelField("Player Setup", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Auto-setup: creates a player with CharacterController, Camera, " +
            "Flashlight, AudioSource, AudioListener, NetworkIdentity, and all " +
            "required scripts. Works like Ashwalker's PlayerAutoSetup.",
            MessageType.Info);

        playerSpawnPos = EditorGUILayout.Vector3Field("Spawn Position", playerSpawnPos);

        EditorGUILayout.Space(5);
        if (GUILayout.Button("Create Player", GUILayout.Height(30)))
        {
            CreatePlayer(playerSpawnPos);
        }

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Auto Setup Existing", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Select a GameObject and click below to auto-add all missing " +
            "player components. Just like [PlayerComponent] auto-setup.",
            MessageType.Info);

        if (GUILayout.Button("Auto-Setup Selected as Player"))
        {
            if (Selection.activeGameObject != null)
                AutoSetupPlayer(Selection.activeGameObject);
            else
                EditorUtility.DisplayDialog("No Selection", "Select a GameObject first.", "OK");
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // ENTITIES TAB
    // ══════════════════════════════════════════════════════════════════════

    private void DrawEntities()
    {
        EditorGUILayout.LabelField("Entity Spawning", EditorStyles.boldLabel);

        mimicCount = EditorGUILayout.IntSlider("Mimics", mimicCount, 0, 10);
        stalkerCount = EditorGUILayout.IntSlider("Stalkers", stalkerCount, 0, 5);
        fraudCount = EditorGUILayout.IntSlider("Frauds", fraudCount, 0, 5);
        phantomCount = EditorGUILayout.IntSlider("Phantoms", phantomCount, 0, 5);
        parasiteCount = EditorGUILayout.IntSlider("Parasites", parasiteCount, 0, 5);
        skinwalkerCount = EditorGUILayout.IntSlider("Skinwalkers", skinwalkerCount, 0, 3);
        wardenCount = EditorGUILayout.IntSlider("Wardens", wardenCount, 0, 3);

        EditorGUILayout.Space(5);
        if (GUILayout.Button("Spawn All Entities", GUILayout.Height(30)))
        {
            SpawnAllEntities();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Individual Spawn (at scene view position)", EditorStyles.miniLabel);

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Mimic")) SpawnSingleEntity<MimicBase>("Mimic", Color.red);
        if (GUILayout.Button("+ Stalker")) SpawnSingleEntity<Stalker>("Stalker", Color.black);
        if (GUILayout.Button("+ Fraud")) SpawnSingleEntity<Fraud>("Fraud", Color.yellow);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("+ Phantom")) SpawnSingleEntity<Phantom>("Phantom", Color.blue);
        if (GUILayout.Button("+ Parasite")) SpawnSingleEntity<Parasite>("Parasite", new Color(0.5f, 0f, 0.5f));
        if (GUILayout.Button("+ Skinwalker")) SpawnSingleEntity<Skinwalker>("Skinwalker", new Color(0.4f, 0f, 0f));
        EditorGUILayout.EndHorizontal();

        if (GUILayout.Button("+ Warden")) SpawnSingleEntity<Warden>("Warden", new Color(0.3f, 0.3f, 0f));

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Auto-Setup Selected as Entity"))
        {
            if (Selection.activeGameObject != null)
                AutoSetupEntity(Selection.activeGameObject);
            else
                EditorUtility.DisplayDialog("No Selection", "Select a GameObject first.", "OK");
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // SYSTEMS TAB
    // ══════════════════════════════════════════════════════════════════════

    private void DrawSystems()
    {
        EditorGUILayout.LabelField("Game Systems", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Each button creates the system if it doesn't exist. " +
            "Dependencies are auto-wired.",
            MessageType.Info);

        DrawSystemButton<GameManager>("GameManager");
        DrawSystemButton<SettingsManager>("SettingsManager");
        DrawSystemButton<FallbackInputManager>("FallbackInputManager");
        DrawSystemButton<RoundManager>("RoundManager", typeof(NetworkIdentity));
        DrawSystemButton<NetworkedGameState>("NetworkedGameState", typeof(NetworkIdentity));
        DrawSystemButton<VerificationSystem>("VerificationSystem", typeof(NetworkIdentity));
        DrawSystemButton<DiagnosticTaskManager>("DiagnosticTaskManager", typeof(NetworkIdentity));
        DrawSystemButton<SessionTracker>("SessionTracker");
        DrawSystemButton<LoreDatabase>("LoreDatabase");
        DrawSystemButton<SpatialAudioProcessor>("SpatialAudioProcessor");

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Director AI Stack", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Full Director AI", GUILayout.Height(25)))
        {
            CreateDirectorAI();
        }

        EditorGUILayout.Space(5);
        EditorGUILayout.LabelField("Horror Systems", EditorStyles.boldLabel);

        if (GUILayout.Button("Create Device Horror Manager"))
        {
            CreateDeviceHorror();
        }

        EditorGUILayout.Space(10);
        GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
        if (GUILayout.Button("Create ALL Missing Systems", GUILayout.Height(30)))
        {
            CreateAllSystems();
        }
        GUI.backgroundColor = Color.white;
    }

    // ══════════════════════════════════════════════════════════════════════
    // VALIDATE TAB
    // ══════════════════════════════════════════════════════════════════════

    private void DrawValidate()
    {
        EditorGUILayout.LabelField("Scene Validation", EditorStyles.boldLabel);

        if (GUILayout.Button("Run Validation", GUILayout.Height(30)))
        {
            ValidateScene();
        }

        EditorGUILayout.Space(5);
        if (GUILayout.Button("Fix All Issues"))
        {
            FixAllIssues();
        }

        EditorGUILayout.Space(10);
        if (GUILayout.Button("Add NetworkIdentity to all NetworkBehaviours"))
        {
            int count = 0;
            foreach (var nb in FindObjectsOfType<NetworkBehaviour>())
            {
                if (nb.GetComponent<NetworkIdentity>() == null)
                {
                    Undo.AddComponent<NetworkIdentity>(nb.gameObject);
                    count++;
                }
            }
            Debug.Log($"[Validate] Added NetworkIdentity to {count} objects.");
        }
    }

    // ══════════════════════════════════════════════════════════════════════
    // BUILD EVERYTHING
    // ══════════════════════════════════════════════════════════════════════

    private void BuildEverything()
    {
        Undo.SetCurrentGroupName("MimicFacility Full Setup");
        int group = Undo.GetCurrentGroup();

        if (qs_generateMap) GenerateProceduralMap();
        CreateAllSystems();
        if (qs_spawnPlayer) CreatePlayer(FindBestSpawnPoint());
        if (qs_spawnDirector) CreateDirectorAI();
        if (qs_spawnEntities) SpawnAllEntities();
        if (qs_spawnGear) SpawnGear();

        // Wire NetworkIdentity
        foreach (var nb in FindObjectsOfType<NetworkBehaviour>())
        {
            if (nb.GetComponent<NetworkIdentity>() == null)
                Undo.AddComponent<NetworkIdentity>(nb.gameObject);
        }

        if (qs_addBootstrap)
        {
            var existing = FindObjectOfType<TestSceneBootstrap>();
            if (existing == null)
            {
                var obj = new GameObject("RuntimeBootstrap");
                Undo.RegisterCreatedObjectUndo(obj, "Create Bootstrap");
                var bs = obj.AddComponent<TestSceneBootstrap>();
                // Disable auto-generation since we already built everything
                SetPrivateField(bs, "generateMap", false);
                SetPrivateField(bs, "spawnPlayer", false);
                SetPrivateField(bs, "spawnDirector", false);
                SetPrivateField(bs, "spawnEntities", false);
                SetPrivateField(bs, "spawnGear", false);
            }
        }

        // Ambient + fog
        RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.08f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.03f);
        RenderSettings.fogMode = FogMode.ExponentialSquared;
        RenderSettings.fogDensity = 0.03f;

        BakeNavMesh();

        Undo.CollapseUndoOperations(group);
        Debug.Log("[MimicFacility] Full scene build complete.");
    }

    // ══════════════════════════════════════════════════════════════════════
    // MAP GENERATION
    // ══════════════════════════════════════════════════════════════════════

    private void GenerateProceduralMap()
    {
        DestroyExisting("GeneratedFacility");

        var root = new GameObject("GeneratedFacility");
        Undo.RegisterCreatedObjectUndo(root, "Generate Map");

        float spacingX = roomSize + 6f;
        float spacingZ = roomSize + 6f;
        int cols = Mathf.CeilToInt(Mathf.Sqrt(roomCount));

        for (int i = 0; i < roomCount; i++)
        {
            int col = i % cols;
            int row = i / cols;
            Vector3 center = new Vector3(col * spacingX, 0f, row * spacingZ);
            string zone = $"Zone{(char)('A' + i)}";

            BuildRoom(root.transform, center, zone, i);

            if (col + 1 < cols && i + 1 < roomCount)
                BuildCorridor(root.transform, center + Vector3.right * roomSize / 2f, Vector3.right, 6f);

            int nextRow = i + cols;
            if (nextRow < roomCount)
                BuildCorridor(root.transform, center + Vector3.forward * roomSize / 2f, Vector3.forward, 6f);
        }

        // Extraction zone
        int lastCol = (roomCount - 1) % cols;
        int lastRow = (roomCount - 1) / cols;
        Vector3 extractPos = new Vector3((lastCol + 1) * spacingX, 0f, lastRow * spacingZ);
        BuildExtractionZone(root.transform, extractPos);

        Debug.Log($"[Map] Generated {roomCount} rooms with corridors and extraction zone.");
    }

    private void BuildRoom(Transform parent, Vector3 center, string zone, int index)
    {
        var room = new GameObject($"Room_{index}_{zone}");
        room.transform.SetParent(parent);
        room.transform.position = center;

        MakePrimitive(room.transform, "Floor", PrimitiveType.Cube, center - Vector3.up * 0.05f,
            new Vector3(roomSize, 0.1f, roomSize), COL_FLOOR, true);
        MakePrimitive(room.transform, "Ceiling", PrimitiveType.Cube, center + Vector3.up * wallHeight,
            new Vector3(roomSize, 0.1f, roomSize), COL_CEILING, true);

        MakeWall(room.transform, center + Vector3.forward * roomSize / 2f, new Vector3(roomSize, wallHeight, 0.3f));
        MakeWall(room.transform, center - Vector3.forward * roomSize / 2f, new Vector3(roomSize, wallHeight, 0.3f));
        MakeWall(room.transform, center + Vector3.right * roomSize / 2f, new Vector3(0.3f, wallHeight, roomSize));
        MakeWall(room.transform, center - Vector3.right * roomSize / 2f, new Vector3(0.3f, wallHeight, roomSize));

        // Room light
        var lightObj = new GameObject($"Light_{zone}");
        lightObj.transform.SetParent(room.transform);
        lightObj.transform.position = center + Vector3.up * (wallHeight - 0.5f);
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.intensity = 1.2f;
        light.range = roomSize * 1.3f;
        light.color = new Color(0.8f, 0.9f, 1f);
        lightObj.AddComponent<NetworkIdentity>();
        lightObj.AddComponent<FacilityLight>();

        // Door on even rooms
        if (index % 2 == 0)
        {
            var doorObj = MakePrimitive(room.transform, $"Door_{zone}", PrimitiveType.Cube,
                center + Vector3.right * roomSize / 2f + Vector3.up * wallHeight / 2f,
                new Vector3(0.2f, wallHeight, corridorWidth), COL_DOOR, false);
            doorObj.AddComponent<NetworkIdentity>();
            doorObj.AddComponent<AudioSource>();
            doorObj.AddComponent<FacilityDoor>();
        }

        // Terminal on odd rooms
        if (index % 2 == 1)
        {
            var termObj = MakePrimitive(room.transform, $"Terminal_{zone}", PrimitiveType.Cube,
                center + Vector3.forward * (roomSize / 2f - 0.5f) + Vector3.up * 1f,
                new Vector3(1f, 1.5f, 0.3f), COL_TERMINAL, false);
            termObj.AddComponent<NetworkIdentity>();
            SetEmission(termObj, Color.green * 0.3f);
            termObj.AddComponent<AudioSource>();
            termObj.AddComponent<ResearchTerminal>();
        }

        // Spore vent every 3rd room
        if (index % 3 == 0)
        {
            var ventObj = new GameObject($"SporeVent_{zone}");
            ventObj.transform.SetParent(room.transform);
            ventObj.transform.position = center + Vector3.up * 0.1f + Vector3.right * 2f;
            var sphere = ventObj.AddComponent<SphereCollider>();
            sphere.radius = 3f;
            sphere.isTrigger = true;
            ventObj.AddComponent<ParticleSystem>();
            ventObj.AddComponent<AudioSource>();
            ventObj.AddComponent<NetworkIdentity>();
            ventObj.AddComponent<SporeVent>();
        }
    }

    private void BuildCorridor(Transform parent, Vector3 start, Vector3 dir, float length)
    {
        var corridor = new GameObject("Corridor");
        corridor.transform.SetParent(parent);
        Vector3 center = start + dir * length / 2f;
        Vector3 perp = Vector3.Cross(dir, Vector3.up);

        float w = dir == Vector3.right ? length : corridorWidth;
        float d = dir == Vector3.right ? corridorWidth : length;

        MakePrimitive(corridor.transform, "Floor", PrimitiveType.Cube, center - Vector3.up * 0.05f,
            new Vector3(w, 0.1f, d), COL_FLOOR, true);
        MakePrimitive(corridor.transform, "Ceiling", PrimitiveType.Cube, center + Vector3.up * wallHeight,
            new Vector3(w, 0.1f, d), COL_CEILING, true);

        MakeWall(corridor.transform, center + perp * corridorWidth / 2f,
            dir == Vector3.right ? new Vector3(length, wallHeight, 0.3f) : new Vector3(0.3f, wallHeight, length));
        MakeWall(corridor.transform, center - perp * corridorWidth / 2f,
            dir == Vector3.right ? new Vector3(length, wallHeight, 0.3f) : new Vector3(0.3f, wallHeight, length));

        var dimLight = new GameObject("CorridorLight");
        dimLight.transform.SetParent(corridor.transform);
        dimLight.transform.position = center + Vector3.up * (wallHeight - 0.5f);
        var l = dimLight.AddComponent<Light>();
        l.type = LightType.Point;
        l.intensity = 0.5f;
        l.range = length * 1.2f;
    }

    private void BuildExtractionZone(Transform parent, Vector3 center)
    {
        var zone = new GameObject("ExtractionZone");
        zone.transform.SetParent(parent);
        zone.transform.position = center;

        MakePrimitive(zone.transform, "Floor", PrimitiveType.Cube, center - Vector3.up * 0.05f,
            new Vector3(8f, 0.1f, 8f), COL_FLOOR, true);

        var trigger = new GameObject("ExtractionTrigger");
        trigger.transform.SetParent(zone.transform);
        trigger.transform.position = center + Vector3.up;
        var col = trigger.AddComponent<BoxCollider>();
        col.size = new Vector3(8f, 3f, 8f);
        col.isTrigger = true;

        var markerLight = new GameObject("ExtractionLight");
        markerLight.transform.SetParent(zone.transform);
        markerLight.transform.position = center + Vector3.up * 3f;
        var light = markerLight.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = Color.green;
        light.intensity = 3f;
        light.range = 15f;
    }

    private void GenerateMarchingCubesMap()
    {
        DestroyExisting("MarchingCubesFacility");

        var obj = new GameObject("MarchingCubesFacility");
        Undo.RegisterCreatedObjectUndo(obj, "Generate MC Map");

        var gen = obj.AddComponent<MimicFacility.Terrain.FacilityTerrainGenerator>();
        SetPrivateField(gen, "gridX", mcGridX);
        SetPrivateField(gen, "gridY", mcGridY);
        SetPrivateField(gen, "gridZ", mcGridZ);
        SetPrivateField(gen, "decayAmount", mcDecay);

        Debug.Log("[Map] Marching Cubes facility will generate on Play.");
    }

    private void GenerateEmptyRoom()
    {
        DestroyExisting("EmptyFacility");
        var root = new GameObject("EmptyFacility");
        Undo.RegisterCreatedObjectUndo(root, "Generate Empty");

        MakePrimitive(root.transform, "Floor", PrimitiveType.Cube, Vector3.zero,
            new Vector3(30f, 0.1f, 30f), COL_FLOOR, true);

        var lightObj = new GameObject("AmbientLight");
        lightObj.transform.SetParent(root.transform);
        lightObj.transform.position = Vector3.up * 4f;
        var light = lightObj.AddComponent<Light>();
        light.type = LightType.Point;
        light.intensity = 1.5f;
        light.range = 40f;
    }

    // ══════════════════════════════════════════════════════════════════════
    // PLAYER CREATION
    // ══════════════════════════════════════════════════════════════════════

    private void CreatePlayer(Vector3 position)
    {
        DestroyExisting("Player");

        var player = new GameObject("Player");
        player.tag = "Player";
        player.transform.position = position;
        Undo.RegisterCreatedObjectUndo(player, "Create Player");

        AutoSetupPlayer(player);
        Debug.Log("[Player] Created with all components at " + position);
    }

    private void AutoSetupPlayer(GameObject player)
    {
        // CharacterController
        var cc = EnsureComponent<CharacterController>(player);
        cc.height = 1.8f;
        cc.radius = 0.3f;
        cc.center = new Vector3(0f, 0.9f, 0f);

        // Camera
        Camera cam = player.GetComponentInChildren<Camera>();
        if (cam == null)
        {
            var camObj = new GameObject("PlayerCamera");
            camObj.transform.SetParent(player.transform);
            camObj.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            cam = camObj.AddComponent<Camera>();
            cam.fieldOfView = 75f;
            cam.nearClipPlane = 0.1f;
            camObj.AddComponent<AudioListener>();
        }

        // Flashlight
        Light flashlight = player.GetComponentInChildren<Light>();
        if (flashlight == null)
        {
            var flObj = new GameObject("Flashlight");
            flObj.transform.SetParent(cam.transform);
            flObj.transform.localPosition = new Vector3(0.3f, -0.2f, 0.5f);
            flashlight = flObj.AddComponent<Light>();
            flashlight.type = LightType.Spot;
            flashlight.intensity = 3f;
            flashlight.range = 20f;
            flashlight.spotAngle = 35f;
            flashlight.enabled = false;
        }

        // Audio
        EnsureComponent<AudioSource>(player);

        // Scripts
        EnsureComponent<PlayerCharacter>(player);
        EnsureComponent<MimicPlayerState>(player);
        EnsureComponent<NetworkIdentity>(player);

        try { player.tag = "Player"; } catch { }
    }

    // ══════════════════════════════════════════════════════════════════════
    // ENTITY SPAWNING
    // ══════════════════════════════════════════════════════════════════════

    private void SpawnAllEntities()
    {
        for (int i = 0; i < mimicCount; i++) SpawnSingleEntity<MimicBase>("Mimic", Color.red);
        for (int i = 0; i < stalkerCount; i++) SpawnSingleEntity<Stalker>("Stalker", Color.black);
        for (int i = 0; i < fraudCount; i++) SpawnSingleEntity<Fraud>("Fraud", Color.yellow);
        for (int i = 0; i < phantomCount; i++) SpawnSingleEntity<Phantom>("Phantom", Color.blue);
        for (int i = 0; i < parasiteCount; i++) SpawnSingleEntity<Parasite>("Parasite", new Color(0.5f, 0f, 0.5f));
        for (int i = 0; i < skinwalkerCount; i++) SpawnSingleEntity<Skinwalker>("Skinwalker", new Color(0.4f, 0f, 0f));
        for (int i = 0; i < wardenCount; i++) SpawnSingleEntity<Warden>("Warden", new Color(0.3f, 0.3f, 0f));

        Debug.Log($"[Entities] Spawned: {mimicCount} mimics, {stalkerCount} stalkers, {fraudCount} frauds, " +
            $"{phantomCount} phantoms, {parasiteCount} parasites, {skinwalkerCount} skinwalkers, {wardenCount} wardens");
    }

    private void SpawnSingleEntity<T>(string name, Color color) where T : Component
    {
        var obj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(obj, "Spawn " + name);

        var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
        visual.name = "Visual";
        visual.transform.SetParent(obj.transform);
        visual.transform.localPosition = Vector3.up * 0.5f;
        visual.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
        ApplyColor(visual, color);
        visual.GetComponent<Collider>().enabled = false;

        AutoSetupEntity(obj);
        obj.AddComponent<T>();

        obj.transform.position = GetSceneViewCenter() + new Vector3(
            UnityEngine.Random.Range(-5f, 5f), 0.5f, UnityEngine.Random.Range(-5f, 5f));
    }

    private void AutoSetupEntity(GameObject obj)
    {
        EnsureComponent<NavMeshAgent>(obj);
        EnsureComponent<CapsuleCollider>(obj);
        var rb = EnsureComponent<Rigidbody>(obj);
        rb.isKinematic = true;
        rb.useGravity = false;
        EnsureComponent<AudioSource>(obj);
        EnsureComponent<NetworkIdentity>(obj);
    }

    // ══════════════════════════════════════════════════════════════════════
    // SYSTEMS CREATION
    // ══════════════════════════════════════════════════════════════════════

    private void CreateAllSystems()
    {
        EnsureSystem<GameManager>("GameManager");
        EnsureSystem<SettingsManager>("SettingsManager");
        EnsureSystem<FallbackInputManager>("FallbackInputManager");
        EnsureSystem<RoundManager>("RoundManager", typeof(NetworkIdentity));
        EnsureSystem<NetworkedGameState>("NetworkedGameState", typeof(NetworkIdentity));
        EnsureSystem<VerificationSystem>("VerificationSystem", typeof(NetworkIdentity));
        EnsureSystem<DiagnosticTaskManager>("DiagnosticTaskManager", typeof(NetworkIdentity));
        EnsureSystem<SessionTracker>("SessionTracker");
        EnsureSystem<LoreDatabase>("LoreDatabase");
        EnsureSystem<SpatialAudioProcessor>("SpatialAudioProcessor");
        Debug.Log("[Systems] All core systems created.");
    }

    private void CreateDirectorAI()
    {
        DestroyExisting("DirectorAI");
        var obj = new GameObject("DirectorAI");
        Undo.RegisterCreatedObjectUndo(obj, "Create Director");

        obj.AddComponent<NetworkIdentity>();
        obj.AddComponent<OllamaClient>();
        obj.AddComponent<CorruptionTracker>();
        obj.AddComponent<DirectorMemory>();
        obj.AddComponent<PersonalWeaponSystem>();
        obj.AddComponent<VoiceLearningSystem>();
        obj.AddComponent<FacilityControlSystem>();
        obj.AddComponent<MimicDialogueManager>();
        obj.AddComponent<DirectorAI>();

        Debug.Log("[Director] Created with full AI stack.");
    }

    private void CreateDeviceHorror()
    {
        var director = FindObjectOfType<DirectorAI>();
        Transform parent = director != null ? director.transform : null;

        var existing = FindObjectOfType<DeviceHorrorManager>();
        if (existing != null) return;

        var obj = new GameObject("DeviceHorrorManager");
        if (parent != null) obj.transform.SetParent(parent);
        Undo.RegisterCreatedObjectUndo(obj, "Create Horror");
        obj.AddComponent<DeviceHorrorManager>();

        Debug.Log("[Horror] Device horror manager created.");
    }

    private void SpawnGear()
    {
        var spawnPoints = new List<Vector3>();
        foreach (var room in FindObjectsOfType<FacilityLight>())
        {
            Vector3 pos = room.transform.position - Vector3.up * 3f;
            for (int i = 0; i < 2; i++)
            {
                spawnPoints.Add(pos + new Vector3(
                    UnityEngine.Random.Range(-3f, 3f), 0.3f, UnityEngine.Random.Range(-3f, 3f)));
            }
        }

        string[] gearNames = { "Flashlight", "AudioScanner", "ContainmentDevice", "SignalJammer" };
        Color[] gearColors = { Color.white, Color.cyan, Color.red, Color.magenta };

        for (int i = 0; i < Mathf.Min(spawnPoints.Count, 12); i++)
        {
            int type = i % gearNames.Length;
            var gearObj = MakePrimitive(null, gearNames[type], PrimitiveType.Cube,
                spawnPoints[i], Vector3.one * 0.3f, gearColors[type], false);
            SetEmission(gearObj, gearColors[type] * 0.3f);
            var sc = gearObj.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.8f;
            Undo.RegisterCreatedObjectUndo(gearObj, "Spawn Gear");
        }

        Debug.Log($"[Gear] Spawned {Mathf.Min(spawnPoints.Count, 12)} gear pickups.");
    }

    // ══════════════════════════════════════════════════════════════════════
    // VALIDATION
    // ══════════════════════════════════════════════════════════════════════

    private void ValidateScene()
    {
        int issues = 0;

        issues += Check<GameManager>("GameManager");
        issues += Check<SettingsManager>("SettingsManager");
        issues += Check<RoundManager>("RoundManager");
        issues += Check<NetworkedGameState>("NetworkedGameState");
        issues += Check<PlayerCharacter>("Player (PlayerCharacter)");

        // Check NetworkIdentity on all NetworkBehaviours
        int missingNI = 0;
        foreach (var nb in FindObjectsOfType<NetworkBehaviour>())
        {
            if (nb.GetComponent<NetworkIdentity>() == null) missingNI++;
        }
        if (missingNI > 0) { Debug.LogWarning($"[Validate] {missingNI} objects missing NetworkIdentity"); issues += missingNI; }

        // Check NavMesh
        if (!HasNavMesh()) { Debug.LogWarning("[Validate] No NavMesh baked — entities can't pathfind"); issues++; }

        // Check Camera
        if (Camera.main == null) { Debug.LogWarning("[Validate] No main camera in scene"); issues++; }

        if (issues == 0)
            Debug.Log("[Validate] Scene is valid — no issues found.");
        else
            Debug.LogWarning($"[Validate] Found {issues} issues. Use 'Fix All Issues' to resolve.");
    }

    private void FixAllIssues()
    {
        CreateAllSystems();

        foreach (var nb in FindObjectsOfType<NetworkBehaviour>())
        {
            if (nb.GetComponent<NetworkIdentity>() == null)
                Undo.AddComponent<NetworkIdentity>(nb.gameObject);
        }

        if (FindObjectOfType<PlayerCharacter>() == null)
            CreatePlayer(FindBestSpawnPoint());

        BakeNavMesh();

        Debug.Log("[Fix] All issues resolved.");
    }

    // ══════════════════════════════════════════════════════════════════════
    // HELPERS
    // ══════════════════════════════════════════════════════════════════════

    private T EnsureComponent<T>(GameObject obj) where T : Component
    {
        T comp = obj.GetComponent<T>();
        if (comp == null) comp = Undo.AddComponent<T>(obj);
        return comp;
    }

    private void EnsureSystem<T>(string name, params Type[] extras) where T : Component
    {
        if (FindObjectOfType<T>() != null) return;
        var obj = new GameObject(name);
        Undo.RegisterCreatedObjectUndo(obj, "Create " + name);
        obj.AddComponent<T>();
        foreach (var t in extras)
        {
            if (obj.GetComponent(t) == null)
                Undo.AddComponent(obj, t);
        }
    }

    private void DrawSystemButton<T>(string label, params Type[] extras) where T : Component
    {
        EditorGUILayout.BeginHorizontal();
        bool exists = FindObjectOfType<T>() != null;
        GUI.enabled = !exists;
        if (GUILayout.Button(exists ? $"  {label}" : $"+ {label}"))
            EnsureSystem<T>(label, extras);
        GUI.enabled = true;
        EditorGUILayout.Toggle(exists, GUILayout.Width(20));
        EditorGUILayout.EndHorizontal();
    }

    private int Check<T>(string label) where T : Component
    {
        if (FindObjectOfType<T>() == null)
        {
            Debug.LogWarning($"[Validate] Missing: {label}");
            return 1;
        }
        return 0;
    }

    private GameObject MakePrimitive(Transform parent, string name, PrimitiveType type,
        Vector3 position, Vector3 scale, Color color, bool isStatic)
    {
        var obj = GameObject.CreatePrimitive(type);
        obj.name = name;
        if (parent != null) obj.transform.SetParent(parent);
        obj.transform.position = position;
        obj.transform.localScale = scale;
        obj.isStatic = isStatic;
        ApplyColor(obj, color);
        return obj;
    }

    private void MakeWall(Transform parent, Vector3 position, Vector3 scale)
    {
        var wall = MakePrimitive(parent, "Wall", PrimitiveType.Cube,
            position + Vector3.up * scale.y / 2f, scale, COL_WALL, true);
    }

    private void AddFacilityObject<T>(string name, PrimitiveType? prim, Vector3 scale, Color color) where T : Component
    {
        Vector3 pos = GetSceneViewCenter();
        GameObject obj;
        if (prim.HasValue)
            obj = MakePrimitive(null, name, prim.Value, pos, scale, color, false);
        else
        {
            obj = new GameObject(name);
            obj.transform.position = pos;
        }
        Undo.RegisterCreatedObjectUndo(obj, "Add " + name);
        obj.AddComponent<AudioSource>();
        if (typeof(NetworkBehaviour).IsAssignableFrom(typeof(T)))
            EnsureComponent<NetworkIdentity>(obj);
        obj.AddComponent<T>();
        Selection.activeGameObject = obj;
    }

    private void AddFacilityLight()
    {
        var obj = new GameObject("FacilityLight");
        obj.transform.position = GetSceneViewCenter();
        Undo.RegisterCreatedObjectUndo(obj, "Add Light");
        var l = obj.AddComponent<Light>();
        l.type = LightType.Point;
        l.intensity = 1.2f;
        l.range = 12f;
        obj.AddComponent<NetworkIdentity>();
        obj.AddComponent<FacilityLight>();
        Selection.activeGameObject = obj;
    }

    private static void ApplyColor(GameObject obj, Color color)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer == null) return;
        var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
        renderer.material = new Material(shader) { color = color };
    }

    private static void SetEmission(GameObject obj, Color emissionColor)
    {
        var renderer = obj.GetComponent<Renderer>();
        if (renderer == null || renderer.sharedMaterial == null) return;
        renderer.sharedMaterial.EnableKeyword("_EMISSION");
        renderer.sharedMaterial.SetColor("_EmissionColor", emissionColor);
    }

    private void DestroyExisting(string name)
    {
        var existing = GameObject.Find(name);
        if (existing != null) Undo.DestroyObjectImmediate(existing);
    }

    private void BakeNavMesh()
    {
        var surface = FindObjectOfType<NavMeshSurface>();
        if (surface == null)
        {
            var floors = GameObject.FindGameObjectsWithTag("Untagged");
            var root = GameObject.Find("GeneratedFacility") ?? GameObject.Find("MarchingCubesFacility");
            if (root == null) return;
            surface = root.AddComponent<NavMeshSurface>();
        }
        surface.BuildNavMesh();
        Debug.Log("[NavMesh] Baked.");
    }

    private Vector3 FindBestSpawnPoint()
    {
        var spawns = GameObject.FindGameObjectsWithTag("Respawn");
        if (spawns.Length > 0) return spawns[0].transform.position;

        var floors = FindObjectsOfType<MeshRenderer>();
        foreach (var f in floors)
        {
            if (f.name == "Floor") return f.transform.position + Vector3.up * 1f;
        }

        return new Vector3(5f, 1f, 5f);
    }

    private Vector3 GetSceneViewCenter()
    {
        if (SceneView.lastActiveSceneView != null)
        {
            var sv = SceneView.lastActiveSceneView;
            return sv.pivot;
        }
        return Vector3.up;
    }

    private bool HasNavMesh()
    {
        return NavMesh.SamplePosition(Vector3.zero, out _, 100f, NavMesh.AllAreas);
    }

    private void ClearScene()
    {
        if (!EditorUtility.DisplayDialog("Clear Scene",
            "Delete all MimicFacility objects? Camera will be kept.", "Clear", "Cancel"))
            return;

        string[] roots = { "GeneratedFacility", "MarchingCubesFacility", "EmptyFacility",
            "Player", "DirectorAI", "RuntimeBootstrap", "GameManager", "SettingsManager",
            "FallbackInputManager", "RoundManager", "NetworkedGameState", "VerificationSystem",
            "DiagnosticTaskManager", "SessionTracker", "LoreDatabase", "SpatialAudioProcessor",
            "DeviceHorrorManager" };

        foreach (var name in roots) DestroyExisting(name);

        foreach (var entity in FindObjectsOfType<MimicBase>()) Undo.DestroyObjectImmediate(entity.gameObject);
        foreach (var entity in FindObjectsOfType<Stalker>()) Undo.DestroyObjectImmediate(entity.gameObject);
        foreach (var entity in FindObjectsOfType<Fraud>()) Undo.DestroyObjectImmediate(entity.gameObject);
        foreach (var entity in FindObjectsOfType<Phantom>()) Undo.DestroyObjectImmediate(entity.gameObject);
        foreach (var entity in FindObjectsOfType<Parasite>()) Undo.DestroyObjectImmediate(entity.gameObject);
        foreach (var entity in FindObjectsOfType<Skinwalker>()) Undo.DestroyObjectImmediate(entity.gameObject);
        foreach (var entity in FindObjectsOfType<Warden>()) Undo.DestroyObjectImmediate(entity.gameObject);

        Debug.Log("[Clear] Scene cleared.");
    }

    private void SetPrivateField(object obj, string fieldName, object value)
    {
        var field = obj.GetType().GetField(fieldName,
            BindingFlags.NonPublic | BindingFlags.Instance);
        if (field != null) field.SetValue(obj, value);
    }
}
#endif
