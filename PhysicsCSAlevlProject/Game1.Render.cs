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
