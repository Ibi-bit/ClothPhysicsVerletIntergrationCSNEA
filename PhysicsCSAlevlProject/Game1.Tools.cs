using System;
using System.Collections.Generic;
using ImGuiNET;
using Microsoft.VisualBasic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Input;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    private string _selectedToolName = "Drag";
    private Dictionary<string, Tool> _interactTools;
    private Dictionary<string, Tool> _buildTools;
    private Dictionary<string, Tool> _currentToolSet
    {
        get { return _currentMode == MeshMode.Edit ? _buildTools : _interactTools; }
    }
    private float dragRadius = 20f;
    private List<int> inspectedParticles = new List<int>();
    private int? _stickToolFirstParticleId = null;

    private void InitializeInteractTools()
    {
        _interactTools = new Dictionary<string, Tool>
        {
            { "Drag", new Tool("Drag", null, null) },
            { "Pin", new Tool("Pin", null, null) },
            { "Cut", new Tool("Cut", null, null) },
            { "Wind", new Tool("Wind", null, null) },
            { "PhysicsDrag", new Tool("PhysicsDrag", null, null) },
            { "LineCut", new Tool("LineCut", null, null) },
            { "Inspect Particles", new Tool("Inspect Particles", null, null) },
        };
        foreach (var tool in _interactTools.Values)
        {
            tool.Properties = new Dictionary<string, object>();
        }

        _interactTools["Drag"].Properties["Radius"] = 20f;
        _interactTools["Drag"].Properties["MaxParticles"] = (int)20;
        _interactTools["Drag"].Properties["InfiniteParticles"] = true;

        _interactTools["Pin"].Properties["Radius"] = 20f;

        _interactTools["Cut"].Properties["Radius"] = 10f;

        _interactTools["Wind"].Properties["MinDistance"] = 5f;
        _interactTools["Wind"].Properties["StrengthScale"] = 1.0f;
        _interactTools["Wind"].Properties["ArrowThickness"] = 3f;

        _interactTools["PhysicsDrag"].Properties["Radius"] = 20f;

        _interactTools["LineCut"].Properties["MinDistance"] = 5f;
        _interactTools["LineCut"].Properties["Thickness"] = 3f;

        _interactTools["Inspect Particles"].Properties["Radius"] = 10f;
        _interactTools["Inspect Particles"].Properties["IsLog"] = false;
    }

    private void InitializeBuildTools()
    {
        _buildTools = new Dictionary<string, Tool>
        {
            { "Add Particle", new Tool("Add Particle", null, null) },
            { "Add Stick Between Particles", new Tool("Add Stick Between Particles", null, null) },
            { "Add Polygon", new Tool("Add Polygon", null, null) },
            { "Remove Stick", new Tool("Remove Stick", null, null) },
        };
        foreach (var tool in _buildTools.Values)
        {
            tool.Properties = new Dictionary<string, object>();
        }
        _buildTools["Add Particle"].Properties["SnapToGrid"] = true;
        _buildTools["Add Stick Between Particles"].Properties["Radius"] = 15f;
        _buildTools["Add Polygon"].Properties["SnapToGrid"] = true;
        _buildTools["Remove Stick"].Properties["Radius"] = 10f;
    }

    private void DrawToolMenuItems()
    {
        EnsureSelectedToolValid();
        foreach (var toolName in _currentToolSet.Keys)
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
        EnsureSelectedToolValid();
        foreach (var toolName in _currentToolSet.Keys)
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
        EnsureSelectedToolValid();
        ImGui.Text($"Settings for {_selectedToolName} Tool:");

        if (_selectedToolName == "Drag")
        {
            var props = _currentToolSet["Drag"].Properties;
            float radius = (float)props["Radius"];
            if (ImGui.SliderFloat("Radius", ref radius, 5f, 100f))
            {
                props["Radius"] = radius;
            }

            bool infiniteParticles = (bool)props["InfiniteParticles"];
            if (ImGui.Checkbox("Infinite Particles", ref infiniteParticles))
            {
                props["InfiniteParticles"] = infiniteParticles;
            }

            int maxParticles = (int)props["MaxParticles"];
            string maxParticlesLabel = infiniteParticles ? "Max Particles: ∞" : "Max Particles";

            ImGui.BeginDisabled(infiniteParticles);
            if (ImGui.SliderInt(maxParticlesLabel, ref maxParticles, 1, 100))
            {
                props["MaxParticles"] = maxParticles;
            }
            ImGui.EndDisabled();
        }
        else if (_selectedToolName == "Pin")
        {
            var props = _currentToolSet["Pin"].Properties;
            float radius = (float)props["Radius"];
            if (ImGui.SliderFloat("Radius", ref radius, 5f, 100f))
            {
                props["Radius"] = radius;
            }
        }
        else if (_selectedToolName == "Cut")
        {
            var props = _currentToolSet["Cut"].Properties;
            float radius = (float)props["Radius"];
            if (ImGui.SliderFloat("Radius", ref radius, 1f, 100f))
            {
                props["Radius"] = radius;
            }
        }
        else if (_selectedToolName == "Wind")
        {
            var props = _currentToolSet["Wind"].Properties;
            float minDist = (float)props["MinDistance"];
            if (ImGui.SliderFloat("Min Distance", ref minDist, 0f, 50f))
            {
                props["MinDistance"] = minDist;
            }

            float strength = (float)props["StrengthScale"];
            if (ImGui.SliderFloat("Strength Scale", ref strength, 0.0f, 5.0f))
            {
                props["StrengthScale"] = strength;
            }

            float thickness = (float)props["ArrowThickness"];
            if (ImGui.SliderFloat("Arrow Thickness", ref thickness, 1f, 10f))
            {
                props["ArrowThickness"] = thickness;
            }
        }
        else if (_selectedToolName == "PhysicsDrag")
        {
            var props = _currentToolSet["PhysicsDrag"].Properties;
            float radius = (float)props["Radius"];
            if (ImGui.SliderFloat("Radius", ref radius, 5f, 100f))
            {
                props["Radius"] = radius;
            }
        }
        else if (_selectedToolName == "LineCut")
        {
            var props = _currentToolSet["LineCut"].Properties;
            float minDist = (float)props["MinDistance"];
            if (ImGui.SliderFloat("Min Distance", ref minDist, 0f, 50f))
            {
                props["MinDistance"] = minDist;
            }

            float thickness = (float)props["Thickness"];
            if (ImGui.SliderFloat("Line Thickness", ref thickness, 1f, 10f))
            {
                props["Thickness"] = thickness;
            }
        }
        else if (_selectedToolName == "Inspect Particles")
        {
            var props = _currentToolSet["Inspect Particles"].Properties;
            float radius = (float)props["Radius"];
            if (ImGui.SliderFloat("Radius", ref radius, 5f, 100f))
            {
                props["Radius"] = radius;
            }

            bool isLog = (bool)props["IsLog"];
            if (ImGui.Checkbox("Log to Console or track realtime in window", ref isLog))
            {
                props["IsLog"] = isLog;
            }
        }
        else if (_selectedToolName == "Add Particle")
        {
            var props = _currentToolSet["Add Particle"].Properties;
            bool snapToGrid = (bool)props["SnapToGrid"];
            if (ImGui.Checkbox("Snap To Grid", ref snapToGrid))
            {
                props["SnapToGrid"] = snapToGrid;
            }
        }
        else if (_selectedToolName == "Add Stick Between Particles")
        {
            var props = _currentToolSet["Add Stick Between Particles"].Properties;
            float radius = (float)props["Radius"];
            if (ImGui.SliderFloat("Radius", ref radius, 5f, 100f))
            {
                props["Radius"] = radius;
            }
        }
        else if (_selectedToolName == "Add Polygon")
        {
            var props = _currentToolSet["Add Polygon"].Properties;
            bool snapToGrid = (bool)props["SnapToGrid"];
            if (ImGui.Checkbox("Snap To Grid", ref snapToGrid))
            {
                props["SnapToGrid"] = snapToGrid;
            }
        }
        else if (_selectedToolName == "Remove Stick")
        {
            var props = _currentToolSet["Remove Stick"].Properties;
            float radius = (float)props["Radius"];
            if (ImGui.SliderFloat("Radius", ref radius, 1f, 100f))
            {
                props["Radius"] = radius;
            }
        }
    }

    private void EnsureSelectedToolValid()
    {
        var set = _currentToolSet;
        if (!set.ContainsKey(_selectedToolName))
        {
            var enumerator = set.Keys.GetEnumerator();
            if (enumerator.MoveNext())
            {
                _selectedToolName = enumerator.Current;
            }
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

    private void PinParticleBuildable(Vector2 center, float radius)
    {
        var particleIDs = GetBuildableMeshParticlesInRadius(center, radius);
        foreach (var id in particleIDs)
            if (_activeMesh.Particles.TryGetValue(id, out var particle))
                particle.IsPinned = !particle.IsPinned;
    }

    private void CutSticksInRadius(Vector2 center, float radius, DrawableStick[][] sticks)
    {
        for (int i = 0; i < sticks.Length; i++)
        {
            for (int j = 0; j < sticks[i].Length; j++)
            {
                if (sticks[i][j] != null && !sticks[i][j].IsCut)
                {
                    Vector2 stickCenter =
                        (sticks[i][j].P1.Position + sticks[i][j].P2.Position) * 0.5f;
                    float distance = Vector2.Distance(stickCenter, center);
                    if (distance <= radius)
                    {
                        if (_currentMode != MeshMode.Cloth)
                            sticks[i][j] = null;
                        else
                            sticks[i][j].IsCut = true;
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

    private void ApplyWindForceFromDrag(Vector2 startPos, Vector2 endPos, float _)
    {
        Vector2 windDirection = endPos - startPos;
        float windDistance = windDirection.Length();

        float minDist = _currentToolSet["Wind"].Properties.ContainsKey("MinDistance")
            ? (float)_currentToolSet["Wind"].Properties["MinDistance"]
            : 5f;

        if (windDistance < minDist)
            return;

        float strength = _currentToolSet["Wind"].Properties.ContainsKey("StrengthScale")
            ? (float)_currentToolSet["Wind"].Properties["StrengthScale"]
            : 1.0f;

        windForce = windDirection * (windDistance / 50f) * strength;
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

        if (_currentMode == MeshMode.Interact || _currentMode == MeshMode.Edit)
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

        if (isDragging && _currentMode != MeshMode.Edit)
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

    private void InspectParticlesInRadiusLog(Vector2 center, float radius)
    {
        if (_currentMode == MeshMode.Cloth)
        {
            for (int i = 0; i < _clothInstance.particles.Length; i++)
            {
                for (int j = 0; j < _clothInstance.particles[i].Length; j++)
                {
                    ref var particle = ref _clothInstance.particles[i][j];
                    float distance = Vector2.Distance(particle.Position, center);
                    particle.Color = Color.White;
                    if (distance <= radius)
                    {
                        _Logger.AddLog(
                            $"Particle [{i},{j}] - Pos: {particle.Position}, Pinned: {particle.IsPinned}",
                            ImGuiLogger.logTypes.Info
                        );
                        particle.Color = Color.Cyan;
                    }
                }
            }
        }
        else
        {
            foreach (var kvp in _activeMesh.Particles)
            {
                var particle = kvp.Value;
                float distance = Vector2.Distance(particle.Position, center);
                particle.Color = Color.White;
                if (distance <= radius)
                {
                    _Logger.AddLog(
                        $"Particle ID {kvp.Key} - Pos: {particle.Position}, Pinned: {particle.IsPinned}",
                        ImGuiLogger.logTypes.Info
                    );
                    particle.Color = Color.Cyan;
                }
                _activeMesh.Particles[kvp.Key] = particle;
            }
        }
    }

    private void InspectParticlesInRadiusWindow(Vector2 center, float radius)
    {
        if (_currentMode == MeshMode.Cloth)
        {
            for (int i = 0; i < _clothInstance.particles.Length; i++)
            {
                for (int j = 0; j < _clothInstance.particles[i].Length; j++)
                {
                    ref var particle = ref _clothInstance.particles[i][j];
                    float distance = Vector2.Distance(particle.Position, center);
                    particle.Color = Color.White;
                    if (distance <= radius)
                    {
                        particle.Color = Color.Cyan;
                    }
                }
            }
        }
        else
        {
            inspectedParticles.Clear();
            foreach (var kvp in _activeMesh.Particles)
            {
                var particle = kvp.Value;
                float distance = Vector2.Distance(particle.Position, center);
                particle.Color = Color.White;

                if (distance <= radius)
                {
                    particle.Color = Color.Cyan;
                    inspectedParticles.Add(kvp.Key);
                }
                _activeMesh.Particles[kvp.Key] = particle;
            }
        }
    }
    private void HandleAddStickBetweenParticlesClick(Vector2 clickPos)
    {
        float radius = _currentToolSet["Add Stick Between Particles"].Properties["Radius"]
            is float r
            ? r
            : 10f;
        var ids = GetBuildableMeshParticlesInRadius(clickPos, radius, 1);
        if (ids.Count >= 1)
        {
            int hitId = ids[0];
            if (_stickToolFirstParticleId == null)
            {
                _stickToolFirstParticleId = hitId;
                if (_buildableMeshInstance.Particles.TryGetValue(hitId, out var p))
                {
                    p.Color = Color.Yellow;
                    _buildableMeshInstance.Particles[hitId] = p;
                }
            }
            else if (_stickToolFirstParticleId.Value != hitId)
            {
                _buildableMeshInstance.AddStickBetween(_stickToolFirstParticleId.Value, hitId);
                if (
                    _buildableMeshInstance.Particles.TryGetValue(
                        _stickToolFirstParticleId.Value,
                        out var p1
                    )
                )
                {
                    p1.Color = Color.White;
                    _buildableMeshInstance.Particles[_stickToolFirstParticleId.Value] = p1;
                }
                if (_buildableMeshInstance.Particles.TryGetValue(hitId, out var p2))
                {
                    p2.Color = Color.White;
                    _buildableMeshInstance.Particles[hitId] = p2;
                }
                _stickToolFirstParticleId = null;
            }
        }
    }
}
