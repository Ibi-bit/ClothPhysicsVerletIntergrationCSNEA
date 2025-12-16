using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using VectorGraphics;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();
        float frameTime = (float)Math.Min(gameTime.ElapsedGameTime.TotalSeconds, 0.1);

        if (keyboardState.IsKeyDown(Keys.Escape) && !_prevKeyboardState.IsKeyDown(Keys.Escape))
        {
            Paused = !Paused;
        }

        if (!Paused)
        {
            _timeAccumulator += frameTime;
        }

        _activeMesh.springConstant = _springConstant;

        MouseState mouseState = Mouse.GetState();
        Vector2 currentMousePos = new Vector2(mouseState.X, mouseState.Y);

        bool imguiWantsMouse = ImGuiNET.ImGui.GetIO().WantCaptureMouse;

        if (!imguiWantsMouse)
        {
            if (mouseState.LeftButton == ButtonState.Pressed && !leftPressed)
            {
                leftPressed = true;
                intitialMousePosWhenPressed = currentMousePos;
                previousMousePos = currentMousePos;

                switch (_selectedToolName)
                {
                    case "Drag":
                        if (_currentMode == MeshMode.Cloth)
                        {
                            bool infiniteParticles =
                                _tools["Drag"].Properties.ContainsKey("InfiniteParticles")
                                && (bool)_tools["Drag"].Properties["InfiniteParticles"];
                            int maxParticles = infiniteParticles
                                ? -1
                                : (
                                    _tools["Drag"].Properties.ContainsKey("MaxParticles")
                                        ? (int)_tools["Drag"].Properties["MaxParticles"]
                                        : -1
                                );

                            particlesInDragArea = GetParticlesInRadius(
                                intitialMousePosWhenPressed,
                                _tools["Drag"].Properties.ContainsKey("Radius")
                                    ? (float)_tools["Drag"].Properties["Radius"]
                                    : dragRadius,
                                maxParticles
                            );
                        }
                        else if (_currentMode == MeshMode.Buildable)
                        {
                            bool infiniteParticles =
                                _tools["Drag"].Properties.ContainsKey("InfiniteParticles")
                                && (bool)_tools["Drag"].Properties["InfiniteParticles"];
                            int maxParticles = infiniteParticles
                                ? -1
                                : (
                                    _tools["Drag"].Properties.ContainsKey("MaxParticles")
                                        ? (int)(float)_tools["Drag"].Properties["MaxParticles"]
                                        : -1
                                );

                            buildableMeshParticlesInDragArea = GetBuildableMeshParticlesInRadius(
                                intitialMousePosWhenPressed,
                                _tools["Drag"].Properties.ContainsKey("Radius")
                                    ? (float)_tools["Drag"].Properties["Radius"]
                                    : dragRadius,
                                maxParticles
                            );
                        }
                        break;
                    case "Pin":
                        PinParticle(intitialMousePosWhenPressed, dragRadius);
                        break;
                    case "Cut":
                        if (_currentMode == MeshMode.Cloth)
                            CutAllSticksInRadius(intitialMousePosWhenPressed, dragRadius);
                        // else if (_currentMode == MeshMode.Buildable)
                        //     CutAllSticksInRadiusBuildable(intitialMousePosWhenPressed, dragRadius);

                        break;
                    case "Wind":
                        break;
                    case "DragOne":
                        if (_currentMode == MeshMode.Cloth)
                        {
                            particlesInDragArea = GetParticlesInRadius(
                                intitialMousePosWhenPressed,
                                10
                            );
                        }
                        else if (_currentMode == MeshMode.Buildable)
                        {
                            buildableMeshParticlesInDragArea = GetBuildableMeshParticlesInRadius(
                                intitialMousePosWhenPressed,
                                10
                            );
                        }
                        break;
                    case "PhysicsDrag":
                        if (_currentMode == MeshMode.Cloth)
                        {
                            particlesInDragArea = GetParticlesInRadius(
                                intitialMousePosWhenPressed,
                                dragRadius
                            );
                        }
                        else if (_currentMode == MeshMode.Buildable)
                        {
                            buildableMeshParticlesInDragArea = GetBuildableMeshParticlesInRadius(
                                intitialMousePosWhenPressed,
                                dragRadius
                            );
                        }
                        break;
                    case "LineCut":
                        break;
                }
            }
            else if (mouseState.LeftButton == ButtonState.Released)
            {
                if (_selectedToolName == "Wind" && leftPressed)
                {
                    ApplyWindForceFromDrag(
                        intitialMousePosWhenPressed,
                        currentMousePos,
                        dragRadius
                    );
                }
                else if (_selectedToolName == "LineCut" && leftPressed)
                {
                    Vector2 cutDirection = currentMousePos - intitialMousePosWhenPressed;
                    float cutDistance = cutDirection.Length();
                    if (cutDistance > 5f)
                    {
                        CutSticksAlongLine(intitialMousePosWhenPressed, currentMousePos);
                    }
                }

                leftPressed = false;
                intitialMousePosWhenPressed = Vector2.Zero;
                windDirectionArrow = null;
                cutLine = null;
            }
        }

        if (_selectedToolName == "Wind" && leftPressed)
        {
            Vector2 windDirection = currentMousePos - intitialMousePosWhenPressed;
            float windDistance = windDirection.Length();

            if (windDistance > 5f)
            {
                windDirectionArrow = new VectorGraphics.PrimitiveBatch.Arrow(
                    intitialMousePosWhenPressed,
                    currentMousePos,
                    Color.Cyan,
                    3f
                );

                windForce = !Paused ? windDirection * (windDistance / 50f) : Vector2.Zero;
            }
            else
            {
                windDirectionArrow = null;
                windForce = Vector2.Zero;
            }
        }
        else if (_selectedToolName == "LineCut" && leftPressed)
        {
            Vector2 cutDirection = currentMousePos - intitialMousePosWhenPressed;
            float cutDistance = cutDirection.Length();
            if (cutDistance > 5f)
            {
                cutLine = new VectorGraphics.PrimitiveBatch.Line(
                    intitialMousePosWhenPressed,
                    currentMousePos,
                    Color.Red,
                    3f
                );
            }
            else
            {
                cutLine = null;
            }
        }

        if (
            _currentMode == MeshMode.PolygonBuilder
            && !imguiWantsMouse
            && _windowBounds.Contains(mouseState.Position)
        )
        {
            _buildableMeshInstance = _polygonBuilderInstance.Update(
                gameTime,
                keyboardState,
                _prevKeyboardState,
                mouseState,
                _prevMouseState,
                _buildableMeshInstance
            );
        }

        int stepsThisFrame = 0;
        const int maxStepsPerFrame = 1000;

        while (_timeAccumulator >= FixedTimeStep && stepsThisFrame < maxStepsPerFrame && !Paused)
        {
            if (_currentMode == MeshMode.Cloth)
            {
                for (int i = 0; i < _clothInstance.particles.Length; i++)
                {
                    for (int j = 0; j < _clothInstance.particles[i].Length; j++)
                    {
                        _clothInstance.particles[i][j].AccumulatedForce = Vector2.Zero;
                    }
                }

                _clothInstance.horizontalSticks = ApplyStickForces(_clothInstance.horizontalSticks);
                _clothInstance.verticalSticks = ApplyStickForces(_clothInstance.verticalSticks);
            }
            else
            {
                foreach (var particle in _activeMesh.Particles.Values)
                {
                    particle.AccumulatedForce = Vector2.Zero;
                }

                ApplyStickForcesDictionary(_activeMesh.Sticks);
            }

            UpdateParticles(FixedTimeStep);

            _timeAccumulator -= FixedTimeStep;
            stepsThisFrame++;
        }

        if (stepsThisFrame == maxStepsPerFrame)
        {
            _timeAccumulator = Math.Min(_timeAccumulator, FixedTimeStep);
        }

        if (_currentMode == MeshMode.Cloth)
        {
            UpdateStickColorsRelative(
                _clothInstance.horizontalSticks,
                _clothInstance.verticalSticks
            );
        }
        else
        {
            UpdateStickColorsDictionary(_activeMesh.Sticks);
        }

        if (_selectedToolName == "Drag")
        {
            if (_currentMode == MeshMode.Cloth)
            {
                DragAreaParticles(mouseState, leftPressed, particlesInDragArea);
            }
            else if (_currentMode == MeshMode.Buildable || _currentMode == MeshMode.PolygonBuilder)
            {
                DragBuildableMeshParticles(
                    mouseState,
                    leftPressed,
                    buildableMeshParticlesInDragArea
                );
            }
        }
        else if (_selectedToolName == "DragOne")
        {
            if (_currentMode == MeshMode.Cloth)
            {
                DragAreaParticles(mouseState, leftPressed, particlesInDragArea);
            }
            else if (_currentMode == MeshMode.Buildable || _currentMode == MeshMode.PolygonBuilder)
            {
                DragBuildableMeshParticles(
                    mouseState,
                    leftPressed,
                    buildableMeshParticlesInDragArea
                );
            }
        }
        else if (_selectedToolName == "PhysicsDrag")
        {
            if (_currentMode == MeshMode.Cloth)
            {
                DragAreaParticlesWithPhysics(mouseState, leftPressed, particlesInDragArea);
            }
            else if (_currentMode == MeshMode.Buildable || _currentMode == MeshMode.PolygonBuilder)
            {
                DragBuildableMeshParticlesWithPhysics(
                    mouseState,
                    leftPressed,
                    buildableMeshParticlesInDragArea
                );
            }
        }

        previousMousePos = currentMousePos;

        base.Update(gameTime);
        _prevKeyboardState = keyboardState;
        _prevMouseState = mouseState;
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
