using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using VectorGraphics;
using VectorGui;

namespace PhysicsCSAlevlProject;

public class Game1 : Game
{
    // Physics Scale: 1 pixel = 1 centimeter
    // Screen size: 500x400 pixels = 5m x 4m (reasonable room size)
    // Gravity: 9.8 m/s² = 980 cm/s² = 980 pixels/s²

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private PrimitiveBatch _primitiveBatch;
    bool leftPressed;
    Vector2 intitialMousePosWhenPressed;

    // private List<DrawableParticle> particles = new List<DrawableParticle>();
    // private List<DrawableStick> sticks = new List<DrawableStick>();
    private Cloth _cloth;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.PreferredBackBufferWidth = 500;
        _graphics.PreferredBackBufferHeight = 400;
    }

    protected override void Initialize()
    {
        _primitiveBatch = new PrimitiveBatch(GraphicsDevice);
        _primitiveBatch.CreateTextures();
        leftPressed = false;

        float naturalLength = 10f;
        float springConstant = 500;
        float mass = 0.1f;

        int cols = (int)(200 / naturalLength);

        var pinnedParticles = new List<Vector2>(
            new Vector2[]
            {
                new Vector2(220, 20),
                new Vector2(220 + (cols - 1) * naturalLength, 20),
            }
        );

        _cloth = new Cloth(
            new Vector2(200, 200),
            pinnedParticles,
            naturalLength,
            springConstant,
            mass
        );

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
    }

    private DrawableParticle KeepInsideScreen(DrawableParticle p)
    {
        bool positionChanged = false;
        Vector2 originalPosition = p.Position;

        if (p.Position.X < 0)
        {
            p.Position.X = 0;
            positionChanged = true;
        }
        else if (p.Position.X > _graphics.PreferredBackBufferWidth)
        {
            p.Position.X = _graphics.PreferredBackBufferWidth;
            positionChanged = true;
        }

        if (p.Position.Y < 0)
        {
            p.Position.Y = 0;
            positionChanged = true;
        }
        else if (p.Position.Y > _graphics.PreferredBackBufferHeight - 10)
        {
            p.Position.Y = _graphics.PreferredBackBufferHeight - 10;
            positionChanged = true;
        }

        if (positionChanged)
        {
            p.PreviousPosition = p.Position;
        }

        return p;
    }

    private DrawableStick[][] CalculateStickForces(DrawableStick[][] sticks)
    {
        for (int i = 0; i < sticks.Length; i++)
        {
            for (int j = 0; j < sticks[i].Length; j++)
            {
                DrawableStick s = sticks[i][j];
                Vector2 stickVector = s.P1.Position - s.P2.Position;
                float currentLength = stickVector.Length();

                if (currentLength > 0)
                {
                    Vector2 stickDir = stickVector / currentLength;
                    float stretch = currentLength - s.Length;

                    float springConstant = _cloth.springConstant;
                    Vector2 springForce = stickDir * stretch * springConstant;

                    s.P1.AccumulatedForce -= springForce;
                    s.P2.AccumulatedForce += springForce;
                }
            }
        }
        return sticks;
    }

    private void UpdateParticles(float deltaTime)
    {
        for (int i = 0; i < _cloth.particles.Length; i++)
        {
            for (int j = 0; j < _cloth.particles[i].Length; j++)
            {
                DrawableParticle p = _cloth.particles[i][j];

                if (p.IsPinned)
                {
                    continue;
                }

                Vector2 totalForce = new Vector2(20, 100f) + p.AccumulatedForce;
                Vector2 acceleration = totalForce / p.Mass;

                Vector2 velocity = p.Position - p.PreviousPosition;
                velocity *= _cloth.drag;

                Vector2 previousPosition = p.Position;
                p.Position = p.Position + velocity + acceleration * (deltaTime * deltaTime);
                p.PreviousPosition = previousPosition;
                p = KeepInsideScreen(p);
                _cloth.particles[i][j] = p;
            }
        }
    }

    private List<(int i, int j)> GetParticlesInRadius(Vector2 mousePosition, float radius)
    {
        var particlesInRadius = new List<(int i, int j)>();
        for (int i = 0; i < _cloth.particles.Length; i++)
        {
            for (int j = 0; j < _cloth.particles[i].Length; j++)
            {
                Vector2 pos = _cloth.particles[i][j].Position;
                if (Vector2.DistanceSquared(pos, mousePosition) < (radius * radius))
                {
                    particlesInRadius.Add((i, j));
                }
            }
        }
        return particlesInRadius;
    }

    private void DragParticles(MouseState mouseState, bool leftMousePressed, bool leftMouseJustPressed)
    {
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
        float dragRadius = 20f;

        if (leftMouseJustPressed)
        {
            var particlesInDragArea = GetParticlesInRadius(mousePos, dragRadius);
            foreach (var (i, j) in particlesInDragArea)
            {
                var p = _cloth.particles[i][j];
                if (!p.IsPinned)
                {
                    p.Position += mousePos - p.Position;
                    _cloth.particles[i][j] = p;
                }
            }
        }
    }

    protected override void Update(GameTime gameTime)
    {
        const float fixedDeltaTime = 1f / 500f;

        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
            Exit();
        MouseState mouseState = Mouse.GetState();

        for (int i = 0; i < _cloth.particles.Length; i++)
        {
            for (int j = 0; j < _cloth.particles[i].Length; j++)
            {
                _cloth.particles[i][j].AccumulatedForce = Vector2.Zero;
            }
        }

        _cloth.horizontalSticks = CalculateStickForces(_cloth.horizontalSticks);
        _cloth.verticalSticks = CalculateStickForces(_cloth.verticalSticks);
        UpdateParticles(fixedDeltaTime);
        DragParticles(
            mouseState,
            mouseState.LeftButton == ButtonState.Pressed,
            mouseState.LeftButton == ButtonState.Pressed && !leftPressed
        );

        base.Update(gameTime);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _spriteBatch.Begin();

        for (int i = 0; i < _cloth.horizontalSticks.Length; i++)
        {
            for (int j = 0; j < _cloth.horizontalSticks[i].Length; j++)
            {
                _cloth.horizontalSticks[i][j].Draw(_spriteBatch, _primitiveBatch);
            }
        }

        for (int i = 0; i < _cloth.verticalSticks.Length; i++)
        {
            for (int j = 0; j < _cloth.verticalSticks[i].Length; j++)
            {
                _cloth.verticalSticks[i][j].Draw(_spriteBatch, _primitiveBatch);
            }
        }

        for (int i = 0; i < _cloth.particles.Length; i++)
        {
            for (int j = 0; j < _cloth.particles[i].Length; j++)
            {
                _cloth.particles[i][j].Draw(_spriteBatch, _primitiveBatch);
            }
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }
}
