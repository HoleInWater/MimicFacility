using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace MimicFacility.Pipeline
{
    public static class FolderMapImporter
    {
        public static MapValidationResult Validate(string folderPath)
        {
            var result = new MapValidationResult();
            result.SourceFolder = folderPath;

            if (string.IsNullOrEmpty(folderPath) || !Directory.Exists(folderPath))
            {
                result.Errors.Add("Invalid or missing folder path.");
                result.IsValid = false;
                return result;
            }

            var prefabs = Directory.GetFiles(folderPath, "*.prefab", SearchOption.TopDirectoryOnly);
            if (prefabs.Length == 0)
                result.Errors.Add("No root prefab found in folder.");
            else if (prefabs.Length > 1)
                result.Errors.Add("Multiple root prefabs found — need exactly one.");
            else
                result.PrefabPath = prefabs[0];

            var configPath = Path.Combine(folderPath, "config.json");
            if (File.Exists(configPath))
            {
                result.ConfigPath = configPath;
                try
                {
                    result.ConfigData = JsonUtility.FromJson<MapConfigData>(File.ReadAllText(configPath));
                }
                catch
                {
                    result.Errors.Add("Failed to parse config.json.");
                }
            }
            else
            {
                result.Warnings.Add("No config.json found — using defaults.");
                result.ConfigData = new MapConfigData();
            }

            foreach (var dir in Directory.GetDirectories(folderPath, "*", SearchOption.TopDirectoryOnly))
            {
                var name = Path.GetFileName(dir).ToLower();
                if (name.Contains("enemy") || name.Contains("entit")) result.EnemyFolders.Add(dir);
                if (name.Contains("audio") || name.Contains("sound")) result.AudioFolders.Add(dir);
            }

            if (result.AudioFolders.Count == 0)
                result.Warnings.Add("No Audio folder detected.");
            if (result.EnemyFolders.Count == 0)
                result.Warnings.Add("No Enemy/Entity folder detected.");

            if (result.ConfigData?.RequiredSystems != null)
                result.MissingDependencies.AddRange(result.ConfigData.RequiredSystems);

            result.IsValid = result.PrefabPath != null && result.Errors.Count == 0;
            return result;
        }

        public static ImportResult Import(
            MapValidationResult validation,
            SystemRegistry registry,
            IReadOnlyCollection<string> presentSystemIds,
            ImportSettings settings)
        {
            var result = new ImportResult();

            if (!validation.IsValid)
                return result.Fail("Validation failed.");

            var mapDef = new MapDefinition
            {
                mapName = validation.ConfigData?.MapName ?? "Imported Map",
                requiredSystems = validation.MissingDependencies
            };

            var resolution = FolderDependencyResolver.Resolve(mapDef, registry, presentSystemIds);

            if (resolution.HasFatalIssues)
                return result.Fail("Dependency resolution failed.", resolution.Issues);

            if (resolution.MissingSystems.Count > 0 && !settings.AutoInstallMissingSystems)
                return result.Fail("Missing systems. Enable AutoInstall or add them manually.");

            foreach (var id in resolution.RequiredSystems)
            {
                if (!resolution.MissingSystems.Contains(id)) continue;

                var installResult = SystemInstaller.Install(id, registry);
                if (!installResult.Success)
                {
                    result.RecordFailure(id, installResult.Reason);
                    if (settings.AbortOnInstallFailure)
                        return result;
                }
                else
                {
                    result.Log.Add($"Installed system: {id}");
                }
            }

            MapContentImporter.ImportContent(validation, result);

            return result.Succeed(resolution);
        }
    }
}
