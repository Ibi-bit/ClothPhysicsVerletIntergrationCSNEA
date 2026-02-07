using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using VectorGraphics;

namespace PhysicsCSAlevlProject;

class Particle
{
    public Vector2 Position;
    public Vector2 PreviousPosition;
    public float Mass;
    public Vector2 AccumulatedForce;
    public int ID;

    public float TotalForceMagnitude;

    public bool IsPinned;
    public bool IsSelected;

    public Particle()
    {
        Position = Vector2.Zero;
        PreviousPosition = Vector2.Zero;
        Mass = 1.0f;
        AccumulatedForce = Vector2.Zero;
        IsPinned = false;
        IsSelected = false;
        ID = -1;
        TotalForceMagnitude = 0f;
    }

    public Particle(Vector2 position, float mass, bool isPinned)
    {
        Position = position;
        PreviousPosition = position;
        Mass = mass;
        AccumulatedForce = Vector2.Zero;

        IsPinned = isPinned || mass <= 0;
        IsSelected = false;
        ID = -1;
    }
}

public abstract class Collider
{
    public Vector2 Position;
    public abstract bool ContainsPoint(Vector2 point, out Vector2 closestPoint);
}

public class CircleCollider : Collider
{
    public float Radius;

    public CircleCollider(Vector2 center, float radius)
    {
        Position = center;
        Radius = radius;
    }

    public override bool ContainsPoint(Vector2 point, out Vector2 closestPoint)
    {
        Vector2 direction = point - Position;
        float distance = direction.LengthSquared();

        if (distance <= Radius * Radius)
        {
            closestPoint = Position + Vector2.Normalize(direction) * Radius;
            return true;
        }

        closestPoint = point;
        return false;
    }
}

public class RectangleCollider(Rectangle rectangle) : Collider
{
    public Rectangle Rectangle = rectangle;

    public override bool ContainsPoint(Vector2 point, out Vector2 closestPoint)
    {
        if (Rectangle.Contains(point))
        {
            float leftDist = point.X - Rectangle.Left;
            float rightDist = Rectangle.Right - point.X;
            float topDist = point.Y - Rectangle.Top;
            float bottomDist = Rectangle.Bottom - point.Y;

            float minDist = leftDist;
            closestPoint = new Vector2(Rectangle.Left, point.Y);

            if (rightDist < minDist)
            {
                minDist = rightDist;
                closestPoint = new Vector2(Rectangle.Right, point.Y);
            }
            if (topDist < minDist)
            {
                minDist = topDist;
                closestPoint = new Vector2(point.X, Rectangle.Top);
            }
            if (bottomDist < minDist)
            {
                closestPoint = new Vector2(point.X, Rectangle.Bottom);
            }

            return true;
        }
        closestPoint = point;

        return false;
    }
}

class DrawableParticle : Particle
{
    private PrimitiveBatch.Rectangle rectangle;
    public Color Color { get; set; }
    public Vector2 Size { get; set; }

    public DrawableParticle()
        : base(Vector2.Zero, 1.0f, false)
    {
        Size = new Vector2(10, 10);
        Color = Color.White;
        UpdateRectangle();
    }

    public DrawableParticle(Vector2 position, float mass, Vector2 size, Color color)
        : base(position, mass, false)
    {
        Size = size;
        Color = color;
        UpdateRectangle();
    }

    public DrawableParticle(Vector2 position, float mass, Color color)
        : base(position, mass, false)
    {
        Size = new Vector2(10, 10);
        Color = color;
        UpdateRectangle();
    }

    public DrawableParticle(Vector2 position, float mass, bool isPinned, Color color)
        : base(position, mass, isPinned)
    {
        Size = new Vector2(5, 5);
        Color = color;
        UpdateRectangle();
    }

    private void UpdateRectangle()
    {
        rectangle = new PrimitiveBatch.Rectangle(Position, Size, Color);
    }

    public void Draw(SpriteBatch spriteBatch, PrimitiveBatch primitiveBatch)
    {
        Vector2 middle = Position - Size / 2;
        if (IsPinned)
        {
            rectangle = new PrimitiveBatch.Rectangle(middle, Size, Color.BlueViolet);
            rectangle.Draw(spriteBatch, primitiveBatch);
            return;
        }
        rectangle = new PrimitiveBatch.Rectangle(middle, Size, Color);
        rectangle.Draw(spriteBatch, primitiveBatch);
    }
}

class Stick
{
    public Particle P1;
    public Particle P2;
    public float Length;

    public Stick()
    {
        P1 = null;
        P2 = null;
        Length = 0f;
    }

    public Stick(Particle p1, Particle p2)
    {
        P1 = p1;
        P2 = p2;
        Length = Vector2.Distance(p1.Position, p2.Position);
    }
}

class DrawableStick : Stick
{
    private PrimitiveBatch.Line line;
    public Color Color { get; set; }
    public float Width { get; set; }
    public bool IsCut { get; set; }

    public DrawableStick()
        : base()
    {
        Color = Color.White;
        Width = 2.0f;
        IsCut = false;
    }

    public DrawableStick(Particle p1, Particle p2, Color color, float width = 2.0f)
        : base(p1, p2)
    {
        Color = color;
        Width = width;
        UpdateLine();
    }

    public DrawableStick(Particle p1, Particle p2, Color color)
        : base(p1, p2)
    {
        Color = color;
        Width = 2.0f;
        UpdateLine();
    }

    private void UpdateLine()
    {
        line = new PrimitiveBatch.Line(P1.Position, P2.Position, Color, Width);
    }

    public void Draw(SpriteBatch spriteBatch, PrimitiveBatch primitiveBatch)
    {
        line = new PrimitiveBatch.Line(P1.Position, P2.Position, Color, Width);
        if (!IsCut)
            line.Draw(spriteBatch, primitiveBatch);
    }
}

class Mesh
{
    private int _nextParticleId = 1;
    private int _nextStickId = 1;

    public Dictionary<int, DrawableParticle> Particles { get; } =
        new Dictionary<int, DrawableParticle>();

    public float meanForceMagnitude = 0f;
    public float forceStdDeviation = 0f;
    public float maxForceMagnitude = 0f;

    public float springConstant = 10000f;
    public float drag = 0.997f;
    public float mass = 1f;

    private int _polygonInitialParticle = -1;
    private int _polygonFinalParticle = -1;
    private readonly List<int> _polygonVertices = new List<int>();
    private bool _isPolygonBuilding = false;

    public class MeshStick : DrawableStick
    {
        public int Id;
        public int P1Id;
        public int P2Id;

        public MeshStick() { }

        public MeshStick(DrawableParticle p1, DrawableParticle p2, Color color, float width = 2.0f)
            : base(p1, p2, color, width) { }
    }

    public Dictionary<int, MeshStick> Sticks { get; } = new Dictionary<int, MeshStick>();

    private readonly Dictionary<int, HashSet<int>> _particleToStickIds =
        new Dictionary<int, HashSet<int>>();

    public void RestoreStickReferences()
    {
        foreach (var stick in Sticks.Values)
        {
            if (
                Particles.ContainsKey(stick.P1Id)
                && Particles.TryGetValue(stick.P2Id, out var particle)
            )
            {
                stick.P1 = Particles[stick.P1Id];
                stick.P2 = particle;
                stick.Length = Vector2.Distance(stick.P1.Position, stick.P2.Position);
            }
        }
        _particleToStickIds.Clear();
        foreach (var particle in Particles.Values)
        {
            _particleToStickIds[particle.ID] = new HashSet<int>();
        }
        foreach (var stick in Sticks.Values)
        {
            if (_particleToStickIds.TryGetValue(stick.P1Id, out var id))
                id.Add(stick.Id);
            if (_particleToStickIds.TryGetValue(stick.P2Id, out var id2))
                id2.Add(stick.Id);
        }
    }

    protected int RegisterParticle(DrawableParticle particle)
    {
        if (particle == null)
            return -1;
        if (particle.ID > 0 && Particles.ContainsKey(particle.ID))
        {
            return particle.ID;
        }
        int id = _nextParticleId++;
        particle.ID = id;
        Particles[id] = particle;
        _particleToStickIds[id] = new HashSet<int>();
        return id;
    }

    public int AddParticle(
        Vector2 position,
        float mass,
        bool isPinned,
        Color color,
        Vector2? size = null
    )
    {
        var particle = new DrawableParticle(position, mass, isPinned, color);
        if (size.HasValue)
        {
            particle.Size = size.Value;
        }
        int id = _nextParticleId++;
        particle.ID = id;
        Particles[id] = particle;
        _particleToStickIds[id] = new HashSet<int>();
        return id;
    }

    public void ResetMesh()
    {
        Particles.Clear();
        Sticks.Clear();
        _nextParticleId = 1;
        _nextStickId = 1;
        _particleToStickIds.Clear();
        ResetPolygonBuilder();
    }

    public void BuildPolygon(
        KeyboardState keyboardState,
        KeyboardState previousKeyboardState,
        MouseState mouseState,
        MouseState previousMouseState,
        bool imguiWantsMouse
    )
    {
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);

        if (
            mouseState.LeftButton == ButtonState.Pressed
            && previousMouseState.LeftButton == ButtonState.Released
            && !imguiWantsMouse
        )
        {
            if (!_isPolygonBuilding)
            {
                _isPolygonBuilding = true;
                _polygonVertices.Clear();

                int newParticleId = AddParticle(mousePos, 0.1f, false, Color.White);
                _polygonVertices.Add(newParticleId);
                _polygonInitialParticle = newParticleId;
                _polygonFinalParticle = newParticleId;
            }
            else
            {
                int newParticleId = AddParticle(mousePos, 0.1f, false, Color.White);

                if (_polygonFinalParticle != -1)
                {
                    AddStickBetween(_polygonFinalParticle, newParticleId);
                }

                _polygonVertices.Add(newParticleId);
                _polygonFinalParticle = newParticleId;
            }
        }

        if (keyboardState.IsKeyDown(Keys.Enter) && !previousKeyboardState.IsKeyDown(Keys.Enter))
        {
            if (_isPolygonBuilding && _polygonVertices.Count >= 3)
            {
                AddStickBetween(_polygonFinalParticle, _polygonInitialParticle);
                ResetPolygonBuilder();
            }
        }
    }

    public void ResetPolygonBuilder()
    {
        _isPolygonBuilding = false;
        _polygonVertices.Clear();
        _polygonInitialParticle = -1;
        _polygonFinalParticle = -1;
    }

    public bool RemoveParticle(int particleId)
    {
        if (!Particles.ContainsKey(particleId))
            return false;

        if (_particleToStickIds.TryGetValue(particleId, out var stickIds))
        {
            foreach (var sid in stickIds.ToList())
            {
                RemoveStick(sid);
            }
        }

        _particleToStickIds.Remove(particleId);
        return Particles.Remove(particleId);
    }

    public void CutSticksAlongLine(Vector2 lineStart, Vector2 lineEnd)
    {
        var sticksToCut = new List<int>();

        foreach (var kvp in Sticks)
        {
            var stick = kvp.Value;
            if (LinesIntersect(lineStart, lineEnd, stick.P1.Position, stick.P2.Position))
            {
                sticksToCut.Add(kvp.Key);
            }
        }

        foreach (var stickId in sticksToCut)
        {
            RemoveStick(stickId);
        }
    }

    private bool LinesIntersect(Vector2 p1, Vector2 p2, Vector2 p3, Vector2 p4)
    {
        float ccw(Vector2 A, Vector2 B, Vector2 C)
        {
            return (C.Y - A.Y) * (B.X - A.X) > (B.Y - A.Y) * (C.X - A.X) ? 1 : -1;
        }

        return Math.Sign(ccw(p1, p3, p4)) != Math.Sign(ccw(p2, p3, p4))
            && Math.Sign(ccw(p1, p2, p3)) != Math.Sign(ccw(p1, p2, p4));
    }

    public int? AddStick(
        int p1Id,
        int p2Id,
        Color color,
        float width = 2.0f,
        float naturalLength = -1f
    )
    {
        if (p1Id == p2Id)
            return null;
        if (!Particles.ContainsKey(p1Id) || !Particles.ContainsKey(p2Id))
            return null;

        bool exists = Sticks.Values.Any(s =>
            (s.P1Id == p1Id && s.P2Id == p2Id) || (s.P1Id == p2Id && s.P2Id == p1Id)
        );
        if (exists)
            return null;

        var s = new MeshStick(Particles[p1Id], Particles[p2Id], color, width)
        {
            Id = _nextStickId++,
            P1Id = p1Id,
            P2Id = p2Id,
        };
        if (naturalLength > 0f)
        {
            s.Length = naturalLength;
        }
        Sticks[s.Id] = s;
        if (!_particleToStickIds.ContainsKey(p1Id))
            _particleToStickIds[p1Id] = new HashSet<int>();
        if (!_particleToStickIds.ContainsKey(p2Id))
            _particleToStickIds[p2Id] = new HashSet<int>();
        _particleToStickIds[p1Id].Add(s.Id);
        _particleToStickIds[p2Id].Add(s.Id);
        return s.Id;
    }

    public void AddSticksAccrossLength(
        Vector2 Start,
        Vector2 End,
        int numberOfSticks,
        float naturalLengthRatio = 1f
    )
    {
        if (numberOfSticks < 1)
            return;

        Vector2 direction = End - Start;
        float segmentLength = direction.Length() / numberOfSticks;
        direction.Normalize();

        List<int> particleIds = new List<int>();
        for (int i = 0; i <= numberOfSticks; i++)
        {
            Vector2 position = Start + direction * segmentLength * i;
            int particleId = AddParticleAt(position);
            particleIds.Add(particleId);
        }

        for (int i = 0; i < particleIds.Count - 1; i++)
        {
            AddStickBetween(particleIds[i], particleIds[i + 1], segmentLength / naturalLengthRatio);
        }
    }

    public bool RemoveStick(int stickId)
    {
        if (!Sticks.TryGetValue(stickId, out var s))
            return false;

        if (_particleToStickIds.TryGetValue(s.P1Id, out var set1))
            set1.Remove(stickId);
        if (_particleToStickIds.TryGetValue(s.P2Id, out var set2))
            set2.Remove(stickId);
        return Sticks.Remove(stickId);
    }

    public int RemoveSticksBetween(int p1Id, int p2Id)
    {
        var toRemove = Sticks
            .Values.Where(s =>
                (s.P1Id == p1Id && s.P2Id == p2Id) || (s.P1Id == p2Id && s.P2Id == p1Id)
            )
            .Select(s => s.Id)
            .ToList();
        foreach (var sid in toRemove)
            RemoveStick(sid);
        return toRemove.Count;
    }

    public IEnumerable<MeshStick> GetSticksForParticle(int particleId)
    {
        if (_particleToStickIds.TryGetValue(particleId, out var ids))
        {
            foreach (var sid in ids)
            {
                if (Sticks.TryGetValue(sid, out var s))
                    yield return s;
            }
        }
    }

    public void Draw(
        SpriteBatch spriteBatch,
        PrimitiveBatch primitiveBatch,
        bool drawParticles,
        bool drawConstraints
    )
    {
        if (drawConstraints)
        {
            foreach (var s in Sticks.Values)
            {
                s.Draw(spriteBatch, primitiveBatch);
            }
        }

        if (drawParticles)
        {
            foreach (var p in Particles.Values)
            {
                p.Draw(spriteBatch, primitiveBatch);
            }
        }
    }

    public int AddParticleAt(Vector2 position, bool isPinned = false)
    {
        return AddParticle(position, isPinned ? 0f : mass, isPinned, Color.White);
    }

    public int? AddStickBetween(int p1Id, int p2Id, float naturalLength = -1f)
    {
        return AddStick(p1Id, p2Id, Color.White, 2, naturalLength);
    }

    public int? FindClosestParticle(Vector2 position, float radius)
    {
        float minDist = radius;
        int? closest = null;
        foreach (var kvp in Particles)
        {
            float dist = Vector2.Distance(kvp.Value.Position, position);
            if (dist < minDist)
            {
                minDist = dist;
                closest = kvp.Key;
            }
        }
        return closest;
    }

    public static Mesh CreateGridMesh(
        Vector2 Start,
        Vector2 End,
        float DistanceBetweenParticles,
        Mesh mesh
    )
    {
        if (DistanceBetweenParticles <= 0)
            return mesh ?? new Mesh();

        int width = (int)Math.Max(1, Math.Abs(End.X - Start.X) / DistanceBetweenParticles) + 1;
        int height = (int)Math.Max(1, Math.Abs(End.Y - Start.Y) / DistanceBetweenParticles) + 1;
        (Start, End) = (Vector2.Min(Start, End), Vector2.Max(Start, End));
        width = Math.Min(width, 1000);
        height = Math.Min(height, 1000);

        int[,] particleIds = new int[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector2 position =
                    Start + new Vector2(x * DistanceBetweenParticles, y * DistanceBetweenParticles);
                particleIds[x, y] = mesh.AddParticleAt(position);
            }
        }

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                int p1Id = particleIds[x, y];

                if (x < width - 1)
                {
                    int p2Id = particleIds[x + 1, y];
                    mesh.AddStickBetween(p1Id, p2Id);
                }

                if (y < height - 1)
                {
                    int p2Id = particleIds[x, y + 1];
                    mesh.AddStickBetween(p1Id, p2Id);
                }
            }
        }
        return mesh;
    }

    public static Mesh CreateClothMesh(
        Vector2 Start,
        Vector2 End,
        float naturalLength,
        Mesh mesh = null,
        float springConstant = 10000f,
        float drag = 0.997f,
        float mass = 1f
    )
    {
        mesh = mesh ?? new Mesh();
        mesh.springConstant = springConstant;
        mesh.drag = drag;
        mesh.mass = mass;
        int offsetid = mesh._nextParticleId;

        CreateGridMesh(Start, End, naturalLength, mesh);
        int idWidth = (int)Math.Max(1, Math.Abs(End.X - Start.X) / naturalLength) + 1;
        int idHeight = (int)Math.Max(1, Math.Abs(End.Y - Start.Y) / naturalLength) + 1;
        int topLeftParticleId = offsetid;
        int topRightParticleId = offsetid + (idWidth - 1) * idHeight;

        if (mesh.Particles.ContainsKey(topLeftParticleId))
        {
            mesh.Particles[topLeftParticleId].IsPinned = true;
            mesh.Particles[topLeftParticleId].Mass = 0f;
        }

        if (mesh.Particles.ContainsKey(topRightParticleId))
        {
            mesh.Particles[topRightParticleId].IsPinned = true;
            mesh.Particles[topRightParticleId].Mass = 0f;
        }
        return mesh;
    }
}

class FileWriteableMesh
{
    public class particleData
    {
        public Vector2 Position;
        public float Mass;
        public bool IsPinned;
    }

    public class stickData
    {
        public int P1Id;
        public int P2Id;
    }

    public List<particleData> Particles = new List<particleData>();
    public List<stickData> Sticks = new List<stickData>();

    // Parameterless constructor for JSON deserialization
    public FileWriteableMesh() { }

    public FileWriteableMesh(Mesh mesh)
    {
        var particleIdMap = new Dictionary<int, int>();
        foreach (var kvp in mesh.Particles)
        {
            var p = kvp.Value;
            particleIdMap[kvp.Key] = Particles.Count;
            Particles.Add(
                new particleData
                {
                    Position = p.Position,
                    Mass = p.Mass,
                    IsPinned = p.IsPinned,
                }
            );
        }
        foreach (var kvp in mesh.Sticks)
        {
            var s = kvp.Value;
            Sticks.Add(
                new stickData { P1Id = particleIdMap[s.P1Id], P2Id = particleIdMap[s.P2Id] }
            );
        }
    }

    public Mesh ToMesh()
    {
        var mesh = new Mesh();

        if (Particles != null)
        {
            foreach (var pData in Particles)
            {
                mesh.AddParticle(pData.Position, pData.Mass, pData.IsPinned, Color.White);
            }
        }

        if (Sticks != null)
        {
            foreach (var sData in Sticks)
            {
                mesh.AddStick(sData.P1Id, sData.P2Id, Color.White);
            }
        }

        return mesh;
    }
}

public class Tool
{
    public string Name;
    public Texture2D Icon;
    public Texture2D CursorIcon;
    public Dictionary<string, object> Properties = new Dictionary<string, object>();

    public Tool(string name, Texture2D icon, Texture2D cursorIcon)
    {
        Name = name;
        Icon = icon;
        CursorIcon = cursorIcon;
    }
}
