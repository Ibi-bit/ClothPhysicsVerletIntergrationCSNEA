using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using VectorGraphics;
using VectorGui;

namespace PhysicsCSAlevlProject;

public class Game1 : Game
{
  
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
                new Vector2(220 + (cols - 1) * naturalLength, 20),\
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

    private DrawableStick[][] PhysicsConstraints(DrawableStick[][] sticks, float springConstant)
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

                    Vector2 springForce = stickDir * stretch * springConstant;

                    s.P1.AccumulatedForce -= springForce;
                    s.P2.AccumulatedForce += springForce;
                }
            }
        }
        return sticks;
    }

    private DrawableParticle[][] PhysicsParticles(
        DrawableParticle[][] particles,
        float deltaTime,
        float drag
    )
    {
        for (int i = 0; i < particles.Length; i++)
        {
            for (int j = 0; j < particles[i].Length; j++)
            {
                DrawableParticle p = particles[i][j];

                if (p.IsPinned)
                {
                    continue;
                }

                Vector2 totalForce = new Vector2(0, 980) + p.AccumulatedForce;
                Vector2 acceleration = totalForce / p.Mass;

                Vector2 velocity = p.Position - p.PreviousPosition;
                velocity *= drag;

                Vector2 previousPosition = p.Position;
                p.Position = p.Position + velocity + acceleration * (deltaTime * deltaTime);
                p.PreviousPosition = previousPosition;
                p = KeepInsideScreen(p);
                particles[i][j] = p;
            }
        }
        return particles;
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

        _cloth.horizontalSticks = PhysicsConstraints(
            _cloth.horizontalSticks,
            _cloth.springConstant,
            _cloth.drag
        );

        _cloth.verticalSticks = PhysicsConstraints(_cloth.verticalSticks, _cloth.springConstant);

        _cloth.particles = PhysicsParticles(_cloth.particles, fixedDeltaTime);

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
