using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace PhysicsCSAlevlProject
{
    [MemoryDiagnoser]
    [SimpleJob(BenchmarkDotNet.Jobs.RuntimeMoniker.Net80)]
    [RPlotExporter]
    public class PhysicsBenchmark
    {
        private Cloth _cloth;
        private float fixedDeltaTime = 1f / 10000f;

        [GlobalSetup]
        public void Setup()
        {
            float naturalLength = 10f;
            float springConstant = 10000;
            float mass = 0.1f;

            int cols = (int)(200 / naturalLength);

            var pinnedParticles = new List<Vector2>(
                new Vector2[]
                {
                    new Vector2(220, 20),
                    new Vector2(220 + (cols - 1) * naturalLength, 20),
                }
            );

            _cloth = new Cloth(
                new Vector2(200, 200),
                pinnedParticles,
                naturalLength,
                springConstant,
                mass
            );
        }

        [Benchmark]
        public void SinglePhysicsStep()
        {
            for (int i = 0; i < _cloth.particles.Length; i++)
            {
                for (int j = 0; j < _cloth.particles[i].Length; j++)
                {
                    _cloth.particles[i][j].AccumulatedForce = Vector2.Zero;
                }
            }

            CalculateStickForces(_cloth.horizontalSticks);
            CalculateStickForces(_cloth.verticalSticks);
            UpdateParticles(fixedDeltaTime);
        }

        [Benchmark]
        public void ClearForces()
        {
            for (int i = 0; i < _cloth.particles.Length; i++)
            {
                for (int j = 0; j < _cloth.particles[i].Length; j++)
                {
                    _cloth.particles[i][j].AccumulatedForce = Vector2.Zero;
                }
            }
        }

        [Benchmark]
        public void CalculateHorizontalStickForces()
        {
            CalculateStickForces(_cloth.horizontalSticks);
        }

        [Benchmark]
        public void CalculateVerticalStickForces()
        {
            CalculateStickForces(_cloth.verticalSticks);
        }

        [Benchmark]
        public void UpdateAllParticles()
        {
            UpdateParticles(fixedDeltaTime);
        }

        private DrawableStick[][] CalculateStickForces(DrawableStick[][] sticks)
        {
            for (int i = 0; i < sticks.Length; i++)
            {
                for (int j = 0; j < sticks[i].Length; j++)
                {
                    if (sticks[i][j] != null)
                    {
                        DrawableStick s = sticks[i][j];
                        Vector2 stickVector = s.P1.Position - s.P2.Position;
                        float currentLength = stickVector.Length();

                        if (currentLength > 0)
                        {
                            Vector2 stickDir = stickVector / currentLength;
                            float stretch = currentLength - s.Length;

                            float springConstant = _cloth.springConstant;
                            Vector2 springForce = stickDir * stretch * springConstant;

                            s.P1.AccumulatedForce -= springForce;
                            s.P2.AccumulatedForce += springForce;
                        }
                    }
                }
            }
            return sticks;
        }

        private void UpdateParticles(float deltaTime)
        {
            for (int i = 0; i < _cloth.particles.Length; i++)
            {
                for (int j = 0; j < _cloth.particles[i].Length; j++)
                {
                    DrawableParticle p = _cloth.particles[i][j];

                    if (p.IsPinned)
                        continue;

                    Vector2 totalForce = new Vector2(0, 980f) + p.AccumulatedForce;
                    Vector2 acceleration = totalForce / p.Mass;

                    Vector2 velocity = p.Position - p.PreviousPosition;
                    velocity *= _cloth.drag;

                    Vector2 previousPosition = p.Position;
                    p.Position = p.Position + velocity + acceleration * (deltaTime * deltaTime);
                    p.PreviousPosition = previousPosition;

                    _cloth.particles[i][j] = p;
                }
            }
        }
    }

    public static class PhysicsBenchmarkRunner
    {
        public static void RunBenchmarks()
        {
            var summary = BenchmarkDotNet.Running.BenchmarkRunner.Run<PhysicsBenchmark>();
        }
    }
}