using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
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

class OscillatingParticle(
    Vector2 position,
    float mass,
    bool isPinned,
    Color color,
    float amplitude,
    float frequency,
    float angle
) : DrawableParticle(position, mass, isPinned, color)
{
    public float OscillationAmplitude = amplitude;
    public float OscillationFrequency = frequency;
    public float OscillationAngle = angle;
    private float _oscillationTime = 0f;
    private Vector2 _anchorPosition = position;

    public void UpdateOscillation(float deltaTime)
    {
        _oscillationTime += deltaTime;
        float oscillationOffset =
            OscillationAmplitude
            * MathF.Sin(2 * MathF.PI * OscillationFrequency * _oscillationTime);

        Position = new Vector2(
            _anchorPosition.X + oscillationOffset * MathF.Cos(OscillationAngle - MathF.PI / 2),
            _anchorPosition.Y + oscillationOffset * MathF.Sin(OscillationAngle - MathF.PI / 2)
        );
        PreviousPosition = Position;
    }

    public void SetAnchorPosition(Vector2 newAnchor)
    {
        _anchorPosition = newAnchor;
    }
}
