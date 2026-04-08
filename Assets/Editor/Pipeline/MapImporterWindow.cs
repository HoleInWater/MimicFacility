#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using MimicFacility.Pipeline;

public class MapImporterWindow : EditorWindow
{
    private string _selectedFolderPath;
    private MapValidationResult _lastValidation;
    private ImportResult _lastImportResult;
    private ImportState _currentState;
    private Vector2 _logScroll;
    private Vector2 _mainScroll;

    private bool _autoInstallSystems = true;
    private bool _bakeNavMesh = true;
    private bool _createNewScene = true;

    private enum ImportState { Idle, Validated, Importing, Done }

    [MenuItem("MimicFacility/Map Importer %#i")]
    public static void Open() =>
        GetWindow<MapImporterWindow>("Map Importer");

    private void OnGUI()
    {
        _mainScroll = EditorGUILayout.BeginScrollView(_mainScroll);

        GUILayout.Label("MimicFacility Map Importer", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "Drop a map folder to import. The folder should contain:\n" +
            "  - One .prefab (root map prefab)\n" +
            "  - config.json (optional — defines systems, lighting, NavMesh)\n" +
            "  - Enemy/ folder (optional — entity prefabs)\n" +
            "  - Audio/ folder (optional — sound assets)",
            MessageType.Info);

        EditorGUILayout.Space();
        DrawFolderDropArea();

        if (!string.IsNullOrEmpty(_selectedFolderPath))
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("Selected:", _selectedFolderPath, EditorStyles.wordWrappedLabel);
            EditorGUILayout.Space();

            if (_currentState == ImportState.Idle)
            {
                GUI.backgroundColor = new Color(0.3f, 0.7f, 1f);
                if (GUILayout.Button("Validate Folder", GUILayout.Height(30)))
                {
                    _lastValidation = FolderMapImporter.Validate(_selectedFolderPath);
                    _currentState = ImportState.Validated;
                }
                GUI.backgroundColor = Color.white;
            }

            if (_currentState >= ImportState.Validated && _lastValidation != null)
            {
                DrawValidationResults();
                EditorGUILayout.Space();
            }

            if (_currentState == ImportState.Validated && _lastValidation != null && _lastValidation.IsValid)
            {
                DrawImportControls();
            }
        }

        if (_lastImportResult != null && _lastImportResult.Log.Count > 0)
        {
            EditorGUILayout.Space();
            GUILayout.Label("Import Log", EditorStyles.boldLabel);
            _logScroll = EditorGUILayout.BeginScrollView(_logScroll, GUILayout.Height(150));
            foreach (var line in _lastImportResult.Log)
                EditorGUILayout.LabelField(line, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.EndScrollView();
        }

        if (_currentState == ImportState.Done)
        {
            EditorGUILayout.Space();
            if (GUILayout.Button("Import Another"))
            {
                _selectedFolderPath = null;
                _lastValidation = null;
                _lastImportResult = null;
                _currentState = ImportState.Idle;
            }
        }

        EditorGUILayout.Space(20);
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);

        if (GUILayout.Button("Import from JSON Definition"))
        {
            string path = EditorUtility.OpenFilePanel("Select Map JSON", "Assets/Data/Maps", "json");
            if (!string.IsNullOrEmpty(path))
            {
                ImportFromJson(path);
            }
        }

        if (GUILayout.Button("Create Example Map Folder"))
        {
            CreateExampleFolder();
        }

        EditorGUILayout.EndScrollView();
    }

    private void DrawFolderDropArea()
    {
        Rect dropArea = GUILayoutUtility.GetRect(0, 60, GUILayout.ExpandWidth(true));
        var style = new GUIStyle(GUI.skin.box)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = 14,
            fontStyle = FontStyle.Bold
        };
        GUI.Box(dropArea, "Drop Map Folder Here", style);

        Event evt = Event.current;
        switch (evt.type)
        {
            case EventType.DragUpdated:
            case EventType.DragPerform:
                if (!dropArea.Contains(evt.mousePosition)) break;

                DragAndDrop.visualMode = DragAndDropVisualMode.Copy;

                if (evt.type == EventType.DragPerform)
                {
                    DragAndDrop.AcceptDrag();
                    var paths = DragAndDrop.paths;

                    if (paths.Length != 1)
                    { Debug.LogWarning("[MapImporter] Drop exactly one folder."); break; }
                    if (!Directory.Exists(paths[0]))
                    { Debug.LogWarning("[MapImporter] Dropped item is not a folder."); break; }

                    _selectedFolderPath = paths[0];
                    _lastValidation = null;
                    _lastImportResult = null;
                    _currentState = ImportState.Idle;
                }
                evt.Use();
                break;
        }

        EditorGUILayout.Space(2);
        if (GUILayout.Button("Or Browse..."))
        {
            string path = EditorUtility.OpenFolderPanel("Select Map Folder", "Assets", "");
            if (!string.IsNullOrEmpty(path))
            {
                if (path.StartsWith(Application.dataPath))
                    path = "Assets" + path.Substring(Application.dataPath.Length);

                _selectedFolderPath = path;
                _lastValidation = null;
                _lastImportResult = null;
                _currentState = ImportState.Idle;
            }
        }
    }

    private void DrawValidationResults()
    {
        GUILayout.Label("Validation Results", EditorStyles.boldLabel);

        var v = _lastValidation;

        EditorGUILayout.BeginVertical("box");

        DrawStatusLine("Root Prefab",
            string.IsNullOrEmpty(v.PrefabPath) ? "Missing" : Path.GetFileName(v.PrefabPath),
            !string.IsNullOrEmpty(v.PrefabPath));

        DrawStatusLine("Config",
            string.IsNullOrEmpty(v.ConfigPath) ? "Using defaults" : "Found",
            !string.IsNullOrEmpty(v.ConfigPath));

        DrawStatusLine("Enemy Folders", $"{v.EnemyFolders.Count} found", v.EnemyFolders.Count > 0);
        DrawStatusLine("Audio Folders", $"{v.AudioFolders.Count} found", v.AudioFolders.Count > 0);

        if (v.ConfigData != null && !string.IsNullOrEmpty(v.ConfigData.MapName))
            EditorGUILayout.LabelField("Map Name:", v.ConfigData.MapName);
        if (v.ConfigData != null && v.ConfigData.RequiredSystems.Count > 0)
            EditorGUILayout.LabelField("Required Systems:", string.Join(", ", v.ConfigData.RequiredSystems));

        EditorGUILayout.EndVertical();

        foreach (var e in v.Errors)
            EditorGUILayout.HelpBox(e, MessageType.Error);
        foreach (var w in v.Warnings)
            EditorGUILayout.HelpBox(w, MessageType.Warning);

        EditorGUILayout.LabelField("Status:",
            v.IsValid ? "Ready to import" : "Not ready — fix errors above");
    }

    private void DrawStatusLine(string label, string value, bool ok)
    {
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField(ok ? "+" : "-", GUILayout.Width(15));
        EditorGUILayout.LabelField(label + ":", GUILayout.Width(120));
        EditorGUILayout.LabelField(value);
        EditorGUILayout.EndHorizontal();
    }

    private void DrawImportControls()
    {
        GUILayout.Label("Import Settings", EditorStyles.boldLabel);

        _autoInstallSystems = EditorGUILayout.Toggle("Auto-install missing systems", _autoInstallSystems);
        _bakeNavMesh = EditorGUILayout.Toggle("Bake NavMesh", _bakeNavMesh);

        EditorGUILayout.Space();

        GUI.backgroundColor = new Color(0.3f, 0.8f, 0.3f);
        if (GUILayout.Button("IMPORT MAP", GUILayout.Height(35)))
        {
            RunImport();
        }
        GUI.backgroundColor = Color.white;
    }

    private void RunImport()
    {
        _currentState = ImportState.Importing;

        var registry = SystemInstaller.CreateDefaultRegistry();
        var presentSystems = FindPresentSystems();

        var settings = new ImportSettings
        {
            AutoInstallMissingSystems = _autoInstallSystems,
            BakeNavMesh = _bakeNavMesh,
            CreateNewScene = _createNewScene,
            AbortOnInstallFailure = false
        };

        _lastImportResult = FolderMapImporter.Import(_lastValidation, registry, presentSystems, settings);
        _currentState = ImportState.Done;

        if (_lastImportResult.Success)
            Debug.Log("[MapImporter] Import completed successfully.");
        else
            Debug.LogError($"[MapImporter] Import failed: {_lastImportResult.Message}");
    }

    private void ImportFromJson(string path)
    {
        string json = File.ReadAllText(path);
        var pipeline = new ImportPipeline();
        var def = pipeline.LoadFromJson(json);
        if (def != null)
        {
            pipeline.Execute(def);
            Debug.Log($"[MapImporter] Imported from JSON: {def.mapName}");
        }
    }

    private HashSet<string> FindPresentSystems()
    {
        var present = new HashSet<string>();
        if (FindObjectOfType<MimicFacility.Core.GameManager>()) present.Add("GameManager");
        if (FindObjectOfType<MimicFacility.Core.SettingsManager>()) present.Add("SettingsManager");
        if (FindObjectOfType<MimicFacility.Core.FallbackInputManager>()) present.Add("InputManager");
        if (FindObjectOfType<MimicFacility.Core.RoundManager>()) present.Add("RoundManager");
        if (FindObjectOfType<MimicFacility.Core.NetworkedGameState>()) present.Add("GameState");
        if (FindObjectOfType<MimicFacility.Core.SessionTracker>()) present.Add("SessionTracker");
        if (FindObjectOfType<MimicFacility.AI.Director.DirectorAI>()) present.Add("DirectorAI");
        if (FindObjectOfType<MimicFacility.Audio.SpatialAudioProcessor>()) present.Add("SpatialAudio");
        if (FindObjectOfType<MimicFacility.Lore.LoreDatabase>()) present.Add("LoreDatabase");
        return present;
    }

    private void CreateExampleFolder()
    {
        string basePath = "Assets/Data/Maps/ExampleMap";
        if (!Directory.Exists(basePath))
            Directory.CreateDirectory(basePath);

        Directory.CreateDirectory(Path.Combine(basePath, "Enemies"));
        Directory.CreateDirectory(Path.Combine(basePath, "Audio"));

        var config = new MapConfigData
        {
            MapName = "Example Facility",
            RequiredSystems = new List<string>
            {
                "GameManager", "SettingsManager", "InputManager",
                "RoundManager", "GameState", "DirectorAI", "SpatialAudio"
            },
            BakeNavMesh = true,
            LightingPreset = "Horror_Dim"
        };

        string json = JsonUtility.ToJson(config, true);
        File.WriteAllText(Path.Combine(basePath, "config.json"), json);

        AssetDatabase.Refresh();
        Debug.Log($"[MapImporter] Created example folder at {basePath}");
        Debug.Log("  Add a .prefab as the root map, then drag the folder into the importer.");
    }
}
#endif
