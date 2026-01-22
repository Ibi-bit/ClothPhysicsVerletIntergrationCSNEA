using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using VectorGraphics;

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

class Cloth : Mesh
{
    public DrawableParticle[][] particles;
    public DrawableStick[][] horizontalSticks;
    public DrawableStick[][] verticalSticks;

    public float naturalLength;
    public float mass;

    public Cloth(
        Vector2 Size,
        List<Vector2> pinnedParticles,
        float naturalLength = 10f,
        float springConstant = 1.0f,
        float mass = 1f
    )
    {
        this.naturalLength = naturalLength;
        int rows = (int)(Size.Y / naturalLength);
        int cols = (int)(Size.X / naturalLength);
        this.springConstant = springConstant;

        this.mass = mass;
        particles = new DrawableParticle[rows][];
        horizontalSticks = new DrawableStick[rows][];
        verticalSticks = new DrawableStick[rows - 1][];

        for (int i = 0; i < rows; i++)
        {
            particles[i] = new DrawableParticle[cols];
            horizontalSticks[i] = new DrawableStick[cols - 1];
            if (i < rows - 1)
            {
                verticalSticks[i] = new DrawableStick[cols];
            }

            for (int j = 0; j < cols; j++)
            {
                bool isPinned = pinnedParticles.Contains(
                    new Vector2(j * naturalLength + 220, i * naturalLength + 20)
                );

                var dp = new DrawableParticle(
                    new Vector2(j * naturalLength + 220, i * naturalLength + 20),
                    isPinned ? 0 : mass,
                    isPinned,
                    Color.White
                );

                RegisterParticle(dp);
                particles[i][j] = dp;

                if (j > 0)
                {
                    var sid = AddStick(particles[i][j - 1].ID, particles[i][j].ID, Color.White);
                    if (sid.HasValue)
                    {
                        horizontalSticks[i][j - 1] = Sticks[sid.Value];
                    }
                }

                if (i > 0)
                {
                    var sid = AddStick(particles[i - 1][j].ID, particles[i][j].ID, Color.White);
                    if (sid.HasValue)
                    {
                        verticalSticks[i - 1][j] = Sticks[sid.Value];
                    }
                }
            }
        }
        foreach (Vector2 p in pinnedParticles)
        {
            int row = (int)((p.Y - 20) / naturalLength);
            int col = (int)((p.X - 220) / naturalLength);
            if (row >= 0 && row < rows && col >= 0 && col < cols)
            {
                particles[row][col].IsPinned = true;
                particles[row][col].Mass = 0;
                particles[row][col].PreviousPosition = particles[row][col].Position;
            }
        }
    }

    public void StickDraw(
        SpriteBatch spriteBatch,
        PrimitiveBatch primitiveBatch,
        DrawableStick[][] sticks
    )
    {
        for (int i = 0; i < sticks.Length; i++)
        {
            for (int j = 0; j < sticks[i].Length; j++)
            {
                sticks[i][j].Draw(spriteBatch, primitiveBatch);
            }
        }
    }

    private float CalculateStressLerp(float forceMagnitude)
    {
        if (forceStdDeviation > 0.0001f)
        {
            float zScore = (forceMagnitude - meanForceMagnitude) / forceStdDeviation;
            const float highlightThreshold = 0.5f;
            const float highlightRange = 1.5f;
            return MathHelper.Clamp((zScore - highlightThreshold) / highlightRange, 0f, 1f);
        }

        if (maxForceMagnitude > 0.0001f)
        {
            return MathHelper.Clamp(forceMagnitude / maxForceMagnitude, 0f, 1f);
        }

        if (meanForceMagnitude > 0.0001f)
        {
            return MathHelper.Clamp(forceMagnitude / meanForceMagnitude, 0f, 1f);
        }

        return 0f;
    }

    public new void Draw(SpriteBatch spriteBatch, PrimitiveBatch primitiveBatch)
    {
        StickDraw(spriteBatch, primitiveBatch, horizontalSticks);
        StickDraw(spriteBatch, primitiveBatch, verticalSticks);

        for (int i = 0; i < particles.Length; i++)
        {
            for (int j = 0; j < particles[i].Length; j++)
            {
                var particle = particles[i][j];

                if (particle.IsPinned)
                {
                    particle.Color = Color.BlueViolet;
                    particle.Draw(spriteBatch, primitiveBatch);
                    continue;
                }

                float forceMagnitude = particle.TotalForceMagnitude;
                float lerpFactor = CalculateStressLerp(forceMagnitude);
                float easedLerp = lerpFactor * lerpFactor;
                particle.Draw(spriteBatch, primitiveBatch);
                particles[i][j] = particle;
            }
        }
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

    public class MeshStick : DrawableStick
    {
        public int Id;
        public int P1Id;
        public int P2Id;

        public MeshStick()
            : base() { }

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
            if (Particles.ContainsKey(stick.P1Id) && Particles.ContainsKey(stick.P2Id))
            {
                stick.P1 = Particles[stick.P1Id];
                stick.P2 = Particles[stick.P2Id];
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
            if (_particleToStickIds.ContainsKey(stick.P1Id))
                _particleToStickIds[stick.P1Id].Add(stick.Id);
            if (_particleToStickIds.ContainsKey(stick.P2Id))
                _particleToStickIds[stick.P2Id].Add(stick.Id);
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

        return ccw(p1, p3, p4) != ccw(p2, p3, p4) && ccw(p1, p2, p3) != ccw(p1, p2, p4);
    }

    public int? AddStick(int p1Id, int p2Id, Color color, float width = 2.0f)
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
        Sticks[s.Id] = s;
        if (!_particleToStickIds.ContainsKey(p1Id))
            _particleToStickIds[p1Id] = new HashSet<int>();
        if (!_particleToStickIds.ContainsKey(p2Id))
            _particleToStickIds[p2Id] = new HashSet<int>();
        _particleToStickIds[p1Id].Add(s.Id);
        _particleToStickIds[p2Id].Add(s.Id);
        return s.Id;
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

    public void Draw(SpriteBatch spriteBatch, PrimitiveBatch primitiveBatch)
    {
        foreach (var s in Sticks.Values)
        {
            s.Draw(spriteBatch, primitiveBatch);
        }
        foreach (var p in Particles.Values)
        {
            p.Draw(spriteBatch, primitiveBatch);
        }
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

class BuildableMesh : Mesh
{
    public float mass = 0.1f;

    public BuildableMesh(float springConstant = 10000f, float mass = 0.1f)
    {
        this.springConstant = springConstant;
        this.mass = mass;
    }

    public int AddParticleAt(Vector2 position, bool isPinned = false)
    {
        return AddParticle(position, isPinned ? 0f : mass, isPinned, Color.White);
    }

    public int? AddStickBetween(int p1Id, int p2Id)
    {
        return AddStick(p1Id, p2Id, Color.White);
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
}
