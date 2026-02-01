using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    private VectorGraphics.PrimitiveBatch.Rectangle _selectRectangle;

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
                            var props = _currentToolSet["Drag"].Properties;
                            bool infiniteParticles =
                                props.ContainsKey("InfiniteParticles")
                                && (bool)props["InfiniteParticles"];
                            int maxParticles = infiniteParticles
                                ? -1
                                : (
                                    props.ContainsKey("MaxParticles")
                                        ? (int)props["MaxParticles"]
                                        : -1
                                );

                            particlesInDragArea = GetParticlesInRadius(
                                intitialMousePosWhenPressed,
                                props.ContainsKey("Radius") ? (float)props["Radius"] : dragRadius,
                                maxParticles
                            );
                        }
                        else if (_currentMode == MeshMode.Interact)
                        {
                            var props = _currentToolSet["Drag"].Properties;
                            bool infiniteParticles =
                                props.ContainsKey("InfiniteParticles")
                                && (bool)props["InfiniteParticles"];
                            int maxParticles = infiniteParticles
                                ? -1
                                : (
                                    props.ContainsKey("MaxParticles")
                                        ? (int)props["MaxParticles"]
                                        : -1
                                );

                            meshParticlesInDragArea = GetMeshParticlesInRadius(
                                intitialMousePosWhenPressed,
                                props.ContainsKey("Radius") ? (float)props["Radius"] : dragRadius,
                                maxParticles
                            );
                        }
                        break;
                    case "Pin":
                        {
                            float pinRadius = _currentToolSet["Pin"]
                                .Properties.ContainsKey("Radius")
                                ? (float)_currentToolSet["Pin"].Properties["Radius"]
                                : dragRadius;
                            if (_currentMode == MeshMode.Cloth)
                                PinParticle(intitialMousePosWhenPressed, pinRadius);
                            else
                                PinParticleBuildable(intitialMousePosWhenPressed, pinRadius);
                        }
                        break;
                    case "Cut":
                        var radius =
                            _currentToolSet["Cut"].Properties["Radius"] != null
                                ? (float)_currentToolSet["Cut"].Properties["Radius"]
                                : 10f;

                        if (_currentMode == MeshMode.Cloth)
                            CutAllSticksInRadius(intitialMousePosWhenPressed, radius);
                        else
                            CutAllSticksInRadiusBuildable(intitialMousePosWhenPressed, radius);

                        break;
                    case "Wind":
                        break;
                    case "PhysicsDrag":
                        {
                            float physRadius = _currentToolSet["PhysicsDrag"]
                                .Properties.ContainsKey("Radius")
                                ? (float)_currentToolSet["PhysicsDrag"].Properties["Radius"]
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
                                meshParticlesInDragArea = GetMeshParticlesInRadius(
                                    intitialMousePosWhenPressed,
                                    physRadius
                                );
                            }
                        }
                        break;
                    case "LineCut":
                        break;
                    case "Add Stick Between Particles":
                        {
                            float stickRadius = _currentToolSet[
                                "Add Stick Between Particles"
                            ].Properties["Radius"]
                                is float r
                                ? r
                                : 10f;
                            HandleAddStickBetweenParticlesClick(intitialMousePosWhenPressed);
                        }
                        break;
                    case "Add Particle":
                        {
                            float particleMass = _currentToolSet["Add Particle"]
                                .Properties.ContainsKey("Mass")
                                ? (float)_currentToolSet["Add Particle"].Properties["Mass"]
                                : 1f;
                            _activeMesh.AddParticle(
                                intitialMousePosWhenPressed,
                                particleMass,
                                false,
                                Color.White
                            );
                        }
                        break;
                    case "Remove Particle":
                        {
                            float removeRadius = _currentToolSet["Remove Particle"]
                                .Properties.ContainsKey("Radius")
                                ? (float)_currentToolSet["Remove Particle"].Properties["Radius"]
                                : 10f;
                            var pS = GetMeshParticlesInRadius(
                                intitialMousePosWhenPressed,
                                removeRadius,
                                1
                            );
                            _activeMesh.RemoveParticle(pS[0]);
                        }
                        break;
                    case "Inspect Particles":
                        {
                            float inspectRadius = _currentToolSet["Inspect Particles"]
                                .Properties.ContainsKey("Radius")
                                ? (float)_currentToolSet["Inspect Particles"].Properties["Radius"]
                                : 10f;
                            if (
                                _currentToolSet["Inspect Particles"].Properties.ContainsKey("IsLog")
                                && (bool)_currentToolSet["Inspect Particles"].Properties["IsLog"]
                            )
                                InspectParticlesInRadiusLog(
                                    intitialMousePosWhenPressed,
                                    inspectRadius
                                );
                            else if (
                                !_currentToolSet["Inspect Particles"]
                                    .Properties.ContainsKey("SelectRectangle")
                            )
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
                else if (
                    _selectedToolName == "Inspect Particles"
                    && leftPressed
                    && _currentMode != MeshMode.Edit
                    && _currentToolSet["Inspect Particles"]
                        .Properties.ContainsKey("RectangleSelect")
                    && (bool)_currentToolSet["Inspect Particles"].Properties["RectangleSelect"]
                )
                {
                    InspectParticlesInRectangle(
                        intitialMousePosWhenPressed,
                        currentMousePos,
                        _currentToolSet["Inspect Particles"].Properties.ContainsKey("IsLog")
                            && (bool)_currentToolSet["Inspect Particles"].Properties["IsLog"],
                        _currentToolSet["Inspect Particles"]
                            .Properties.ContainsKey("Clear When Use")
                            && (bool)
                                _currentToolSet["Inspect Particles"].Properties["Clear When Use"]
                    );
                }

                leftPressed = false;
                intitialMousePosWhenPressed = Vector2.Zero;
                windDirectionArrow = null;
                cutLine = null;
                _selectRectangle = null;
                windForce = Vector2.Zero;
            }
        }

        if (_selectedToolName == "Wind" && leftPressed)
        {
            Vector2 windDirection = currentMousePos - intitialMousePosWhenPressed;
            float windDistance = windDirection.Length();

            float minDist = _currentToolSet["Wind"].Properties.ContainsKey("MinDistance")
                ? (float)_currentToolSet["Wind"].Properties["MinDistance"]
                : 5f;
            if (windDistance > minDist)
            {
                float arrowThickness = _currentToolSet["Wind"]
                    .Properties.ContainsKey("ArrowThickness")
                    ? (float)_currentToolSet["Wind"].Properties["ArrowThickness"]
                    : 3f;
                windDirectionArrow = new VectorGraphics.PrimitiveBatch.Arrow(
                    intitialMousePosWhenPressed,
                    currentMousePos,
                    Color.Cyan,
                    arrowThickness
                );

                float strength = _currentToolSet["Wind"].Properties.ContainsKey("StrengthScale")
                    ? (float)_currentToolSet["Wind"].Properties["StrengthScale"]
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
            float minDist = _currentToolSet["LineCut"].Properties.ContainsKey("MinDistance")
                ? (float)_currentToolSet["LineCut"].Properties["MinDistance"]
                : 5f;
            if (cutDistance > minDist)
            {
                float thickness = _currentToolSet["LineCut"].Properties.ContainsKey("Thickness")
                    ? (float)_currentToolSet["LineCut"].Properties["Thickness"]
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
            _activeMesh = _polygonBuilderInstance.BuildPolygon(
                keyboardState,
                _prevKeyboardState,
                mouseState,
                _prevMouseState,
                _activeMesh,
                imguiWantsMouse
            );
        }
        else if (_selectedToolName == "Create Grid Mesh")
        {
            var props = _currentToolSet["Create Grid Mesh"].Properties;
            float distance = (float)props["DistanceBetweenParticles"];
            if (keyboardState.IsKeyDown(Keys.C) && !_prevKeyboardState.IsKeyDown(Keys.C))
            {
                _activeMesh.CreateGridMesh(intitialMousePosWhenPressed, currentMousePos, distance);
            }

            if (leftPressed)
                _selectRectangle = new VectorGraphics.PrimitiveBatch.Rectangle(
                    GetRectangleFromPoints(intitialMousePosWhenPressed, currentMousePos),
                    new Color(Color.DarkGreen, 0.05f),
                    true,
                    2,
                    Color.Yellow
                );
            else
            {
                _selectRectangle = null;
            }
        }
        else if (_selectedToolName == "Inspect Particles")
        {
            var props = _currentToolSet["Inspect Particles"].Properties;
            float inspectRadius = props.ContainsKey("Radius") ? (float)props["Radius"] : 10f;
            if (props.ContainsKey("RectangleSelect") && leftPressed)
            {
                _selectRectangle = new VectorGraphics.PrimitiveBatch.Rectangle(
                    GetRectangleFromPoints(intitialMousePosWhenPressed, currentMousePos),
                    new Color(Color.Green, 0.05f),
                    true,
                    2,
                    Color.Green
                );
            }
            else
            {
                _selectRectangle = null;
            }
        }
        else
        {
            _polygonBuilderInstance.Reset();
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
                DragMeshParticles(mouseState, leftPressed, meshParticlesInDragArea);
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
                DragMeshParticlesWithPhysics(mouseState, leftPressed, meshParticlesInDragArea);
            }
        }

        previousMousePos = currentMousePos;

        base.Update(gameTime);
        _prevKeyboardState = keyboardState;
        _prevMouseState = mouseState;
    }
}
