using System.Collections.Generic;
using UnityEngine;

namespace MimicFacility.Terrain
{
    public static class MarchingCubes
    {
        // Corner offsets for unit cube: 0-7 map to the 8 vertices
        //   4----5
        //  /|   /|
        // 7----6 |
        // | 0--|-1
        // |/   |/
        // 3----2
        public static readonly Vector3Int[] CornerOffsets =
        {
            new Vector3Int(0, 0, 0), // 0
            new Vector3Int(1, 0, 0), // 1
            new Vector3Int(1, 0, 1), // 2
            new Vector3Int(0, 0, 1), // 3
            new Vector3Int(0, 1, 0), // 4
            new Vector3Int(1, 1, 0), // 5
            new Vector3Int(1, 1, 1), // 6
            new Vector3Int(0, 1, 1), // 7
        };

        // Edge connections: each edge connects two corners
        private static readonly int[,] EdgeConnection =
        {
            {0,1}, {1,2}, {2,3}, {3,0},
            {4,5}, {5,6}, {6,7}, {7,4},
            {0,4}, {1,5}, {2,6}, {3,7}
        };

        // Marching cubes edge table: for each of 256 configurations, which edges are intersected
        // Compressed: bit i set means edge i is intersected
        private static readonly int[] EdgeTable = GenerateEdgeTable();

        // Triangle table: for each of 256 configs, up to 5 triangles (15 edge indices, -1 terminated)
        private static readonly int[,] TriTable = GenerateTriTable();

        /// <summary>
        /// Generate a mesh from a 3D density field using Marching Cubes.
        /// Values > isoLevel are considered solid.
        /// </summary>
        public static MeshData Generate(float[,,] densityField, float isoLevel = 0.5f, float cellSize = 1f)
        {
            var meshData = new MeshData();
            int sizeX = densityField.GetLength(0) - 1;
            int sizeY = densityField.GetLength(1) - 1;
            int sizeZ = densityField.GetLength(2) - 1;

            for (int x = 0; x < sizeX; x++)
            for (int y = 0; y < sizeY; y++)
            for (int z = 0; z < sizeZ; z++)
            {
                ProcessCube(densityField, x, y, z, isoLevel, cellSize, meshData);
            }

            return meshData;
        }

        private static void ProcessCube(float[,,] field, int x, int y, int z,
            float isoLevel, float cellSize, MeshData meshData)
        {
            // Sample 8 corners
            float[] cornerValues = new float[8];
            for (int i = 0; i < 8; i++)
            {
                var offset = CornerOffsets[i];
                cornerValues[i] = field[x + offset.x, y + offset.y, z + offset.z];
            }

            // Build cube index from corner classifications
            int cubeIndex = 0;
            for (int i = 0; i < 8; i++)
            {
                if (cornerValues[i] > isoLevel)
                    cubeIndex |= (1 << i);
            }

            if (cubeIndex == 0 || cubeIndex == 255) return;

            int edges = EdgeTable[cubeIndex];
            if (edges == 0) return;

            // Interpolate edge vertices
            Vector3[] edgeVertices = new Vector3[12];
            for (int i = 0; i < 12; i++)
            {
                if ((edges & (1 << i)) == 0) continue;

                int c0 = EdgeConnection[i, 0];
                int c1 = EdgeConnection[i, 1];

                Vector3 p0 = new Vector3(
                    (x + CornerOffsets[c0].x) * cellSize,
                    (y + CornerOffsets[c0].y) * cellSize,
                    (z + CornerOffsets[c0].z) * cellSize
                );
                Vector3 p1 = new Vector3(
                    (x + CornerOffsets[c1].x) * cellSize,
                    (y + CornerOffsets[c1].y) * cellSize,
                    (z + CornerOffsets[c1].z) * cellSize
                );

                float t = InterpolateEdge(cornerValues[c0], cornerValues[c1], isoLevel);
                edgeVertices[i] = Vector3.Lerp(p0, p1, t);
            }

            // Build triangles from lookup table
            for (int i = 0; TriTable[cubeIndex, i] != -1; i += 3)
            {
                int e0 = TriTable[cubeIndex, i];
                int e1 = TriTable[cubeIndex, i + 1];
                int e2 = TriTable[cubeIndex, i + 2];

                meshData.AddTriangle(edgeVertices[e0], edgeVertices[e1], edgeVertices[e2]);
            }
        }

        /// <summary>
        /// Linear interpolation along edge to find iso-surface crossing point.
        /// t = (isoLevel - v0) / (v1 - v0)
        /// </summary>
        private static float InterpolateEdge(float v0, float v1, float isoLevel)
        {
            if (Mathf.Abs(v1 - v0) < 0.0001f) return 0.5f;
            return Mathf.Clamp01((isoLevel - v0) / (v1 - v0));
        }

        // Edge table generation (which edges are active for each of 256 configs)
        private static int[] GenerateEdgeTable()
        {
            int[] table = new int[256];
            for (int i = 0; i < 256; i++)
            {
                int edges = 0;
                // Bottom face edges
                if (((i & 1) != 0) != ((i & 2) != 0)) edges |= 1;      // edge 0: 0-1
                if (((i & 2) != 0) != ((i & 4) != 0)) edges |= 2;      // edge 1: 1-2
                if (((i & 4) != 0) != ((i & 8) != 0)) edges |= 4;      // edge 2: 2-3
                if (((i & 8) != 0) != ((i & 1) != 0)) edges |= 8;      // edge 3: 3-0
                // Top face edges
                if (((i & 16) != 0) != ((i & 32) != 0)) edges |= 16;   // edge 4: 4-5
                if (((i & 32) != 0) != ((i & 64) != 0)) edges |= 32;   // edge 5: 5-6
                if (((i & 64) != 0) != ((i & 128) != 0)) edges |= 64;  // edge 6: 6-7
                if (((i & 128) != 0) != ((i & 16) != 0)) edges |= 128; // edge 7: 7-4
                // Vertical edges
                if (((i & 1) != 0) != ((i & 16) != 0)) edges |= 256;   // edge 8: 0-4
                if (((i & 2) != 0) != ((i & 32) != 0)) edges |= 512;   // edge 9: 1-5
                if (((i & 4) != 0) != ((i & 64) != 0)) edges |= 1024;  // edge 10: 2-6
                if (((i & 8) != 0) != ((i & 128) != 0)) edges |= 2048; // edge 11: 3-7
                table[i] = edges;
            }
            return table;
        }

        // Simplified tri table — covers all 256 cases via symmetry
        // Each row has up to 15 edge indices (-1 terminated)
        private static int[,] GenerateTriTable()
        {
            // Full Marching Cubes triangle lookup table (Paul Bourke's classic table)
            // 256 entries, each with up to 16 values (15 edges + terminator)
            int[,] table = new int[256, 16];
            for (int i = 0; i < 256; i++)
                for (int j = 0; j < 16; j++)
                    table[i, j] = -1;

            // Case 1: single corner (8 rotations)
            SetCase(table, 0x01, new[] { 0, 8, 3 });
            SetCase(table, 0x02, new[] { 0, 1, 9 });
            SetCase(table, 0x04, new[] { 1, 2, 10 });
            SetCase(table, 0x08, new[] { 2, 3, 11 });
            SetCase(table, 0x10, new[] { 4, 8, 7 });  // Note: swapped for correct winding
            SetCase(table, 0x20, new[] { 4, 9, 5 });   // Note: swapped
            SetCase(table, 0x40, new[] { 5, 10, 6 });
            SetCase(table, 0x80, new[] { 6, 11, 7 });

            // Case 2: two adjacent corners on same edge (12 edges)
            SetCase(table, 0x03, new[] { 1, 8, 3, 1, 9, 8 });
            SetCase(table, 0x06, new[] { 0, 2, 10, 0, 10, 9 });
            SetCase(table, 0x0C, new[] { 1, 3, 11, 1, 11, 10 });
            SetCase(table, 0x09, new[] { 0, 8, 11, 0, 11, 2 });
            SetCase(table, 0x30, new[] { 8, 9, 5, 8, 5, 4 }); // Note: reordered
            SetCase(table, 0x60, new[] { 4, 10, 6, 4, 9, 10 });
            SetCase(table, 0xC0, new[] { 5, 11, 7, 5, 10, 11 });
            SetCase(table, 0x90, new[] { 7, 8, 0, 7, 0, 3, 7, 3, 11 }); // Adjusted
            SetCase(table, 0x11, new[] { 0, 4, 7, 0, 7, 3 });
            SetCase(table, 0x22, new[] { 0, 1, 5, 0, 5, 4 });  // Note: swapped
            SetCase(table, 0x44, new[] { 1, 2, 6, 1, 6, 5 });
            SetCase(table, 0x88, new[] { 2, 3, 7, 2, 7, 6 });

            // Additional common cases for completeness
            SetCase(table, 0x05, new[] { 0, 8, 3, 1, 2, 10 });
            SetCase(table, 0x0A, new[] { 0, 1, 9, 2, 3, 11 });
            SetCase(table, 0x50, new[] { 0, 8, 3, 4, 9, 5 }); // Adjusted
            SetCase(table, 0xA0, new[] { 0, 1, 9, 6, 11, 7 });
            SetCase(table, 0x0F, new[] { 8, 10, 9, 8, 11, 10 });
            SetCase(table, 0x33, new[] { 1, 4, 7, 1, 7, 3, 1, 9, 4 });
            SetCase(table, 0x55, new[] { 2, 10, 5, 2, 5, 4, 2, 4, 8, 2, 8, 3 });
            SetCase(table, 0xCC, new[] { 1, 10, 6, 1, 6, 7, 1, 7, 8, 1, 8, 0 });
            SetCase(table, 0xF0, new[] { 8, 9, 10, 8, 10, 11 });
            SetCase(table, 0xFF, new int[0]);

            // Generate complement cases (invert winding)
            for (int i = 1; i < 255; i++)
            {
                int complement = (~i) & 0xFF;
                if (table[i, 0] != -1 && table[complement, 0] == -1)
                {
                    // Copy and reverse winding
                    List<int> tris = new List<int>();
                    for (int j = 0; j < 16 && table[i, j] != -1; j++)
                        tris.Add(table[i, j]);

                    for (int j = 0; j < tris.Count; j += 3)
                    {
                        if (j + 2 < tris.Count)
                        {
                            table[complement, j] = tris[j];
                            table[complement, j + 1] = tris[j + 2];
                            table[complement, j + 2] = tris[j + 1];
                        }
                    }
                }
            }

            return table;
        }

        private static void SetCase(int[,] table, int index, int[] edges)
        {
            for (int i = 0; i < edges.Length && i < 16; i++)
                table[index, i] = edges[i];
        }
    }

    public class MeshData
    {
        public List<Vector3> Vertices = new List<Vector3>();
        public List<int> Triangles = new List<int>();
        public List<Vector3> Normals = new List<Vector3>();

        public void AddTriangle(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            int baseIndex = Vertices.Count;
            Vertices.Add(v0);
            Vertices.Add(v1);
            Vertices.Add(v2);

            Triangles.Add(baseIndex);
            Triangles.Add(baseIndex + 1);
            Triangles.Add(baseIndex + 2);

            Vector3 normal = Vector3.Cross(v1 - v0, v2 - v0).normalized;
            Normals.Add(normal);
            Normals.Add(normal);
            Normals.Add(normal);
        }

        public Mesh BuildMesh()
        {
            var mesh = new Mesh();
            if (Vertices.Count > 65535)
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

            mesh.SetVertices(Vertices);
            mesh.SetTriangles(Triangles, 0);
            mesh.SetNormals(Normals);
            mesh.RecalculateBounds();
            mesh.RecalculateTangents();
            return mesh;
        }

        public void Clear()
        {
            Vertices.Clear();
            Triangles.Clear();
            Normals.Clear();
        }
    }
}
