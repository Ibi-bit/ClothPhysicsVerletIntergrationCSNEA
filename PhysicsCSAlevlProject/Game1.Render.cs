using System;
using System.Collections.Generic;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using VectorGraphics;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
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
        var collisionBounds = new PrimitiveBatch.Rectangle(rect, Color.Black, false, 4);
        collisionBounds.Draw(_spriteBatch, _primitiveBatch);

        _activeMesh.Draw(_spriteBatch, _primitiveBatch);

        if (_currentMode == MeshMode.PolygonBuilder && _font != null)
        {
            _polygonBuilderInstance.Draw(_spriteBatch, _primitiveBatch, _font);
        }

        if (windDirectionArrow != null)
        {
            windDirectionArrow.Draw(_spriteBatch, _primitiveBatch);
        }

        if (cutLine != null)
        {
            cutLine.Draw(_spriteBatch, _primitiveBatch);
        }
        _spriteBatch.End();
        ImGuiDraw(gameTime);
        // HandleModeSelection();

        base.Draw(gameTime);
    }

    private void HandleModeSelection()
    {
        switch (_modeIndex)
        {
            case 0:
                _currentMode = MeshMode.Cloth;
                _activeMesh = _clothInstance;
                break;
            case 1:
                _currentMode = MeshMode.Buildable;
                _activeMesh = _buildableMeshInstance;
                break;
            case 2:
                _currentMode = MeshMode.PolygonBuilder;
                _activeMesh = _buildableMeshInstance;
                break;
        }
        leftPressed = false;
        windDirectionArrow = null;
        cutLine = null;
        particlesInDragArea.Clear();
        buildableMeshParticlesInDragArea.Clear();
    }
}
