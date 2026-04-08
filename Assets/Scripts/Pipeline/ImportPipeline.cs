using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.AI;
using Mirror;

namespace MimicFacility.Pipeline
{
    public class ImportPipeline
    {
        public event Action<string> OnLog;
        public event Action<string> OnWarning;
        public event Action<string> OnError;

        private readonly DependencyResolver resolver = new DependencyResolver();
        private readonly List<IPipelineProcessor> processors = new List<IPipelineProcessor>();

        public DependencyResolver Resolver => resolver;

        public ImportPipeline()
        {
            processors.Add(new SystemsProcessor());
            processors.Add(new MapProcessor());
            processors.Add(new EntityProcessor());
            processors.Add(new GearProcessor());
            processors.Add(new LightingProcessor());
            processors.Add(new NetworkProcessor());
            processors.Add(new NavMeshProcessor());
        }

        public void Execute(MapDefinition definition)
        {
            Log($"=== Import Pipeline: {definition.mapName} ===");

            if (definition.seed >= 0)
                UnityEngine.Random.InitState(definition.seed);

            var context = new PipelineContext
            {
                definition = definition,
                resolver = resolver,
                roomCenters = new List<Vector3>(),
                spawnedObjects = new List<GameObject>(),
                root = new GameObject(definition.mapName)
            };

            foreach (var processor in processors)
            {
                string name = processor.GetType().Name;
                Log($"Running {name}...");

                try
                {
                    processor.Process(context);
                    Log($"  {name} complete.");
                }
                catch (Exception e)
                {
                    Error($"  {name} failed: {e.Message}");
                }
            }

            // Final validation
            var issues = resolver.Validate();
            foreach (var issue in issues)
                Warning(issue);

            foreach (var w in resolver.Warnings)
                Warning(w);

            Log($"=== Pipeline complete: {context.spawnedObjects.Count} objects created ===");
        }

        public MapDefinition LoadFromJson(string json)
        {
            return JsonUtility.FromJson<MapDefinition>(json);
        }

        public MapDefinition LoadFromFile(string path)
        {
            if (!File.Exists(path))
            {
                Error($"File not found: {path}");
                return null;
            }
            string json = File.ReadAllText(path);
            return LoadFromJson(json);
        }

        public string SaveToJson(MapDefinition definition)
        {
            return JsonUtility.ToJson(definition, true);
        }

        public static MapDefinition CreateDefaultDefinition()
        {
            var def = new MapDefinition
            {
                mapName = "TestFacility",
                requiredSystems = new List<string>
                {
                    "GameManager", "SettingsManager", "InputManager",
                    "RoundManager", "GameState", "DirectorAI",
                    "SpatialAudio", "SessionTracker"
                },
                entitySpawns = new List<EntitySpawn>
                {
                    new EntitySpawn { entityType = "Mimic", count = 2 },
                    new EntitySpawn { entityType = "Stalker", count = 1 },
                    new EntitySpawn { entityType = "Fraud", count = 1 },
                    new EntitySpawn { entityType = "Phantom", count = 1 },
                },
                gearSpawns = new List<GearSpawn>
                {
                    new GearSpawn { gearType = "Flashlight", count = 4 },
                    new GearSpawn { gearType = "AudioScanner", count = 3 },
                    new GearSpawn { gearType = "ContainmentDevice", count = 2 },
                    new GearSpawn { gearType = "SignalJammer", count = 2 },
                }
            };
            return def;
        }

        private void Log(string msg) { OnLog?.Invoke(msg); Debug.Log($"[Pipeline] {msg}"); }
        private void Warning(string msg) { OnWarning?.Invoke(msg); Debug.LogWarning($"[Pipeline] {msg}"); }
        private void Error(string msg) { OnError?.Invoke(msg); Debug.LogError($"[Pipeline] {msg}"); }
    }

    public class PipelineContext
    {
        public MapDefinition definition;
        public DependencyResolver resolver;
        public GameObject root;
        public List<Vector3> roomCenters;
        public List<GameObject> spawnedObjects;
    }

    public interface IPipelineProcessor
    {
        void Process(PipelineContext context);
    }
}
