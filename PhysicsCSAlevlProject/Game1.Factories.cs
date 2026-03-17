using System;
using System.Collections.Generic;

namespace PhysicsCSAlevlProject;

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

public sealed class TireFactory : Factory
{
    public TireFactory(Action<object[]> method)
        : base(
            "Create Hub Spoke Tire",
            method,
            new Dictionary<string, string>
            {
                { "Tire Center X", "0" },
                { "Tire Center Y", "0" },
                { "Inner Radius", "20" },
                { "Outer Radius", "40" },
                { "Segments", "16" },
            }
        ) { }
}
