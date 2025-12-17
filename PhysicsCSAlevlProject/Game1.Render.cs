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

        base.Draw(gameTime);
    }

    private void HandleModeSelection()
    {
        if (ImGui.Combo("Mesh Mode", ref _modeIndex, _modes, _modes.Length))
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

    private void PinParticle(Vector2 center, float radius)
    {
        float closestDistance = float.MaxValue;
        int closestI = -1;
        int closestJ = -1;

        for (int i = 0; i < _clothInstance.particles.Length; i++)
        {
            for (int j = 0; j < _clothInstance.particles[i].Length; j++)
            {
                float distance = Vector2.Distance(_clothInstance.particles[i][j].Position, center);
                if (distance <= radius && distance < closestDistance)
                {
                    closestDistance = distance;
                    closestI = i;
                    closestJ = j;
                }
            }
        }

        if (closestI >= 0 && closestJ >= 0)
        {
            _clothInstance.particles[closestI][closestJ].IsPinned = !_clothInstance
                .particles[closestI][closestJ]
                .IsPinned;
        }
    }

    private void CutSticksInRadius(Vector2 center, float radius, DrawableStick[][] sticks)
    {
        for (int i = 0; i < sticks.Length; i++)
        {
            for (int j = 0; j < sticks[i].Length; j++)
            {
                if (sticks[i][j] != null)
                {
                    Vector2 stickCenter =
                        (sticks[i][j].P1.Position + sticks[i][j].P2.Position) * 0.5f;
                    float distance = Vector2.Distance(stickCenter, center);
                    if (distance <= radius)
                    {
                        sticks[i][j] = null;
                    }
                }
            }
        }
    }

    private void CutAllSticksInRadius(Vector2 center, float radius)
    {
        CutSticksInRadius(center, radius, _clothInstance.horizontalSticks);
        CutSticksInRadius(center, radius, _clothInstance.verticalSticks);
    }

    private void CutAllSticksInRadiusBuildable(Vector2 center, float radius)
    {
        var sticksToRemove = new List<int>();

        foreach (var kvp in _buildableMeshInstance.Sticks)
        {
            var stick = kvp.Value;
            Vector2 stickCenter = (stick.P1.Position + stick.P2.Position) * 0.5f;
            float distance = Vector2.Distance(stickCenter, center);
            if (distance <= radius)
            {
                sticksToRemove.Add(kvp.Key);
            }
        }

        foreach (var stickId in sticksToRemove)
        {
            _buildableMeshInstance.RemoveStick(stickId);
        }
    }

    private void ApplyWindForceFromDrag(Vector2 startPos, Vector2 endPos, float radius)
    {
        Vector2 windDirection = endPos - startPos;
        float windDistance = windDirection.Length();

        if (windDistance < 5f)
            return;
        else
        {
            windForce = windDirection * (windDistance / 50f);
        }
    }

    private bool DoTwoLinesIntersect(
        Vector2 line1Start,
        Vector2 line1End,
        Vector2 line2Start,
        Vector2 line2End
    )
    {
        Vector2 r = line1End - line1Start;
        Vector2 s = line2End - line2Start;
        Vector2 qMinusP = line2Start - line1Start;

        float rCrossS = r.X * s.Y - r.Y * s.X;
        float qMinusPCrossR = qMinusP.X * r.Y - qMinusP.Y * r.X;

        if (Math.Abs(rCrossS) < 0.0001f)
        {
            return false;
        }

        float t = (qMinusP.X * s.Y - qMinusP.Y * s.X) / rCrossS;
        float u = qMinusPCrossR / rCrossS;

        return (t >= 0 && t <= 1 && u >= 0 && u <= 1);
    }

    private DrawableStick[][] DoLinesIntersect(
        DrawableStick[][] sticks,
        Vector2 lineStart,
        Vector2 lineEnd
    )
    {
        for (int i = 0; i < sticks.Length; i++)
        {
            for (int j = 0; j < sticks[i].Length; j++)
            {
                if (sticks[i][j] != null)
                {
                    Vector2 stickStart = sticks[i][j].P1.Position;
                    Vector2 stickEnd = sticks[i][j].P2.Position;

                    if (DoTwoLinesIntersect(lineStart, lineEnd, stickStart, stickEnd))
                    {
                        System.Diagnostics.Debug.WriteLine($"Cutting stick at [{i},{j}]");
                        sticks[i][j].IsCut = true;
                    }
                }
            }
        }
        return sticks;
    }

    private void CutSticksAlongLine(Vector2 lineStart, Vector2 lineEnd)
    {
        if (_currentMode == MeshMode.Cloth)
        {
            _clothInstance.horizontalSticks = DoLinesIntersect(
                _clothInstance.horizontalSticks,
                lineStart,
                lineEnd
            );
            _clothInstance.verticalSticks = DoLinesIntersect(
                _clothInstance.verticalSticks,
                lineStart,
                lineEnd
            );
        }
        else
        {
            _buildableMeshInstance.CutSticksAlongLine(lineStart, lineEnd);
        }
    }
}
