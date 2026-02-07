using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Microsoft.Xna.Framework;

using Microsoft.Xna.Framework.Input;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    private string _selectedToolName;
    private Dictionary<string, Tool> _interactTools;
    private Dictionary<string, Tool> _buildTools;
    private Dictionary<string, Tool> _currentToolSet => _currentMode == MeshMode.Edit ? _buildTools : _interactTools;
    private float _dragRadius;
    private List<int> _inspectedParticles;
    private int? _stickToolFirstParticleId;

    private void InitializeTools()
    {
        _selectedToolName = "Drag";
        _dragRadius = 20f;
        _inspectedParticles = new List<int>();
        _stickToolFirstParticleId = null;

        InitializeInteractTools();
        InitializeBuildTools();
    }

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
            {"Cursor Collider", new Tool("Cursor Collider", null, null)},

        };
        foreach (var tool in _interactTools.Values)
        {
            tool.Properties = new Dictionary<string, object>();
        }

        _interactTools["Drag"].Properties["Radius"] = 20f;
        _interactTools["Drag"].Properties["MaxParticles"] = 20;
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
        _interactTools["Inspect Particles"].Properties["Clear When Use"] = false;
        _interactTools["Inspect Particles"].Properties["RectangleSelect"] = true;

        _interactTools["Cursor Collider"].Properties["Radius"] = 50f;
        _interactTools["Cursor Collider"].Properties["Shape"] = "Circle";
    }

    private void InitializeBuildTools()
    {
        _buildTools = new Dictionary<string, Tool>
        {
            { "Add Particle", new Tool("Add Particle", null, null) },
            { "Add Stick Between Particles", new Tool("Add Stick Between Particles", null, null) },
            { "Remove Particle", new Tool("Remove Particle", null, null) },
            { "LineCut", new Tool("LineCut", null, null) },
            { "Pin", new Tool("Pin", null, null) },
            { "Create Grid Mesh", new Tool("Create Grid Mesh", null, null) },
            { "Line Tool", new Tool("Line Tool", null, null) },
            { "Add Polygon", new Tool("Add Polygon", null, null) },
        };
        foreach (var tool in _buildTools.Values)
        {
            tool.Properties = new Dictionary<string, object>();
        }
        _buildTools["Add Particle"].Properties["SnapToGrid"] = true;
        _buildTools["Add Stick Between Particles"].Properties["Radius"] = 15f;
        _buildTools["Remove Particle"].Properties["Radius"] = 10f;
        _interactTools["LineCut"].Properties["MinDistance"] = 5f;
        _interactTools["LineCut"].Properties["Thickness"] = 3f;
        _buildTools["Pin"].Properties["Radius"] = 20f;
        _buildTools["Create Grid Mesh"].Properties["DistanceBetweenParticles"] = 10f;
        _buildTools["Line Tool"].Properties["Constraints in Line"] = 100;
        _buildTools["Line Tool"].Properties["Natural Length Ratio"] = 1.0f;
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
                    ImGuiCol.Text,
                    new System.Numerics.Vector4(0.2f, 1f, 0.2f, 1f)
                );
            }

            if (ImGui.MenuItem(toolName))
            {
                if (!string.Equals(_selectedToolName, toolName, StringComparison.Ordinal))
                {
                    _logger.AddLog($"Selected tool: {toolName}");
                }
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
        // foreach (var toolName in _currentToolSet.Keys)
        // {
        //     bool isSelected = _selectedToolName == toolName;
        //     if (isSelected)
        //     {
        //         ImGui.PushStyleColor(
        //             ImGuiCol.Button,
        //             new System.Numerics.Vector4(0.2f, 0.6f, 0.2f, 1f)
        //         );
        //     }

        //     if (ImGui.Button(toolName))
        //     {
        //         _selectedToolName = toolName;
        //     }

        //     if (isSelected)
        //     {
        //         ImGui.PopStyleColor();
        //     }

        //     ImGui.SameLine();
        // }

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
            bool clearWhenUse = (bool)props["Clear When Use"];
            if (ImGui.Checkbox("Clear When Use", ref clearWhenUse))
            {
                props["Clear When Use"] = clearWhenUse;
            }
            bool rectangleSelect = (bool)props["RectangleSelect"];
            if (ImGui.Checkbox("Select with Rectangle", ref rectangleSelect))
            {
                props["RectangleSelect"] = rectangleSelect;
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
        else if (_selectedToolName == "Remove Stick")
        {
            var props = _currentToolSet["Remove Stick"].Properties;
            float radius = (float)props["Radius"];
            if (ImGui.SliderFloat("Radius", ref radius, 1f, 100f))
            {
                props["Radius"] = radius;
            }
        }
        else if (_selectedToolName == "Create Grid Mesh")
        {
            var props = _currentToolSet["Create Grid Mesh"].Properties;
            float distance = (float)props["DistanceBetweenParticles"];
            if (ImGui.SliderFloat("Distance Between Particles", ref distance, 5f, 100f))
            {
                props["DistanceBetweenParticles"] = distance;
            }
        }
        else if (string.Equals(_selectedToolName, "Cursor Collider", StringComparison.Ordinal))
        {
            var props = _currentToolSet["Cursor Collider"].Properties;
            float radius = (float)props["Radius"];
            if (ImGui.SliderFloat("Radius", ref radius, 1f, 100f)) props["Radius"] = radius;

            string[] shapes = _cursorColliderStore.Keys.ToArray();
            int shapeIndex = Array.IndexOf(shapes, (string)props["Shape"]);
            if (shapeIndex < 0) shapeIndex = 0;
            if (ImGui.Combo("Shape", ref shapeIndex, shapes, shapes.Length))
            {
                props["Shape"] = shapes[shapeIndex];
            }
        }
        else if(string.Equals(_selectedToolName, "Line Tool", StringComparison.Ordinal))
        {
            var props = _currentToolSet["Line Tool"].Properties;
            int constraintsInLine = (int)props["Constraints in Line"];
            if (ImGui.SliderInt("Constraints in Line", ref constraintsInLine, 1, 500))
            {
                props["Constraints in Line"] = constraintsInLine;
            }
            float naturalLengthRatio = (float)props["Natural Length Ratio"];
            if (ImGui.InputFloat("Natural Length Ratio", ref naturalLengthRatio, 0.1f, 3.0f))
            {
                props["Natural Length Ratio"] = naturalLengthRatio;
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

   

    private void PinParticleBuildable(Vector2 center, float radius)
    {
        var particleIDs = GetMeshParticlesInRadius(center, radius);
        if (particleIDs.Count == 0)
        {
            _logger.AddLog(
                $"Pin tool: no particles found within radius {radius} at {center}",
                ImGuiLogger.LogTypes.Warning
            );
            return;
        }
        foreach (var id in particleIDs)
            if (_activeMesh.Particles.TryGetValue(id, out var particle))
            {
                particle.IsPinned = !particle.IsPinned;
                string action = particle.IsPinned ? "pinned" : "unpinned";
                _logger.AddLog($"Mesh particle {id} {action} at {particle.Position}");
            }
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
                        sticks[i][j].IsCut = true;
                        _logger.AddLog($"Cut stick [{i},{j}] at {stickCenter}");
                    }
                }
            }
        }
    }

   
    private void CutAllSticksInRadiusBuildable(Vector2 center, float radius)
    {
        var sticksToRemove = new List<int>();

        foreach (var kvp in _activeMesh.Sticks)
        {
            var stick = kvp.Value;
            Vector2 stickCenter = (stick.P1.Position + stick.P2.Position) * 0.5f;
            float distance = Vector2.Distance(stickCenter, center);
            if (distance <= radius)
            {
                sticksToRemove.Add(kvp.Key);
                _logger.AddLog($"Cut stick {kvp.Key} at {stickCenter}");
            }
        }

        foreach (var stickId in sticksToRemove)
        {
            _activeMesh.RemoveStick(stickId);
        }

        if (sticksToRemove.Count == 0)
        {
            _logger.AddLog(
                $"Cut tool: no sticks found within radius {radius} at {center}",
                ImGuiLogger.LogTypes.Warning
            );
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
        {
            _logger.AddLog(
                $"Wind tool: drag distance {windDistance:0.00} below minimum {minDist:0.00}",
                ImGuiLogger.LogTypes.Warning
            );
            return;
        }

        float strength = _currentToolSet["Wind"].Properties.ContainsKey("StrengthScale")
            ? (float)_currentToolSet["Wind"].Properties["StrengthScale"]
            : 1.0f;

        _windForce = windDirection * (windDistance / 50f) * strength;
        _logger.AddLog($"Wind applied: force {_windForce}");
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
                        _logger.AddLog($"Cutting stick at [{i},{j}]");
                        sticks[i][j].IsCut = true;
                    }
                }
            }
        }
        return sticks;
    }

    private void CutSticksAlongLine(Vector2 lineStart, Vector2 lineEnd)
    {
        _activeMesh.CutSticksAlongLine(lineStart, lineEnd);
    }

    private List<Vector2> GetParticlesInRadius(
        Vector2 mousePosition,
        float radius,
        int maxParticles = -1
    )
    {
        var particlesInRadius = new List<Vector2>();

        return particlesInRadius;
    }

    private List<int> GetMeshParticlesInRadius(
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

    

    private void DragMeshParticles(MouseState mouseState, bool isDragging, List<int> particleIds)
    {
        Vector2 mousePos = new Vector2(mouseState.X, mouseState.Y);

        if (isDragging && _currentMode != MeshMode.Edit)
        {
            Vector2 frameDelta = mousePos - _previousMousePos;
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

    private void DragMeshParticlesWithPhysics(
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


    

    private void InspectParticlesInRadiusLog(Vector2 center, float radius)
    {
        foreach (var kvp in _activeMesh.Particles)
        {
            var particle = kvp.Value;
            float distance = Vector2.Distance(particle.Position, center);
            particle.Color = Color.White;
            if (distance <= radius)
            {
                _logger.AddLog(
                    $"Particle ID {kvp.Key} - Pos: {particle.Position}, Pinned: {particle.IsPinned}"
                );
                particle.Color = Color.Cyan;
            }
            _activeMesh.Particles[kvp.Key] = particle;
        }
    }

    private void InspectParticlesInRadiusWindow(Vector2 center, float radius)
    {
        if ((bool)_interactTools["Inspect Particles"].Properties["Clear When Use"])
            _inspectedParticles.Clear();
        foreach (var kvp in _activeMesh.Particles)
        {
            var particle = kvp.Value;
            float distance = Vector2.Distance(particle.Position, center);
            particle.Color = Color.White;

            if (distance <= radius)
            {
                particle.Color = Color.Cyan;
                _inspectedParticles.Add(kvp.Key);
            }
            _activeMesh.Particles[kvp.Key] = particle;
        }
    }

    private void InspectParticlesInRectangle(
        Vector2 rectStart,
        Vector2 rectEnd,
        bool isLog,
        bool clearWhenUse
    )
    {
        if (clearWhenUse)
            _inspectedParticles.Clear();

        var particles = GetParticlesInRectangle(rectStart, rectEnd);
        foreach (var id in particles)
        {
            if (!_activeMesh.Particles.TryGetValue(id, out var particle))
                continue;

            particle.Color = Color.Cyan;
            _activeMesh.Particles[id] = particle;

            if (!_inspectedParticles.Contains(id))
                _inspectedParticles.Add(id);

            if (isLog)
            {
                _logger.AddLog($"Particle ID {id} at {particle.Position}");
            }
        }
    }

    private List<int> GetParticlesInRectangle(Vector2 rectStart, Vector2 rectEnd)
    {
        var result = new List<int>();
        Rectangle rect = GetRectangleFromPoints(rectStart, rectEnd);
       
        foreach (var kvp in _activeMesh.Particles)
        {
            var particle = kvp.Value;
            if (rect.Contains(particle.Position.ToPoint()))
            {
                result.Add(kvp.Key);
            }
        }
        return result;
    }

    private void HandleAddStickBetweenParticlesClick(Vector2 clickPos)
    {
        float radius = _currentToolSet["Add Stick Between Particles"].Properties["Radius"]
            is float r
            ? r
            : 10f;
        var ids = GetMeshParticlesInRadius(clickPos, radius, 1);
        if (ids.Count == 0)
        {
            _logger.AddLog(
                $"Add Stick tool: no particle found within radius {radius} at {clickPos}",
                ImGuiLogger.LogTypes.Warning
            );
            return;
        }
        if (ids.Count >= 1)
        {
            int hitId = ids[0];
            if (_stickToolFirstParticleId == null)
            {
                _stickToolFirstParticleId = hitId;
                if (_activeMesh.Particles.TryGetValue(hitId, out var p))
                {
                    p.Color = Color.Yellow;
                    _activeMesh.Particles[hitId] = p;
                }
            }
            else if (_stickToolFirstParticleId.Value != hitId)
            {
                _activeMesh.AddStickBetween(_stickToolFirstParticleId.Value, hitId);
                _logger.AddLog(
                    $"Added stick between particles {_stickToolFirstParticleId.Value} and {hitId}",
                    ImGuiLogger.LogTypes.Info
                );
                if (_activeMesh.Particles.TryGetValue(_stickToolFirstParticleId.Value, out var p1))
                {
                    p1.Color = Color.White;
                    _activeMesh.Particles[_stickToolFirstParticleId.Value] = p1;
                }
                if (_activeMesh.Particles.TryGetValue(hitId, out var p2))
                {
                    p2.Color = Color.White;
                    _activeMesh.Particles[hitId] = p2;
                }
                _stickToolFirstParticleId = null;
            }
            else
            {
                _logger.AddLog(
                    "Cannot create stick to the same particle. Select a different particle."
                );
            }
        }
    }

    private Rectangle GetRectangleFromPoints(Vector2 point1, Vector2 point2)
    {
        int x = (int)Math.Min(point1.X, point2.X);
        int y = (int)Math.Min(point1.Y, point2.Y);
        int width = (int)Math.Abs(point1.X - point2.X);
        int height = (int)Math.Abs(point1.Y - point2.Y);
        return new Rectangle(x, y, width, height);
    }
}
