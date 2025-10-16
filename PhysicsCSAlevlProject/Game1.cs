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

    bool Paused = false;

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

    private Mesh _activeMesh;
    private Cloth _clothInstance;
    private BuildableMesh _buildableMeshInstance;

    private enum MeshMode
    {
        Cloth,
        Buildable,
    }

    private MeshMode _currentMode = MeshMode.Cloth;
    private bool _tabKeyWasPressed = false;

    private KeyboardState _prevKeyboardState;

    private VectorGui.Gui.DropDownMenu _mainMenu;

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

        _clothInstance = new Cloth(
            new Vector2(200, 200),
            pinnedParticles,
            naturalLength,
            _springConstant,
            mass
        );

        _buildableMeshInstance = new BuildableMesh(_springConstant, mass);

        _activeMesh = _clothInstance;
        _currentMode = MeshMode.Cloth;

        _mainMenu = new VectorGui.Gui.DropDownMenu(
            new Vector2(
                _graphics.PreferredBackBufferWidth / 2 - 110,
                _graphics.PreferredBackBufferHeight / 2 - 80
            ),
            new Vector2(220, 160),
            new Vector2(180, 40),
            3,
            Color.Black
        );
        _mainMenu.Initialize();
        _mainMenu.IsVisible = false;

        base.Initialize();
    }

    private void SwitchMode()
    {
        if (_currentMode == MeshMode.Cloth)
        {
            _currentMode = MeshMode.Buildable;
            _activeMesh = _buildableMeshInstance;
        }
        else
        {
            _currentMode = MeshMode.Cloth;
            _activeMesh = _clothInstance;
        }

        leftPressed = false;
        radialMenuPressed = false;
        windDirectionArrow = null;
        cutLine = null;
        particlesInDragArea.Clear();
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

    private DrawableStick[][] ApplyStickForces(DrawableStick[][] sticks)
    {
        for (int i = 0; i < sticks.Length; i++)
        {
            for (int j = 0; j < sticks[i].Length; j++)
            {
                var s = sticks[i][j];
                if (s.IsCut)
                {
                    continue;
                }
                float L0 = s.Length;
                if (L0 <= 0f)
                {
                    continue;
                }
                Vector2 v = s.P1.Position - s.P2.Position;
                float L = v.Length();
                if (L <= 0f)
                {
                    continue;
                }
                Vector2 dir = v / L;
                float stretch = L - L0;
                Vector2 springForce = dir * stretch * _activeMesh.springConstant;
                s.P1.AccumulatedForce -= springForce;
                s.P2.AccumulatedForce += springForce;
            }
        }
        return sticks;
    }

    private void ApplyStickForcesDictionary(Dictionary<int, Mesh.MeshStick> sticks)
    {
        foreach (var stick in sticks.Values)
        {
            if (stick.Length <= 0f)
                continue;
            Vector2 v = stick.P1.Position - stick.P2.Position;
            float L = v.Length();
            if (L <= 0f)
                continue;
            Vector2 dir = v / L;
            float stretch = L - stick.Length;
            Vector2 springForce = dir * stretch * _activeMesh.springConstant;
            stick.P1.AccumulatedForce -= springForce;
            stick.P2.AccumulatedForce += springForce;
        }
    }

    private void UpdateStickColorsDictionary(Dictionary<int, Mesh.MeshStick> sticks)
    {
        int count = 0;
        float sum = 0f;
        float sumSq = 0f;

        foreach (var s in sticks.Values)
        {
            if (s.Length <= 0f)
                continue;
            Vector2 v = s.P1.Position - s.P2.Position;
            float L = v.Length();
            if (L <= 0f)
                continue;
            float e = (L - s.Length) / s.Length;
            sum += e;
            sumSq += e * e;
            count++;
        }

        float mean = count > 0 ? sum / count : 0f;
        float variance = count > 0 ? (sumSq / count) - mean * mean : 0f;
        if (variance < 0f)
            variance = 0f;
        float std = (float)Math.Sqrt(variance);

        foreach (var s in sticks.Values)
        {
            if (s.Length <= 0f)
                continue;
            Vector2 v = s.P1.Position - s.P2.Position;
            float L = v.Length();
            if (L <= 0f)
                continue;
            float e = (L - s.Length) / s.Length;

            float intensity = 0f;
            if (count > 0 && std > 1e-5f)
            {
                float z = (e - mean) / std;
                intensity = MathHelper.Clamp((z - 0.5f) / 1.5f, 0f, 1f);
            }
            else
            {
                intensity = MathHelper.Clamp((L / s.Length - 1f) / 0.5f, 0f, 1f);
            }
            float eased = intensity * intensity;
            s.Color = Color.Lerp(Color.White, Color.Red, eased);
        }
    }

    private void UpdateStickColorsRelative(DrawableStick[][] horizontal, DrawableStick[][] vertical)
    {
        int count = 0;
        float sum = 0f;
        float sumSq = 0f;

        void Accumulate(DrawableStick[][] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                for (int j = 0; j < arr[i].Length; j++)
                {
                    var s = arr[i][j];
                    if (s == null)
                    {
                        continue;
                    }
                    if (s.Length <= 0f)
                    {
                        continue;
                    }
                    Vector2 v = s.P1.Position - s.P2.Position;
                    float L = v.Length();
                    if (L <= 0f)
                    {
                        continue;
                    }
                    float e = (L - s.Length) / s.Length;
                    sum += e;
                    sumSq += e * e;
                    count++;
                }
            }
        }

        Accumulate(horizontal);
        Accumulate(vertical);

        float mean = count > 0 ? sum / count : 0f;
        float variance = count > 0 ? (sumSq / count) - mean * mean : 0f;
        if (variance < 0f)
        {
            variance = 0f;
        }
        float std = (float)Math.Sqrt(variance);

        void Colorize(DrawableStick[][] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                for (int j = 0; j < arr[i].Length; j++)
                {
                    var s = arr[i][j];
                    if (s == null)
                    {
                        continue;
                    }
                    if (s.Length <= 0f)
                    {
                        continue;
                    }
                    Vector2 v = s.P1.Position - s.P2.Position;
                    float L = v.Length();
                    if (L <= 0f)
                    {
                        continue;
                    }
                    float e = (L - s.Length) / s.Length;

                    float intensity = 0f;
                    if (count > 0 && std > 1e-5f)
                    {
                        float z = (e - mean) / std;
                        intensity = MathHelper.Clamp((z - 0.5f) / 1.5f, 0f, 1f);
                    }
                    else
                    {
                        intensity = MathHelper.Clamp((L / s.Length - 1f) / 0.5f, 0f, 1f);
                    }
                    float eased = intensity * intensity;
                    s.Color = Color.Lerp(Color.White, Color.Red, eased);
                }
            }
        }

        Colorize(horizontal);
        Colorize(vertical);
    }

    private void UpdateParticles(float deltaTime)
    {
        float forceMagnitudeSum = 0f;
        float forceMagnitudeSquaredSum = 0f;
        float maxForceMagnitude = 0f;
        int totalForceCount = 0;

        if (_currentMode == MeshMode.Cloth)
        {
            for (int i = 0; i < _clothInstance.particles.Length; i++)
            {
                for (int j = 0; j < _clothInstance.particles[i].Length; j++)
                {
                    DrawableParticle p = _clothInstance.particles[i][j];

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
                    if (leftPressed && (_selectedToolIndex == 0 || _selectedToolIndex == 4))
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
                        velocity *= _clothInstance.drag;

                        Vector2 previousPosition = p.Position;
                        p.Position = p.Position + velocity + acceleration * (deltaTime * deltaTime);
                        p.PreviousPosition = previousPosition;
                        p = KeepInsideScreen(p);
                        _clothInstance.particles[i][j] = p;
                    }
                }
            }
        }
        else
        {
            foreach (var particle in _activeMesh.Particles.Values)
            {
                Vector2 totalForce = BaseForce + particle.AccumulatedForce + windForce;
                particle.TotalForceMagnitude = totalForce.Length();
                forceMagnitudeSum += particle.TotalForceMagnitude;
                forceMagnitudeSquaredSum +=
                    particle.TotalForceMagnitude * particle.TotalForceMagnitude;
                totalForceCount++;

                if (particle.TotalForceMagnitude > maxForceMagnitude)
                {
                    maxForceMagnitude = particle.TotalForceMagnitude;
                }

                if (particle.IsPinned)
                {
                    particle.AccumulatedForce = Vector2.Zero;
                    continue;
                }

                Vector2 acceleration = totalForce / particle.Mass;
                Vector2 velocity = particle.Position - particle.PreviousPosition;
                velocity *= _activeMesh.drag;

                Vector2 previousPosition = particle.Position;
                particle.Position =
                    particle.Position + velocity + acceleration * (deltaTime * deltaTime);
                particle.PreviousPosition = previousPosition;

                // Keep inside screen
                bool positionChanged = false;
                if (particle.Position.X < 0)
                {
                    particle.Position.X = 0;
                    positionChanged = true;
                }
                else if (particle.Position.X > _graphics.PreferredBackBufferWidth)
                {
                    particle.Position.X = _graphics.PreferredBackBufferWidth;
                    positionChanged = true;
                }

                if (particle.Position.Y < 0)
                {
                    particle.Position.Y = 0;
                    positionChanged = true;
                }
                else if (particle.Position.Y > _graphics.PreferredBackBufferHeight - 10)
                {
                    particle.Position.Y = _graphics.PreferredBackBufferHeight - 10;
                    positionChanged = true;
                }

                if (positionChanged)
                {
                    particle.PreviousPosition = particle.Position;
                }
            }
        }

        if (totalForceCount > 0)
        {
            _activeMesh.meanForceMagnitude = forceMagnitudeSum / totalForceCount;
            float meanSquare = _activeMesh.meanForceMagnitude * _activeMesh.meanForceMagnitude;
            float variance = (forceMagnitudeSquaredSum / totalForceCount) - meanSquare;
            if (variance < 0f)
            {
                variance = 0f;
            }

            _activeMesh.forceStdDeviation = (float)Math.Sqrt(variance);
            _activeMesh.maxForceMagnitude = maxForceMagnitude;
        }
        else
        {
            _activeMesh.meanForceMagnitude = 0f;
            _activeMesh.forceStdDeviation = 0f;
            _activeMesh.maxForceMagnitude = 0f;
        }
    }

    private List<Vector2> GetParticlesInRadius(Vector2 mousePosition, float radius)
    {
        var particlesInRadius = new List<Vector2>();
        for (int i = 0; i < _clothInstance.particles.Length; i++)
        {
            for (int j = 0; j < _clothInstance.particles[i].Length; j++)
            {
                Vector2 pos = _clothInstance.particles[i][j].Position;
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
                var p = _clothInstance.particles[(int)particle.X][(int)particle.Y];
                if (!p.IsPinned)
                {
                    p.Position += frameDelta;
                    p.PreviousPosition += frameDelta;
                    _clothInstance.particles[(int)particle.X][(int)particle.Y] = p;
                    _clothInstance.particles[(int)particle.X][(int)particle.Y].Color = Color.Yellow;
                }
            }
        }
        else
        {
            foreach (Vector2 particle in particlesInDragArea)
            {
                var p = _clothInstance.particles[(int)particle.X][(int)particle.Y];
                if (!p.IsPinned)
                {
                    _clothInstance.particles[(int)particle.X][(int)particle.Y].Color = Color.White;
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
                var p = _clothInstance.particles[(int)particle.X][(int)particle.Y];
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

                        _clothInstance.particles[(int)particle.X][(int)particle.Y] = p;
                        _clothInstance.particles[(int)particle.X][(int)particle.Y].Color =
                            Color.Orange;
                    }
                }
            }
        }
        else
        {
            foreach (Vector2 particle in particlesInDragArea)
            {
                var p = _clothInstance.particles[(int)particle.X][(int)particle.Y];
                if (!p.IsPinned)
                {
                    _clothInstance.particles[(int)particle.X][(int)particle.Y].Color = Color.White;
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

            for (int i = 0; i < _clothInstance.particles.Length; i++)
            {
                for (int j = 0; j < _clothInstance.particles[i].Length; j++)
                {
                    float distance = Vector2.Distance(
                        _clothInstance.particles[i][j].Position,
                        mousePos
                    );
                    if (distance < closestDistance)
                    {
                        closestDistance = distance;
                        closestParticle = _clothInstance.particles[i][j];
                    }
                }
            }
        }
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();
        float frameTime = (float)Math.Min(gameTime.ElapsedGameTime.TotalSeconds, 0.1);
        _timeAccumulator += frameTime;

        if (keyboardState.IsKeyDown(Keys.Escape) && !_prevKeyboardState.IsKeyDown(Keys.Escape))
        {
            _mainMenu.IsVisible = !_mainMenu.IsVisible;
        }
        if (keyboardState.IsKeyDown(Keys.P) && !_prevKeyboardState.IsKeyDown(Keys.P))
        {
            Paused = !Paused;
        }

        if (_mainMenu.IsVisible)
        {
            int selected = _mainMenu.components.IndexOf(
                _mainMenu.OpenComponent as VectorGui.Gui.GuiRectangle
            );
            if (keyboardState.IsKeyDown(Keys.Up) && !_prevKeyboardState.IsKeyDown(Keys.Up))
            {
                selected = (selected - 1 + _mainMenu.components.Count) % _mainMenu.components.Count;
                _mainMenu.OpenComponent = _mainMenu.components[selected];
            }
            if (keyboardState.IsKeyDown(Keys.Down) && !_prevKeyboardState.IsKeyDown(Keys.Down))
            {
                selected = (selected + 1) % _mainMenu.components.Count;
                _mainMenu.OpenComponent = _mainMenu.components[selected];
            }
            if (keyboardState.IsKeyDown(Keys.Enter) && !_prevKeyboardState.IsKeyDown(Keys.Enter))
            {
                switch (selected)
                {
                    case 0:
                        _mainMenu.IsVisible = false;
                        break;
                    case 1:
                        SwitchMode();
                        _mainMenu.IsVisible = false;
                        break;
                    case 2:
                        Exit();
                        break;
                }
            }
            _prevKeyboardState = keyboardState;
            return;
        }

        _springConstantSlider.Update(Mouse.GetState());
        _activeMesh.springConstant = _springConstantSlider.Value;

        MouseState mouseState = Mouse.GetState();
        Vector2 currentMousePos = new Vector2(mouseState.X, mouseState.Y);

        
        if (keyboardState.IsKeyDown(Keys.Tab) && !_tabKeyWasPressed)
        {
            _tabKeyWasPressed = true;
            SwitchMode();
        }
        else if (keyboardState.IsKeyUp(Keys.Tab))
        {
            _tabKeyWasPressed = false;
        }

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
                    intitialMousePosWhenRadialMenuPressed,
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

        while (_timeAccumulator >= FixedTimeStep && stepsThisFrame < maxStepsPerFrame && !Paused)
        {
            
            if (_currentMode == MeshMode.Cloth)
            {
                for (int i = 0; i < _clothInstance.particles.Length; i++)
                {
                    for (int j = 0; j < _clothInstance.particles[i].Length; j++)
                    {
                        _clothInstance.particles[i][j].AccumulatedForce = Vector2.Zero;
                    }
                }

                _clothInstance.horizontalSticks = ApplyStickForces(_clothInstance.horizontalSticks);
                _clothInstance.verticalSticks = ApplyStickForces(_clothInstance.verticalSticks);
            }
            else
            {
                
                foreach (var particle in _activeMesh.Particles.Values)
                {
                    particle.AccumulatedForce = Vector2.Zero;
                }

                
                ApplyStickForcesDictionary(_activeMesh.Sticks);
            }

            UpdateParticles(FixedTimeStep);

            _timeAccumulator -= FixedTimeStep;
            stepsThisFrame++;
        }

        if (stepsThisFrame == maxStepsPerFrame)
        {
            _timeAccumulator = Math.Min(_timeAccumulator, FixedTimeStep);
        }

        
        if (_currentMode == MeshMode.Cloth)
        {
            UpdateStickColorsRelative(
                _clothInstance.horizontalSticks,
                _clothInstance.verticalSticks
            );
        }
        else
        {
            UpdateStickColorsDictionary(_activeMesh.Sticks);
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
        _prevKeyboardState = keyboardState;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _spriteBatch.Begin();

        _activeMesh.Draw(_spriteBatch, _primitiveBatch);

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

            string modeText = $"Mode: {_currentMode} (Press Tab to switch)";
            _spriteBatch.DrawString(_font, modeText, new Vector2(10, 30), Color.Yellow);
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

        if (_mainMenu.IsVisible)
        {
            _mainMenu.Draw(_spriteBatch, _primitiveBatch);
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    private void PinParticle(Vector2 center, float radius)
    {
        float closestDistance = float.MaxValue;
        int closestI = -1;
        int closestJ = -1;

        for (int i = 0; i < _clothInstance.particles.Length; i++)
        {
            for (int j = 0; j < _clothInstance.particles[i].Length; j++)
            {
                float distance = Vector2.Distance(_clothInstance.particles[i][j].Position, center);
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
            _clothInstance.particles[closestI][closestJ].IsPinned = !_clothInstance
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
        CutSticksInRadius(center, radius, _clothInstance.horizontalSticks);
        CutSticksInRadius(center, radius, _clothInstance.verticalSticks);
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
                        System.Diagnostics.Debug.WriteLine($"Cutting stick at [{i},{j}]");
                        sticks[i][j].IsCut = true;
                    }
                }
            }
        }
        return sticks;
    }

    private void CutSticksAlongLine(Vector2 lineStart, Vector2 lineEnd)
    {
        _clothInstance.horizontalSticks = DoLinesIntersect(
            _clothInstance.horizontalSticks,
            lineStart,
            lineEnd
        );
        _clothInstance.verticalSticks = DoLinesIntersect(
            _clothInstance.verticalSticks,
            lineStart,
            lineEnd
        );
    }
}
