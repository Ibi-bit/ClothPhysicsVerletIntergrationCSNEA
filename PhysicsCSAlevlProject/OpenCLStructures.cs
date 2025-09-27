using System;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;

namespace PhysicsCSAlevlProject
{
    // GPU-friendly particle structure that matches the OpenCL kernel
    [StructLayout(LayoutKind.Sequential)]
    public struct GPUParticle
    {
        public Vector2 Position;         // Current position
        public Vector2 PreviousPosition; // Previous position
        public Vector2 AccumulatedForce; // Accumulated forces
        public float Mass;               // Particle mass
        public int IsPinned;             // Whether particle is pinned (0 or 1)

        public GPUParticle(DrawableParticle particle)
        {
            Position = particle.Position;
            PreviousPosition = particle.PreviousPosition;
            AccumulatedForce = particle.AccumulatedForce;
            Mass = particle.Mass;
            IsPinned = particle.IsPinned ? 1 : 0;
        }

        public DrawableParticle ToDrawableParticle(Color color)
        {
            var particle = new DrawableParticle(Position, PreviousPosition, Mass)
            {
                AccumulatedForce = AccumulatedForce,
                IsPinned = IsPinned == 1,
                Color = color
            };
            return particle;
        }
    }

    // GPU-friendly stick structure that matches the OpenCL kernel
    [StructLayout(LayoutKind.Sequential)]
    public struct GPUStick
    {
        public Vector2 P1Index;    // Index of first particle (i, j)
        public Vector2 P2Index;    // Index of second particle (i, j)
        public float Length;       // Rest length of stick
        public int IsValid;        // Whether this stick is valid (not null)

        public GPUStick(DrawableStick stick, int p1I, int p1J, int p2I, int p2J)
        {
            P1Index = new Vector2(p1I, p1J);
            P2Index = new Vector2(p2I, p2J);
            Length = stick?.Length ?? 0f;
            IsValid = stick != null ? 1 : 0;
        }
    }

    // Physics parameters structure for OpenCL
    [StructLayout(LayoutKind.Sequential)]
    public struct PhysicsParams
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 1024)]
        public Vector2[] DraggedParticleIndices; // Max 1024 dragged particles
        public int DraggedCount;                 // Number of dragged particles
        public Vector2 MousePosition;            // Current mouse position
        public Vector2 Gravity;                  // Gravity force
        public Vector2 WindForce;                // Wind force
        public float DeltaTime;                  // Time step
        public float Drag;                       // Drag coefficient
        public float ScreenWidth;                // Screen bounds
        public float ScreenHeight;

        public PhysicsParams(float deltaTime, Vector2 gravity, Vector2 windForce, float drag, 
                           float screenWidth, float screenHeight, Vector2 mousePos)
        {
            DraggedParticleIndices = new Vector2[1024];
            DraggedCount = 0;
            MousePosition = mousePos;
            Gravity = gravity;
            WindForce = windForce;
            DeltaTime = deltaTime;
            Drag = drag;
            ScreenWidth = screenWidth;
            ScreenHeight = screenHeight;
        }
    }

    // Stick parameters structure for OpenCL
    [StructLayout(LayoutKind.Sequential)]
    public struct StickParams
    {
        public float SpringConstant;  // Spring stiffness
        public int Width;            // Cloth width
        public int Height;           // Cloth height

        public StickParams(float springConstant, int width, int height)
        {
            SpringConstant = springConstant;
            Width = width;
            Height = height;
        }
    }
}