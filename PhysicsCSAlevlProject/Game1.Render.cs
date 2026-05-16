using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using VectorGraphics;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    /// <summary>
    /// determines whether to draw particles
    /// </summary>
    private bool _drawParticles;

    /// <summary>
    /// determines whether to draw constraints (sticks)
    /// </summary>
    private bool _drawConstraints;

    /// <summary>
    /// initiales rendering related variables and settings
    /// </summary>
    private void InitializeRender()
    {
        _drawParticles = true;
        _drawConstraints = true;
    }

    private void DrawCollisionBounds()
    {
        var rect = new Rectangle(
            new Point(_windowBounds.X, _windowBounds.Y),
            new Point(
                (int)(_windowBounds.Width + _collisonBoundsDifference.X),
                (int)(_windowBounds.Height + _collisonBoundsDifference.Y)
            )
        );

        var collisionBounds = new PrimitiveBatch.Rectangle(rect, Color.Black, false, 2);
        collisionBounds.Draw(_spriteBatch, _primitiveBatch);
    }

    private void ConfigureBasicEffect()
    {
        if (_basicEffect == null)
        {
            return;
        }

        GraphicsDevice.RasterizerState = new RasterizerState
        {
            CullMode = CullMode.None,
            FillMode = FillMode.Solid,
        };

        _basicEffect.World = Matrix.Identity;
        _basicEffect.View = Matrix.Identity;
        _basicEffect.Projection = Matrix.CreateOrthographicOffCenter(
            0,
            GraphicsDevice.Viewport.Width,
            GraphicsDevice.Viewport.Height,
            0,
            0,
            1
        );
        _basicEffect.VertexColorEnabled = true;
    }

    private void DrawSceneContent()
    {
        if (_activeMesh?.Colliders != null)
        {
            foreach (var collider in _activeMesh.Colliders)
            {
                collider?.Draw(_spriteBatch, _primitiveBatch);
            }
        }

        _activeMesh.Draw(_spriteBatch, _primitiveBatch, _drawParticles, _drawConstraints);

        if (_windDirectionArrow != null)
        {
            _windDirectionArrow.Draw(_spriteBatch, _primitiveBatch);
        }

        if (_cutLine != null)
        {
            _cutLine.Draw(_spriteBatch, _primitiveBatch);
        }

        if (_selectRectangle != null)
        {
            _selectRectangle.Draw(_spriteBatch, _primitiveBatch);
        }
    }

    private void DrawCursorOverlay(Vector2 currentMousePos, bool imguiWantsMouse)
    {
        if (imguiWantsMouse)
        {
            return;
        }

        const float cursorAlpha = 0.4f;
        float radius = 0f;
        Color cursorColor = Color.White * cursorAlpha;
        bool shouldDrawCursor = false;

        if (!string.IsNullOrEmpty(_selectedToolName) && _currentToolSet != null)
        {
            if (_currentToolSet.ContainsKey(_selectedToolName))
            {
                var props = _currentToolSet[_selectedToolName].Properties;

                ConfigureToolCursor(
                    currentMousePos,
                    props,
                    cursorAlpha,
                    ref radius,
                    ref cursorColor,
                    ref shouldDrawCursor
                );
            }
        }

        if (shouldDrawCursor && radius > 0f)
        {
            DrawCircleCursor(currentMousePos, radius, cursorColor);
            return;
        }

        DrawCrosshairCursor(currentMousePos, cursorColor);
    }

    /// <summary>
    /// the central draw loop for the application where every other draw function is called from
    /// </summary>
    /// <param name="gameTime"></param>
    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        ConfigureBasicEffect();

        if (_activeMesh?._components != null)
        {
            foreach (var comp in _activeMesh._components)
            {
                comp.Draw(GraphicsDevice, _basicEffect);
            }
        }

        _spriteBatch.Begin();

        MouseState mouseState = Mouse.GetState();
        Vector2 currentMousePos = new Vector2(mouseState.X, mouseState.Y);
        bool imguiWantsMouse = ImGuiNET.ImGui.GetIO().WantCaptureMouse;

        DrawCollisionBounds();
        DrawSceneContent();

        _activeMesh.RefreshComponentMeshes(_activeMesh.Particles);

        DrawCursorOverlay(currentMousePos, imguiWantsMouse);

        _spriteBatch.End();

        ImGuiDraw(gameTime);

        // HandleModeSelection();
        // GraphicsDevice.DrawUserPrimitives(PrimitiveType.LineList, _primitiveBatch.VertexBuffer, 0, _primitiveBatch.CurrentVertexCount / 2);

        base.Draw(gameTime);
    }

    private void ConfigureToolCursor(
        Vector2 currentMousePos,
        Dictionary<string, object> props,
        float cursorAlpha,
        ref float radius,
        ref Color cursorColor,
        ref bool shouldDrawCursor
    )
    {
        switch (_selectedToolName)
        {
            case "Drag":
                radius = props.TryGetValue("Radius", out var dragRadius) ? (float)dragRadius : 20f;
                cursorColor = Color.Yellow * cursorAlpha;
                shouldDrawCursor = true;
                break;

            case "Pin":
                radius = props.TryGetValue("Radius", out var pinRadius) ? (float)pinRadius : 20f;
                cursorColor = Color.BlueViolet * cursorAlpha;
                shouldDrawCursor = true;
                break;

            case "Cut":
                radius = props.TryGetValue("Radius", out var cutRadius) ? (float)cutRadius : 10f;
                cursorColor = Color.Red * cursorAlpha;
                shouldDrawCursor = true;
                break;

            case "PhysicsDrag":
                radius = props.TryGetValue("Radius", out var physRadius) ? (float)physRadius : 20f;
                cursorColor = Color.Orange * cursorAlpha;
                shouldDrawCursor = true;
                break;

            case "Inspect Particles":
                radius = props.TryGetValue("Radius", out var inspectRadius)
                    ? (float)inspectRadius
                    : 10f;
                cursorColor = Color.Cyan * cursorAlpha;
                shouldDrawCursor = true;
                break;

            case "Cursor Collider":
                DrawCursorColliderPreview(currentMousePos, props, cursorAlpha);
                shouldDrawCursor = false;
                break;

            case "Add Stick Between Particles":
                radius = props.TryGetValue("Radius", out var stickRadius)
                    ? (float)stickRadius
                    : 15f;
                cursorColor = Color.Green * cursorAlpha;
                shouldDrawCursor = true;
                break;

            case "Remove Particle":
                radius = props.TryGetValue("Radius", out var removeRadius)
                    ? (float)removeRadius
                    : 10f;
                cursorColor = Color.Red * cursorAlpha;
                shouldDrawCursor = true;
                break;

            case "Place Collider":
                HandlePlaceColliderPreview(
                    currentMousePos,
                    props,
                    cursorAlpha,
                    ref radius,
                    ref cursorColor,
                    ref shouldDrawCursor
                );
                break;

            default:
                shouldDrawCursor = false;
                break;
        }
    }

    private void DrawCursorColliderPreview(
        Vector2 currentMousePos,
        Dictionary<string, object> props,
        float cursorAlpha
    )
    {
        float radius = props.TryGetValue("Radius", out var colliderRadius)
            ? (float)colliderRadius
            : 50f;
        string shape = props.TryGetValue("Shape", out var shapeObj) ? (string)shapeObj : "Circle";

        if (shape == "Circle")
        {
            var cursorCollider = new PrimitiveBatch.Circle(
                currentMousePos,
                radius,
                Color.Red * cursorAlpha,
                true
            );
            cursorCollider.Draw(_spriteBatch, _primitiveBatch);
            return;
        }

        if (shape == "Rectangle")
        {
            int size = (int)(radius * 2f);
            var cursorRect = new Rectangle(
                (int)(currentMousePos.X - size / 2f),
                (int)(currentMousePos.Y - size / 2f),
                size,
                size
            );
            var cursorCollider = new PrimitiveBatch.Rectangle(
                cursorRect,
                Color.Red * cursorAlpha,
                true
            );
            cursorCollider.Draw(_spriteBatch, _primitiveBatch);
        }
    }

    private void HandlePlaceColliderPreview(
        Vector2 currentMousePos,
        Dictionary<string, object> props,
        float cursorAlpha,
        ref float radius,
        ref Color cursorColor,
        ref bool shouldDrawCursor
    )
    {
        string selectedType = props.TryGetValue(
            "SelectedColliderType",
            out var selectedColliderType
        )
            ? selectedColliderType?.ToString() ?? "Circle"
            : "Circle";

        props.TryGetValue("Object", out var objectValue);
        var objectDict = objectValue as Dictionary<string, object>;

        if (selectedType == "Rectangle")
        {
            object rectangleObj = null;
            if (objectDict != null)
            {
                objectDict.TryGetValue("Rectangle", out rectangleObj);
            }

            var rectangleDict = rectangleObj as Dictionary<string, object>;
            float width =
                rectangleDict != null && rectangleDict.TryGetValue("Width", out var widthObj)
                    ? Convert.ToSingle(widthObj)
                    : 40f;
            float height =
                rectangleDict != null && rectangleDict.TryGetValue("Height", out var heightObj)
                    ? Convert.ToSingle(heightObj)
                    : 20f;
            float rotation =
                rectangleDict != null && rectangleDict.TryGetValue("Rotation", out var rotationObj)
                    ? Convert.ToSingle(rotationObj)
                    : 0f;

            var cursorRect = new PrimitiveBatch.Rectangle(
                currentMousePos - new Vector2(width, height) / 2f,
                new Vector2(width, height),
                Color.Red * cursorAlpha,
                true
            );
            cursorRect.rotation = rotation;
            cursorRect.Draw(_spriteBatch, _primitiveBatch);
            shouldDrawCursor = false;
            return;
        }

        object circleObj = null;
        if (objectDict != null)
        {
            objectDict.TryGetValue("Circle", out circleObj);
        }
        var circleDict = circleObj as Dictionary<string, object>;
        float placeRadius = 20f;
        if (circleDict != null && circleDict.TryGetValue("Radius", out var radiusObj))
        {
            placeRadius = Convert.ToSingle(radiusObj);
        }

        radius = placeRadius;
        cursorColor = Color.Red * cursorAlpha;
        shouldDrawCursor = true;
    }

    private void DrawCircleCursor(Vector2 position, float radius, Color color)
    {
        var cursorCircleFilled = new PrimitiveBatch.Circle(position, radius, color, true);
        cursorCircleFilled.Draw(_spriteBatch, _primitiveBatch);

        var cursorCircleOutline = new PrimitiveBatch.Circle(position, radius, color, false);
        cursorCircleOutline.Draw(_spriteBatch, _primitiveBatch);
    }

    private void DrawCrosshairCursor(Vector2 position, Color color)
    {
        const int crosshairSize = 10;

        var horizontalLine = new PrimitiveBatch.Line(
            position - new Vector2(crosshairSize, 0),
            position + new Vector2(crosshairSize, 0),
            color,
            2
        );
        var verticalLine = new PrimitiveBatch.Line(
            position - new Vector2(0, crosshairSize),
            position + new Vector2(0, crosshairSize),
            color,
            2
        );

        horizontalLine.Draw(_spriteBatch, _primitiveBatch);
        verticalLine.Draw(_spriteBatch, _primitiveBatch);
    }
}
