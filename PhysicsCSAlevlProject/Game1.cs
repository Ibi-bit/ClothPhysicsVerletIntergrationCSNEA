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
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private PrimitiveBatch _primitiveBatch;
    bool leftPressed;
    bool radialMenuPressed;
    Vector2 intitialMousePosWhenPressed;
    Vector2 intitialMousePosWhenRadialMenuPressed;

    float _springConstant;

    Vector2 windForce;
    Vector2 previousMousePos;
    float dragRadius = 20f;

    Vector2 BaseForce = new Vector2(0, 980f);
    List<Vector2> particlesInDragArea = new List<Vector2>();

    private RadialMenu _radialMenu;
    private List<Tool> _tools;

    private VectorGraphics.PrimitiveBatch.Arrow windDirectionArrow;
    private VectorGraphics.PrimitiveBatch.Line cutLine;
    private int _selectedToolIndex = 0;
    private SpriteFont _font;

    private Gui.Slider _springConstantSlider;
    private const float FixedTimeStep = 1f / 10000f;
    private float _timeAccumulator = 0f;

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

        windDirectionArrow = null;

        _springConstantSlider = new Gui.Slider(
            new Vector2(10, 50),
            100f,
            100000f,
            10000f,
            Color.Gray,
            Color.LightGray,
            Color.White
        );
        _springConstantSlider.Initialize();

        _tools = new List<Tool>
        {
            new Tool("Drag", null, null),
            new Tool("Pin", null, null),
            new Tool("Cut", null, null),
            new Tool("Wind", null, null),
            new Tool("DragOne", null, null),
            new Tool("PhysicsDrag", null, null),
            new Tool("LineCut", null, null),
        };

        _radialMenu = new RadialMenu(_tools, 80f, 32f);

        float naturalLength = 10f;
        _springConstant = 100000;
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
            _springConstant,
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
                        s.Color = Color.Lerp(Color.White,Color.Red,currentLength/(s.Length*8));

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
        float forceMagnitudeSum = 0f;
        float forceMagnitudeSquaredSum = 0f;
        float maxForceMagnitude = 0f;
        int totalForceCount = 0;

        for (int i = 0; i < _cloth.particles.Length; i++)
        {
            for (int j = 0; j < _cloth.particles[i].Length; j++)
            {
                DrawableParticle p = _cloth.particles[i][j];

                Vector2 totalForce = BaseForce + p.AccumulatedForce + windForce;
                p.TotalForceMagnitude = totalForce.Length();
                forceMagnitudeSum += p.TotalForceMagnitude;
                forceMagnitudeSquaredSum += p.TotalForceMagnitude * p.TotalForceMagnitude;
                totalForceCount++;

                if (p.TotalForceMagnitude > maxForceMagnitude)
                {
                    maxForceMagnitude = p.TotalForceMagnitude;
                }

                if (p.IsPinned)
                {
                    p.AccumulatedForce = Vector2.Zero;
                    continue;
                }

                bool isBeingDragged = false;
                if (
                    leftPressed && _selectedToolIndex == 0
                    || leftPressed && _selectedToolIndex == 4
                )
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

        if (totalForceCount > 0)
        {
            _cloth.meanForceMagnitude = forceMagnitudeSum / totalForceCount;
            float meanSquare = _cloth.meanForceMagnitude * _cloth.meanForceMagnitude;
            float variance = (forceMagnitudeSquaredSum / totalForceCount) - meanSquare;
            if (variance < 0f)
            {
                variance = 0f;
            }

            _cloth.forceStdDeviation = (float)Math.Sqrt(variance);
            _cloth.maxForceMagnitude = maxForceMagnitude;
        }
        else
        {
            _cloth.meanForceMagnitude = 0f;
            _cloth.forceStdDeviation = 0f;
            _cloth.maxForceMagnitude = 0f;
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
            foreach (Vector2 particle in particlesInDragArea)
            {
                var p = _cloth.particles[(int)particle.X][(int)particle.Y];
                if (!p.IsPinned)
                {
                    Vector2 displacement = mousePos - p.Position;
                    float distance = displacement.Length();

                    if (distance > 1f)
                    {
                        float moveSpeed = 0.1f;
                        Vector2 positionDelta = displacement * moveSpeed;

                        p.Position += positionDelta;
                        p.PreviousPosition += positionDelta * 0.9f;

                        _cloth.particles[(int)particle.X][(int)particle.Y] = p;
                        _cloth.particles[(int)particle.X][(int)particle.Y].Color = Color.Orange;
                    }
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
        float frameTime = (float)Math.Min(gameTime.ElapsedGameTime.TotalSeconds, 0.1);
        _timeAccumulator += frameTime;

        _springConstantSlider.Update(Mouse.GetState());
        _cloth.springConstant = _springConstantSlider.Value;

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
                        CutAllSticksInRadius(intitialMousePosWhenPressed, dragRadius);
                        break;
                    case 3:

                        break;
                    case 4:
                        particlesInDragArea = GetParticlesInRadius(intitialMousePosWhenPressed, 10);
                        break;
                    case 5:
                        particlesInDragArea = GetParticlesInRadius(
                            intitialMousePosWhenPressed,
                            dragRadius
                        );
                        break;
                    case 6:
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
                else if (_selectedToolIndex == 6 && leftPressed)
                {
                    Vector2 cutDirection = currentMousePos - intitialMousePosWhenPressed;
                    float cutDistance = cutDirection.Length();
                    if (cutDistance > 5f)
                    {
                        CutSticksAlongLine(intitialMousePosWhenPressed, currentMousePos);
                    }
                }

                leftPressed = false;
                intitialMousePosWhenPressed = Vector2.Zero;
                windDirectionArrow = null;
                cutLine = null;
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
        else if (_selectedToolIndex == 6 && leftPressed)
        {
            Vector2 cutDirection = currentMousePos - intitialMousePosWhenPressed;
            float cutDistance = cutDirection.Length();
            if (cutDistance > 5f)
            {
                cutLine = new VectorGraphics.PrimitiveBatch.Line(
                    intitialMousePosWhenPressed,
                    currentMousePos,
                    Color.Red,
                    3f
                );
            }
            else
            {
                cutLine = null;
            }
        }

        int stepsThisFrame = 0;
        const int maxStepsPerFrame = 1000;

        while (_timeAccumulator >= FixedTimeStep && stepsThisFrame < maxStepsPerFrame)
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
            UpdateParticles(FixedTimeStep);

            _timeAccumulator -= FixedTimeStep;
            stepsThisFrame++;
        }

        if (stepsThisFrame == maxStepsPerFrame)
        {
            _timeAccumulator = Math.Min(_timeAccumulator, FixedTimeStep);
        }

        if (_selectedToolIndex == 0)
        {
            DragAreaParticles(mouseState, leftPressed, particlesInDragArea);
        }
        else if (_selectedToolIndex == 4)
        {
            DragAreaParticles(mouseState, leftPressed, particlesInDragArea);
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

        _cloth.Draw(_spriteBatch, _primitiveBatch);

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

        if (windDirectionArrow != null)
        {
            windDirectionArrow.Draw(_spriteBatch, _primitiveBatch);
        }

        if (cutLine != null)
        {
            cutLine.Draw(_spriteBatch, _primitiveBatch);
        }
        _springConstantSlider.Draw(_spriteBatch, _primitiveBatch);
        string sliderLabel = $"Spring Constant: {_springConstantSlider.Value:F1}";

        _spriteBatch.DrawString(_font, sliderLabel, new Vector2(10, 70), Color.White);

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

    private void CutSticksInRadius(Vector2 center, float radius, DrawableStick[][] sticks)
    {
        for (int i = 0; i < sticks.Length; i++)
        {
            for (int j = 0; j < sticks[i].Length; j++)
            {
                if (sticks[i][j] != null)
                {
                    Vector2 stickCenter =
                        (sticks[i][j].P1.Position + sticks[i][j].P2.Position) * 0.5f;
                    float distance = Vector2.Distance(stickCenter, center);
                    if (distance <= radius)
                    {
                        sticks[i][j] = null;
                    }
                }
            }
        }
    }

    private void CutAllSticksInRadius(Vector2 center, float radius)
    {
        CutSticksInRadius(center, radius, _cloth.horizontalSticks);
        CutSticksInRadius(center, radius, _cloth.verticalSticks);
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

    private bool DoTwoLinesIntersect(
        Vector2 line1Start,
        Vector2 line1End,
        Vector2 line2Start,
        Vector2 line2End
    )
    {
        Vector2 r = line1End - line1Start;
        Vector2 s = line2End - line2Start;
        Vector2 qMinusP = line2Start - line1Start;

        float rCrossS = r.X * s.Y - r.Y * s.X;
        float qMinusPCrossR = qMinusP.X * r.Y - qMinusP.Y * r.X;

        if (Math.Abs(rCrossS) < 0.0001f)
        {
            return false;
        }

        float t = (qMinusP.X * s.Y - qMinusP.Y * s.X) / rCrossS;
        float u = qMinusPCrossR / rCrossS;

        return (t >= 0 && t <= 1 && u >= 0 && u <= 1);
    }

    private DrawableStick[][] DoLinesIntersect(
        DrawableStick[][] sticks,
        Vector2 lineStart,
        Vector2 lineEnd
    )
    {
        for (int i = 0; i < sticks.Length; i++)
        {
            for (int j = 0; j < sticks[i].Length; j++)
            {
                if (sticks[i][j] != null)
                {
                    Vector2 stickStart = sticks[i][j].P1.Position;
                    Vector2 stickEnd = sticks[i][j].P2.Position;

                    if (DoTwoLinesIntersect(lineStart, lineEnd, stickStart, stickEnd))
                    {
                        _cloth.horizontalSticks[i][j] = null;
                    }
                }
            }
        }
        return sticks;
    }

    private void CutSticksAlongLine(Vector2 lineStart, Vector2 lineEnd)
    {
        _cloth.horizontalSticks = DoLinesIntersect(_cloth.horizontalSticks, lineStart, lineEnd);
        _cloth.verticalSticks = DoLinesIntersect(_cloth.verticalSticks, lineStart, lineEnd);
    }
}
