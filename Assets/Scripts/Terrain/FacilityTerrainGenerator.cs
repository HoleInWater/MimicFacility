using UnityEngine;

namespace MimicFacility.Terrain
{
    public class FacilityTerrainGenerator : MonoBehaviour
    {
        [Header("Grid Size")]
        [SerializeField] private int gridX = 64;
        [SerializeField] private int gridY = 16;
        [SerializeField] private int gridZ = 64;
        [SerializeField] private float cellSize = 1f;

        [Header("Facility Shape")]
        [SerializeField] private float floorThickness = 2f;
        [SerializeField] private float ceilingHeight = 5f;
        [SerializeField] private float wallThickness = 1.5f;
        [SerializeField] private float corridorWidth = 3f;
        [SerializeField] private float roomSize = 10f;

        [Header("Surface Smoothing")]
        [SerializeField] private float isoLevel = 0.5f;
        [SerializeField] private float noiseScale = 0.15f;
        [SerializeField] private float noiseAmplitude = 0.3f;
        [SerializeField] private float slopeBlendDistance = 1.5f;

        [Header("Damage & Decay")]
        [SerializeField] [Range(0f, 1f)] private float decayAmount = 0f;
        [SerializeField] private float decayNoiseScale = 0.08f;

        [Header("Materials")]
        [SerializeField] private Material facilityMaterial;

        private float[,,] densityField;
        private MeshFilter meshFilter;
        private MeshCollider meshCollider;

        private void Start()
        {
            meshFilter = gameObject.GetComponent<MeshFilter>();
            if (meshFilter == null) meshFilter = gameObject.AddComponent<MeshFilter>();

            var renderer = gameObject.GetComponent<MeshRenderer>();
            if (renderer == null) renderer = gameObject.AddComponent<MeshRenderer>();

            meshCollider = gameObject.GetComponent<MeshCollider>();
            if (meshCollider == null) meshCollider = gameObject.AddComponent<MeshCollider>();

            if (facilityMaterial == null)
            {
                var shader = Shader.Find("Standard") ?? Shader.Find("Universal Render Pipeline/Lit");
                facilityMaterial = new Material(shader);
                facilityMaterial.color = new Color(0.3f, 0.3f, 0.33f);
            }
            renderer.material = facilityMaterial;

            GenerateFacility();
        }

        public void GenerateFacility()
        {
            densityField = new float[gridX + 1, gridY + 1, gridZ + 1];

            FillBaseFacility();
            CarveRooms();
            CarveCorridors();
            ApplySurfaceNoise();
            ApplyDecay();
            ApplySlopeSmoothing();

            var meshData = MarchingCubes.Generate(densityField, isoLevel, cellSize);
            var mesh = meshData.BuildMesh();

            meshFilter.mesh = mesh;
            meshCollider.sharedMesh = mesh;

            gameObject.isStatic = true;
        }

        /// <summary>
        /// Fill the density field with solid material (floor + ceiling + walls).
        /// Density > isoLevel = solid, < isoLevel = empty.
        /// </summary>
        private void FillBaseFacility()
        {
            for (int x = 0; x <= gridX; x++)
            for (int y = 0; y <= gridY; y++)
            for (int z = 0; z <= gridZ; z++)
            {
                float worldY = y * cellSize;
                float density;

                // Floor: solid below floorThickness
                // Ceiling: solid above ceilingHeight
                // Walls: solid at grid borders
                if (worldY < floorThickness)
                {
                    // Smooth floor surface using signed distance
                    // d = floorThickness - worldY; positive = solid
                    density = SmoothStep(floorThickness - worldY, slopeBlendDistance);
                }
                else if (worldY > ceilingHeight)
                {
                    density = SmoothStep(worldY - ceilingHeight, slopeBlendDistance);
                }
                else
                {
                    // Interior — start as solid, rooms/corridors will carve out
                    float borderDistX = Mathf.Min(x * cellSize, (gridX - x) * cellSize);
                    float borderDistZ = Mathf.Min(z * cellSize, (gridZ - z) * cellSize);
                    float borderDist = Mathf.Min(borderDistX, borderDistZ);

                    if (borderDist < wallThickness)
                        density = SmoothStep(wallThickness - borderDist, slopeBlendDistance);
                    else
                        density = 1f;
                }

                densityField[x, y, z] = density;
            }
        }

        /// <summary>
        /// Carve rooms into the solid mass using signed distance fields.
        /// Room centers are placed on a grid pattern.
        /// </summary>
        private void CarveRooms()
        {
            float spacing = roomSize + corridorWidth + wallThickness * 2;
            int roomsX = Mathf.FloorToInt((gridX * cellSize - wallThickness * 2) / spacing);
            int roomsZ = Mathf.FloorToInt((gridZ * cellSize - wallThickness * 2) / spacing);

            float startX = wallThickness + roomSize / 2f;
            float startZ = wallThickness + roomSize / 2f;

            for (int rx = 0; rx < roomsX; rx++)
            for (int rz = 0; rz < roomsZ; rz++)
            {
                float centerX = startX + rx * spacing;
                float centerZ = startZ + rz * spacing;
                CarveBox(centerX, centerZ, roomSize, roomSize, floorThickness + 0.1f, ceilingHeight - 0.1f);
            }
        }

        /// <summary>
        /// Carve corridors connecting adjacent rooms.
        /// Uses SDF subtraction with smooth blending.
        /// </summary>
        private void CarveCorridors()
        {
            float spacing = roomSize + corridorWidth + wallThickness * 2;
            int roomsX = Mathf.FloorToInt((gridX * cellSize - wallThickness * 2) / spacing);
            int roomsZ = Mathf.FloorToInt((gridZ * cellSize - wallThickness * 2) / spacing);

            float startX = wallThickness + roomSize / 2f;
            float startZ = wallThickness + roomSize / 2f;

            for (int rx = 0; rx < roomsX; rx++)
            for (int rz = 0; rz < roomsZ; rz++)
            {
                float cx = startX + rx * spacing;
                float cz = startZ + rz * spacing;

                // Corridor to the right
                if (rx + 1 < roomsX)
                {
                    float nextCx = startX + (rx + 1) * spacing;
                    float corridorCenterX = (cx + nextCx) / 2f;
                    float corridorLengthX = nextCx - cx;
                    CarveBox(corridorCenterX, cz, corridorLengthX, corridorWidth,
                        floorThickness + 0.1f, ceilingHeight - 0.5f);
                }

                // Corridor forward
                if (rz + 1 < roomsZ)
                {
                    float nextCz = startZ + (rz + 1) * spacing;
                    float corridorCenterZ = (cz + nextCz) / 2f;
                    float corridorLengthZ = nextCz - cz;
                    CarveBox(cx, corridorCenterZ, corridorWidth, corridorLengthZ,
                        floorThickness + 0.1f, ceilingHeight - 0.5f);
                }
            }
        }

        /// <summary>
        /// Carve a box-shaped void using smooth signed distance subtraction.
        /// SDF_box = max(|px - cx| - halfW, |pz - cz| - halfD)
        /// Smooth min subtraction blends the carved edge into a slope.
        /// </summary>
        private void CarveBox(float centerX, float centerZ, float width, float depth,
            float floorY, float ceilY)
        {
            float halfW = width / 2f;
            float halfD = depth / 2f;

            int minX = Mathf.Max(0, Mathf.FloorToInt((centerX - halfW - slopeBlendDistance) / cellSize));
            int maxX = Mathf.Min(gridX, Mathf.CeilToInt((centerX + halfW + slopeBlendDistance) / cellSize));
            int minZ = Mathf.Max(0, Mathf.FloorToInt((centerZ - halfD - slopeBlendDistance) / cellSize));
            int maxZ = Mathf.Min(gridZ, Mathf.CeilToInt((centerZ + halfD + slopeBlendDistance) / cellSize));
            int minY = Mathf.Max(0, Mathf.FloorToInt(floorY / cellSize));
            int maxY = Mathf.Min(gridY, Mathf.CeilToInt(ceilY / cellSize));

            for (int x = minX; x <= maxX; x++)
            for (int y = minY; y <= maxY; y++)
            for (int z = minZ; z <= maxZ; z++)
            {
                float px = x * cellSize;
                float py = y * cellSize;
                float pz = z * cellSize;

                // Signed distance to box interior (negative = inside)
                float dx = Mathf.Abs(px - centerX) - halfW;
                float dy = Mathf.Max(floorY - py, py - ceilY);
                float dz = Mathf.Abs(pz - centerZ) - halfD;

                // Box SDF: max of all axes (negative inside, positive outside)
                float sdfBox = Mathf.Max(dx, Mathf.Max(dy, dz));

                // Smooth subtraction: blend from solid to empty over slopeBlendDistance
                // This creates natural slopes instead of sharp edges
                float carveAmount = 1f - SmoothStep(sdfBox + slopeBlendDistance, slopeBlendDistance * 2f);

                densityField[x, y, z] = Mathf.Min(densityField[x, y, z], 1f - carveAmount);
            }
        }

        /// <summary>
        /// Add noise to surfaces for organic/industrial feel.
        /// Only affects voxels near the iso-surface (within slopeBlendDistance).
        /// Uses 3D Perlin noise offset to avoid axis-aligned patterns.
        /// </summary>
        private void ApplySurfaceNoise()
        {
            if (noiseAmplitude <= 0f) return;

            float offsetX = Random.Range(0f, 1000f);
            float offsetZ = Random.Range(0f, 1000f);

            for (int x = 0; x <= gridX; x++)
            for (int y = 0; y <= gridY; y++)
            for (int z = 0; z <= gridZ; z++)
            {
                float current = densityField[x, y, z];
                if (current < 0.1f || current > 0.9f) continue;

                float nx = (x * cellSize + offsetX) * noiseScale;
                float ny = y * cellSize * noiseScale * 0.5f;
                float nz = (z * cellSize + offsetZ) * noiseScale;

                float noise = Perlin3D(nx, ny, nz) * noiseAmplitude;
                densityField[x, y, z] = Mathf.Clamp01(current + noise);
            }
        }

        /// <summary>
        /// Apply decay/damage based on a corruption parameter.
        /// Higher decay = more material removed, creating holes and cracks.
        /// </summary>
        private void ApplyDecay()
        {
            if (decayAmount <= 0f) return;

            float offset = Random.Range(0f, 1000f);

            for (int x = 0; x <= gridX; x++)
            for (int y = 0; y <= gridY; y++)
            for (int z = 0; z <= gridZ; z++)
            {
                float current = densityField[x, y, z];
                if (current < isoLevel) continue;

                float nx = (x * cellSize + offset) * decayNoiseScale;
                float ny = y * cellSize * decayNoiseScale;
                float nz = (z * cellSize + offset * 0.7f) * decayNoiseScale;

                float decayNoise = Perlin3D(nx, ny, nz);

                if (decayNoise < decayAmount)
                {
                    float strength = 1f - (decayNoise / decayAmount);
                    densityField[x, y, z] = Mathf.Lerp(current, 0f, strength * decayAmount);
                }
            }
        }

        /// <summary>
        /// Gaussian blur pass on the density field to smooth transitions.
        /// Creates natural-looking slopes where walls meet floors and ceilings.
        /// Kernel: f(d) = e^(-d^2 / (2 * sigma^2))
        /// </summary>
        private void ApplySlopeSmoothing()
        {
            int radius = Mathf.CeilToInt(slopeBlendDistance / cellSize);
            if (radius < 1) return;

            float sigma = slopeBlendDistance / (2f * cellSize);
            float[,,] smoothed = new float[gridX + 1, gridY + 1, gridZ + 1];

            for (int x = 0; x <= gridX; x++)
            for (int y = 0; y <= gridY; y++)
            for (int z = 0; z <= gridZ; z++)
            {
                float totalWeight = 0f;
                float totalValue = 0f;

                for (int dx = -radius; dx <= radius; dx++)
                for (int dy = -radius; dy <= radius; dy++)
                for (int dz = -radius; dz <= radius; dz++)
                {
                    int sx = x + dx;
                    int sy = y + dy;
                    int sz = z + dz;

                    if (sx < 0 || sx > gridX || sy < 0 || sy > gridY || sz < 0 || sz > gridZ)
                        continue;

                    // Gaussian weight: w = e^(-(dx^2 + dy^2 + dz^2) / (2 * sigma^2))
                    float distSq = dx * dx + dy * dy + dz * dz;
                    float weight = Mathf.Exp(-distSq / (2f * sigma * sigma));

                    totalWeight += weight;
                    totalValue += densityField[sx, sy, sz] * weight;
                }

                smoothed[x, y, z] = totalValue / totalWeight;
            }

            densityField = smoothed;
        }

        /// <summary>
        /// Smooth step function: 0 when x <= 0, 1 when x >= edge.
        /// f(t) = 3t^2 - 2t^3 (Hermite interpolation)
        /// Creates C1-continuous transitions instead of hard edges.
        /// </summary>
        private float SmoothStep(float x, float edge)
        {
            if (edge <= 0f) return x > 0f ? 1f : 0f;
            float t = Mathf.Clamp01(x / edge);
            return t * t * (3f - 2f * t);
        }

        /// <summary>
        /// 3D Perlin noise from Unity's 2D Perlin.
        /// Combines three 2D samples on different planes.
        /// f(x,y,z) = (Perlin(x,y) + Perlin(y,z) + Perlin(x,z)) / 3 - 0.5
        /// Range: approximately [-0.5, 0.5]
        /// </summary>
        private float Perlin3D(float x, float y, float z)
        {
            float xy = Mathf.PerlinNoise(x, y);
            float yz = Mathf.PerlinNoise(y, z);
            float xz = Mathf.PerlinNoise(x, z);
            return (xy + yz + xz) / 3f - 0.5f;
        }

        public void SetDecay(float amount)
        {
            decayAmount = Mathf.Clamp01(amount);
            GenerateFacility();
        }
    }
}
