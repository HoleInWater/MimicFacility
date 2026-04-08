using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using Mirror;
using MimicFacility.Core;
using MimicFacility.Characters;
using MimicFacility.Entities;
using MimicFacility.Facility;

namespace MimicFacility.Pipeline
{
    public class SystemsProcessor : IPipelineProcessor
    {
        public void Process(PipelineContext ctx)
        {
            var nodes = ctx.resolver.Resolve(ctx.definition.requiredSystems);

            foreach (var node in nodes)
            {
                var existing = UnityEngine.Object.FindObjectOfType(node.componentType);
                if (existing != null) continue;

                // Group related components onto shared objects
                string objName = node.systemName;
                bool attachToDirector = IsDirectorComponent(node.systemName);

                GameObject target;
                if (attachToDirector)
                {
                    target = GameObject.Find("DirectorAI") ?? new GameObject("DirectorAI");
                }
                else
                {
                    target = new GameObject(objName);
                }

                target.AddComponent(node.componentType);

                foreach (var req in node.requiredComponents)
                {
                    if (target.GetComponent(req) == null)
                        target.AddComponent(req);
                }

                ctx.spawnedObjects.Add(target);
            }
        }

        private bool IsDirectorComponent(string name)
        {
            return name == "DirectorAI" || name == "OllamaClient" || name == "CorruptionTracker" ||
                   name == "DirectorMemory" || name == "PersonalWeapons" || name == "VoiceLearning" ||
                   name == "FacilityControl" || name == "DialogueManager" || name == "DeviceHorror";
        }
    }

    public class MapProcessor : IPipelineProcessor
    {
        static readonly Color COL_FLOOR = new Color(0.25f, 0.25f, 0.28f);
        static readonly Color COL_WALL = new Color(0.35f, 0.35f, 0.38f);
        static readonly Color COL_CEILING = new Color(0.20f, 0.20f, 0.22f);

        public void Process(PipelineContext ctx)
        {
            var layout = ctx.definition.layout;
            var mapRoot = new GameObject("Facility").transform;
            mapRoot.SetParent(ctx.root.transform);

            if (layout.useMarchingCubes)
            {
                var gen = mapRoot.gameObject.AddComponent<Terrain.FacilityTerrainGenerator>();
                return;
            }

            int cols = Mathf.CeilToInt(Mathf.Sqrt(layout.roomCount));
            float spacingX = layout.roomWidth + 6f;
            float spacingZ = layout.roomDepth + 6f;

            for (int i = 0; i < layout.roomCount; i++)
            {
                int col = i % cols;
                int row = i / cols;
                Vector3 center = new Vector3(col * spacingX, 0f, row * spacingZ);

                BuildRoom(mapRoot, center, layout, i);
                ctx.roomCenters.Add(center);

                if (col + 1 < cols && i + 1 < layout.roomCount)
                    BuildCorridor(mapRoot, center + Vector3.right * layout.roomWidth / 2f, Vector3.right, 6f, layout);

                if (i + cols < layout.roomCount)
                    BuildCorridor(mapRoot, center + Vector3.forward * layout.roomDepth / 2f, Vector3.forward, 6f, layout);
            }

            if (layout.generateExtractionZone && ctx.roomCenters.Count > 0)
            {
                int lastCol = (layout.roomCount - 1) % cols;
                int lastRow = (layout.roomCount - 1) / cols;
                Vector3 extractPos = new Vector3((lastCol + 1) * spacingX, 0f, lastRow * spacingZ);
                BuildExtraction(mapRoot, extractPos);
                ctx.roomCenters.Add(extractPos);
            }
        }

        private void BuildRoom(Transform parent, Vector3 center, MapLayout layout, int index)
        {
            var room = new GameObject($"Room_{index}");
            room.transform.SetParent(parent);

            Prim(room.transform, "Floor", center - Vector3.up * 0.05f,
                new Vector3(layout.roomWidth, 0.1f, layout.roomDepth), COL_FLOOR);
            Prim(room.transform, "Ceiling", center + Vector3.up * layout.wallHeight,
                new Vector3(layout.roomWidth, 0.1f, layout.roomDepth), COL_CEILING);

            float w = layout.roomWidth, h = layout.wallHeight, d = layout.roomDepth;
            Wall(room.transform, center + Vector3.forward * d / 2f, new Vector3(w, h, 0.3f));
            Wall(room.transform, center - Vector3.forward * d / 2f, new Vector3(w, h, 0.3f));
            Wall(room.transform, center + Vector3.right * w / 2f, new Vector3(0.3f, h, d));
            Wall(room.transform, center - Vector3.right * w / 2f, new Vector3(0.3f, h, d));

            var lightObj = new GameObject("Light");
            lightObj.transform.SetParent(room.transform);
            lightObj.transform.position = center + Vector3.up * (h - 0.5f);
            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.intensity = 1.2f;
            light.range = Mathf.Max(w, d) * 1.3f;
            lightObj.AddComponent<FacilityLight>();

            if (index % 2 == 0)
            {
                var door = Prim(room.transform, "Door", center + Vector3.right * w / 2f + Vector3.up * h / 2f,
                    new Vector3(0.2f, h, layout.corridorWidth), new Color(0.4f, 0.3f, 0.2f));
                door.AddComponent<AudioSource>();
                door.AddComponent<FacilityDoor>();
            }

            if (index % 3 == 0)
            {
                var vent = new GameObject("SporeVent");
                vent.transform.SetParent(room.transform);
                vent.transform.position = center + Vector3.right * 2f + Vector3.up * 0.1f;
                vent.AddComponent<SphereCollider>().isTrigger = true;
                vent.AddComponent<ParticleSystem>();
                vent.AddComponent<AudioSource>();
                vent.AddComponent<SporeVent>();
            }

            if (index % 2 == 1)
            {
                var term = Prim(room.transform, "Terminal",
                    center + Vector3.forward * (d / 2f - 0.5f) + Vector3.up,
                    new Vector3(1f, 1.5f, 0.3f), new Color(0.1f, 0.1f, 0.1f));
                term.AddComponent<AudioSource>();
                term.AddComponent<ResearchTerminal>();
            }
        }

        private void BuildCorridor(Transform parent, Vector3 start, Vector3 dir, float len, MapLayout layout)
        {
            var corr = new GameObject("Corridor");
            corr.transform.SetParent(parent);
            Vector3 center = start + dir * len / 2f;
            Vector3 perp = Vector3.Cross(dir, Vector3.up);
            float w = dir == Vector3.right ? len : layout.corridorWidth;
            float d = dir == Vector3.right ? layout.corridorWidth : len;

            Prim(corr.transform, "Floor", center - Vector3.up * 0.05f, new Vector3(w, 0.1f, d), COL_FLOOR);
            Prim(corr.transform, "Ceiling", center + Vector3.up * layout.wallHeight, new Vector3(w, 0.1f, d), COL_CEILING);
            float wallLen = dir == Vector3.right ? len : d;
            Wall(corr.transform, center + perp * layout.corridorWidth / 2f,
                dir == Vector3.right ? new Vector3(len, layout.wallHeight, 0.3f) : new Vector3(0.3f, layout.wallHeight, len));
            Wall(corr.transform, center - perp * layout.corridorWidth / 2f,
                dir == Vector3.right ? new Vector3(len, layout.wallHeight, 0.3f) : new Vector3(0.3f, layout.wallHeight, len));
        }

        private void BuildExtraction(Transform parent, Vector3 center)
        {
            var zone = new GameObject("ExtractionZone");
            zone.transform.SetParent(parent);
            Prim(zone.transform, "Floor", center, new Vector3(8, 0.1f, 8), COL_FLOOR);
            var trigger = new GameObject("Trigger");
            trigger.transform.SetParent(zone.transform);
            trigger.transform.position = center + Vector3.up;
            trigger.AddComponent<BoxCollider>().isTrigger = true;

            var light = new GameObject("Light");
            light.transform.SetParent(zone.transform);
            light.transform.position = center + Vector3.up * 3f;
            var l = light.AddComponent<Light>();
            l.color = Color.green;
            l.intensity = 3f;
            l.range = 15f;
        }

        private GameObject Prim(Transform parent, string name, Vector3 pos, Vector3 scale, Color color)
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

        private void Wall(Transform parent, Vector3 pos, Vector3 scale)
        {
            Prim(parent, "Wall", pos + Vector3.up * scale.y / 2f, scale, COL_WALL);
        }
    }

    public class EntityProcessor : IPipelineProcessor
    {
        private static readonly Dictionary<string, Type> EntityTypes = new Dictionary<string, Type>
        {
            { "Mimic", typeof(MimicBase) },
            { "Stalker", typeof(Stalker) },
            { "Fraud", typeof(Fraud) },
            { "Phantom", typeof(Phantom) },
            { "Parasite", typeof(Parasite) },
            { "Skinwalker", typeof(Skinwalker) },
            { "Warden", typeof(Warden) },
        };

        private static readonly Dictionary<string, Color> EntityColors = new Dictionary<string, Color>
        {
            { "Mimic", Color.red },
            { "Stalker", Color.black },
            { "Fraud", Color.yellow },
            { "Phantom", Color.blue },
            { "Parasite", new Color(0.5f, 0, 0.5f) },
            { "Skinwalker", new Color(0.4f, 0, 0) },
            { "Warden", new Color(0.3f, 0.3f, 0) },
        };

        public void Process(PipelineContext ctx)
        {
            foreach (var spawn in ctx.definition.entitySpawns)
            {
                if (!EntityTypes.TryGetValue(spawn.entityType, out Type type))
                {
                    Debug.LogWarning($"[EntityProcessor] Unknown entity type: {spawn.entityType}");
                    continue;
                }

                Color color = EntityColors.ContainsKey(spawn.entityType) ? EntityColors[spawn.entityType] : Color.gray;

                for (int i = 0; i < spawn.count; i++)
                {
                    var obj = new GameObject($"{spawn.entityType}_{i}");
                    obj.transform.SetParent(ctx.root.transform);

                    var visual = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                    visual.name = "Visual";
                    visual.transform.SetParent(obj.transform);
                    visual.transform.localPosition = Vector3.up * 0.5f;
                    visual.transform.localScale = new Vector3(0.6f, 0.9f, 0.6f);
                    visual.GetComponent<Collider>().enabled = false;
                    var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                    visual.GetComponent<Renderer>().material = new Material(shader) { color = color };

                    obj.AddComponent<NavMeshAgent>();
                    obj.AddComponent<CapsuleCollider>();
                    var rb = obj.AddComponent<Rigidbody>();
                    rb.isKinematic = true;
                    obj.AddComponent<AudioSource>();
                    obj.AddComponent<NetworkIdentity>();
                    obj.AddComponent(type);

                    Vector3 spawnPos = Vector3.up;
                    if (ctx.roomCenters.Count > 0)
                    {
                        int roomIdx = (i + spawn.entityType.GetHashCode()) % ctx.roomCenters.Count;
                        spawnPos = ctx.roomCenters[Mathf.Abs(roomIdx)] +
                            new Vector3(UnityEngine.Random.Range(-3f, 3f), 0.5f, UnityEngine.Random.Range(-3f, 3f));
                    }
                    obj.transform.position = spawnPos;

                    ctx.spawnedObjects.Add(obj);
                }
            }
        }
    }

    public class GearProcessor : IPipelineProcessor
    {
        private static readonly Dictionary<string, Color> GearColors = new Dictionary<string, Color>
        {
            { "Flashlight", Color.white },
            { "AudioScanner", Color.cyan },
            { "ContainmentDevice", Color.red },
            { "SignalJammer", Color.magenta },
            { "SporeFilter", Color.green },
        };

        public void Process(PipelineContext ctx)
        {
            int gearIndex = 0;
            foreach (var spawn in ctx.definition.gearSpawns)
            {
                Color color = GearColors.ContainsKey(spawn.gearType) ? GearColors[spawn.gearType] : Color.gray;

                for (int i = 0; i < spawn.count; i++)
                {
                    var obj = GameObject.CreatePrimitive(PrimitiveType.Cube);
                    obj.name = $"{spawn.gearType}_{i}";
                    obj.transform.SetParent(ctx.root.transform);
                    obj.transform.localScale = Vector3.one * 0.3f;

                    var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                    var mat = new Material(shader) { color = color };
                    mat.EnableKeyword("_EMISSION");
                    mat.SetColor("_EmissionColor", color * 0.3f);
                    obj.GetComponent<Renderer>().material = mat;

                    obj.AddComponent<SphereCollider>().isTrigger = true;
                    obj.AddComponent<Rigidbody>().isKinematic = true;

                    if (ctx.roomCenters.Count > 0)
                    {
                        int roomIdx = gearIndex % ctx.roomCenters.Count;
                        obj.transform.position = ctx.roomCenters[roomIdx] +
                            new Vector3(UnityEngine.Random.Range(-3f, 3f), 0.3f, UnityEngine.Random.Range(-3f, 3f));
                    }

                    ctx.spawnedObjects.Add(obj);
                    gearIndex++;
                }
            }
        }
    }

    public class LightingProcessor : IPipelineProcessor
    {
        public void Process(PipelineContext ctx)
        {
            var config = ctx.definition.lighting;

            RenderSettings.ambientLight = config.ambientColor;
            RenderSettings.fog = config.enableFog;
            RenderSettings.fogColor = config.fogColor;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = config.fogDensity;
        }
    }

    public class NetworkProcessor : IPipelineProcessor
    {
        public void Process(PipelineContext ctx)
        {
            foreach (var obj in ctx.spawnedObjects)
            {
                if (obj == null) continue;
                var nb = obj.GetComponent<NetworkBehaviour>();
                if (nb != null && obj.GetComponent<NetworkIdentity>() == null)
                    obj.AddComponent<NetworkIdentity>();
            }

            foreach (var nb in UnityEngine.Object.FindObjectsOfType<NetworkBehaviour>())
            {
                if (nb.GetComponent<NetworkIdentity>() == null)
                    nb.gameObject.AddComponent<NetworkIdentity>();
            }
        }
    }

    public class NavMeshProcessor : IPipelineProcessor
    {
        public void Process(PipelineContext ctx)
        {
            var facility = ctx.root.transform.Find("Facility");
            if (facility == null) return;

            var surface = facility.GetComponent<NavMeshSurface>();
            if (surface == null)
                surface = facility.gameObject.AddComponent<NavMeshSurface>();

            surface.BuildNavMesh();
        }
    }
}
