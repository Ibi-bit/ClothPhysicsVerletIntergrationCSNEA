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
    bool leftPressed;
    Vector2 intitialMousePosWhenPressed;

    bool Paused = false;

    float _springConstant;

    Vector2 windForce;
    Vector2 previousMousePos;
    float dragRadius = 20f;

    Vector2 BaseForce = new Vector2(0, 980f);
    List<Vector2> particlesInDragArea = new List<Vector2>();
    List<int> buildableMeshParticlesInDragArea = new List<int>();

    private Dictionary<string, Tool> _tools;

    private PrimitiveBatch.Arrow windDirectionArrow;
    private PrimitiveBatch.Line cutLine;
    private string _selectedToolName = "Drag";
    private SpriteFont _font;
    private const float FixedTimeStep = 1f / 1000f;
    private float _timeAccumulator = 0f;

    private int _modeIndex = 2;
    private string[] _modes = { "Cloth", "Buildable", "PolygonBuilder" };

    private Mesh _activeMesh;
    private Cloth _clothInstance;
    private BuildableMesh _buildableMeshInstance;
    private PolygonBuilder _polygonBuilderInstance;
    private Rectangle _windowBounds;

    private enum MeshMode
    {
        Cloth,
        Buildable,
        PolygonBuilder,
    }

    private MeshMode _currentMode = MeshMode.PolygonBuilder;

    private KeyboardState _prevKeyboardState;
    private MouseState _prevMouseState;

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

        _windowBounds = new Rectangle(
            0,
            0,
            _graphics.PreferredBackBufferWidth,
            _graphics.PreferredBackBufferHeight
        );

        leftPressed = false;

        windDirectionArrow = null;

        _springConstant = 100;

        _tools = new Dictionary<string, Tool>
        {
            { "Drag", new Tool("Drag", null, null) },
            { "Pin", new Tool("Pin", null, null) },
            { "Cut", new Tool("Cut", null, null) },
            { "Wind", new Tool("Wind", null, null) },
            { "DragOne", new Tool("DragOne", null, null) },
            { "PhysicsDrag", new Tool("PhysicsDrag", null, null) },
            { "LineCut", new Tool("LineCut", null, null) },
        };

        foreach (var tool in _tools.Values)
        {
            tool.Properties = new Dictionary<string, object>();
        }
        _tools["Drag"].Properties["Radius"] = 20f;
        _tools["Drag"].Properties["MaxParticles"] = (int)20;
        _tools["Drag"].Properties["InfiniteParticles"] = true;

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

        _buildableMeshInstance = new BuildableMesh(_springConstant, mass);
        _polygonBuilderInstance = new PolygonBuilder();

        _activeMesh = _buildableMeshInstance;
        _currentMode = MeshMode.PolygonBuilder;

        _guiRenderer = new ImGuiRenderer(this);

        base.Initialize();
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);

        _font = Content.Load<SpriteFont>("Font");
        _guiRenderer.RebuildFontAtlas();
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
}
