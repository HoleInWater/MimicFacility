using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

namespace MimicFacility.Pipeline
{
    [Serializable]
    public class DependencyNode
    {
        public string systemName;
        public Type componentType;
        public Type[] requiredComponents;
        public string[] dependsOn;
        public bool isRequired;
    }

    public class DependencyResolver
    {
        private readonly Dictionary<string, DependencyNode> registry = new Dictionary<string, DependencyNode>();
        private readonly List<string> resolved = new List<string>();
        private readonly List<string> warnings = new List<string>();

        public IReadOnlyList<string> Warnings => warnings;

        public DependencyResolver()
        {
            RegisterDefaults();
        }

        public void Register(string name, Type type, Type[] required = null, string[] dependsOn = null, bool isRequired = false)
        {
            registry[name] = new DependencyNode
            {
                systemName = name,
                componentType = type,
                requiredComponents = required ?? Array.Empty<Type>(),
                dependsOn = dependsOn ?? Array.Empty<string>(),
                isRequired = isRequired
            };
        }

        public List<DependencyNode> Resolve(List<string> requestedSystems)
        {
            resolved.Clear();
            warnings.Clear();

            var toResolve = new HashSet<string>(requestedSystems);

            // Add dependencies recursively
            bool changed = true;
            while (changed)
            {
                changed = false;
                foreach (var name in toResolve.ToList())
                {
                    if (!registry.ContainsKey(name))
                    {
                        warnings.Add($"Unknown system: {name}");
                        continue;
                    }

                    foreach (var dep in registry[name].dependsOn)
                    {
                        if (!toResolve.Contains(dep))
                        {
                            toResolve.Add(dep);
                            changed = true;
                        }
                    }
                }
            }

            // Topological sort
            var visited = new HashSet<string>();
            var sorted = new List<string>();

            foreach (var name in toResolve)
            {
                TopologicalSort(name, visited, sorted, new HashSet<string>());
            }

            var result = new List<DependencyNode>();
            foreach (var name in sorted)
            {
                if (registry.ContainsKey(name))
                    result.Add(registry[name]);
            }

            return result;
        }

        private void TopologicalSort(string name, HashSet<string> visited, List<string> sorted, HashSet<string> inProgress)
        {
            if (visited.Contains(name)) return;

            if (inProgress.Contains(name))
            {
                warnings.Add($"Circular dependency detected involving: {name}");
                return;
            }

            if (!registry.ContainsKey(name)) return;

            inProgress.Add(name);

            foreach (var dep in registry[name].dependsOn)
                TopologicalSort(dep, visited, sorted, inProgress);

            inProgress.Remove(name);
            visited.Add(name);
            sorted.Add(name);
        }

        public List<string> Validate()
        {
            var issues = new List<string>();

            foreach (var kvp in registry)
            {
                if (!kvp.Value.isRequired) continue;

                var existing = UnityEngine.Object.FindObjectOfType(kvp.Value.componentType);
                if (existing == null)
                    issues.Add($"Required system missing: {kvp.Key}");
            }

            // Check NetworkIdentity
            foreach (var nb in UnityEngine.Object.FindObjectsOfType<NetworkBehaviour>())
            {
                if (nb.GetComponent<NetworkIdentity>() == null)
                    issues.Add($"Missing NetworkIdentity on: {nb.gameObject.name}");
            }

            return issues;
        }

        public void EnsureComponent(GameObject obj, Type type)
        {
            if (obj.GetComponent(type) == null)
                obj.AddComponent(type);
        }

        public void EnsureComponents(GameObject obj, Type[] types)
        {
            foreach (var t in types)
                EnsureComponent(obj, t);
        }

        private void RegisterDefaults()
        {
            Register("GameManager",
                typeof(Core.GameManager),
                isRequired: true);

            Register("SettingsManager",
                typeof(Core.SettingsManager),
                isRequired: true);

            Register("InputManager",
                typeof(Core.FallbackInputManager),
                isRequired: true);

            Register("RoundManager",
                typeof(Core.RoundManager),
                required: new[] { typeof(NetworkIdentity) },
                dependsOn: new[] { "GameManager" },
                isRequired: true);

            Register("GameState",
                typeof(Core.NetworkedGameState),
                required: new[] { typeof(NetworkIdentity) },
                dependsOn: new[] { "GameManager" },
                isRequired: true);

            Register("SessionTracker",
                typeof(Core.SessionTracker));

            Register("VerificationSystem",
                typeof(Gameplay.VerificationSystem),
                required: new[] { typeof(NetworkIdentity) },
                dependsOn: new[] { "GameState" });

            Register("DiagnosticTaskManager",
                typeof(Gameplay.DiagnosticTaskManager),
                required: new[] { typeof(NetworkIdentity) },
                dependsOn: new[] { "GameState" });

            Register("LoreDatabase",
                typeof(Lore.LoreDatabase));

            Register("SpatialAudio",
                typeof(Audio.SpatialAudioProcessor));

            Register("OllamaClient",
                typeof(AI.LLM.OllamaClient));

            Register("CorruptionTracker",
                typeof(AI.Persistence.CorruptionTracker));

            Register("DirectorMemory",
                typeof(AI.Persistence.DirectorMemory));

            Register("PersonalWeapons",
                typeof(AI.Weapons.PersonalWeaponSystem));

            Register("VoiceLearning",
                typeof(AI.Voice.VoiceLearningSystem));

            Register("FacilityControl",
                typeof(Facility.FacilityControlSystem));

            Register("DirectorAI",
                typeof(AI.Director.DirectorAI),
                required: new[] { typeof(NetworkIdentity) },
                dependsOn: new[] { "OllamaClient", "CorruptionTracker", "DirectorMemory",
                    "PersonalWeapons", "VoiceLearning", "FacilityControl" });

            Register("DialogueManager",
                typeof(AI.Dialogue.MimicDialogueManager),
                required: new[] { typeof(NetworkIdentity) },
                dependsOn: new[] { "DirectorAI", "VoiceLearning" });

            Register("DeviceHorror",
                typeof(Horror.DeviceHorrorManager),
                dependsOn: new[] { "DirectorAI" });
        }
    }
}
