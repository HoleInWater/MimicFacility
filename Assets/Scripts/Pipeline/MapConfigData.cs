using System.Collections.Generic;

namespace MimicFacility.Pipeline
{
    [System.Serializable]
    public class MapConfigData
    {
        public string MapName;
        public List<string> RequiredSystems = new List<string>();
        public bool BakeNavMesh;
        public string LightingPreset;
    }
}
