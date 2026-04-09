using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    /// <summary>
    /// the rectangle used for selecting particles
    /// </summary>
    private VectorGraphics.PrimitiveBatch.Rectangle _selectRectangle;

    /// <summary>
    /// the current wind force being applied by the wind tool
    /// </summary>
    private Vector2 _windForce;

    /// <summary>
    /// the arrow primitive used to show the direction and strength of the wind tool while dragging
    /// </summary>
    private VectorGraphics.PrimitiveBatch.Arrow _windDirectionArrow;

    /// <summary>
    /// the line primitive used to show where the cut tool will cut while dragging and other tools where a line is used to show the area of effect
    /// </summary>
    private VectorGraphics.PrimitiveBatch.Line _cutLine;

    /// <summary>
    /// a list of particle IDs that are currently within the area of effect of a tool that is being dragged
    /// </summary>
    private List<int> _meshParticlesInDragArea;

    /// <summary>
    /// the collider that is currently being dragged by the Move Collider tool,
    /// </summary>
    private Collider _draggedCollider;

    /// <summary>
    /// initializes variables used in the Update method,
    /// </summary>
    private void InitializeUpdate()
    {
        _selectRectangle = null;
        _windForce = Vector2.Zero;
        _windDirectionArrow = null;
        _cutLine = null;
        _meshParticlesInDragArea = new();
    }

    /// <summary>
    /// main game loop for everything not related to drawing
    /// </summary>
    /// <param name="gameTime"></param>
    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();
        float frameTime = (float)Math.Min(gameTime.ElapsedGameTime.TotalSeconds, 0.1);
        ProcessDebugCommands();
        bool ctrlHeld =
            keyboardState.IsKeyDown(Keys.LeftControl) || keyboardState.IsKeyDown(Keys.RightControl);
        bool shiftHeld =
            keyboardState.IsKeyDown(Keys.LeftShift) || keyboardState.IsKeyDown(Keys.RightShift);

        HandleUndoRedoShortcuts(keyboardState, ctrlHeld, shiftHeld);
        UpdateCursorColliderFromToolSettings();
        HandlePauseAndStepHotkeys(keyboardState);

        if (!_paused)
        {
            _timeAccumulator += frameTime;
        }

        MouseState mouseState = Mouse.GetState();
        Vector2 currentMousePos = new Vector2(mouseState.X, mouseState.Y);

        SetCursorColliderCenter(currentMousePos);

        bool imguiWantsMouse = ImGuiNET.ImGui.GetIO().WantCaptureMouse;

        HandleMouseAndToolInput(mouseState, currentMousePos, imguiWantsMouse);
        UpdateActiveToolVisualsAndActions(
            keyboardState,
            mouseState,
            currentMousePos,
            imguiWantsMouse
        );
        RunPhysicsUpdate(currentMousePos);
        ApplyPostPhysicsToolEffects(mouseState, currentMousePos);

        _previousMousePos = currentMousePos;

        base.Update(gameTime);
        _prevKeyboardState = keyboardState;
        _prevMouseState = mouseState;
    }

    private Vector2 GetCursorColliderCenter()
    {
        if (_cursorCollider is CircleCollider circle)
        {
            return circle.Position;
        }

        if (_cursorCollider is RectangleCollider rectangle)
        {
            return new Vector2(rectangle.Rectangle.Center.X, rectangle.Rectangle.Center.Y);
        }

        return Vector2.Zero;
    }

    /// <summary>
    /// sets the center of the cursor collider to a given position, which is used for tools that require a cursor area of effect,
    /// </summary>
    /// <param name="center"></param>
    private void SetCursorColliderCenter(Vector2 center)
    {
        if (_cursorCollider is CircleCollider circle)
        {
            circle.Position = center;
            return;
        }

        if (_cursorCollider is RectangleCollider rectangle)
        {
            float halfSize = rectangle.Rectangle.Width / 2f;
            rectangle.Rectangle = new Rectangle(
                (int)(center.X - halfSize),
                (int)(center.Y - halfSize),
                rectangle.Rectangle.Width,
                rectangle.Rectangle.Height
            );
        }
    }

    /// <summary>
    /// updates the size of the cursor collider based on a given radius,
    /// which is used for tools that require a circular or square area of effect around the cursor.
    /// </summary>
    /// <param name="radius"></param>
    private void UpdateCursorColliderSize(float radius)
    {
        int size = Math.Max(1, (int)MathF.Round(radius * 2f));

        if (_cursorCollider is CircleCollider circle)
        {
            circle.Radius = radius;
            return;
        }

        if (_cursorCollider is RectangleCollider rectangle)
        {
            Vector2 center = new Vector2(
                rectangle.Rectangle.Center.X,
                rectangle.Rectangle.Center.Y
            );
            rectangle.Rectangle = new Rectangle(
                (int)(center.X - size / 2f),
                (int)(center.Y - size / 2f),
                size,
                size
            );
        }
    }
}
