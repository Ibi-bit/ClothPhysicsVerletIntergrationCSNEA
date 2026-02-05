using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System.Collections.Generic;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    private VectorGraphics.PrimitiveBatch.Rectangle _selectRectangle;
    private float _previousDeltaTime;
    private Vector2 _windForce;
    private VectorGraphics.PrimitiveBatch.Arrow _windDirectionArrow;
    private VectorGraphics.PrimitiveBatch.Line _cutLine;
    private List<Vector2> _particlesInDragArea;
    private List<int> _meshParticlesInDragArea;

    private void InitializeUpdate()
    {
        _selectRectangle = null;
        _previousDeltaTime = 0f;
        _windForce = Vector2.Zero;
        _windDirectionArrow = null;
        _cutLine = null;
        _particlesInDragArea = new List<Vector2>();
        _meshParticlesInDragArea = new List<int>();
    }

    protected override void Update(GameTime gameTime)
    {
        KeyboardState keyboardState = Keyboard.GetState();
        float frameTime = (float)Math.Min(gameTime.ElapsedGameTime.TotalSeconds, 0.1);
        
        if (_currentToolSet.ContainsKey("Cursor Collider"))
        {
            var props = _currentToolSet["Cursor Collider"].Properties;
            float radius = props.ContainsKey("Radius") ? (float)props["Radius"] : 20f;
            string shape = props.ContainsKey("Shape") ? (string)props["Shape"] : "Circle";
            if (_cursorColliderStore.TryGetValue(shape, out var collider))
            {
                _cursorCollider = collider;
            }
            UpdateCursorColliderSize(radius);
        }

        if (keyboardState.IsKeyDown(Keys.Escape) && !_prevKeyboardState.IsKeyDown(Keys.Escape))
        {
            _paused = !_paused;
        }

        // Step one physics frame when paused (press Space)
        if (_paused && keyboardState.IsKeyDown(Keys.Space) && !_prevKeyboardState.IsKeyDown(Keys.Space))
        {
            _stepsToStep = 1;
        }

        if (!_paused)
        {
            _timeAccumulator += frameTime;
        }

        MouseState mouseState = Mouse.GetState();
        Vector2 currentMousePos = new Vector2(mouseState.X, mouseState.Y);

        bool imguiWantsMouse = ImGuiNET.ImGui.GetIO().WantCaptureMouse;

        if (!imguiWantsMouse)
        {
            if (mouseState.LeftButton == ButtonState.Pressed && !_leftPressed)
            {
                _leftPressed = true;
                _initialMousePosWhenPressed = currentMousePos;
                _previousMousePos = currentMousePos;

                switch (_selectedToolName)
                {
                    case "Drag":

                        if (_currentMode == MeshMode.Interact)
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

                            _meshParticlesInDragArea = GetMeshParticlesInRadius(
                                _initialMousePosWhenPressed,
                                props.ContainsKey("Radius") ? (float)props["Radius"] : _dragRadius,
                                maxParticles
                            );
                        }
                        break;
                    case "Pin":
                        {
                            float pinRadius = _currentToolSet["Pin"]
                                .Properties.ContainsKey("Radius")
                                ? (float)_currentToolSet["Pin"].Properties["Radius"]
                                : _dragRadius;

                            PinParticleBuildable(_initialMousePosWhenPressed, pinRadius);
                        }
                        break;
                    case "Cut":
                        var radius =
                            _currentToolSet["Cut"].Properties["Radius"] != null
                                ? (float)_currentToolSet["Cut"].Properties["Radius"]
                                : 10f;

                        CutAllSticksInRadiusBuildable(_initialMousePosWhenPressed, radius);

                        break;
                    case "Wind":
                        break;
                    case "PhysicsDrag":
                        {
                            float physRadius = _currentToolSet["PhysicsDrag"]
                                .Properties.ContainsKey("Radius")
                                ? (float)_currentToolSet["PhysicsDrag"].Properties["Radius"]
                                : _dragRadius;

                            if (_currentMode == MeshMode.Interact)
                            {
                                _meshParticlesInDragArea = GetMeshParticlesInRadius(
                                    _initialMousePosWhenPressed,
                                    physRadius
                                );
                                _meshParticlesInDragArea = GetMeshParticlesInRadius(
                                    _initialMousePosWhenPressed,
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
                            HandleAddStickBetweenParticlesClick(_initialMousePosWhenPressed);
                        }
                        break;
                    case "Add Particle":
                        {
                            float particleMass = _currentToolSet["Add Particle"]
                                .Properties.ContainsKey("Mass")
                                ? (float)_currentToolSet["Add Particle"].Properties["Mass"]
                                : 1f;
                            _activeMesh.AddParticle(
                                _initialMousePosWhenPressed,
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
                                _initialMousePosWhenPressed,
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
                                    _initialMousePosWhenPressed,
                                    inspectRadius
                                );
                            else if (
                                !_currentToolSet["Inspect Particles"]
                                    .Properties.ContainsKey("SelectRectangle")
                            )
                                InspectParticlesInRadiusWindow(
                                    _initialMousePosWhenPressed,
                                    inspectRadius
                                );
                        }
                        break;
                }
            }
            else if (mouseState.LeftButton == ButtonState.Released)
            {
                if (_selectedToolName == "Wind" && _leftPressed)
                {
                    ApplyWindForceFromDrag(
                        _initialMousePosWhenPressed,
                        currentMousePos,
                        _dragRadius
                    );
                }
                else if (
                    _selectedToolName == "LineCut"
                    && _leftPressed
                    && _currentMode != MeshMode.Edit
                )
                {
                    Vector2 cutDirection = currentMousePos - _initialMousePosWhenPressed;
                    float cutDistance = cutDirection.Length();
                    if (cutDistance > 5f)
                    {
                        CutSticksAlongLine(_initialMousePosWhenPressed, currentMousePos);
                    }
                }
                else if (
                    _selectedToolName == "Inspect Particles"
                    && _leftPressed
                    && _currentMode != MeshMode.Edit
                    && _currentToolSet["Inspect Particles"]
                        .Properties.ContainsKey("RectangleSelect")
                    && (bool)_currentToolSet["Inspect Particles"].Properties["RectangleSelect"]
                )
                {
                    InspectParticlesInRectangle(
                        _initialMousePosWhenPressed,
                        currentMousePos,
                        _currentToolSet["Inspect Particles"].Properties.ContainsKey("IsLog")
                            && (bool)_currentToolSet["Inspect Particles"].Properties["IsLog"],
                        _currentToolSet["Inspect Particles"]
                            .Properties.ContainsKey("Clear When Use")
                            && (bool)
                                _currentToolSet["Inspect Particles"].Properties["Clear When Use"]
                    );
                }
                else if (_selectedToolName == "Line Tool" && _leftPressed)
                {
                    var props = _currentToolSet["Line Tool"].Properties;
                    int constraintsInLine = (int)props["Constraints in Line"];
                    float naturalLengthRatio = (float)props["Natural Length Ratio"];
                        

                    _activeMesh.AddSticksAccrossLength(
                        _initialMousePosWhenPressed,
                        currentMousePos,
                        constraintsInLine,
                        naturalLengthRatio
                    );
                }

                _leftPressed = false;
                _initialMousePosWhenPressed = Vector2.Zero;
                _windDirectionArrow = null;
                _cutLine = null;
                _selectRectangle = null;
                _windForce = Vector2.Zero;
            }
        }

        if (_selectedToolName == "Wind" && _leftPressed)
        {
            Vector2 windDirection = currentMousePos - _initialMousePosWhenPressed;
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
                _windDirectionArrow = new VectorGraphics.PrimitiveBatch.Arrow(
                    _initialMousePosWhenPressed,
                    currentMousePos,
                    Color.Cyan,
                    arrowThickness
                );

                float strength = _currentToolSet["Wind"].Properties.ContainsKey("StrengthScale")
                    ? (float)_currentToolSet["Wind"].Properties["StrengthScale"]
                    : 1.0f;
                _windForce = !_paused
                    ? windDirection * (windDistance / 50f) * strength
                    : Vector2.Zero;
            }
            else
            {
                _windDirectionArrow = null;
                _windForce = Vector2.Zero;
            }
        }
        else if (_selectedToolName == "LineCut" && _leftPressed)
        {
            Vector2 cutDirection = currentMousePos - _initialMousePosWhenPressed;
            float cutDistance = cutDirection.Length();
            float minDist = _currentToolSet["LineCut"].Properties.ContainsKey("MinDistance")
                ? (float)_currentToolSet["LineCut"].Properties["MinDistance"]
                : 5f;
            if (cutDistance > minDist)
            {
                float thickness = _currentToolSet["LineCut"].Properties.ContainsKey("Thickness")
                    ? (float)_currentToolSet["LineCut"].Properties["Thickness"]
                    : 3f;
                _cutLine = new VectorGraphics.PrimitiveBatch.Line(
                    _initialMousePosWhenPressed,
                    currentMousePos,
                    Color.Red,
                    thickness
                );
            }
            else
            {
                _cutLine = null;
            }
        }
        else if (_selectedToolName == "Create Grid Mesh")
        {
            var props = _currentToolSet["Create Grid Mesh"].Properties;
            float distance = (float)props["DistanceBetweenParticles"];
            if (keyboardState.IsKeyDown(Keys.C) && !_prevKeyboardState.IsKeyDown(Keys.C))
            {
                _activeMesh = Mesh.CreateGridMesh(_initialMousePosWhenPressed, currentMousePos, distance, _activeMesh);
            }

            if (_leftPressed)
                _selectRectangle = new VectorGraphics.PrimitiveBatch.Rectangle(
                    GetRectangleFromPoints(_initialMousePosWhenPressed, currentMousePos),
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
            if (props.ContainsKey("RectangleSelect") && _leftPressed)
            {
                _selectRectangle = new VectorGraphics.PrimitiveBatch.Rectangle(
                    GetRectangleFromPoints(_initialMousePosWhenPressed, currentMousePos),
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
        else if (_selectedToolName == "Add Polygon")
        {
            _activeMesh.BuildPolygon(
                keyboardState,
                _prevKeyboardState,
                mouseState,
                _prevMouseState,
                imguiWantsMouse
            );
        }
        int stepsThisFrame = 0;
        const int maxStepsPerFrame = 10000;
        int subSteps = Math.Max(1, _subSteps);
        Vector2 mouseDelta = currentMousePos - _previousMousePos;
        int plannedStepsThisFrame = _paused 
            ? _stepsToStep 
            : Math.Min((int)(_timeAccumulator / FixedTimeStep), maxStepsPerFrame);
        int totalIterations = Math.Max(1, plannedStepsThisFrame * subSteps);
        Vector2 deltaPerIteration = mouseDelta / totalIterations;

        while ((_timeAccumulator >= FixedTimeStep || _stepsToStep > 0) && stepsThisFrame < maxStepsPerFrame)
        {
            float subDt = FixedTimeStep / subSteps;

            for (int subStepIndex = 0; subStepIndex < subSteps; subStepIndex++)
            {
                foreach (var particle in _activeMesh.Particles.Values)
                {
                    particle.AccumulatedForce = Vector2.Zero;
                }
                if (!_useConstraintSolver)
                {
                    float timeRatio = FixedTimeStep / subDt;
                    ApplyStickForcesDictionary(_activeMesh.Sticks, timeRatio);
                }
                float iterationLerpFactor = 1f / (stepsThisFrame + 1f);
                
                Vector2 cursorCenter = GetCursorColliderCenter();
                cursorCenter = Vector2.Lerp(cursorCenter, currentMousePos, iterationLerpFactor);
                SetCursorColliderCenter(cursorCenter);
                
                if (_leftPressed && _selectedToolName == "Drag" && _currentMode == MeshMode.Interact)
                {
                    foreach (int particleId in _meshParticlesInDragArea)
                    {
                        if (_activeMesh.Particles.TryGetValue(particleId, out var particle) && !particle.IsPinned)
                        {
                            particle.PreviousPosition = particle.Position;
                            particle.Position += deltaPerIteration;
                        }
                    }
                }
                
                UpdateParticles(subDt);
                _previousDeltaTime = subDt;
            }

            // Consume time or manual step
            if (_stepsToStep > 0)
                _stepsToStep--;
            else
                _timeAccumulator -= FixedTimeStep;
            
            stepsThisFrame++;
        }

        if (stepsThisFrame == maxStepsPerFrame)
        {
            _timeAccumulator = Math.Min(_timeAccumulator, FixedTimeStep);
        }

        UpdateStickColorsDictionary(_activeMesh.Sticks);

        if (_selectedToolName == "Drag")
        {
            foreach (int particleId in _meshParticlesInDragArea)
            {
                if (_activeMesh.Particles.TryGetValue(particleId, out var particle))
                {
                    particle.Color = _leftPressed ? Color.Yellow : Color.White;
                }
            }
        }
        else if (_selectedToolName == "PhysicsDrag")
        {
            

            if (_currentMode == MeshMode.Interact || _currentMode == MeshMode.Edit)
            {
                DragMeshParticlesWithPhysics(mouseState, _leftPressed, _meshParticlesInDragArea);
            }
        }

        _previousMousePos = currentMousePos;

        base.Update(gameTime);
        _prevKeyboardState = keyboardState;
        _prevMouseState = mouseState;
    }

    private void UpdateStep()
    {
        
    }

    private Vector2 GetCursorColliderCenter()
    {
        if (_cursorCollider is CircleCollider circle)
        {
            return circle.Position;
        }

        if (_cursorCollider is RectangleCollider rectangle)
        {
            return new Vector2(rectangle.Rectangle.Center.X, rectangle.Rectangle.Center.Y);
        }

        

        return Vector2.Zero;
    }

    private void SetCursorColliderCenter(Vector2 center)
    {
        if (_cursorCollider is CircleCollider circle)
        {
            circle.Position = center;
            return;
        }

        if (_cursorCollider is RectangleCollider rectangle)
        {
            float halfSize = rectangle.Rectangle.Width / 2f;
            rectangle.Rectangle = new Rectangle(
                (int)(center.X - halfSize),
                (int)(center.Y - halfSize),
                rectangle.Rectangle.Width,
                rectangle.Rectangle.Height
            );
            return;
        }

       
    }

    private void UpdateCursorColliderSize(float radius)
    {
        int size = Math.Max(1, (int)MathF.Round(radius * 2f));

        if (_cursorCollider is CircleCollider circle)
        {
            circle.Radius = radius;
            return;
        }

        if (_cursorCollider is RectangleCollider rectangle)
        {
            Vector2 center = new Vector2(rectangle.Rectangle.Center.X, rectangle.Rectangle.Center.Y);
            rectangle.Rectangle = new Rectangle(
                (int)(center.X - size / 2f),
                (int)(center.Y - size / 2f),
                size,
                size
            );
            
        }

        
    }
}
