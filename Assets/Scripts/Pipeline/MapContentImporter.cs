using UnityEngine;
using UnityEngine.AI;
using Mirror;
using System.IO;

namespace MimicFacility.Pipeline
{
    public static class MapContentImporter
    {
        public static void ImportContent(MapValidationResult validation, ImportResult result)
        {
#if UNITY_EDITOR
            if (!string.IsNullOrEmpty(validation.PrefabPath))
            {
                var prefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(validation.PrefabPath);
                if (prefab != null)
                {
                    var instance = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(prefab);
                    instance.name = validation.ConfigData?.MapName ?? "ImportedMap";
                    instance.isStatic = true;

                    EnsureNetworkIdentities(instance);

                    result.Log.Add($"Instantiated prefab: {validation.PrefabPath}");

                    if (validation.ConfigData != null && validation.ConfigData.BakeNavMesh)
                    {
                        var surface = instance.GetComponent<Unity.AI.Navigation.NavMeshSurface>();
                        if (surface == null)
                            surface = instance.AddComponent<Unity.AI.Navigation.NavMeshSurface>();
                        surface.BuildNavMesh();
                        result.Log.Add("NavMesh baked.");
                    }
                }
                else
                {
                    result.Log.Add($"Failed to load prefab at: {validation.PrefabPath}");
                }
            }

            foreach (var enemyFolder in validation.EnemyFolders)
            {
                var enemyPrefabs = Directory.GetFiles(enemyFolder, "*.prefab", SearchOption.AllDirectories);
                foreach (var ep in enemyPrefabs)
                {
                    var enemyPrefab = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(ep);
                    if (enemyPrefab != null)
                    {
                        result.Log.Add($"Found enemy prefab: {ep}");
                    }
                }
            }

            foreach (var audioFolder in validation.AudioFolders)
            {
                var audioClips = Directory.GetFiles(audioFolder, "*.wav", SearchOption.AllDirectories);
                foreach (var ac in audioClips)
                {
                    result.Log.Add($"Found audio clip: {ac}");
                }
                var oggClips = Directory.GetFiles(audioFolder, "*.ogg", SearchOption.AllDirectories);
                foreach (var ac in oggClips)
                {
                    result.Log.Add($"Found audio clip: {ac}");
                }
            }

            if (validation.ConfigData != null && !string.IsNullOrEmpty(validation.ConfigData.LightingPreset))
            {
                ApplyLightingPreset(validation.ConfigData.LightingPreset);
                result.Log.Add($"Applied lighting preset: {validation.ConfigData.LightingPreset}");
            }
#endif
        }

        private static void EnsureNetworkIdentities(GameObject root)
        {
            foreach (var nb in root.GetComponentsInChildren<NetworkBehaviour>())
            {
                if (nb.GetComponent<NetworkIdentity>() == null)
                    nb.gameObject.AddComponent<NetworkIdentity>();
            }
        }

        private static void ApplyLightingPreset(string preset)
        {
            switch (preset.ToLower())
            {
                case "horror_dim":
                    RenderSettings.ambientLight = new Color(0.03f, 0.03f, 0.05f);
                    RenderSettings.fog = true;
                    RenderSettings.fogColor = new Color(0.02f, 0.02f, 0.03f);
                    RenderSettings.fogDensity = 0.03f;
                    RenderSettings.fogMode = FogMode.ExponentialSquared;
                    break;
                case "horror_bright":
                    RenderSettings.ambientLight = new Color(0.08f, 0.08f, 0.10f);
                    RenderSettings.fog = true;
                    RenderSettings.fogColor = new Color(0.05f, 0.05f, 0.06f);
                    RenderSettings.fogDensity = 0.015f;
                    RenderSettings.fogMode = FogMode.ExponentialSquared;
                    break;
                case "clinical":
                    RenderSettings.ambientLight = new Color(0.15f, 0.15f, 0.18f);
                    RenderSettings.fog = false;
                    break;
                default:
                    RenderSettings.ambientLight = new Color(0.05f, 0.05f, 0.08f);
                    RenderSettings.fog = true;
                    RenderSettings.fogDensity = 0.02f;
                    break;
            }
        }
    }
}
