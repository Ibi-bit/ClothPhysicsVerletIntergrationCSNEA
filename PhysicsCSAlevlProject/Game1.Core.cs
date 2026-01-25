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
    bool leftPressed;
    Vector2 intitialMousePosWhenPressed;

    bool Paused = false;

    float _springConstant;

    Vector2 windForce;
    Vector2 previousMousePos;

    Vector2 BaseForce = new Vector2(0, 980f);
    List<Vector2> particlesInDragArea = new List<Vector2>();
    List<int> buildableMeshParticlesInDragArea = new List<int>();

    private PrimitiveBatch.Arrow windDirectionArrow;
    private PrimitiveBatch.Line cutLine;

    // private SpriteFont _font;
    private const float FixedTimeStep = 1f / 1000f;
    private float _timeAccumulator = 0f;

    private bool _useConstraintSolver = false;
    private int _constraintIterations = 5;
    private Mesh _activeMesh;
    private Cloth _clothInstance;
    private Mesh _defaultBuildableMesh;
    private PolygonBuilder _polygonBuilderInstance;
    private static Rectangle _windowBounds;
    bool keepAspectRatio = true;
    float _lockedAspectRatio = 1f;

    private Rectangle changedBounds = Rectangle.Empty;

    private enum MeshMode
    {
        Cloth,
        Interact,
        Edit,
    }

    private MeshMode _currentMode = MeshMode.Cloth;

    public void SetWindowSize(int width, int height)
    {
        _graphics.PreferredBackBufferWidth = width;
        _graphics.PreferredBackBufferHeight = height;
        _graphics.ApplyChanges();
        var cb = Window.ClientBounds;
        _windowBounds = new Rectangle(0, 0, cb.Width, cb.Height);
        changedBounds = _windowBounds;
    }

    private KeyboardState _prevKeyboardState;
    private MouseState _prevMouseState;

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
        _primitiveBatch.CreateTextures();

        _graphics.PreferredBackBufferWidth = 800;
        _graphics.PreferredBackBufferHeight = 640;
        _graphics.ApplyChanges();

        _database = new Game1Database();

        var cbInit = Window.ClientBounds;
        _windowBounds = new Rectangle(0, 0, cbInit.Width, cbInit.Height);
        changedBounds = _windowBounds;
        _lockedAspectRatio = cbInit.Height > 0 ? cbInit.Width / (float)cbInit.Height : 1f;
        Window.ClientSizeChanged += (_, __) =>
        {
            var cbNow = Window.ClientBounds;
            _windowBounds = new Rectangle(0, 0, cbNow.Width, cbNow.Height);
            changedBounds = _windowBounds;
        };

        leftPressed = false;

        windDirectionArrow = null;

        _springConstant = 5000f;

        InitializeInteractTools();
        InitializeBuildTools();

        float naturalLength = 10f;
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

        _defaultBuildableMesh = new Mesh();
        _defaultBuildableMesh.springConstant = _springConstant;
        _defaultBuildableMesh.mass = mass;
        _polygonBuilderInstance = new PolygonBuilder();

        _activeMesh = _clothInstance;
        _currentMode = MeshMode.Cloth;

        _guiRenderer = new ImGuiRenderer(this);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        // _font = Content.Load<SpriteFont>("Font");
        _guiRenderer.RebuildFontAtlas();
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
