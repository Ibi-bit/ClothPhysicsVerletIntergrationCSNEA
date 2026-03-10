using System;
using PhysicsCSAlevlProject;

namespace PhysicsCSAlevlProject
{
    class Program
    {
        static void Main(string[] args)
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Console.WriteLine("=== UNHANDLED EXCEPTION ===");
                Console.WriteLine(e.ExceptionObject.ToString());
                Console.WriteLine("===========================");
            };
            
            try
            {
                using var game = new PhysicsCSAlevlProject.Game1();
                game.Run();
            }
            catch (Exception ex)
            {
                Console.WriteLine("=== CAUGHT EXCEPTION ===");
                Console.WriteLine(ex.ToString());
                Console.WriteLine("========================");
                throw;
            }
        }
    }
}
