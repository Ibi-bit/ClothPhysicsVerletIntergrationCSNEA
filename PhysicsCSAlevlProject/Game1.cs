using System;
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
    // Gravity: 9.8 m/s² = 980 cm/s² = 980 pixels/s

    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private PrimitiveBatch _primitiveBatch;
    bool leftPressed;
    bool radialMenuPressed;
    Vector2 intitialMousePosWhenPressed;
    Vector2 intitialMousePosWhenRadialMenuPressed;

    Vector2 windForce;
    Vector2 previousMousePos;
    float dragRadius = 20f;
    List<Vector2> particlesInDragArea = new List<Vector2>();

    private RadialMenu _radialMenu;
    private List<Tool> _tools;

    private VectorGraphics.PrimitiveBatch.Arrow windDirectionArrow;
    private int _selectedToolIndex = 0;
    private SpriteFont _font;

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
        radialMenuPressed = false;

        windDirectionArrow = null; // Initialize as null

        _tools = new List<Tool>
        {
            new Tool("Drag", null, null),
            new Tool("Pin", null, null),
            new Tool("Cut", null, null),
            new Tool("Wind", null, null),
            new Tool("DragOne", null, null),
            new Tool("PhysicsDrag", null, null),
        };

        _radialMenu = new RadialMenu(_tools, 80f, 32f);

        float naturalLength = 10f;
        float springConstant = 10000;
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

        _font = Content.Load<SpriteFont>("Font");
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
                if (sticks[i][j] != null)
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

                bool isBeingDragged = false;
                if (leftPressed && _selectedToolIndex == 0) // Only skip physics for direct drag (tool 0)
                {
                    foreach (Vector2 draggedParticle in particlesInDragArea)
                    {
                        if ((int)draggedParticle.X == i && (int)draggedParticle.Y == j)
                        {
                            isBeingDragged = true;
                            break;
                        }
                    }
                }

                if (!isBeingDragged)
                {
                    Vector2 totalForce = new Vector2(0, 980f) + p.AccumulatedForce + windForce;
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
    }

    private List<Vector2> GetParticlesInRadius(Vector2 mousePosition, float radius)
    {
        var particlesInRadius = new List<Vector2>();
        for (int i = 0; i < _cloth.particles.Length; i++)
        {
            for (int j = 0; j < _cloth.particles[i].Length; j++)
            {
                Vector2 pos = _cloth.particles[i][j].Position;
                if (Vector2.DistanceSquared(pos, mousePosition) < (radius * radius))
                {
                    particlesInRadius.Add(new Vector2(i, j));
                }
            }
        }
        return particlesInRadius;
    }

    private void DragAreaParticles(
        MouseState mouseState,
        bool isDragging,
        List<Vector2> particlesInDragArea
    )
    {
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);

        if (isDragging)
        {
            Vector2 frameDelta = mousePos - previousMousePos;
            foreach (Vector2 particle in particlesInDragArea)
            {
                var p = _cloth.particles[(int)particle.X][(int)particle.Y];
                if (!p.IsPinned)
                {
                    p.Position += frameDelta;
                    p.PreviousPosition += frameDelta;
                    _cloth.particles[(int)particle.X][(int)particle.Y] = p;
                    _cloth.particles[(int)particle.X][(int)particle.Y].Color = Color.Yellow;
                }
            }
        }
        else
        {
            foreach (Vector2 particle in particlesInDragArea)
            {
                var p = _cloth.particles[(int)particle.X][(int)particle.Y];
                if (!p.IsPinned)
                {
                    _cloth.particles[(int)particle.X][(int)particle.Y].Color = Color.White;
                    { }
                    ;
                }
            }
        }
    }

    private void DragAreaParticlesWithPhysics(
        MouseState mouseState,
        bool isDragging,
        List<Vector2> particlesInDragArea
    )
    {
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);

        if (isDragging)
        {
            // Apply constraint forces to gently pull particles toward mouse
            foreach (Vector2 particle in particlesInDragArea)
            {
                var p = _cloth.particles[(int)particle.X][(int)particle.Y];
                if (!p.IsPinned)
                {
                    // Calculate displacement from particle to mouse
                    Vector2 displacement = mousePos - p.Position;
                    float distance = displacement.Length();

                    if (distance > 1f) // Only apply force if there's meaningful distance
                    {
                        // Apply a gentle constraint force proportional to distance
                        float constraintStrength = 200f; // Softer than spring force
                        Vector2 normalizedDisplacement = Vector2.Normalize(displacement);
                        Vector2 constraintForce =
                            normalizedDisplacement * Math.Min(distance * constraintStrength, 1000f);

                        // Add to accumulated force so it gets processed in physics update
                        p.AccumulatedForce += constraintForce;
                        _cloth.particles[(int)particle.X][(int)particle.Y] = p;
                        _cloth.particles[(int)particle.X][(int)particle.Y].Color = Color.Orange;
                    }
                }
            }
        }
        else
        {
            // Reset colors when not dragging
            foreach (Vector2 particle in particlesInDragArea)
            {
                var p = _cloth.particles[(int)particle.X][(int)particle.Y];
                if (!p.IsPinned)
                {
                    _cloth.particles[(int)particle.X][(int)particle.Y].Color = Color.White;
                }
            }
        }
    }

    private void DragOneParticle(MouseState mouseState, bool isDragging)
    {
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);

        if (isDragging)
        {
            Vector2 frameDelta = mousePos - previousMousePos;

            Particle closestParticle = null;
            float closestDistance = float.MaxValue;

            for (int i = 0; i < _cloth.particles.Length; i++)
            {
                for (int j = 0; j < _cloth.particles[i].Length; j++)
                {
                    float distance = Vector2.Distance(_cloth.particles[i][j].Position, mousePos);
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestParticle = _cloth.particles[i][j];
                    }
                }
            }
        }
    }

    protected override void Update(GameTime gameTime)
    {
        const float fixedDeltaTime = 1f / 10000f; 
        float frameTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
        int physicsSteps = Math.Max(1, (int)Math.Ceiling(frameTime / fixedDeltaTime)); /

        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
            Exit();
        MouseState mouseState = Mouse.GetState();
        KeyboardState keyboardState = Keyboard.GetState();
        Vector2 currentMousePos = new Vector2(mouseState.X, mouseState.Y);

        if (mouseState.RightButton == ButtonState.Pressed && !radialMenuPressed)
        {
            radialMenuPressed = true;
            intitialMousePosWhenRadialMenuPressed = currentMousePos;
        }
        else if (mouseState.RightButton == ButtonState.Released && radialMenuPressed)
        {
            radialMenuPressed = false;
        }

        if (radialMenuPressed)
        {
            _selectedToolIndex = _radialMenu.RadialToolMenuLogic(
                mouseState,
                keyboardState,
                intitialMousePosWhenRadialMenuPressed,
                radialMenuPressed,
                _selectedToolIndex,
                _tools
            );
        }

        if (!radialMenuPressed)
        {
            if (mouseState.LeftButton == ButtonState.Pressed && !leftPressed)
            {
                leftPressed = true;
                intitialMousePosWhenPressed = currentMousePos;
                previousMousePos = currentMousePos;

                switch (_selectedToolIndex)
                {
                    case 0:
                        particlesInDragArea = GetParticlesInRadius(
                            intitialMousePosWhenPressed,
                            dragRadius
                        );
                        break;
                    case 1:
                        PinParticle(intitialMousePosWhenPressed, dragRadius);
                        break;
                    case 2:
                        CutSticksInRadius(intitialMousePosWhenPressed, dragRadius);
                        break;
                    case 3:

                        break;
                    case 4:
                        DragOneParticle(mouseState, leftPressed);
                        break;
                    case 5:
                        particlesInDragArea = GetParticlesInRadius(
                            intitialMousePosWhenPressed,
                            dragRadius
                        );
                        break;
                }
            }
            else if (mouseState.LeftButton == ButtonState.Released)
            {
                if (_selectedToolIndex == 3 && leftPressed)
                {
                    ApplyWindForceFromDrag(
                        intitialMousePosWhenPressed,
                        currentMousePos,
                        dragRadius
                    );
                }

                leftPressed = false;
                intitialMousePosWhenPressed = Vector2.Zero;
                windDirectionArrow = null;
            }
        }

        if (_selectedToolIndex == 3 && leftPressed)
        {
            Vector2 windDirection = currentMousePos - intitialMousePosWhenPressed;
            float windDistance = windDirection.Length();

            if (windDistance > 5f)
            {
                windDirectionArrow = new VectorGraphics.PrimitiveBatch.Arrow(
                    intitialMousePosWhenPressed,
                    currentMousePos,
                    Color.Cyan,
                    3f
                );

                windForce = windDirection * (windDistance / 50f);
            }
            else
            {
                windDirectionArrow = null;
                windForce = Vector2.Zero;
            }
        }

        for (int step = 0; step < physicsSteps; step++)
        {
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
        }

        if (_selectedToolIndex == 0)
        {
            DragAreaParticles(mouseState, leftPressed, particlesInDragArea);
        }
        else if (_selectedToolIndex == 4)
        {
            DragOneParticle(mouseState, leftPressed);
        }
        else if (_selectedToolIndex == 5)
        {
            DragAreaParticlesWithPhysics(mouseState, leftPressed, particlesInDragArea);
        }

        previousMousePos = currentMousePos;

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
                if (_cloth.horizontalSticks[i][j] != null)
                    _cloth.horizontalSticks[i][j].Draw(_spriteBatch, _primitiveBatch);
            }
        }

        for (int i = 0; i < _cloth.verticalSticks.Length; i++)
        {
            for (int j = 0; j < _cloth.verticalSticks[i].Length; j++)
            {
                if (_cloth.verticalSticks[i][j] != null)
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

        if (radialMenuPressed)
        {
            _radialMenu.index = _selectedToolIndex;
            _radialMenu.Draw(
                _spriteBatch,
                intitialMousePosWhenRadialMenuPressed,
                _font,
                _primitiveBatch
            );
        }

        if (_font != null)
        {
            string currentTool = $"Current Tool: {_tools[_selectedToolIndex].Name}";
            _spriteBatch.DrawString(_font, currentTool, new Vector2(10, 10), Color.White);
        }

        // Draw wind direction arrow if active
        if (windDirectionArrow != null)
        {
            windDirectionArrow.Draw(_spriteBatch, _primitiveBatch);
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void PinParticle(Vector2 center, float radius)
    {
        float closestDistance = float.MaxValue;
        int closestI = -1;
        int closestJ = -1;

        for (int i = 0; i < _cloth.particles.Length; i++)
        {
            for (int j = 0; j < _cloth.particles[i].Length; j++)
            {
                float distance = Vector2.Distance(_cloth.particles[i][j].Position, center);
                if (distance <= radius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestI = i;
                    closestJ = j;
                }
            }
        }

        if (closestI >= 0 && closestJ >= 0)
        {
            _cloth.particles[closestI][closestJ].IsPinned = !_cloth
                .particles[closestI][closestJ]
                .IsPinned;
        }
    }

    private void CutSticksInRadius(Vector2 center, float radius)
    {
        for (int i = 0; i < _cloth.horizontalSticks.Length; i++)
        {
            for (int j = 0; j < _cloth.horizontalSticks[i].Length; j++)
            {
                if (_cloth.horizontalSticks[i][j] != null)
                {
                    Vector2 stickCenter =
                        (
                            _cloth.horizontalSticks[i][j].P1.Position
                            + _cloth.horizontalSticks[i][j].P2.Position
                        ) * 0.5f;
                    float distance = Vector2.Distance(stickCenter, center);
                    if (distance <= radius)
                    {
                        _cloth.horizontalSticks[i][j] = null;
                    }
                }
            }
        }

        for (int i = 0; i < _cloth.verticalSticks.Length; i++)
        {
            for (int j = 0; j < _cloth.verticalSticks[i].Length; j++)
            {
                if (_cloth.verticalSticks[i][j] != null)
                {
                    Vector2 stickCenter =
                        (
                            _cloth.verticalSticks[i][j].P1.Position
                            + _cloth.verticalSticks[i][j].P2.Position
                        ) * 0.5f;
                    float distance = Vector2.Distance(stickCenter, center);
                    if (distance <= radius)
                    {
                        _cloth.verticalSticks[i][j] = null; // Cut the stick
                    }
                }
            }
        }
    }

    private void ApplyWindForceFromDrag(Vector2 startPos, Vector2 endPos, float radius)
    {
        Vector2 windDirection = endPos - startPos;
        float windDistance = windDirection.Length();

        if (windDistance < 5f)
            return;
        else
        {
            windForce = windDirection * (windDistance / 50f);
        }
    }
}
