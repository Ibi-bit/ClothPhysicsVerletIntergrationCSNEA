using Microsoft.Xna.Framework;
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

        if (_selectedToolName == "Cursor Collider")
        {
            if (_cursorCollider is CircleCollider circle)
            {
                var cursorCollider = new PrimitiveBatch.Circle(
                    circle.Position,
                    circle.Radius,
                    Color.Red,
                    true
                );
                cursorCollider.Draw(_spriteBatch, _primitiveBatch);
            }
            else if (_cursorCollider is RectangleCollider rectangle)
            {
                var cursorCollider = new PrimitiveBatch.Rectangle(
                    rectangle.Rectangle,
                    Color.Red,
                    true
                );
                cursorCollider.Draw(_spriteBatch, _primitiveBatch);
            }
            
        }

        // _physicsActionsQueue.TryDequeue(out var meshToDraw);
        _activeMesh.Draw(_spriteBatch, _primitiveBatch,_drawParticles, _drawConstraints);

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
