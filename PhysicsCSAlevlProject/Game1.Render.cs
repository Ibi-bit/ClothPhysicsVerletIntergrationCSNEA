using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using VectorGraphics;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    private bool _drawParticles;
    private bool _drawConstraints;

    private void InitializeRender()
    {
        _drawParticles = true;
        _drawConstraints = true;
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(Color.CornflowerBlue);
        _spriteBatch.Begin();

        var rect = new Rectangle(
            new Point(_windowBounds.X, _windowBounds.Y),
            new Point(
                (int)(_windowBounds.Width + _collisonBoundsDifference.X),
                (int)(_windowBounds.Height + _collisonBoundsDifference.Y)
            )
        );
        var collisionBounds = new PrimitiveBatch.Rectangle(rect, Color.Black, false, 2);
        collisionBounds.Draw(_spriteBatch, _primitiveBatch);

        // Draw cursor radius for tools that use it
        MouseState mouseState = Mouse.GetState();
        Vector2 currentMousePos = new Vector2(mouseState.X, mouseState.Y);
        bool imguiWantsMouse = ImGuiNET.ImGui.GetIO().WantCaptureMouse;

        if (!imguiWantsMouse)
        {
            float radius = 0f;
            Color cursorColor = Color.White * 0.5f;
            bool shouldDrawCursor = false;

            if (_currentToolSet.ContainsKey(_selectedToolName))
            {
                var props = _currentToolSet[_selectedToolName].Properties;

                switch (_selectedToolName)
                {
                    case "Drag":
                        radius = props.TryGetValue("Radius", out var dragRadius)
                            ? (float)dragRadius
                            : 20f;
                        cursorColor = Color.Yellow * 0.5f;
                        shouldDrawCursor = true;
                        break;

                    case "Pin":
                        radius = props.TryGetValue("Radius", out var pinRadius)
                            ? (float)pinRadius
                            : 20f;
                        cursorColor = Color.BlueViolet * 0.5f;
                        shouldDrawCursor = true;
                        break;

                    case "Cut":
                        radius = props.TryGetValue("Radius", out var cutRadius)
                            ? (float)cutRadius
                            : 10f;
                        cursorColor = Color.Red * 0.5f;
                        shouldDrawCursor = true;
                        break;

                    case "PhysicsDrag":
                        radius = props.TryGetValue("Radius", out var physRadius)
                            ? (float)physRadius
                            : 20f;
                        cursorColor = Color.Orange * 0.5f;
                        shouldDrawCursor = true;
                        break;

                    case "Inspect Particles":
                        radius = props.TryGetValue("Radius", out var inspectRadius)
                            ? (float)inspectRadius
                            : 10f;
                        cursorColor = Color.Cyan * 0.5f;
                        shouldDrawCursor = true;
                        break;

                    case "Cursor Collider":
                        radius = props.TryGetValue("Radius", out var colliderRadius)
                            ? (float)colliderRadius
                            : 50f;
                        string shape = props.TryGetValue("Shape", out var shapeObj)
                            ? (string)shapeObj
                            : "Circle";

                        if (shape == "Circle")
                        {
                            var cursorCollider = new PrimitiveBatch.Circle(
                                currentMousePos,
                                radius,
                                Color.Red * 0.7f,
                                true
                            );
                            cursorCollider.Draw(_spriteBatch, _primitiveBatch);
                        }
                        else if (shape == "Rectangle")
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
                                Color.Red * 0.7f,
                                true
                            );
                            cursorCollider.Draw(_spriteBatch, _primitiveBatch);
                        }
                        break;

                    case "Add Stick Between Particles":
                        radius = props.TryGetValue("Radius", out var stickRadius)
                            ? (float)stickRadius
                            : 15f;
                        cursorColor = Color.Green * 0.5f;
                        shouldDrawCursor = true;
                        break;

                    case "Remove Particle":
                        radius = props.TryGetValue("Radius", out var removeRadius)
                            ? (float)removeRadius
                            : 10f;
                        cursorColor = Color.Red * 0.5f;
                        shouldDrawCursor = true;
                        break;
                }

                if (shouldDrawCursor && radius > 0)
                {
                    var cursorCircleFilled = new PrimitiveBatch.Circle(
                        currentMousePos,
                        radius,
                        cursorColor,
                        true
                    );
                    cursorCircleFilled.Draw(_spriteBatch, _primitiveBatch);

                    Color outlineColor = cursorColor;
                    outlineColor.A = 255;
                    var cursorCircleOutline = new PrimitiveBatch.Circle(
                        currentMousePos,
                        radius,
                        outlineColor,
                        false
                    );

                    cursorCircleOutline.Draw(_spriteBatch, _primitiveBatch);
                }
                else
                {
                    var crosshairSize = 10;
                    var horizontalLine = new PrimitiveBatch.Line(
                        currentMousePos - new Vector2(crosshairSize, 0),
                        currentMousePos + new Vector2(crosshairSize, 0),
                        cursorColor,
                        2
                    );
                    var verticalLine = new PrimitiveBatch.Line(
                        currentMousePos - new Vector2(0, crosshairSize),
                        currentMousePos + new Vector2(0, crosshairSize),
                        cursorColor,
                        2
                    );
                    horizontalLine.Draw(_spriteBatch, _primitiveBatch);
                    verticalLine.Draw(_spriteBatch, _primitiveBatch);
                }
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
        _spriteBatch.End();
        ImGuiDraw(gameTime);
        // HandleModeSelection();

        base.Draw(gameTime);
    }
}
