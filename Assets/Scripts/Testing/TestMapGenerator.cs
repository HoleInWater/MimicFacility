using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;
using MimicFacility.Facility;

namespace MimicFacility.Testing
{
    public class TestMapGenerator : MonoBehaviour
    {
        [Header("Map Settings")]
        [SerializeField] private int roomCount = 6;
        [SerializeField] private float roomWidth = 10f;
        [SerializeField] private float roomDepth = 10f;
        [SerializeField] private float wallHeight = 4f;
        [SerializeField] private float corridorWidth = 3f;
        [SerializeField] private float corridorLength = 6f;
        [SerializeField] private float wallThickness = 0.3f;

        [Header("Materials")]
        [SerializeField] private Material floorMaterial;
        [SerializeField] private Material wallMaterial;
        [SerializeField] private Material ceilingMaterial;

        [Header("Lighting")]
        [SerializeField] private Color normalLightColor = new Color(0.8f, 0.9f, 1f);
        [SerializeField] private float lightIntensity = 1.2f;

        private Transform mapRoot;
        private readonly List<Vector3> roomCenters = new List<Vector3>();
        private readonly List<string> zoneTags = new List<string>
        {
            "ZoneA", "ZoneB", "ZoneC", "ZoneD", "ZoneE", "ZoneF",
            "ZoneG", "ZoneH", "ZoneI", "ZoneJ", "ZoneK", "ZoneL"
        };

        public List<Vector3> RoomCenters => roomCenters;

        public void GenerateMap()
        {
            if (mapRoot != null) DestroyImmediate(mapRoot.gameObject);

            mapRoot = new GameObject("GeneratedFacility").transform;
            EnsureMaterials();

            int cols = Mathf.CeilToInt(Mathf.Sqrt(roomCount));
            int rows = Mathf.CeilToInt((float)roomCount / cols);

            float spacingX = roomWidth + corridorLength;
            float spacingZ = roomDepth + corridorLength;

            int roomIndex = 0;
            for (int row = 0; row < rows && roomIndex < roomCount; row++)
            {
                for (int col = 0; col < cols && roomIndex < roomCount; col++)
                {
                    Vector3 center = new Vector3(col * spacingX, 0f, row * spacingZ);
                    string zone = zoneTags[roomIndex % zoneTags.Count];

                    BuildRoom(center, zone, roomIndex);
                    roomCenters.Add(center);

                    if (col < cols - 1 && roomIndex + 1 < roomCount)
                    {
                        Vector3 corridorStart = center + Vector3.right * (roomWidth / 2f);
                        BuildCorridor(corridorStart, Vector3.right, corridorLength, zone);
                    }

                    if (row < rows - 1)
                    {
                        int nextRowIndex = roomIndex + cols;
                        if (nextRowIndex < roomCount)
                        {
                            Vector3 corridorStart = center + Vector3.forward * (roomDepth / 2f);
                            BuildCorridor(corridorStart, Vector3.forward, corridorLength, zone);
                        }
                    }

                    roomIndex++;
                }
            }

            BuildExtractionZone(roomCenters[roomCenters.Count - 1] + Vector3.right * spacingX);

            var surface = mapRoot.gameObject.AddComponent<NavMeshSurface>();
            surface.collectObjects = CollectObjects.Children;
            surface.BuildNavMesh();
        }

        private void BuildRoom(Vector3 center, string zoneTag, int roomIndex)
        {
            Transform room = new GameObject($"Room_{roomIndex}_{zoneTag}").transform;
            room.SetParent(mapRoot);
            room.position = center;

            CreateFloor(room, center, roomWidth, roomDepth);
            CreateCeiling(room, center, roomWidth, roomDepth);

            CreateWall(room, center + Vector3.forward * roomDepth / 2f, roomWidth, wallHeight, wallThickness, Quaternion.identity, "NorthWall");
            CreateWall(room, center - Vector3.forward * roomDepth / 2f, roomWidth, wallHeight, wallThickness, Quaternion.identity, "SouthWall");
            CreateWall(room, center + Vector3.right * roomWidth / 2f, roomDepth, wallHeight, wallThickness, Quaternion.Euler(0, 90, 0), "EastWall");
            CreateWall(room, center - Vector3.right * roomWidth / 2f, roomDepth, wallHeight, wallThickness, Quaternion.Euler(0, 90, 0), "WestWall");

            CreateRoomLight(room, center + Vector3.up * (wallHeight - 0.5f), zoneTag);

            if (roomIndex % 2 == 0)
                CreateDoor(room, center + Vector3.right * roomWidth / 2f + Vector3.up * wallHeight / 2f, zoneTag);

            if (roomIndex % 3 == 0)
                CreateSporeVent(room, center + Vector3.up * 0.1f + Vector3.right * 2f, zoneTag);

            if (roomIndex % 2 == 1)
                CreateTerminal(room, center + Vector3.forward * (roomDepth / 2f - 0.5f) + Vector3.up * 1f, zoneTag, roomIndex);

            for (int i = 0; i < 3; i++)
            {
                Vector3 gearPos = center + new Vector3(
                    Random.Range(-roomWidth / 3f, roomWidth / 3f),
                    0.3f,
                    Random.Range(-roomDepth / 3f, roomDepth / 3f)
                );
                CreateGearSpawnPoint(room, gearPos);
            }

            var spawnPoint = new GameObject($"SpawnPoint_{roomIndex}");
            spawnPoint.transform.SetParent(room);
            spawnPoint.transform.position = center + Vector3.up * 0.5f;
            spawnPoint.tag = "Respawn";
        }

        private void BuildCorridor(Vector3 start, Vector3 direction, float length, string zoneTag)
        {
            Transform corridor = new GameObject("Corridor").transform;
            corridor.SetParent(mapRoot);

            Vector3 center = start + direction * length / 2f;
            Vector3 perpendicular = Vector3.Cross(direction, Vector3.up);

            float corridorDepth = direction == Vector3.right ? corridorWidth : length;
            float corridorWidthActual = direction == Vector3.right ? length : corridorWidth;

            CreateFloor(corridor, center, corridorWidthActual, corridorDepth);
            CreateCeiling(corridor, center, corridorWidthActual, corridorDepth);

            CreateWall(corridor, center + perpendicular * corridorWidth / 2f, length, wallHeight, wallThickness,
                Quaternion.LookRotation(direction), "CorridorWallL");
            CreateWall(corridor, center - perpendicular * corridorWidth / 2f, length, wallHeight, wallThickness,
                Quaternion.LookRotation(direction), "CorridorWallR");

            var dimLight = new GameObject("CorridorLight");
            dimLight.transform.SetParent(corridor);
            dimLight.transform.position = center + Vector3.up * (wallHeight - 0.5f);
            var light = dimLight.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = normalLightColor * 0.5f;
            light.intensity = lightIntensity * 0.4f;
            light.range = length;
        }

        private void BuildExtractionZone(Vector3 center)
        {
            Transform zone = new GameObject("ExtractionZone").transform;
            zone.SetParent(mapRoot);
            zone.position = center;

            CreateFloor(zone, center, 8f, 8f);

            var trigger = new GameObject("ExtractionTrigger");
            trigger.transform.SetParent(zone);
            trigger.transform.position = center + Vector3.up;
            var col = trigger.AddComponent<BoxCollider>();
            col.size = new Vector3(8f, 3f, 8f);
            col.isTrigger = true;
            trigger.tag = "ExtractionZone";

            var marker = new GameObject("ExtractionLight");
            marker.transform.SetParent(zone);
            marker.transform.position = center + Vector3.up * 3f;
            var light = marker.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = Color.green;
            light.intensity = 3f;
            light.range = 15f;

            roomCenters.Add(center);
        }

        private void CreateFloor(Transform parent, Vector3 center, float width, float depth)
        {
            var floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
            floor.name = "Floor";
            floor.transform.SetParent(parent);
            floor.transform.position = center - Vector3.up * 0.05f;
            floor.transform.localScale = new Vector3(width, 0.1f, depth);
            floor.isStatic = true;
            if (floorMaterial != null) floor.GetComponent<Renderer>().material = floorMaterial;
        }

        private void CreateCeiling(Transform parent, Vector3 center, float width, float depth)
        {
            var ceiling = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ceiling.name = "Ceiling";
            ceiling.transform.SetParent(parent);
            ceiling.transform.position = center + Vector3.up * wallHeight;
            ceiling.transform.localScale = new Vector3(width, 0.1f, depth);
            ceiling.isStatic = true;
            if (ceilingMaterial != null) ceiling.GetComponent<Renderer>().material = ceilingMaterial;
        }

        private void CreateWall(Transform parent, Vector3 position, float length, float height, float thickness, Quaternion rotation, string name)
        {
            var wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
            wall.name = name;
            wall.transform.SetParent(parent);
            wall.transform.position = position + Vector3.up * height / 2f;
            wall.transform.rotation = rotation;
            wall.transform.localScale = new Vector3(length, height, thickness);
            wall.isStatic = true;
            if (wallMaterial != null) wall.GetComponent<Renderer>().material = wallMaterial;
        }

        private void CreateRoomLight(Transform parent, Vector3 position, string zoneTag)
        {
            var lightObj = new GameObject($"Light_{zoneTag}");
            lightObj.transform.SetParent(parent);
            lightObj.transform.position = position;

            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = normalLightColor;
            light.intensity = lightIntensity;
            light.range = Mathf.Max(roomWidth, roomDepth) * 1.2f;

            var facilityLight = lightObj.AddComponent<FacilityLight>();
        }

        private void CreateDoor(Transform parent, Vector3 position, string zoneTag)
        {
            var doorObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            doorObj.name = $"Door_{zoneTag}";
            doorObj.transform.SetParent(parent);
            doorObj.transform.position = position;
            doorObj.transform.localScale = new Vector3(0.2f, wallHeight, corridorWidth);
            if (wallMaterial != null)
            {
                var renderer = doorObj.GetComponent<Renderer>();
                renderer.material = new Material(wallMaterial);
                renderer.material.color = new Color(0.4f, 0.3f, 0.2f);
            }

            doorObj.AddComponent<AudioSource>();
            var door = doorObj.AddComponent<FacilityDoor>();
        }

        private void CreateSporeVent(Transform parent, Vector3 position, string zoneTag)
        {
            var ventObj = new GameObject($"SporeVent_{zoneTag}");
            ventObj.transform.SetParent(parent);
            ventObj.transform.position = position;

            var sphere = ventObj.AddComponent<SphereCollider>();
            sphere.radius = 3f;
            sphere.isTrigger = true;

            ventObj.AddComponent<ParticleSystem>();
            ventObj.AddComponent<AudioSource>();
            var vent = ventObj.AddComponent<SporeVent>();
        }

        private void CreateTerminal(Transform parent, Vector3 position, string zoneTag, int roomIndex)
        {
            var termObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
            termObj.name = $"Terminal_{zoneTag}";
            termObj.transform.SetParent(parent);
            termObj.transform.position = position;
            termObj.transform.localScale = new Vector3(1f, 1.5f, 0.3f);
            if (wallMaterial != null)
            {
                var renderer = termObj.GetComponent<Renderer>();
                renderer.material = new Material(wallMaterial);
                renderer.material.color = new Color(0.1f, 0.1f, 0.1f);
                renderer.material.SetColor("_EmissionColor", Color.green * 0.3f);
            }

            termObj.AddComponent<AudioSource>();
            var terminal = termObj.AddComponent<ResearchTerminal>();
        }

        private void CreateGearSpawnPoint(Transform parent, Vector3 position)
        {
            var point = new GameObject("GearSpawnPoint");
            point.transform.SetParent(parent);
            point.transform.position = position;

            var visual = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            visual.name = "GearMarker";
            visual.transform.SetParent(point.transform);
            visual.transform.localPosition = Vector3.zero;
            visual.transform.localScale = Vector3.one * 0.2f;
            visual.GetComponent<Collider>().enabled = false;
            var renderer = visual.GetComponent<Renderer>();
            renderer.material = new Material(Shader.Find("Standard"));
            renderer.material.color = Color.yellow;
            renderer.material.SetColor("_EmissionColor", Color.yellow * 0.5f);
            renderer.material.EnableKeyword("_EMISSION");
        }

        private void EnsureMaterials()
        {
            var shader = Shader.Find("Standard");
            if (shader == null) shader = Shader.Find("Universal Render Pipeline/Lit");

            if (floorMaterial == null)
            {
                floorMaterial = new Material(shader);
                floorMaterial.color = new Color(0.25f, 0.25f, 0.28f);
            }
            if (wallMaterial == null)
            {
                wallMaterial = new Material(shader);
                wallMaterial.color = new Color(0.35f, 0.35f, 0.38f);
            }
            if (ceilingMaterial == null)
            {
                ceilingMaterial = new Material(shader);
                ceilingMaterial.color = new Color(0.2f, 0.2f, 0.22f);
            }
        }
    }
}
