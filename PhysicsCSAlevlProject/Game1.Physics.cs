using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using VectorGraphics;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    private DrawableStick[][] ApplyStickForces(DrawableStick[][] sticks)
    {
        float k = _activeMesh.springConstant;
        const float LengthEpsilonSq = 1e-8f;
        const float StretchEpsilon = 1e-4f;

        for (int i = 0; i < sticks.Length; i++)
        {
            var row = sticks[i];
            for (int j = 0, jl = row.Length; j < jl; j++)
            {
                var s = row[j];
                if (s.IsCut)
                    continue;

                float L0 = s.Length;
                if (L0 <= 0f)
                    continue;

                var p1 = s.P1;
                var p2 = s.P2;

                Vector2 v = p1.Position - p2.Position;
                float lenSq = v.LengthSquared();
                if (lenSq <= LengthEpsilonSq)
                    continue;

                float L = MathF.Sqrt(lenSq);
                float stretch = L - L0;
                if (MathF.Abs(stretch) <= StretchEpsilon)
                    continue;

                float invL = 1f / L;
                float factor = (stretch * invL) * k;
                Vector2 springForce = v * factor;

                p1.AccumulatedForce -= springForce;
                p2.AccumulatedForce += springForce;
            }
        }
        return sticks;
    }

    private void ApplyStickForcesDictionary(Dictionary<int, Mesh.MeshStick> sticks)
    {
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
            Vector2 springForce = dir * stretch * _activeMesh.springConstant;
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

    private void UpdateStickColorsRelative(DrawableStick[][] horizontal, DrawableStick[][] vertical)
    {
        int count = 0;
        float sum = 0f;
        float sumSq = 0f;

        void Accumulate(DrawableStick[][] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                for (int j = 0; j < arr[i].Length; j++)
                {
                    var s = arr[i][j];
                    if (s == null)
                    {
                        continue;
                    }
                    if (s.Length <= 0f)
                    {
                        continue;
                    }
                    Vector2 v = s.P1.Position - s.P2.Position;
                    float L = v.Length();
                    if (L <= 0f)
                    {
                        continue;
                    }
                    float e = (L - s.Length) / s.Length;
                    sum += e;
                    sumSq += e * e;
                    count++;
                }
            }
        }

        Accumulate(horizontal);
        Accumulate(vertical);

        float mean = count > 0 ? sum / count : 0f;
        float variance = count > 0 ? (sumSq / count) - mean * mean : 0f;
        if (variance < 0f)
        {
            variance = 0f;
        }
        float std = (float)Math.Sqrt(variance);

        void Colorize(DrawableStick[][] arr)
        {
            for (int i = 0; i < arr.Length; i++)
            {
                for (int j = 0; j < arr[i].Length; j++)
                {
                    var s = arr[i][j];
                    if (s == null)
                    {
                        continue;
                    }
                    if (s.Length <= 0f)
                    {
                        continue;
                    }
                    Vector2 v = s.P1.Position - s.P2.Position;
                    float L = v.Length();
                    if (L <= 0f)
                    {
                        continue;
                    }
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
        }

        Colorize(horizontal);
        Colorize(vertical);
    }

    private void UpdateParticles(float deltaTime)
    {
        float forceMagnitudeSum = 0f;
        float forceMagnitudeSquaredSum = 0f;
        float maxForceMagnitude = 0f;
        int totalForceCount = 0;

        if (_currentMode == MeshMode.Cloth)
        {
            for (int i = 0; i < _clothInstance.particles.Length; i++)
            {
                for (int j = 0; j < _clothInstance.particles[i].Length; j++)
                {
                    DrawableParticle p = _clothInstance.particles[i][j];

                    Vector2 totalForce = BaseForce + p.AccumulatedForce + windForce;
                    p.TotalForceMagnitude = totalForce.Length();
                    forceMagnitudeSum += p.TotalForceMagnitude;
                    forceMagnitudeSquaredSum += p.TotalForceMagnitude * p.TotalForceMagnitude;
                    totalForceCount++;

                    if (p.TotalForceMagnitude > maxForceMagnitude)
                    {
                        maxForceMagnitude = p.TotalForceMagnitude;
                    }

                    if (p.IsPinned)
                    {
                        p.AccumulatedForce = Vector2.Zero;
                        continue;
                    }

                    bool isBeingDragged = false;
                    if (
                        leftPressed
                        && (_selectedToolName == "Drag" || _selectedToolName == "DragOne")
                    )
                    {
                        foreach (Vector2 draggedParticle in particlesInDragArea)
                        {
                            if ((int)draggedParticle.X == i && (int)draggedParticle.Y == j)
                            {
                                isBeingDragged = true;
                                break;
                            }
                        }
                    }

                    if (!isBeingDragged)
                    {
                        Vector2 acceleration = totalForce / p.Mass;
                        Vector2 velocity = p.Position - p.PreviousPosition;
                        velocity *= _clothInstance.drag;

                        Vector2 previousPosition = p.Position;
                        p.Position = p.Position + velocity + acceleration * (deltaTime * deltaTime);
                        p.PreviousPosition = previousPosition;
                        p = KeepInsideScreen(p);
                        _clothInstance.particles[i][j] = p;
                    }
                }
            }
        }
        else
        {
            foreach (var particle in _activeMesh.Particles.Values)
            {
                Vector2 totalForce = BaseForce + particle.AccumulatedForce + windForce;
                particle.TotalForceMagnitude = totalForce.Length();
                forceMagnitudeSum += particle.TotalForceMagnitude;
                forceMagnitudeSquaredSum +=
                    particle.TotalForceMagnitude * particle.TotalForceMagnitude;
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
                if (
                    leftPressed
                    && (
                        _selectedToolName == "Drag"
                        || _selectedToolName == "DragOne"
                        || _selectedToolName == "PhysicsDrag"
                    )
                )
                {
                    if (buildableMeshParticlesInDragArea.Contains(particle.ID))
                    {
                        isBeingDragged = true;
                    }
                }

                if (!isBeingDragged)
                {
                    Vector2 acceleration = totalForce / particle.Mass;
                    Vector2 velocity = particle.Position - particle.PreviousPosition;
                    velocity *= _activeMesh.drag;

                    Vector2 previousPosition = particle.Position;
                    particle.Position =
                        particle.Position + velocity + acceleration * (deltaTime * deltaTime);
                    particle.PreviousPosition = previousPosition;
                }

                bool positionChanged = false;
                if (particle.Position.X < 0)
                {
                    particle.Position.X = 0;
                    positionChanged = true;
                }
                else if (particle.Position.X > _windowBounds.Width)
                {
                    particle.Position.X = _windowBounds.Width;
                    positionChanged = true;
                }

                if (particle.Position.Y < 0)
                {
                    particle.Position.Y = 0;
                    positionChanged = true;
                }
                else if (particle.Position.Y > _windowBounds.Height - 10)
                {
                    particle.Position.Y = _windowBounds.Height - 10;
                    positionChanged = true;
                }

                if (positionChanged)
                {
                    particle.PreviousPosition = particle.Position;
                }
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

    private void SatisfyClothConstraints(int iterations)
    {
        if (_clothInstance == null)
            return;

        for (int it = 0; it < iterations; it++)
        {
            // Horizontal sticks
            for (int i = 0; i < _clothInstance.horizontalSticks.Length; i++)
            {
                var row = _clothInstance.horizontalSticks[i];
                for (int j = 0; j < row.Length; j++)
                {
                    var s = row[j];
                    if (s == null || s.IsCut)
                        continue;

                    var p1 = (DrawableParticle)s.P1;
                    var p2 = (DrawableParticle)s.P2;

                    Vector2 delta = p2.Position - p1.Position;
                    float len = delta.Length();
                    if (len <= 1e-6f)
                        continue;
                    float diff = (len - s.Length) / len;
                    Vector2 correction = delta * 0.5f * diff;

                    bool p1Pinned = p1.IsPinned;
                    bool p2Pinned = p2.IsPinned;

                    if (!p1Pinned && !p2Pinned)
                    {
                        p1.Position += correction;
                        p2.Position -= correction;
                    }
                    else if (!p1Pinned && p2Pinned)
                    {
                        p1.Position += correction * 2f;
                    }
                    else if (p1Pinned && !p2Pinned)
                    {
                        p2.Position -= correction * 2f;
                    }
                }
            }

            // Vertical sticks
            for (int i = 0; i < _clothInstance.verticalSticks.Length; i++)
            {
                var col = _clothInstance.verticalSticks[i];
                for (int j = 0; j < col.Length; j++)
                {
                    var s = col[j];
                    if (s == null || s.IsCut)
                        continue;

                    var p1 = (DrawableParticle)s.P1;
                    var p2 = (DrawableParticle)s.P2;

                    Vector2 delta = p2.Position - p1.Position;
                    float len = delta.Length();
                    if (len <= 1e-6f)
                        continue;
                    float diff = (len - s.Length) / len;
                    Vector2 correction = delta * 0.5f * diff;

                    bool p1Pinned = p1.IsPinned;
                    bool p2Pinned = p2.IsPinned;

                    if (!p1Pinned && !p2Pinned)
                    {
                        p1.Position += correction;
                        p2.Position -= correction;
                    }
                    else if (!p1Pinned && p2Pinned)
                    {
                        p1.Position += correction * 2f;
                    }
                    else if (p1Pinned && !p2Pinned)
                    {
                        p2.Position -= correction * 2f;
                    }
                }
            }

            // Keep inside bounds after each pass for stability
            for (int i = 0; i < _clothInstance.particles.Length; i++)
            {
                for (int j = 0; j < _clothInstance.particles[i].Length; j++)
                {
                    var p = _clothInstance.particles[i][j];
                    _clothInstance.particles[i][j] = KeepInsideScreen(p);
                }
            }
        }
    }

    private void SatisfyBuildableConstraints(int iterations)
    {
        if (_activeMesh == null)
            return;

        for (int it = 0; it < iterations; it++)
        {
            foreach (var s in _activeMesh.Sticks.Values)
            {
                if (s == null)
                    continue;

                var p1 = s.P1;
                var p2 = s.P2;
                Vector2 delta = p2.Position - p1.Position;
                float len = delta.Length();
                if (len <= 1e-6f)
                    continue;
                float diff = (len - s.Length) / len;
                Vector2 correction = delta * 0.5f * diff;

                bool p1Pinned = p1.IsPinned;
                bool p2Pinned = p2.IsPinned;

                if (!p1Pinned && !p2Pinned)
                {
                    p1.Position += correction;
                    p2.Position -= correction;
                }
                else if (!p1Pinned && p2Pinned)
                {
                    p1.Position += correction * 2f;
                }
                else if (p1Pinned && !p2Pinned)
                {
                    p2.Position -= correction * 2f;
                }
            }

            // Keep inside bounds
            foreach (var kvp in _activeMesh.Particles)
            {
                var id = kvp.Key;
                var p = kvp.Value;
                _activeMesh.Particles[id] = KeepInsideScreen(p);
            }
        }
    }
}
