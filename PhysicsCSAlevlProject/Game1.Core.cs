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
    List<int> meshParticlesInDragArea = new List<int>();

    private PrimitiveBatch.Arrow windDirectionArrow;
    private PrimitiveBatch.Line cutLine;

    // private SpriteFont _font;
    private const float FixedTimeStep = 1f / 1000f;
    private float _timeAccumulator = 0f;

    private bool _useConstraintSolver = false;
    private int _constraintIterations = 5;
    private Mesh _activeMesh;
    private Cloth _clothInstance;
    private Mesh _defaultMesh;
    private PolygonBuilder _polygonBuilderInstance;
    private static Rectangle _windowBounds;
    bool keepAspectRatio = true;
    float _lockedAspectRatio = 1f;

    private Rectangle changedBounds = Rectangle.Empty;

    private Cloth BuildClothTemplate(
        Vector2 size,
        float naturalLength,
        float mass,
        float springConstant,
        Vector2 offset
    )
    {
        int cols = (int)(size.X / naturalLength);
        var pinnedParticles = new List<Vector2>
        {
            new Vector2(offset.X, offset.Y),
            new Vector2(offset.X + Math.Max(0, (cols - 1) * naturalLength), offset.Y),
        };

        var cloth = new Cloth(size, pinnedParticles, naturalLength, springConstant, mass)
        {
            drag = 0.997f,
            mass = mass,
        };

        return cloth;
    }

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

        _quickMeshes = new Dictionary<string, Func<Mesh>>
        {
            {
                "Cloth 20x20 (light)",
                () => BuildClothTemplate(new Vector2(200, 200), 10f, 0.1f, 5000f, new Vector2(220, 20))
            },
            {
                "Cloth 30x20 (light)",
                () => BuildClothTemplate(new Vector2(300, 200), 10f, 0.1f, 5000f, new Vector2(220, 20))
            },
            {
                "Cloth 20x20 (stiff)",
                () => BuildClothTemplate(new Vector2(200, 200), 10f, 0.2f, 8000f, new Vector2(220, 20))
            },
        };
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

        _clothInstance = _quickMeshes["Cloth 20x20 (light)"]() as Cloth;
        _springConstant = _clothInstance?.springConstant ?? _springConstant;

        _defaultMesh = new Mesh
        {
            springConstant = _springConstant,
            mass = _clothInstance?.mass ?? 0.1f,
            drag = _clothInstance?.drag ?? 0.997f,
        };
        _polygonBuilderInstance = new PolygonBuilder();

        _activeMesh = _defaultMesh;
        _currentMode = MeshMode.Interact;

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
