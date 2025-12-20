using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

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
                                _interactTools["Drag"].Properties.ContainsKey("InfiniteParticles")
                                && (bool)_interactTools["Drag"].Properties["InfiniteParticles"];
                            int maxParticles = infiniteParticles
                                ? -1
                                : (
                                    _interactTools["Drag"].Properties.ContainsKey("MaxParticles")
                                        ? (int)_interactTools["Drag"].Properties["MaxParticles"]
                                        : -1
                                );

                            particlesInDragArea = GetParticlesInRadius(
                                intitialMousePosWhenPressed,
                                _interactTools["Drag"].Properties.ContainsKey("Radius")
                                    ? (float)_interactTools["Drag"].Properties["Radius"]
                                    : dragRadius,
                                maxParticles
                            );
                        }
                        else if (_currentMode == MeshMode.Interact)
                        {
                            bool infiniteParticles =
                                _interactTools["Drag"].Properties.ContainsKey("InfiniteParticles")
                                && (bool)_interactTools["Drag"].Properties["InfiniteParticles"];
                            int maxParticles = infiniteParticles
                                ? -1
                                : (
                                    _interactTools["Drag"].Properties.ContainsKey("MaxParticles")
                                        ? (int)
                                            (float)_interactTools["Drag"].Properties["MaxParticles"]
                                        : -1
                                );

                            buildableMeshParticlesInDragArea = GetBuildableMeshParticlesInRadius(
                                intitialMousePosWhenPressed,
                                _interactTools["Drag"].Properties.ContainsKey("Radius")
                                    ? (float)_interactTools["Drag"].Properties["Radius"]
                                    : dragRadius,
                                maxParticles
                            );
                        }
                        break;
                    case "Pin":
                        {
                            float pinRadius = _interactTools["Pin"].Properties.ContainsKey("Radius")
                                ? (float)_interactTools["Pin"].Properties["Radius"]
                                : dragRadius;
                            if (_currentMode == MeshMode.Cloth)
                                PinParticle(intitialMousePosWhenPressed, pinRadius);
                            else if (_currentMode == MeshMode.Interact)
                                PinParticleBuildable(intitialMousePosWhenPressed, pinRadius);
                        }
                        break;
                    case "Cut":
                        var radius =
                            _interactTools["Cut"].Properties["Radius"] != null
                                ? (float)_interactTools["Cut"].Properties["Radius"]
                                : 10f;

                        if (_currentMode == MeshMode.Cloth)
                            CutAllSticksInRadius(intitialMousePosWhenPressed, radius);
                        else if (_currentMode == MeshMode.Interact)
                            CutAllSticksInRadiusBuildable(intitialMousePosWhenPressed, radius);

                        break;
                    case "Wind":
                        break;
                    case "PhysicsDrag":
                        {
                            float physRadius = _interactTools["PhysicsDrag"]
                                .Properties.ContainsKey("Radius")
                                ? (float)_interactTools["PhysicsDrag"].Properties["Radius"]
                                : dragRadius;
                            if (_currentMode == MeshMode.Cloth)
                            {
                                particlesInDragArea = GetParticlesInRadius(
                                    intitialMousePosWhenPressed,
                                    physRadius
                                );
                            }
                            else if (_currentMode == MeshMode.Interact)
                            {
                                buildableMeshParticlesInDragArea =
                                    GetBuildableMeshParticlesInRadius(
                                        intitialMousePosWhenPressed,
                                        physRadius
                                    );
                            }
                        }
                        break;
                    case "LineCut":
                        break;
                    case "Inspect Particles":
                        {
                            float inspectRadius = _interactTools["Inspect Particles"]
                                .Properties.ContainsKey("Radius")
                                ? (float)_interactTools["Inspect Particles"].Properties["Radius"]
                                : 10f;
                            if (
                                _interactTools["Inspect Particles"].Properties.ContainsKey("IsLog")
                                && (bool)_interactTools["Inspect Particles"].Properties["IsLog"]
                            )
                                InspectParticlesInRadiusLog(
                                    intitialMousePosWhenPressed,
                                    inspectRadius
                                );
                            else
                                InspectParticlesInRadiusWindow(
                                    intitialMousePosWhenPressed,
                                    inspectRadius
                                );
                        }
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
                    && _currentMode != MeshMode.Edit
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

            float minDist = _interactTools["Wind"].Properties.ContainsKey("MinDistance")
                ? (float)_interactTools["Wind"].Properties["MinDistance"]
                : 5f;
            if (windDistance > minDist)
            {
                float arrowThickness = _interactTools["Wind"]
                    .Properties.ContainsKey("ArrowThickness")
                    ? (float)_interactTools["Wind"].Properties["ArrowThickness"]
                    : 3f;
                windDirectionArrow = new VectorGraphics.PrimitiveBatch.Arrow(
                    intitialMousePosWhenPressed,
                    currentMousePos,
                    Color.Cyan,
                    arrowThickness
                );

                float strength = _interactTools["Wind"].Properties.ContainsKey("StrengthScale")
                    ? (float)_interactTools["Wind"].Properties["StrengthScale"]
                    : 1.0f;
                windForce = !Paused
                    ? windDirection * (windDistance / 50f) * strength
                    : Vector2.Zero;
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
            float minDist = _interactTools["LineCut"].Properties.ContainsKey("MinDistance")
                ? (float)_interactTools["LineCut"].Properties["MinDistance"]
                : 5f;
            if (cutDistance > minDist)
            {
                float thickness = _interactTools["LineCut"].Properties.ContainsKey("Thickness")
                    ? (float)_interactTools["LineCut"].Properties["Thickness"]
                    : 3f;
                cutLine = new VectorGraphics.PrimitiveBatch.Line(
                    intitialMousePosWhenPressed,
                    currentMousePos,
                    Color.Red,
                    thickness
                );
            }
            else
            {
                cutLine = null;
            }
        }
        else if (_selectedToolName == "Add Polygon")
        {
            _buildableMeshInstance = _polygonBuilderInstance.BuildPolygon(
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
            else if (_currentMode == MeshMode.Interact || _currentMode == MeshMode.Edit)
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
            else if (_currentMode == MeshMode.Interact || _currentMode == MeshMode.Edit)
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
