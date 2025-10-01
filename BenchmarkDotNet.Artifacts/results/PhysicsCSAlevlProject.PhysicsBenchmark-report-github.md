```

BenchmarkDotNet v0.15.4, macOS 26.1 (25B5042k) [Darwin 25.1.0]
Apple M1, 1 CPU, 8 logical and 8 physical cores
.NET SDK 8.0.120
  [Host]   : .NET 8.0.20 (8.0.20, 8.0.2025.41914), Arm64 RyuJIT armv8.0-a
  .NET 8.0 : .NET 8.0.20 (8.0.20, 8.0.2025.41914), Arm64 RyuJIT armv8.0-a

Job=.NET 8.0  Runtime=.NET 8.0  

```
| Method                         | Mean       | Error    | StdDev   | Allocated |
|------------------------------- |-----------:|---------:|---------:|----------:|
| SinglePhysicsStep              | 4,033.7 ns | 60.95 ns | 47.58 ns |         - |
| ClearForces                    |   194.3 ns |  2.35 ns |  1.96 ns |         - |
| CalculateHorizontalStickForces |   962.3 ns | 11.30 ns | 10.57 ns |         - |
| CalculateVerticalStickForces   |   945.6 ns | 16.19 ns | 13.52 ns |         - |
| UpdateAllParticles             | 1,877.7 ns | 23.15 ns | 18.07 ns |         - |
