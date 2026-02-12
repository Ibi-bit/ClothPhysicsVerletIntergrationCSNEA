using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using VectorGraphics;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    private const float FixedTimeStep = 1f / 60f;

    private Vector2 _collisonBoundsDifference;
    private Vector2 _baseForce;
    private float _timeAccumulator;
    private bool _useConstraintSolver;
    private int _subSteps;
    private int _stepsToStep;
    


    private void InitializePhysics()
    {
        _collisonBoundsDifference = new Vector2(0, -10);
        _baseForce = new Vector2(0, 980f);
        _timeAccumulator = 0f;
        _useConstraintSolver = false;
        _activeMesh.Colliders = new();
        _activeMesh.Colliders.Add(
            
            new SeperatedAxisRectangleCollider(
                new Rectangle(200, 300, 400, 20),
                Single.Pi/4f
            ));

        _subSteps = 50;
    }

    private void ApplyStickForcesDictionary(
        Dictionary<int, Mesh.MeshStick> sticks,
        float timeRatio = 1f
    )
    {
        float scaledK = _activeMesh.springConstant * 1;

        foreach (var stick in sticks.Values)
        {
            if (stick.Length <= 0f)
                continue;
            Vector2 v = stick.P1.Position - stick.P2.Position;
            float L = v.Length();
            if (L <= 0f)
                continue;
            Vector2 dir = v / L;
            float stretch = L - stick.Length;
            Vector2 springForce = dir * stretch * scaledK;
            stick.P1.AccumulatedForce -= springForce;
            stick.P2.AccumulatedForce += springForce;
        }
    }

    private void UpdateStickColorsDictionary(Dictionary<int, Mesh.MeshStick> sticks)
    {
        int count = 0;
        float sum = 0f;
        float sumSq = 0f;

        foreach (var s in sticks.Values)
        {
            if (s.Length <= 0f)
                continue;
            Vector2 v = s.P1.Position - s.P2.Position;
            float L = v.Length();
            if (L <= 0f)
                continue;
            float e = (L - s.Length) / s.Length;
            sum += e;
            sumSq += e * e;
            count++;
        }

        float mean = count > 0 ? sum / count : 0f;
        float variance = count > 0 ? (sumSq / count) - mean * mean : 0f;
        if (variance < 0f)
            variance = 0f;
        float std = (float)Math.Sqrt(variance);

        foreach (var s in sticks.Values)
        {
            if (s.Length <= 0f)
                continue;
            Vector2 v = s.P1.Position - s.P2.Position;
            float L = v.Length();
            if (L <= 0f)
                continue;
            float e = (L - s.Length) / s.Length;

            float intensity = 0f;
            if (count > 0 && std > 1e-5f)
            {
                float z = (e - mean) / std;
                intensity = MathHelper.Clamp((z - 0.5f) / 1.5f, 0f, 1f);
            }
            else
            {
                intensity = MathHelper.Clamp((L / s.Length - 1f) / 0.5f, 0f, 1f);
            }
            float eased = intensity * intensity;
            s.Color = Color.Lerp(Color.White, Color.Red, eased);
        }
    }

    private void UpdateParticles(float deltaTime)
    {
        float forceMagnitudeSum = 0f;
        float forceMagnitudeSquaredSum = 0f;
        float maxForceMagnitude = 0f;
        int totalForceCount = 0;

        foreach (var particle in _activeMesh.Particles.Values)
        {
            if (particle is OscillatingParticle op)
            {
                op.UpdateOscillation(deltaTime);
                particle.AccumulatedForce = Vector2.Zero;
                continue;
            }

            Vector2 totalForce = _baseForce + particle.AccumulatedForce + _windForce;

            particle.TotalForceMagnitude = totalForce.Length();
            forceMagnitudeSum += particle.TotalForceMagnitude;
            forceMagnitudeSquaredSum += particle.TotalForceMagnitude * particle.TotalForceMagnitude;
            totalForceCount++;

            if (particle.TotalForceMagnitude > maxForceMagnitude)
            {
                maxForceMagnitude = particle.TotalForceMagnitude;
            }

            if (particle.IsPinned)
            {
                particle.AccumulatedForce = Vector2.Zero;
                continue;
            }

            bool isBeingDragged = false;
            if (_leftPressed && (_selectedToolName == "Drag" || _selectedToolName == "PhysicsDrag"))
            {
                if (_meshParticlesInDragArea.Contains(particle.ID))
                {
                    isBeingDragged = true;
                }
            }

            if (!isBeingDragged)
            {
                Vector2 acceleration = _baseForce;
                if (_activeMesh.mass > 0f)
                {
                    acceleration += (particle.AccumulatedForce + _windForce) / _activeMesh.mass;
                }
                Vector2 velocity = particle.Position - particle.PreviousPosition;
                


                float substepsPerFrame = FixedTimeStep / deltaTime;
                float dragPerSubstep = MathF.Pow(_activeMesh.drag, 1f / substepsPerFrame);
                velocity *= dragPerSubstep;

                Vector2 oldPosition = particle.Position;

                particle.Position =
                    particle.Position + velocity + acceleration * (deltaTime * deltaTime);

                particle.Position = new Vector2(
                    float.IsNaN(particle.Position.X) ? oldPosition.X : particle.Position.X,
                    float.IsNaN(particle.Position.Y) ? oldPosition.Y : particle.Position.Y
                );
                
                particle.PreviousPosition = oldPosition;
            }
            Vector2 beforeCorrection = particle.Position;
            if (_selectedToolName == "Cursor Collider")
            {
                _cursorCollider.ContainsPoint(particle.Position, out particle.Position);
            }

            if (!isBeingDragged)
            {
                foreach (var collider in _activeMesh.Colliders)
                {
                    Vector2 clampedPosition;
                    if (collider.ContainsPoint(particle.Position, out clampedPosition))
                    {
                        particle.Position = clampedPosition;
                    }
                }
            }

            if (particle.Position.X < 0)
            {
                particle.Position.X = 0;
            }
            else if (particle.Position.X > _windowBounds.Width)
            {
                particle.Position.X = _windowBounds.Width;
            }

            if (particle.Position.Y < 0)
            {
                particle.Position.Y = 0;
            }
            else if (particle.Position.Y > _windowBounds.Height - 10)
            {
                particle.Position.Y = _windowBounds.Height - 10;
            }

            
            if (particle.Position != beforeCorrection)
            {
                particle.PreviousPosition = particle.Position;
            }

        }

        if (totalForceCount > 0)
        {
            _activeMesh.meanForceMagnitude = forceMagnitudeSum / totalForceCount;
            float meanSquare = _activeMesh.meanForceMagnitude * _activeMesh.meanForceMagnitude;
            float variance = (forceMagnitudeSquaredSum / totalForceCount) - meanSquare;
            if (variance < 0f)
            {
                variance = 0f;
            }

            _activeMesh.forceStdDeviation = (float)Math.Sqrt(variance);
            _activeMesh.maxForceMagnitude = maxForceMagnitude;
        }
        else
        {
            _activeMesh.meanForceMagnitude = 0f;
            _activeMesh.forceStdDeviation = 0f;
            _activeMesh.maxForceMagnitude = 0f;
        }
    }
}
