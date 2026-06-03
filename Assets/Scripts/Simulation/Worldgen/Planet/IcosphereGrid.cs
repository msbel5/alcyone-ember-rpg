using System;
using System.Collections.Generic;

namespace EmberCrpg.Simulation.Worldgen.Planet
{
    /// <summary>Deterministic icosahedron-subdivision grid for spherical worldgen.</summary>
    public sealed class IcosphereGrid
    {
        public const int MaxSubdivisionLevel = 8;

        private readonly IcosphereTile[] _tiles;

        private IcosphereGrid(int subdivisionLevel, IcosphereTile[] tiles)
        {
            SubdivisionLevel = subdivisionLevel;
            _tiles = tiles;
            Tiles = Array.AsReadOnly(_tiles);
        }

        public int SubdivisionLevel { get; }
        public int Count => _tiles.Length;
        public IReadOnlyList<IcosphereTile> Tiles { get; }

        public IcosphereTile TileAt(int id)
        {
            if (id < 0 || id >= _tiles.Length)
                throw new ArgumentOutOfRangeException(nameof(id), id, "Tile id is outside the grid.");
            return _tiles[id];
        }

        public static int ExpectedTileCount(int subdivisionLevel)
        {
            ValidateLevel(subdivisionLevel);
            int multiplier = 1;
            for (int i = 0; i < subdivisionLevel; i++)
                multiplier *= 4;
            return (10 * multiplier) + 2;
        }

        public static IcosphereGrid Build(int subdivisionLevel)
        {
            ValidateLevel(subdivisionLevel);

            // Start from the 12 normalized vertices and 20 faces of a regular
            // icosahedron. Each subdivision splits every triangle into four,
            // with midpoint vertices de-duplicated by the sorted parent edge
            // key so shared face edges always reuse the same tile id.
            var vertices = CreateIcosahedronVertices();
            var faces = CreateIcosahedronFaces();

            for (int level = 0; level < subdivisionLevel; level++)
            {
                var midpointCache = new Dictionary<long, int>();
                var nextFaces = new List<Triangle>(faces.Count * 4);
                for (int i = 0; i < faces.Count; i++)
                {
                    Triangle face = faces[i];
                    int ab = GetMidpoint(face.A, face.B, vertices, midpointCache);
                    int bc = GetMidpoint(face.B, face.C, vertices, midpointCache);
                    int ca = GetMidpoint(face.C, face.A, vertices, midpointCache);

                    nextFaces.Add(new Triangle(face.A, ab, ca));
                    nextFaces.Add(new Triangle(face.B, bc, ab));
                    nextFaces.Add(new Triangle(face.C, ca, bc));
                    nextFaces.Add(new Triangle(ab, bc, ca));
                }

                faces = nextFaces;
            }

            var neighbors = BuildNeighborLists(vertices.Count, faces);
            var tiles = new IcosphereTile[vertices.Count];
            for (int i = 0; i < vertices.Count; i++)
                tiles[i] = new IcosphereTile(i, vertices[i], Array.AsReadOnly(neighbors[i].ToArray()));

            return new IcosphereGrid(subdivisionLevel, tiles);
        }

        private static void ValidateLevel(int subdivisionLevel)
        {
            if (subdivisionLevel < 0 || subdivisionLevel > MaxSubdivisionLevel)
                throw new ArgumentOutOfRangeException(nameof(subdivisionLevel), subdivisionLevel, "Subdivision level is outside the supported deterministic range.");
        }

        private static List<PlanetVector> CreateIcosahedronVertices()
        {
            double t = (1d + Math.Sqrt(5d)) / 2d;
            var vertices = new List<PlanetVector>
            {
                new PlanetVector(-1d, t, 0d).Normalize(),
                new PlanetVector(1d, t, 0d).Normalize(),
                new PlanetVector(-1d, -t, 0d).Normalize(),
                new PlanetVector(1d, -t, 0d).Normalize(),
                new PlanetVector(0d, -1d, t).Normalize(),
                new PlanetVector(0d, 1d, t).Normalize(),
                new PlanetVector(0d, -1d, -t).Normalize(),
                new PlanetVector(0d, 1d, -t).Normalize(),
                new PlanetVector(t, 0d, -1d).Normalize(),
                new PlanetVector(t, 0d, 1d).Normalize(),
                new PlanetVector(-t, 0d, -1d).Normalize(),
                new PlanetVector(-t, 0d, 1d).Normalize(),
            };
            return vertices;
        }

        private static List<Triangle> CreateIcosahedronFaces()
        {
            return new List<Triangle>
            {
                new Triangle(0, 11, 5),
                new Triangle(0, 5, 1),
                new Triangle(0, 1, 7),
                new Triangle(0, 7, 10),
                new Triangle(0, 10, 11),
                new Triangle(1, 5, 9),
                new Triangle(5, 11, 4),
                new Triangle(11, 10, 2),
                new Triangle(10, 7, 6),
                new Triangle(7, 1, 8),
                new Triangle(3, 9, 4),
                new Triangle(3, 4, 2),
                new Triangle(3, 2, 6),
                new Triangle(3, 6, 8),
                new Triangle(3, 8, 9),
                new Triangle(4, 9, 5),
                new Triangle(2, 4, 11),
                new Triangle(6, 2, 10),
                new Triangle(8, 6, 7),
                new Triangle(9, 8, 1),
            };
        }

        private static int GetMidpoint(int a, int b, List<PlanetVector> vertices, Dictionary<long, int> midpointCache)
        {
            long key = EdgeKey(a, b);
            if (midpointCache.TryGetValue(key, out int existing))
                return existing;

            int id = vertices.Count;
            vertices.Add(PlanetVector.UnitMidpoint(vertices[a], vertices[b]));
            midpointCache.Add(key, id);
            return id;
        }

        private static List<int>[] BuildNeighborLists(int vertexCount, List<Triangle> faces)
        {
            var neighbors = new List<int>[vertexCount];
            for (int i = 0; i < vertexCount; i++)
                neighbors[i] = new List<int>(6);

            for (int i = 0; i < faces.Count; i++)
            {
                Triangle face = faces[i];
                AddNeighborPair(neighbors, face.A, face.B);
                AddNeighborPair(neighbors, face.B, face.C);
                AddNeighborPair(neighbors, face.C, face.A);
            }

            for (int i = 0; i < neighbors.Length; i++)
                neighbors[i].Sort();

            return neighbors;
        }

        private static void AddNeighborPair(List<int>[] neighbors, int a, int b)
        {
            AddNeighbor(neighbors[a], b);
            AddNeighbor(neighbors[b], a);
        }

        private static void AddNeighbor(List<int> neighbors, int neighbor)
        {
            for (int i = 0; i < neighbors.Count; i++)
            {
                if (neighbors[i] == neighbor)
                    return;
            }

            neighbors.Add(neighbor);
        }

        private static long EdgeKey(int a, int b)
        {
            int low = a < b ? a : b;
            int high = a < b ? b : a;
            return ((long)low << 32) | (uint)high;
        }

        private struct Triangle
        {
            public Triangle(int a, int b, int c)
            {
                A = a;
                B = b;
                C = c;
            }

            public int A { get; }
            public int B { get; }
            public int C { get; }
        }
    }
}
