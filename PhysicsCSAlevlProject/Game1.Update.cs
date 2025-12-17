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
                        if (_currentMode == MeshMode.Cloth)
                            PinParticle(intitialMousePosWhenPressed, dragRadius);
                        else if (_currentMode == MeshMode.Buildable)
                            PinParticleBuildable(intitialMousePosWhenPressed, dragRadius);
                        break;
                    case "Cut":
                        var radius =
                            _tools["Cut"].Properties["Radius"] != null
                                ? (float)_tools["Cut"].Properties["Radius"]
                                : 10f;

                        if (_currentMode == MeshMode.Cloth)
                            CutAllSticksInRadius(intitialMousePosWhenPressed, radius);
                        else if (_currentMode == MeshMode.Buildable)
                            CutAllSticksInRadiusBuildable(intitialMousePosWhenPressed, radius);

                        break;
                    case "Wind":
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
                else if (
                    _selectedToolName == "LineCut"
                    && leftPressed
                    && _currentMode != MeshMode.PolygonBuilder
                )
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
            && !ImGuiNET.ImGui.IsAnyItemHovered()
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
                if (!_useConstraintSolver)
                {
                    _clothInstance.horizontalSticks = ApplyStickForces(
                        _clothInstance.horizontalSticks
                    );
                    _clothInstance.verticalSticks = ApplyStickForces(_clothInstance.verticalSticks);
                }
            }
            else
            {
                foreach (var particle in _activeMesh.Particles.Values)
                {
                    particle.AccumulatedForce = Vector2.Zero;
                }
                if (!_useConstraintSolver)
                {
                    ApplyStickForcesDictionary(_activeMesh.Sticks);
                }
            }

            UpdateParticles(FixedTimeStep);

            if (_useConstraintSolver)
            {
                if (_currentMode == MeshMode.Cloth)
                {
                    SatisfyClothConstraints(_constraintIterations);
                }
                else
                {
                    SatisfyBuildableConstraints(_constraintIterations);
                }
            }

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
}
