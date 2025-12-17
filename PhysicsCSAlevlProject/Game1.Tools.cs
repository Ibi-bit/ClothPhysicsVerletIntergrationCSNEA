using System;
using System.Collections.Generic;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    private string _selectedToolName = "Drag";
    private Dictionary<string, Tool> _tools;
    private float dragRadius = 20f;

    private void InitializeTools()
    {
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

        _tools["Cut"].Properties["Radius"] = 10f;
    }

    private void DrawToolMenuItems()
    {
        foreach (var toolName in _tools.Keys)
        {
            bool isSelected = _selectedToolName == toolName;
            if (isSelected)
            {
                ImGui.PushStyleColor(
                    ImGuiCol.Button,
                    new System.Numerics.Vector4(0.2f, 0.6f, 0.2f, 1f)
                );
            }

            if (ImGui.MenuItem(toolName))
            {
                _selectedToolName = toolName;
            }

            if (isSelected)
            {
                ImGui.PopStyleColor();
            }
        }
    }

    private void DrawToolButtons()
    {
        foreach (var toolName in _tools.Keys)
        {
            bool isSelected = _selectedToolName == toolName;
            if (isSelected)
            {
                ImGui.PushStyleColor(
                    ImGuiCol.Button,
                    new System.Numerics.Vector4(0.2f, 0.6f, 0.2f, 1f)
                );
            }

            if (ImGui.Button(toolName))
            {
                _selectedToolName = toolName;
            }

            if (isSelected)
            {
                ImGui.PopStyleColor();
            }

            ImGui.SameLine();
        }

        ImGui.NewLine();
    }

    private void DrawSelectedToolSettings()
    {
        ImGui.Text($"Settings for {_selectedToolName} Tool:");

        if (_selectedToolName == "Drag")
        {
            float radius = (float)_tools["Drag"].Properties["Radius"];
            if (ImGui.SliderFloat("Radius", ref radius, 5f, 100f))
            {
                _tools["Drag"].Properties["Radius"] = radius;
            }

            bool infiniteParticles = (bool)_tools["Drag"].Properties["InfiniteParticles"];
            if (ImGui.Checkbox("Infinite Particles", ref infiniteParticles))
            {
                _tools["Drag"].Properties["InfiniteParticles"] = infiniteParticles;
            }

            int maxParticles = (int)_tools["Drag"].Properties["MaxParticles"];
            string maxParticlesLabel = infiniteParticles ? "Max Particles: ∞" : "Max Particles";

            ImGui.BeginDisabled(infiniteParticles);
            if (ImGui.SliderInt(maxParticlesLabel, ref maxParticles, 1, 100))
            {
                _tools["Drag"].Properties["MaxParticles"] = maxParticles;
            }
            ImGui.EndDisabled();
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
            var particle = _clothInstance.particles[closestI][closestJ];
            bool shouldPin = !particle.IsPinned;

            particle.IsPinned = shouldPin;
            particle.Mass = shouldPin ? 0f : _clothInstance.mass;
            particle.AccumulatedForce = Vector2.Zero;
            particle.PreviousPosition = particle.Position;

            _clothInstance.particles[closestI][closestJ] = particle;
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

        windForce = windDirection * (windDistance / 50f);
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
                        _Logger.AddLog($"Cutting stick at [{i},{j}]", ImGuiLogger.logTypes.Info);
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

    private List<Vector2> GetParticlesInRadius(
        Vector2 mousePosition,
        float radius,
        int maxParticles = -1
    )
    {
        var particlesInRadius = new List<Vector2>();

        if (_currentMode == MeshMode.Cloth)
        {
            for (int i = 0; i < _clothInstance.particles.Length; i++)
            {
                for (int j = 0; j < _clothInstance.particles[i].Length; j++)
                {
                    Vector2 pos = _clothInstance.particles[i][j].Position;
                    if (Vector2.DistanceSquared(pos, mousePosition) < (radius * radius))
                    {
                        particlesInRadius.Add(new Vector2(i, j));
                    }
                    if (maxParticles > 0 && particlesInRadius.Count >= maxParticles)
                    {
                        return particlesInRadius;
                    }
                }
            }
        }

        return particlesInRadius;
    }

    private List<int> GetBuildableMeshParticlesInRadius(
        Vector2 mousePosition,
        float radius,
        int maxParticles = -1
    )
    {
        var particleIds = new List<int>();

        if (_currentMode == MeshMode.Buildable || _currentMode == MeshMode.PolygonBuilder)
        {
            foreach (var kvp in _activeMesh.Particles)
            {
                Vector2 pos = kvp.Value.Position;
                if (Vector2.DistanceSquared(pos, mousePosition) < (radius * radius))
                {
                    particleIds.Add(kvp.Key);
                }
                if (maxParticles > 0 && particleIds.Count >= maxParticles)
                {
                    return particleIds;
                }
            }
        }

        return particleIds;
    }

    private void DragAreaParticles(
        MouseState mouseState,
        bool isDragging,
        List<Vector2> particlesInDragArea
    )
    {
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);

        if (isDragging)
        {
            Vector2 frameDelta = mousePos - previousMousePos;
            foreach (Vector2 particle in particlesInDragArea)
            {
                var p = _clothInstance.particles[(int)particle.X][(int)particle.Y];
                if (!p.IsPinned)
                {
                    p.Position += frameDelta;
                    p.PreviousPosition += frameDelta;
                    _clothInstance.particles[(int)particle.X][(int)particle.Y] = p;
                    _clothInstance.particles[(int)particle.X][(int)particle.Y].Color = Color.Yellow;
                }
            }
        }
        else
        {
            foreach (Vector2 particle in particlesInDragArea)
            {
                var p = _clothInstance.particles[(int)particle.X][(int)particle.Y];
                if (!p.IsPinned)
                {
                    _clothInstance.particles[(int)particle.X][(int)particle.Y].Color = Color.White;
                }
            }
        }
    }

    private void DragBuildableMeshParticles(
        MouseState mouseState,
        bool isDragging,
        List<int> particleIds
    )
    {
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);

        if (isDragging && _currentMode != MeshMode.PolygonBuilder)
        {
            Vector2 frameDelta = mousePos - previousMousePos;
            foreach (int particleId in particleIds)
            {
                if (_activeMesh.Particles.TryGetValue(particleId, out var particle))
                {
                    if (!particle.IsPinned)
                    {
                        particle.Position += frameDelta;
                        particle.PreviousPosition += frameDelta;
                        particle.Color = Color.Yellow;
                    }
                }
            }
        }
        else
        {
            foreach (int particleId in particleIds)
            {
                if (_activeMesh.Particles.TryGetValue(particleId, out var particle))
                {
                    if (!particle.IsPinned)
                    {
                        particle.Color = Color.White;
                    }
                }
            }
        }
    }

    private void DragBuildableMeshParticlesWithPhysics(
        MouseState mouseState,
        bool isDragging,
        List<int> particleIds
    )
    {
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);

        if (isDragging)
        {
            foreach (int particleId in particleIds)
            {
                if (_activeMesh.Particles.TryGetValue(particleId, out var particle))
                {
                    if (!particle.IsPinned)
                    {
                        particle.Position = mousePos;
                        particle.PreviousPosition = mousePos;
                        particle.Color = Color.Red;
                    }
                }
            }
        }
        else
        {
            foreach (int particleId in particleIds)
            {
                if (_activeMesh.Particles.TryGetValue(particleId, out var particle))
                {
                    if (!particle.IsPinned)
                    {
                        particle.Color = Color.White;
                    }
                }
            }
        }
    }

    private void DragAreaParticlesWithPhysics(
        MouseState mouseState,
        bool isDragging,
        List<Vector2> particlesInDragArea
    )
    {
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);

        if (isDragging)
        {
            foreach (Vector2 particle in particlesInDragArea)
            {
                var p = _clothInstance.particles[(int)particle.X][(int)particle.Y];
                if (!p.IsPinned)
                {
                    Vector2 displacement = mousePos - p.Position;
                    float distance = displacement.Length();

                    if (distance > 1f)
                    {
                        float moveSpeed = 0.1f;
                        Vector2 positionDelta = displacement * moveSpeed;

                        p.Position += positionDelta;
                        p.PreviousPosition += positionDelta * 0.9f;

                        _clothInstance.particles[(int)particle.X][(int)particle.Y] = p;
                        _clothInstance.particles[(int)particle.X][(int)particle.Y].Color =
                            Color.Orange;
                    }
                }
            }
        }
        else
        {
            foreach (Vector2 particle in particlesInDragArea)
            {
                var p = _clothInstance.particles[(int)particle.X][(int)particle.Y];
                if (!p.IsPinned)
                {
                    _clothInstance.particles[(int)particle.X][(int)particle.Y].Color = Color.White;
                }
            }
        }
    }
}
