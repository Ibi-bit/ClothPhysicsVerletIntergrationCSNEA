using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace PhysicsCSAlevlProject;

public class MeshComponent
{
    private HashSet<int> _stickIDs;
    public HashSet<int> StickIDs
    {
        get => _stickIDs;
        set
        {
            if (value == null)
                throw new ArgumentNullException(nameof(StickIDs), "StickIDs cannot be null.");
            _stickIDs = value;
            _triangleVertices = Array.Empty<VertexPositionColor>();
        }
    }
    public Vector3 _color = new Vector3(1, 1, 1);
    public bool DrawPolygon = false;
    private VertexPositionColor[] _triangleVertices;

    public MeshComponent(HashSet<int> stickIDs)
    {
        StickIDs = stickIDs;
    }

    
    public bool DrawPolygonMouse(Vector2 mousePos)
    {
        if (StickIDs.Count == 0 || _triangleVertices == null || _triangleVertices.Length < 3)
        {
            return false;
        }

        for (int i = 0; i <= _triangleVertices.Length - 3; i += 3)
        {
            Vector2 a = new(_triangleVertices[i].Position.X, _triangleVertices[i].Position.Y);
            Vector2 b = new(
                _triangleVertices[i + 1].Position.X,
                _triangleVertices[i + 1].Position.Y
            );
            Vector2 c = new(
                _triangleVertices[i + 2].Position.X,
                _triangleVertices[i + 2].Position.Y
            );

            if (PointInTriangle(mousePos, a, b, c))
            {
                DrawPolygon = !DrawPolygon;
                return true;
            }
        }

        return false;
    }

    private static bool PointInTriangle(Vector2 p, Vector2 a, Vector2 b, Vector2 c)
    {
        float Sign(Vector2 p1, Vector2 p2, Vector2 p3)
        {
            return (p1.X - p3.X) * (p2.Y - p3.Y) - (p2.X - p3.X) * (p1.Y - p3.Y);
        }

        float d1 = Sign(p, a, b);
        float d2 = Sign(p, b, c);
        float d3 = Sign(p, c, a);

        bool hasNeg = d1 < 0f || d2 < 0f || d3 < 0f;
        bool hasPos = d1 > 0f || d2 > 0f || d3 > 0f;

        return !(hasNeg && hasPos);
    }

    internal void UpdateMesh(
        IReadOnlyDictionary<int, DrawableParticle> particles,
        IReadOnlyDictionary<int, Mesh.MeshStick> sticks
    )
    {
        var allPoints = new List<Vector2>();

        Color fillColor = new Color(_color);

        foreach (int stickId in StickIDs)
        {
            if (!sticks.TryGetValue(stickId, out var stick))
            {
                continue;
            }

            if (particles.TryGetValue(stick.P1Id, out var p1))
            {
                allPoints.Add(p1.Position);
            }
            if (particles.TryGetValue(stick.P2Id, out var p2))
            {
                allPoints.Add(p2.Position);
            }
        }

        _triangleVertices = BuildTriangleFan(BuildConvexHull(allPoints), fillColor);
    }

    private List<int> BuildOrderedBoundaryParticles(IReadOnlyDictionary<int, Mesh.MeshStick> sticks)
    {
        var adjacency = new Dictionary<int, List<int>>();

        foreach (int stickId in StickIDs)
        {
            if (!sticks.TryGetValue(stickId, out var stick))
            {
                continue;
            }

            if (!adjacency.TryGetValue(stick.P1Id, out var neighbors1))
            {
                neighbors1 = new List<int>();
                adjacency[stick.P1Id] = neighbors1;
            }
            if (!adjacency.TryGetValue(stick.P2Id, out var neighbors2))
            {
                neighbors2 = new List<int>();
                adjacency[stick.P2Id] = neighbors2;
            }

            neighbors1.Add(stick.P2Id);
            neighbors2.Add(stick.P1Id);
        }

        if (adjacency.Count == 0)
        {
            return new List<int>();
        }

        int start = -1;
        foreach (var kvp in adjacency)
        {
            if (kvp.Value.Count == 1)
            {
                start = kvp.Key;
                break;
            }
        }

        if (start == -1)
        {
            start = adjacency.Keys.First();
        }

        var ordered = new List<int>();
        var visited = new HashSet<int>();
        int current = start;
        int previous = -1;

        while (!visited.Contains(current) && adjacency.ContainsKey(current))
        {
            ordered.Add(current);
            visited.Add(current);

            var neighbors = adjacency[current];
            int next = -1;
            foreach (var neighbor in neighbors)
            {
                if (neighbor != previous)
                {
                    next = neighbor;
                    break;
                }
            }

            if (next == -1)
            {
                break;
            }

            previous = current;
            current = next;

            if (current == start)
            {
                break;
            }
        }

        return ordered;
    }

    private VertexPositionColor[] BuildTriangleFan(List<Vector2> points, Color color)
    {
        if (points == null || points.Count < 3)
        {
            return Array.Empty<VertexPositionColor>();
        }

        var boundary = new List<Vector2>(points);
        if (boundary.Count > 1 && boundary[0] == boundary[boundary.Count - 1])
        {
            boundary.RemoveAt(boundary.Count - 1);
        }

        if (boundary.Count < 3)
        {
            return Array.Empty<VertexPositionColor>();
        }

        var vertices = new List<VertexPositionColor>((boundary.Count - 2) * 3);

        for (int i = 1; i < boundary.Count - 1; i++)
        {
            vertices.Add(new VertexPositionColor(new Vector3(boundary[0], 0), color));
            vertices.Add(new VertexPositionColor(new Vector3(boundary[i], 0), color));
            vertices.Add(new VertexPositionColor(new Vector3(boundary[i + 1], 0), color));
        }

        return vertices.ToArray();
    }

    private static List<Vector2> BuildConvexHull(List<Vector2> points)
    {
        if (points == null || points.Count == 0)
        {
            return new List<Vector2>();
        }

        List<Vector2> sorted = new List<Vector2>(points);
        sorted.Sort(
            (a, b) =>
            {
                int xComparison = a.X.CompareTo(b.X);
                if (xComparison != 0)
                {
                    return xComparison;
                }

                return a.Y.CompareTo(b.Y);
            }
        );

        List<Vector2> hull = new List<Vector2>();

        static float Cross(Vector2 o, Vector2 a, Vector2 b)
        {
            return (a.X - o.X) * (b.Y - o.Y) - (a.Y - o.Y) * (b.X - o.X);
        }

        foreach (var point in sorted)
        {
            while (hull.Count >= 2 && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0)
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(point);
        }

        int lowerCount = hull.Count;
        for (int i = sorted.Count - 2; i >= 0; i--)
        {
            Vector2 point = sorted[i];
            while (
                hull.Count > lowerCount
                && Cross(hull[hull.Count - 2], hull[hull.Count - 1], point) <= 0
            )
            {
                hull.RemoveAt(hull.Count - 1);
            }

            hull.Add(point);
        }

        if (hull.Count > 1)
        {
            hull.RemoveAt(hull.Count - 1);
        }

        return hull;
    }

    public void Draw(GraphicsDevice graphicsDevice, BasicEffect basicEffect)
    {
        if (
            _triangleVertices == null
            || _triangleVertices.Length < 3
            || basicEffect == null
            || !DrawPolygon
        )
            return;

        foreach (var pass in basicEffect.CurrentTechnique.Passes)
        {
            pass.Apply();
            graphicsDevice.DrawUserPrimitives(
                PrimitiveType.TriangleList,
                _triangleVertices,
                0,
                _triangleVertices.Length / 3
            );
        }
    }
}
