using System;
using System.Collections.Generic;

namespace PhysicsCSAlevlProject;

/// <summary>
/// Base class for factories that create different types of structures in the mesh. Each factory has a name, a method to execute when the factory is invoked, and a dictionary of parameters that can be configured by the user. The Factory class serves as a template for specific factories like TireFactory and ClothFactory, which define their own parameters and methods for creating specific structures in the mesh. This design allows for easy extension by simply creating new factory classes that inherit from Factory and implement their own creation logic and parameters. The factories can then be used in the UI to allow users to quickly create complex structures with configurable parameters without needing to write custom code for each structure type.
/// </summary>
public class Factory
{
    public Action<object[]> Method { get; }
    public string Name { get; }
    public Dictionary<string, string> Parameters { get; }

    public Factory(string name, Action<object[]> method, Dictionary<string, string> parameters)
    {
        Name = name;
        Method = method;
        Parameters = parameters;
    }
}

/// <summary>
/// Factory for creating a hub-and-spoke tire structure in the mesh.
/// </summary>
public sealed class TireFactory : Factory
{
    public TireFactory(Action<object[]> method)
        : base(
            "Create Hub Spoke Tire",
            method,
            new Dictionary<string, string>
            {
                { "Tire Center X", "200" },
                { "Tire Center Y", "200" },
                { "Inner Radius", "20" },
                { "Outer Radius", "40" },
                { "Segments", "16" },
            }
        ) { }
}

/// <summary>
/// /// Factory for creating a rectangular cloth structure in the mesh.
/// </summary>
public sealed class ClothFactory : Factory
{
    public ClothFactory(Action<object[]> method)
        : base(
            "Create Cloth",
            method,
            new Dictionary<string, string>
            {
                { "Top Left X", "100" },
                { "Top Left Y", "100" },
                { "Bottom Right X", "200" },
                { "Bottom Right Y", "200" },
                { "Natural Length", "10" },
            }
        ) { }
}
