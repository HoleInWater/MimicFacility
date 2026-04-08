using UnityEngine;
using Mirror;
using MimicFacility.Core;
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
using System;
using System.Collections.Generic;

namespace MimicFacility.Pipeline
{
    public class SystemInstallResult
    {
        public bool Success;
        public string Reason;
        public GameObject CreatedObject;
    }

    public static class SystemInstaller
    {
        private static readonly Dictionary<string, Type> SystemTypes = new Dictionary<string, Type>
        {
            { "GameManager", typeof(GameManager) },
            { "SettingsManager", typeof(SettingsManager) },
            { "InputManager", typeof(FallbackInputManager) },
            { "RoundManager", typeof(RoundManager) },
            { "GameState", typeof(NetworkedGameState) },
            { "VerificationSystem", typeof(VerificationSystem) },
            { "DiagnosticTaskManager", typeof(DiagnosticTaskManager) },
            { "SessionTracker", typeof(SessionTracker) },
            { "LoreDatabase", typeof(LoreDatabase) },
            { "SpatialAudio", typeof(SpatialAudioProcessor) },
            { "OllamaClient", typeof(OllamaClient) },
            { "CorruptionTracker", typeof(CorruptionTracker) },
            { "DirectorMemory", typeof(DirectorMemory) },
            { "PersonalWeapons", typeof(PersonalWeaponSystem) },
            { "VoiceLearning", typeof(VoiceLearningSystem) },
            { "FacilityControl", typeof(FacilityControlSystem) },
            { "DirectorAI", typeof(DirectorAI) },
            { "DialogueManager", typeof(MimicDialogueManager) },
            { "DeviceHorror", typeof(DeviceHorrorManager) },
        };

        private static readonly HashSet<string> DirectorGroupSystems = new HashSet<string>
        {
            "DirectorAI", "OllamaClient", "CorruptionTracker", "DirectorMemory",
            "PersonalWeapons", "VoiceLearning", "FacilityControl", "DialogueManager", "DeviceHorror"
        };

        private static readonly HashSet<string> NetworkedSystems = new HashSet<string>
        {
            "RoundManager", "GameState", "VerificationSystem", "DiagnosticTaskManager",
            "DirectorAI", "DialogueManager"
        };

        public static SystemInstallResult Install(string systemId, SystemRegistry registry)
        {
            var result = new SystemInstallResult();

            if (!SystemTypes.TryGetValue(systemId, out Type componentType))
            {
                result.Success = false;
                result.Reason = $"No installer for system: {systemId}";
                return result;
            }

            if (UnityEngine.Object.FindObjectOfType(componentType) != null)
            {
                result.Success = true;
                result.Reason = "Already exists.";
                return result;
            }

            GameObject target;
            if (DirectorGroupSystems.Contains(systemId))
            {
                target = GameObject.Find("DirectorAI") ?? new GameObject("DirectorAI");
            }
            else
            {
                target = new GameObject(systemId);
            }

            if (NetworkedSystems.Contains(systemId) || typeof(NetworkBehaviour).IsAssignableFrom(componentType))
            {
                if (target.GetComponent<NetworkIdentity>() == null)
                    target.AddComponent<NetworkIdentity>();
            }

            target.AddComponent(componentType);

            result.Success = true;
            result.CreatedObject = target;
            return result;
        }

        public static SystemRegistry CreateDefaultRegistry()
        {
            var registry = new SystemRegistry();

            registry.Register(new SystemDescriptor { Id = "GameManager" });
            registry.Register(new SystemDescriptor { Id = "SettingsManager" });
            registry.Register(new SystemDescriptor { Id = "InputManager" });
            registry.Register(new SystemDescriptor { Id = "SessionTracker" });
            registry.Register(new SystemDescriptor { Id = "LoreDatabase" });
            registry.Register(new SystemDescriptor { Id = "SpatialAudio" });

            registry.Register(new SystemDescriptor
            {
                Id = "RoundManager",
                Dependencies = new List<string> { "GameManager" }
            });
            registry.Register(new SystemDescriptor
            {
                Id = "GameState",
                Dependencies = new List<string> { "GameManager" }
            });
            registry.Register(new SystemDescriptor
            {
                Id = "VerificationSystem",
                Dependencies = new List<string> { "GameState" }
            });
            registry.Register(new SystemDescriptor
            {
                Id = "DiagnosticTaskManager",
                Dependencies = new List<string> { "GameState" }
            });
            registry.Register(new SystemDescriptor { Id = "OllamaClient" });
            registry.Register(new SystemDescriptor { Id = "CorruptionTracker" });
            registry.Register(new SystemDescriptor { Id = "DirectorMemory" });
            registry.Register(new SystemDescriptor { Id = "PersonalWeapons" });
            registry.Register(new SystemDescriptor { Id = "VoiceLearning" });
            registry.Register(new SystemDescriptor { Id = "FacilityControl" });
            registry.Register(new SystemDescriptor
            {
                Id = "DirectorAI",
                Dependencies = new List<string>
                {
                    "OllamaClient", "CorruptionTracker", "DirectorMemory",
                    "PersonalWeapons", "VoiceLearning", "FacilityControl"
                }
            });
            registry.Register(new SystemDescriptor
            {
                Id = "DialogueManager",
                Dependencies = new List<string> { "DirectorAI", "VoiceLearning" }
            });
            registry.Register(new SystemDescriptor
            {
                Id = "DeviceHorror",
                Dependencies = new List<string> { "DirectorAI" }
            });

            return registry;
        }
    }
}
