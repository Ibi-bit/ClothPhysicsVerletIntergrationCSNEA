using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using MonoGame.ImGuiNet;
using VectorGraphics;

namespace PhysicsCSAlevlProject;

public partial class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private PrimitiveBatch _primitiveBatch;
    public static ImGuiRenderer _guiRenderer;
    public Game1Database _database;

    private bool _leftPressed;
    private Vector2 _initialMousePosWhenPressed;
    private KeyboardState _prevKeyboardState;
    private MouseState _prevMouseState;
    private Vector2 _previousMousePos;

    private bool _paused;
    private static Rectangle _windowBounds;
    private Rectangle _changedBounds;
    private bool _keepAspectRatio;
    private float _lockedAspectRatio;

    private Mesh _activeMesh;
    private Mesh _defaultMesh;

    private enum MeshMode
    {
        // Cloth,
        Interact,
        Edit,
    }

    private MeshMode _currentMode = MeshMode.Interact;

    public void SetWindowSize(int width, int height)
    {
        _graphics.PreferredBackBufferWidth = width;
        _graphics.PreferredBackBufferHeight = height;
        _graphics.ApplyChanges();
        var cb = Window.ClientBounds;
        _windowBounds = new Rectangle(0, 0, cb.Width, cb.Height);
        _changedBounds = _windowBounds;
    }

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;
        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 640;
        Window.AllowUserResizing = false;
    }

    protected override void Initialize()
    {
        _primitiveBatch = new PrimitiveBatch(GraphicsDevice);
        _primitiveBatch.CreateTextures(20f);

        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 640;
        _graphics.ApplyChanges();

        _database = new Game1Database();

        var cbInit = Window.ClientBounds;
        _windowBounds = new Rectangle(0, 0, cbInit.Width, cbInit.Height);
        _changedBounds = _windowBounds;
        _keepAspectRatio = true;
        _lockedAspectRatio = cbInit.Height > 0 ? cbInit.Width / (float)cbInit.Height : 1f;
        Window.ClientSizeChanged += (_, __) =>
        {
            var cbNow = Window.ClientBounds;
            _windowBounds = new Rectangle(0, 0, cbNow.Width, cbNow.Height);
            _changedBounds = _windowBounds;
        };

        _leftPressed = false;
        _paused = false;

        _defaultMesh = new Mesh
        {
            springConstant = 5000f,
            mass = 0.1f,
            drag = 0.997f,
        };
        _activeMesh = _defaultMesh;
        _currentMode = MeshMode.Interact;

        InitializeImGui();
        InitializePhysics();
        InitializeRender();
        InitializeTools();
        InitializeUpdate();

        _guiRenderer = new ImGuiRenderer(this);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // _font = Content.Load<SpriteFont>("Font");
        _guiRenderer.RebuildFontAtlas();
    }
     protected override void UnloadContent()
{
    _physicsRunning = false;
    _physicsSignal.Set();
    _physicsThread?.Join(1000);
    base.UnloadContent();
}

    private DrawableParticle KeepInsideRect(DrawableParticle p, Rectangle rect, Vector2 difference)
    {
        bool positionChanged = false;

        if (p.Position.X < 0)
        {
            p.Position.X = 0;
            positionChanged = true;
        }
        else if (p.Position.X > rect.Width)
        {
            p.Position.X = rect.Width;
            positionChanged = true;
        }

        if (p.Position.Y < 0)
        {
            p.Position.Y = 0;
            positionChanged = true;
        }
        else if (p.Position.Y > rect.Height + difference.Y)
        {
            p.Position.Y = rect.Height + difference.Y;
            positionChanged = true;
        }

        if (positionChanged)
        {
            p.PreviousPosition = p.Position;
        }

        return p;
    }
}
