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

        float naturalLength = 10f;
        float springConstant = 1000;
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

    protected override void Update(GameTime gameTime)
    {
        const float fixedDeltaTime = 1f / 1000f;

        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
            Exit();

        for (int i = 0; i < _cloth.particles.Length; i++)
        {
            for (int j = 0; j < _cloth.particles[i].Length; j++)
            {
                _cloth.particles[i][j].AccumulatedForce = Vector2.Zero;
            }
        }

        for (int i = 0; i < _cloth.horizontalSticks.Length; i++)
        {
            for (int j = 0; j < _cloth.horizontalSticks[i].Length; j++)
            {
                DrawableStick s = _cloth.horizontalSticks[i][j];
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

        for (int i = 0; i < _cloth.verticalSticks.Length; i++)
        {
            for (int j = 0; j < _cloth.verticalSticks[i].Length; j++)
            {
                DrawableStick s = _cloth.verticalSticks[i][j];
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

        for (int i = 0; i < _cloth.particles.Length; i++)
        {
            for (int j = 0; j < _cloth.particles[i].Length; j++)
            {
                DrawableParticle p = _cloth.particles[i][j];

                if (p.IsPinned)
                {
                    continue;
                }

                Vector2 totalForce = new Vector2(0, 100f) + p.AccumulatedForce;
                Vector2 acceleration = totalForce / p.Mass;

                Vector2 velocity = p.Position - p.PreviousPosition;
                velocity *= _cloth.drag;

                Vector2 previousPosition = p.Position;
                p.Position =
                    p.Position + velocity + acceleration * (fixedDeltaTime * fixedDeltaTime);
                p.PreviousPosition = previousPosition;
                p = KeepInsideScreen(p);
                _cloth.particles[i][j] = p;
            }
        }

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
