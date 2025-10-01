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

    public bool IsPinned;
    public bool IsSelected;

    public Particle(Vector2 position, float mass, bool isPinned)
    {
        Position = position;
        PreviousPosition = position;
        Mass = mass;
        AccumulatedForce = Vector2.Zero;
        IsPinned = isPinned;
        IsPinned = mass <= 0;
        IsSelected = false;
        ID = -1;
    }
}

class DrawableParticle : Particle
{
    private PrimitiveBatch.Rectangle rectangle;
    public Color Color { get; set; }
    public Vector2 Size { get; set; }

    public DrawableParticle(Vector2 position, float mass, Vector2 size, Color color)
        : base(position, mass, false) // Default to not pinned
    {
        Size = size;
        Color = color;
        UpdateRectangle();
    }

    public DrawableParticle(Vector2 position, float mass, Color color)
        : base(position, mass, false) // Default to not pinned
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
        line.Draw(spriteBatch, primitiveBatch);
    }
}

class Cloth : Mesh
{
    public DrawableParticle[][] particles;
    public DrawableStick[][] horizontalSticks;
    public DrawableStick[][] verticalSticks;

    public float naturalLength;
    public float springConstant = 1.0f;
    public float drag = 0.99f;
    public float mass;

    public Cloth(
        Vector2 Size,
        List<Vector2> pinnedParticles,
        float naturalLength = 10f,
        float springConstant = 1.0f,
        float mass = 1f
    )
    {
        this.naturalLength = naturalLength; // Assign the naturalLength field
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
    
    public void Draw(SpriteBatch spriteBatch, PrimitiveBatch primitiveBatch)
    {
        foreach (var s in horizontalSticks)
        {
            foreach (var stick in s)
            {
                stick?.Draw(spriteBatch, primitiveBatch);
            }
        }
        foreach (var s in verticalSticks)
        {
            foreach (var stick in s)
            {
                stick?.Draw(spriteBatch, primitiveBatch);
            }
        }
        foreach (var row in particles)
        {
            foreach (var p in row)
            {
                p?.Draw(spriteBatch, primitiveBatch);
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

    public class MeshStick : DrawableStick
    {
        public int Id;
        public int P1Id;
        public int P2Id;

        public MeshStick(DrawableParticle p1, DrawableParticle p2, Color color, float width = 2.0f)
            : base(p1, p2, color, width) { }
    }

    public Dictionary<int, MeshStick> Sticks { get; } = new Dictionary<int, MeshStick>();

    private readonly Dictionary<int, HashSet<int>> _particleToStickIds =
        new Dictionary<int, HashSet<int>>();

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

    public Tool(string name, Texture2D icon, Texture2D cursorIcon)
    {
        Name = name;
        Icon = icon;
        CursorIcon = cursorIcon;
    }
}
