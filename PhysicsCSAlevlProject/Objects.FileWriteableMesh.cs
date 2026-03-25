using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace PhysicsCSAlevlProject;

class FileWriteableMesh
{
    public class particleData
    {
        public Vector2 Position;
        public float Mass;
        public bool IsPinned;
    }

    public class OscillatingParticleData : particleData
    {
        public float OscillationAmplitude;
        public float OscillationFrequency;
        public float OscillationAngle;
    }

    public class stickData
    {
        public int P1Id;
        public int P2Id;
    }

    public List<particleData> Particles = new List<particleData>();

    public List<stickData> Sticks = new List<stickData>();
    public List<Collider> Colliders = new();

    public float SpringConstant = 10000f;
    public float Drag = 0.997f;
    public float Mass = 1f;

    public FileWriteableMesh() { }

    public FileWriteableMesh(Mesh mesh)
    {
        SpringConstant = mesh.springConstant;
        Drag = mesh.drag;
        Mass = mesh.mass;

        var particleIdMap = new Dictionary<int, int>();
        foreach (var kvp in mesh.Particles)
        {
            var p = kvp.Value;
            particleIdMap[kvp.Key] = Particles.Count;
            if (p is OscillatingParticle op)
            {
                Particles.Add(
                    new OscillatingParticleData
                    {
                        Position = p.Position,
                        Mass = p.Mass,
                        IsPinned = p.IsPinned,
                        OscillationAmplitude = op.OscillationAmplitude,
                        OscillationFrequency = op.OscillationFrequency,
                        OscillationAngle = op.OscillationAngle,
                    }
                );
                continue;
            }
            Particles.Add(
                new particleData
                {
                    Position = p.Position,
                    Mass = p.Mass,
                    IsPinned = p.IsPinned,
                }
            );
        }
        foreach (var kvp in mesh.Sticks)
        {
            var s = kvp.Value;
            Sticks.Add(
                new stickData { P1Id = particleIdMap[s.P1Id], P2Id = particleIdMap[s.P2Id] }
            );
        }
        foreach (var c in mesh.Colliders)
        {
            Colliders.Add(c.DeepCopy());
        }
    }

    public Mesh ToMesh()
    {
        var mesh = new Mesh();

        mesh.springConstant = SpringConstant;
        mesh.drag = Drag;
        mesh.mass = Mass;

        var indexToParticleId = new Dictionary<int, int>();

        if (Particles != null)
        {
            for (int i = 0; i < Particles.Count; i++)
            {
                var pData = Particles[i];
                if (pData is OscillatingParticleData opData)
                {
                    int particleId = mesh.AddParticle(
                        opData.Position,
                        opData.Mass,
                        opData.IsPinned,
                        Color.White,
                        null,
                        new OscillatingParticle(
                            opData.Position,
                            opData.Mass,
                            opData.IsPinned,
                            Color.White,
                            opData.OscillationAmplitude,
                            opData.OscillationFrequency,
                            opData.OscillationAngle
                        )
                    );
                    indexToParticleId[i] = particleId;
                    continue;
                }

                int Id = mesh.AddParticle(pData.Position, pData.Mass, pData.IsPinned, Color.White);
                indexToParticleId[i] = Id;
            }
        }

        if (Sticks != null)
        {
            foreach (var sData in Sticks)
            {
                if (
                    indexToParticleId.TryGetValue(sData.P1Id, out int p1Id)
                    && indexToParticleId.TryGetValue(sData.P2Id, out int p2Id)
                )
                {
                    mesh.AddStick(p1Id, p2Id, Color.White);
                }
            }
        }
        if (Colliders != null)
        {
            foreach (var c in Colliders)
            {
                mesh.Colliders.Add(c.DeepCopy());
            }
        }

        return mesh;
    }
}
