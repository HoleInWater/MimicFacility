using System.Collections.Generic;

namespace MimicFacility.Pipeline
{
    public class MapValidationResult
    {
        public string SourceFolder;
        public bool IsValid;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
        public string PrefabPath;
        public string ConfigPath;
        public List<string> EnemyFolders = new List<string>();
        public List<string> AudioFolders = new List<string>();
        public MapConfigData ConfigData;
        public List<string> MissingDependencies = new List<string>();
    }
}
