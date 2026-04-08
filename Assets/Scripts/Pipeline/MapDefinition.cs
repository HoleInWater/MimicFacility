using System;
using System.Collections.Generic;
using UnityEngine;

namespace MimicFacility.Pipeline
{
    [Serializable]
    public class MapDefinition
    {
        public string mapName = "Unnamed Facility";
        public string lightingPreset = "Horror_Dim";
        public int seed = -1;

        public MapLayout layout = new MapLayout();
        public List<EntitySpawn> entitySpawns = new List<EntitySpawn>();
        public List<GearSpawn> gearSpawns = new List<GearSpawn>();
        public LightingConfig lighting = new LightingConfig();
        public AudioConfig audio = new AudioConfig();
        public List<string> requiredSystems = new List<string>();
    }

    [Serializable]
    public class MapLayout
    {
        public int roomCount = 6;
        public float roomWidth = 10f;
        public float roomDepth = 10f;
        public float wallHeight = 4f;
        public float corridorWidth = 3f;
        public bool generateExtractionZone = true;
        public bool useMarchingCubes = false;
        public int mcGridX = 64;
        public int mcGridY = 16;
        public int mcGridZ = 64;
        public float mcDecay = 0f;
    }

    [Serializable]
    public class EntitySpawn
    {
        public string entityType;
        public int count = 1;
        public string preferredZone = "";
        public float spawnDelay = 0f;
    }

    [Serializable]
    public class GearSpawn
    {
        public string gearType;
        public int count = 1;
        public string preferredZone = "";
    }

    [Serializable]
    public class LightingConfig
    {
        public Color ambientColor = new Color(0.05f, 0.05f, 0.08f);
        public bool enableFog = true;
        public Color fogColor = new Color(0.02f, 0.02f, 0.03f);
        public float fogDensity = 0.03f;
        public float roomLightIntensity = 1.2f;
        public Color roomLightColor = new Color(0.8f, 0.9f, 1f);
    }

    [Serializable]
    public class AudioConfig
    {
        public float reverbLevel = 0.5f;
        public float ambientVolume = 0.3f;
        public bool enableProximityChat = true;
        public float voiceMaxRange = 30f;
    }
}
