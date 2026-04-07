using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using VectorGraphics;

namespace PhysicsCSAlevlProject;

class Mesh
{
    private int _nextParticleId = 1;
    private int _nextStickId = 1;

    public float stickDrawThickness = -1;

    public Dictionary<int, DrawableParticle> Particles { get; } = new();

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

    public List<Collider> Colliders = new List<Collider>();

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

    public Mesh DeepCopy()
    {
        var copy = new Mesh
        {
            stickDrawThickness = stickDrawThickness,
            meanForceMagnitude = meanForceMagnitude,
            forceStdDeviation = forceStdDeviation,
            maxForceMagnitude = maxForceMagnitude,
            springConstant = springConstant,
            drag = drag,
            mass = mass,
            _nextParticleId = _nextParticleId,
            _nextStickId = _nextStickId,
            _polygonInitialParticle = _polygonInitialParticle,
            _polygonFinalParticle = _polygonFinalParticle,
            _isPolygonBuilding = _isPolygonBuilding,
        };

        copy._polygonVertices.AddRange(_polygonVertices);

        foreach (var kvp in Particles)
        {
            var p = kvp.Value;
            DrawableParticle cloned;

            if (p is OscillatingParticle op)
            {
                var clonedOp = new OscillatingParticle(
                    op.Position,
                    op.Mass,
                    op.IsPinned,
                    op.Color,
                    op.OscillationAmplitude,
                    op.OscillationFrequency,
                    op.OscillationAngle
                )
                {
                    Size = op.Size,
                    PreviousPosition = op.PreviousPosition,
                    AccumulatedForce = op.AccumulatedForce,
                    ID = op.ID,
                    IsSelected = op.IsSelected,
                    TotalForceMagnitude = op.TotalForceMagnitude,
                };
                cloned = clonedOp;
            }
            else
            {
                cloned = new DrawableParticle(p.Position, p.Mass, p.IsPinned, p.Color)
                {
                    Size = p.Size,
                    PreviousPosition = p.PreviousPosition,
                    AccumulatedForce = p.AccumulatedForce,
                    ID = p.ID,
                    IsSelected = p.IsSelected,
                    TotalForceMagnitude = p.TotalForceMagnitude,
                };
            }
            copy.Particles[kvp.Key] = cloned;
            copy._particleToStickIds[kvp.Key] = new HashSet<int>();
        }
        Colliders ??= new List<Collider>();
        copy.Colliders = new List<Collider>();
        copy.Colliders.AddRange(Colliders);

        foreach (var kvp in Sticks)
        {
            var s = kvp.Value;
            var cloned = new MeshStick(
                copy.Particles[s.P1Id],
                copy.Particles[s.P2Id],
                s.Color,
                s.Width
            )
            {
                Id = s.Id,
                P1Id = s.P1Id,
                P2Id = s.P2Id,
                Length = s.Length,
                IsCut = s.IsCut,
            };
            copy.Sticks[kvp.Key] = cloned;
            if (copy._particleToStickIds.TryGetValue(cloned.P1Id, out var set1))
                set1.Add(cloned.Id);
            if (copy._particleToStickIds.TryGetValue(cloned.P2Id, out var set2))
                set2.Add(cloned.Id);
        }

        return copy;
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
        Vector2? size = null,
        OscillatingParticle oscillatingProps = null
    )
    {
        var particle = oscillatingProps ?? new DrawableParticle(position, mass, isPinned, color);
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

    public int NextParticle => _nextParticleId;

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
        bool imguiWantsMouse,
        Action beforeChange = null
    )
    {
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);

        if (
            mouseState.LeftButton == ButtonState.Pressed
            && previousMouseState.LeftButton == ButtonState.Released
            && !imguiWantsMouse
        )
        {
            beforeChange?.Invoke();
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
                beforeChange?.Invoke();
                AddStickBetween(_polygonFinalParticle, _polygonInitialParticle);
                ResetPolygonBuilder();
            }
        }
        else if (
            keyboardState.IsKeyDown(Keys.Escape) && !previousKeyboardState.IsKeyDown(Keys.Escape)
        )
        {
            if (_isPolygonBuilding)
            {
                beforeChange?.Invoke();
                ResetPolygonBuilder();
            }
        }
        else if (keyboardState.IsKeyDown(Keys.C) && !previousKeyboardState.IsKeyDown(Keys.C))
        {
            if (_isPolygonBuilding && _polygonVertices.Count >= 2)
            {
                Colliders.Add(
                    new PolygonSeperatedAxisCollider(
                        _polygonVertices.Select(id => Particles[id].Position).ToArray()
                    )
                );
                foreach (var id in _polygonVertices)
                {
                    RemoveParticle(id);
                }
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
                s.Draw(spriteBatch, primitiveBatch, stickDrawThickness);
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

    [ConsoleCommand("Mesh.ResetSimulation")]
    public void ResetSimulation(string[] parameters)
    {
        
        foreach (var p in Particles.Values)
        {
            p.PreviousPosition = p.Position;
            p.AccumulatedForce = Vector2.Zero;
        }
    }

    public void CreateHubSpokeTire(object[] args)
    {
        if (args.Length < 5)
        {
            throw new ArgumentException(
                "CreateHubSpokeTire needs 5 inputs: centerX, centerY, innerRadius, outerRadius, segments."
            );
        }
        Vector2 center = new Vector2(float.Parse((string)args[0]), float.Parse((string)args[1]));
        float innerRadius = float.Parse((string)args[2]);
        float outerRadius = float.Parse((string)args[3]);
        int rimParticleCount = int.Parse((string)args[4]);

        if (innerRadius <= 0f || outerRadius <= 0f)
        {
            throw new ArgumentException("Inner and outer radius must be greater than zero.");
        }

        if (outerRadius <= innerRadius)
        {
            throw new ArgumentException("Outer radius must be greater than inner radius.");
        }

        if (rimParticleCount < 3)
        {
            throw new ArgumentException("Segments must be at least 3.");
        }

        List<int> innerRimIds = new List<int>();
        List<int> outerRimIds = new List<int>();

        for (int i = 0; i < rimParticleCount; i++)
        {
            float angle = 2 * MathF.PI * i / rimParticleCount;

            Vector2 innerPos =
                center
                + new Vector2(innerRadius * MathF.Cos(angle), innerRadius * MathF.Sin(angle));
            int innerId = AddParticleAt(innerPos);
            innerRimIds.Add(innerId);

            Vector2 outerPos =
                center
                + new Vector2(outerRadius * MathF.Cos(angle), outerRadius * MathF.Sin(angle));
            int outerId = AddParticleAt(outerPos);
            outerRimIds.Add(outerId);
        }

        float innerStickLength = 2f * innerRadius * MathF.Sin(MathF.PI / rimParticleCount);
        for (int i = 0; i < rimParticleCount; i++)
        {
            int next = (i + 1) % rimParticleCount;
            AddStickBetween(innerRimIds[i], innerRimIds[next], innerStickLength);
        }

        float outerStickLength = 2f * outerRadius * MathF.Sin(MathF.PI / rimParticleCount);
        for (int i = 0; i < rimParticleCount; i++)
        {
            int next = (i + 1) % rimParticleCount;
            AddStickBetween(outerRimIds[i], outerRimIds[next], outerStickLength);
        }

        float radialStickLength = outerRadius - innerRadius;
        float diagonalStickLength = MathF.Sqrt(
            radialStickLength * radialStickLength + outerStickLength * outerStickLength
        );
        for (int i = 0; i < rimParticleCount; i++)
        {
            int next = (i + 1) % rimParticleCount;
            AddStickBetween(innerRimIds[i], outerRimIds[next], diagonalStickLength);
            AddStickBetween(innerRimIds[i], outerRimIds[i], radialStickLength);
            AddStickBetween(outerRimIds[i], innerRimIds[next], diagonalStickLength);
        }
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

    public void ArgClothMeshFactory(object[] args)
    {
        if (args.Length < 5)
        {
            throw new ArgumentException(
                "CreateClothMesh needs 5 inputs: startX, startY, endX, endY, naturalLength."
            );
        }
        try
        {
            Vector2 Start = new Vector2(float.Parse((string)args[0]), float.Parse((string)args[1]));
            Vector2 End = new Vector2(float.Parse((string)args[2]), float.Parse((string)args[3]));
            float naturalLength = float.Parse((string)args[4]);
            CreateClothMesh(Start, End, naturalLength, this, springConstant, drag, mass);
        }
        catch (Exception ex)
        {
            throw new ArgumentException("Invalid input for CreateClothMesh: " + ex.Message);
        }
    }
}
