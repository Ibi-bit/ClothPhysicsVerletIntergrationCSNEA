using BenchmarkDotNet.Running;
using PhysicsCSAlevlProject;

namespace PhysicsCSAlevlProject
{
    class Program
    {
        static void Main(string[] args)
        {
            if (args.Length > 0 && args[0] == "--benchmark")
            {
                PhysicsBenchmarkRunner.RunBenchmarks();
            }
            else
            {
                using var game = new PhysicsCSAlevlProject.Game1();
                game.Run();
            }
        }
    }
}
