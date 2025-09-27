using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using VectorGraphics;
using VectorGui;
using Cloo;

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

    // OpenCL fields
    private ComputeContext _computeContext;
    private ComputeCommandQueue _commandQueue;
    private ComputeProgram _computeProgram;
    private ComputeKernel _updateParticlesKernel;
    private ComputeKernel _calculateStickForcesKernel;
    private ComputeBuffer<GPUParticle> _particleBuffer;
    private ComputeBuffer<GPUStick> _stickBuffer;
    private ComputeBuffer<PhysicsParams> _physicsParamsBuffer;
    private ComputeBuffer<StickParams> _stickParamsBuffer;
    private bool _useOpenCL = true; // Toggle between OpenCL and CPU
    private KeyboardState _previousKeyboardState; // Track previous keyboard state

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
        };

        _radialMenu = new RadialMenu(_tools, 80f, 32f);

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

        _font = Content.Load<SpriteFont>("Font");
        
        // Initialize OpenCL
        InitializeOpenCL();
    }

    private void InitializeOpenCL()
    {
        try
        {
            Console.WriteLine("Attempting to initialize OpenCL...");
            
            // Check if any platforms are available
            if (ComputePlatform.Platforms.Count == 0)
            {
                Console.WriteLine("No OpenCL platforms found on this system.");
                _useOpenCL = false;
                return;
            }
            
            Console.WriteLine($"Found {ComputePlatform.Platforms.Count} OpenCL platform(s):");
            
            ComputePlatform selectedPlatform = null;
            ComputeDevice selectedDevice = null;
            
            // Try to find a suitable platform and device
            for (int i = 0; i < ComputePlatform.Platforms.Count; i++)
            {
                var platform = ComputePlatform.Platforms[i];
                Console.WriteLine($"  Platform {i}: {platform.Name} ({platform.Vendor})");
                
                if (platform.Devices.Count > 0)
                {
                    Console.WriteLine($"    Found {platform.Devices.Count} device(s):");
                    foreach (var device in platform.Devices)
                    {
                        Console.WriteLine($"      - {device.Name} ({device.Type})");
                        if (selectedDevice == null)
                        {
                            selectedPlatform = platform;
                            selectedDevice = device;
                        }
                    }
                }
                else
                {
                    Console.WriteLine("    No devices found on this platform.");
                }
            }
            
            if (selectedPlatform == null || selectedDevice == null)
            {
                Console.WriteLine("No suitable OpenCL device found. Falling back to CPU implementation.");
                _useOpenCL = false;
                return;
            }
            
            Console.WriteLine($"Selected: {selectedDevice.Name} on {selectedPlatform.Name}");
            
            // Create compute context
            _computeContext = new ComputeContext(ComputeDeviceTypes.All, 
                new ComputeContextPropertyList(selectedPlatform), null, IntPtr.Zero);
            
            // Create command queue
            _commandQueue = new ComputeCommandQueue(_computeContext, selectedDevice, 
                ComputeCommandQueueFlags.None);
            
            // Check if kernel files exist
            string kernelPath1 = "kernels/UpdateParticles.cl";
            string kernelPath2 = "kernels/CalculateStickForces.cl";
            
            if (!File.Exists(kernelPath1) || !File.Exists(kernelPath2))
            {
                Console.WriteLine($"Kernel files not found:");
                Console.WriteLine($"  {kernelPath1}: {File.Exists(kernelPath1)}");
                Console.WriteLine($"  {kernelPath2}: {File.Exists(kernelPath2)}");
                _useOpenCL = false;
                return;
            }
            
            // Load and build OpenCL kernels
            string updateParticlesSource = File.ReadAllText(kernelPath1);
            string calculateStickForcesSource = File.ReadAllText(kernelPath2);
            string combinedSource = updateParticlesSource + "\n" + calculateStickForcesSource;
            
            _computeProgram = new ComputeProgram(_computeContext, combinedSource);
            _computeProgram.Build(null, null, null, IntPtr.Zero);
            
            // Create kernels
            _updateParticlesKernel = _computeProgram.CreateKernel("updateParticles");
            _calculateStickForcesKernel = _computeProgram.CreateKernel("calculateStickForces");
            
            Console.WriteLine("OpenCL initialized successfully!");
            Console.WriteLine($"Using OpenCL: {_useOpenCL}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Failed to initialize OpenCL: {ex.Message}");
            Console.WriteLine($"Stack trace: {ex.StackTrace}");
            _useOpenCL = false; // Fall back to CPU implementation
        }
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
        if (_useOpenCL && _calculateStickForcesKernel != null)
        {
            return CalculateStickForcesGPU(sticks);
        }
        else
        {
            return CalculateStickForcesCPU(sticks);
        }
    }

    private DrawableStick[][] CalculateStickForcesCPU(DrawableStick[][] sticks)
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

    private DrawableStick[][] CalculateStickForcesGPU(DrawableStick[][] sticks)
    {
        try
        {
            int width = _cloth.particles.Length;
            int height = _cloth.particles[0].Length;
            int particleCount = width * height;

            // Convert particles to GPU format (needed for force accumulation)
            GPUParticle[] gpuParticles = new GPUParticle[particleCount];
            int index = 0;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    gpuParticles[index] = new GPUParticle(_cloth.particles[i][j]);
                    index++;
                }
            }

            // Convert sticks to GPU format and flatten the array
            List<GPUStick> gpuSticksList = new List<GPUStick>();
            for (int i = 0; i < sticks.Length; i++)
            {
                for (int j = 0; j < sticks[i].Length; j++)
                {
                    if (sticks[i][j] != null)
                    {
                        // Find particle indices for this stick
                        int p1I = -1, p1J = -1, p2I = -1, p2J = -1;
                        DrawableParticle p1 = sticks[i][j].P1 as DrawableParticle;
                        DrawableParticle p2 = sticks[i][j].P2 as DrawableParticle;

                        // Find indices by comparing references (this is a simplification)
                        // In a real implementation, you'd want to store indices directly
                        bool foundP1 = false, foundP2 = false;
                        for (int pi = 0; pi < width && (!foundP1 || !foundP2); pi++)
                        {
                            for (int pj = 0; pj < height && (!foundP1 || !foundP2); pj++)
                            {
                                if (!foundP1 && _cloth.particles[pi][pj] == p1)
                                {
                                    p1I = pi; p1J = pj; foundP1 = true;
                                }
                                if (!foundP2 && _cloth.particles[pi][pj] == p2)
                                {
                                    p2I = pi; p2J = pj; foundP2 = true;
                                }
                            }
                        }

                        if (foundP1 && foundP2)
                        {
                            gpuSticksList.Add(new GPUStick(sticks[i][j], p1I, p1J, p2I, p2J));
                        }
                    }
                }
            }

            GPUStick[] gpuSticks = gpuSticksList.ToArray();
            if (gpuSticks.Length == 0) return sticks; // No sticks to process

            // Create or update buffers
            if (_particleBuffer == null || _particleBuffer.Count != particleCount)
            {
                _particleBuffer?.Dispose();
                _particleBuffer = new ComputeBuffer<GPUParticle>(_computeContext,
                    ComputeMemoryFlags.ReadWrite, gpuParticles);
            }
            else
            {
                _commandQueue.WriteToBuffer(gpuParticles, _particleBuffer, false, null);
            }

            if (_stickBuffer == null || _stickBuffer.Count != gpuSticks.Length)
            {
                _stickBuffer?.Dispose();
                _stickBuffer = new ComputeBuffer<GPUStick>(_computeContext,
                    ComputeMemoryFlags.ReadOnly, gpuSticks);
            }
            else
            {
                _commandQueue.WriteToBuffer(gpuSticks, _stickBuffer, false, null);
            }

            // Prepare stick parameters
            StickParams stickParams = new StickParams(_cloth.springConstant, width, height);
            if (_stickParamsBuffer == null)
            {
                _stickParamsBuffer = new ComputeBuffer<StickParams>(_computeContext,
                    ComputeMemoryFlags.ReadOnly, new StickParams[] { stickParams });
            }
            else
            {
                _commandQueue.WriteToBuffer(new StickParams[] { stickParams },
                    _stickParamsBuffer, false, null);
            }

            // Set kernel arguments
            _calculateStickForcesKernel.SetMemoryArgument(0, _particleBuffer);
            _calculateStickForcesKernel.SetMemoryArgument(1, _stickBuffer);
            _calculateStickForcesKernel.SetMemoryArgument(2, _stickParamsBuffer);
            _calculateStickForcesKernel.SetValueArgument(3, gpuSticks.Length);

            // Execute kernel
            _commandQueue.Execute(_calculateStickForcesKernel, null, new long[] { gpuSticks.Length }, null, null);

            // Read results back
            _commandQueue.ReadFromBuffer(_particleBuffer, ref gpuParticles, false, null);
            _commandQueue.Finish();

            // Convert back to DrawableParticles and update forces
            index = 0;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    _cloth.particles[i][j].AccumulatedForce = gpuParticles[index].AccumulatedForce;
                    index++;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GPU stick forces calculation failed: {ex.Message}");
            // Fall back to CPU
            return CalculateStickForcesCPU(sticks);
        }

        return sticks;
    }

    private void UpdateParticles(float deltaTime)
    {
        if (_useOpenCL && _updateParticlesKernel != null)
        {
            UpdateParticlesGPU(deltaTime);
        }
        else
        {
            UpdateParticlesCPU(deltaTime);
        }
    }

    private void UpdateParticlesCPU(float deltaTime)
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
                if (leftPressed)
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
                    Vector2 totalForce = new Vector2(20, 100f) + p.AccumulatedForce + windForce;
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

    private void UpdateParticlesGPU(float deltaTime)
    {
        try
        {
            int width = _cloth.particles.Length;
            int height = _cloth.particles[0].Length;
            int particleCount = width * height;

            // Convert particles to GPU format
            GPUParticle[] gpuParticles = new GPUParticle[particleCount];
            int index = 0;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    gpuParticles[index] = new GPUParticle(_cloth.particles[i][j]);
                    index++;
                }
            }

            // Create or update particle buffer
            if (_particleBuffer == null || _particleBuffer.Count != particleCount)
            {
                _particleBuffer?.Dispose();
                _particleBuffer = new ComputeBuffer<GPUParticle>(_computeContext,
                    ComputeMemoryFlags.ReadWrite, gpuParticles);
            }
            else
            {
                _commandQueue.WriteToBuffer(gpuParticles, _particleBuffer, false, null);
            }

            // Prepare physics parameters
            MouseState mouseState = Mouse.GetState();
            Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);
            PhysicsParams physicsParams = new PhysicsParams(deltaTime,
                new Vector2(0, 980f), windForce, _cloth.drag,
                _graphics.PreferredBackBufferWidth, _graphics.PreferredBackBufferHeight, mousePos);

            // Set dragged particles
            physicsParams.DraggedCount = Math.Min(particlesInDragArea.Count, 1024);
            for (int i = 0; i < physicsParams.DraggedCount; i++)
            {
                physicsParams.DraggedParticleIndices[i] = particlesInDragArea[i];
            }

            // Create physics parameters buffer
            if (_physicsParamsBuffer == null)
            {
                _physicsParamsBuffer = new ComputeBuffer<PhysicsParams>(_computeContext,
                    ComputeMemoryFlags.ReadOnly, new PhysicsParams[] { physicsParams });
            }
            else
            {
                _commandQueue.WriteToBuffer(new PhysicsParams[] { physicsParams },
                    _physicsParamsBuffer, false, null);
            }

            // Set kernel arguments
            _updateParticlesKernel.SetMemoryArgument(0, _particleBuffer);
            _updateParticlesKernel.SetMemoryArgument(1, _physicsParamsBuffer);
            _updateParticlesKernel.SetValueArgument(2, width);
            _updateParticlesKernel.SetValueArgument(3, height);

            // Execute kernel
            _commandQueue.Execute(_updateParticlesKernel, null, new long[] { particleCount }, null, null);

            // Read results back
            _commandQueue.ReadFromBuffer(_particleBuffer, ref gpuParticles, false, null);
            _commandQueue.Finish();

            // Convert back to DrawableParticles
            index = 0;
            for (int i = 0; i < width; i++)
            {
                for (int j = 0; j < height; j++)
                {
                    _cloth.particles[i][j] = gpuParticles[index].ToDrawableParticle(_cloth.particles[i][j].Color);
                    index++;
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GPU particle update failed: {ex.Message}");
            // Fall back to CPU
            UpdateParticlesCPU(deltaTime);
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

    private void SelectTool(int index)
    {
        switch (index)
        {
            case 0:
                // Drag tool selected
                break;
            case 1:
                // Pin tool selected
                break;
            case 2:
                // Cut tool selected
                break;
            case 3:
                // Wind tool selected
                break;
            case 4:
            // DragOne tool selected
            default:
                break;
        }
    }

    protected override void Update(GameTime gameTime)
    {
        const float fixedDeltaTime = 1f / 1000f;

        if (
            GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed
            || Keyboard.GetState().IsKeyDown(Keys.Escape)
        )
            Exit();
        MouseState mouseState = Mouse.GetState();
        KeyboardState keyboardState = Keyboard.GetState();
        Vector2 currentMousePos = new Vector2(mouseState.X, mouseState.Y);

        // Toggle OpenCL with 'O' key
        if (keyboardState.IsKeyDown(Keys.O) && !_previousKeyboardState.IsKeyDown(Keys.O))
        {
            _useOpenCL = !_useOpenCL;
            Console.WriteLine($"Switched to {(_useOpenCL ? "GPU" : "CPU")} computation");
        }
        _previousKeyboardState = keyboardState;

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

        if (_selectedToolIndex == 0)
        {
            DragAreaParticles(mouseState, leftPressed, particlesInDragArea);
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
            
            string computeMode = $"Compute: {(_useOpenCL && _updateParticlesKernel != null ? "GPU (OpenCL)" : "CPU")} - Press 'O' to toggle";
            _spriteBatch.DrawString(_font, computeMode, new Vector2(10, 30), Color.Yellow);
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

    protected override void UnloadContent()
    {
        // Dispose OpenCL resources
        _particleBuffer?.Dispose();
        _stickBuffer?.Dispose();
        _physicsParamsBuffer?.Dispose();
        _stickParamsBuffer?.Dispose();
        _updateParticlesKernel?.Dispose();
        _calculateStickForcesKernel?.Dispose();
        _computeProgram?.Dispose();
        _commandQueue?.Dispose();
        _computeContext?.Dispose();
        
        base.UnloadContent();
    }
}
