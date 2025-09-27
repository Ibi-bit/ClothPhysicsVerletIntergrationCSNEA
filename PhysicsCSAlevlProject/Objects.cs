using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using VectorGraphics;

class Particle
{
    public Vector2 Position;
    public Vector2 PreviousPosition;
    public float Mass;
    public Vector2 AccumulatedForce;

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

class Cloth
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

                particles[i][j] = new DrawableParticle(
                    new Vector2(j * naturalLength + 220, i * naturalLength + 20),
                    isPinned ? 0 : mass,
                    isPinned,
                    Color.White
                );

                if (j > 0)
                {
                    horizontalSticks[i][j - 1] = new DrawableStick(
                        particles[i][j - 1],
                        particles[i][j],
                        Color.White
                    );
                }

                if (i > 0)
                {
                    verticalSticks[i - 1][j] = new DrawableStick(
                        particles[i - 1][j],
                        particles[i][j],
                        Color.White
                    );
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

        
