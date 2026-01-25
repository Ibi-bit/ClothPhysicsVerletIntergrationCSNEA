using System.Text;
using Microsoft.Build.Locator;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.MSBuild;

namespace CallGraphTool;

internal static class Program
{
    private sealed record Options(string TargetPath, string OutputPath, bool IncludeFramework);

    private static async Task<int> Main(string[] args)
    {
        var options = ParseOptions(args);
        if (options == null)
        {
            PrintUsage();
            return 1;
        }

        MSBuildLocator.RegisterDefaults();
        using var workspace = MSBuildWorkspace.Create();
        workspace.WorkspaceFailed += (_, e) =>
            Console.Error.WriteLine($"[workspace] {e.Diagnostic}");

        var projects = await LoadProjectsAsync(workspace, options.TargetPath);
        if (projects.Count == 0)
        {
            Console.Error.WriteLine("No projects loaded.");
            return 1;
        }

        var nodes = new HashSet<string>(StringComparer.Ordinal);
        var edges = new HashSet<(string Caller, string Callee)>(new EdgeComparer());

        foreach (var project in projects)
        {
            var compilation = await project.GetCompilationAsync();
            if (compilation == null)
            {
                Console.Error.WriteLine($"Skipping project {project.Name}: no compilation.");
                continue;
            }

            foreach (
                var document in project.Documents.Where(d =>
                    d.SourceCodeKind == SourceCodeKind.Regular
                )
            )
            {
                var tree = await document.GetSyntaxTreeAsync();
                if (tree == null)
                    continue;

                var model = compilation.GetSemanticModel(tree);
                var root = await tree.GetRootAsync();

                var methods = root.DescendantNodes().OfType<BaseMethodDeclarationSyntax>();
                foreach (var method in methods)
                {
                    if (model.GetDeclaredSymbol(method) is not IMethodSymbol callerSymbol)
                        continue;
                    if (!options.IncludeFramework && !IsInSource(callerSymbol))
                        continue;

                    var callerName = FormatSymbol(callerSymbol);
                    nodes.Add(callerName);

                    foreach (var callee in CollectCallees(method, model))
                    {
                        if (!options.IncludeFramework && !IsInSource(callee))
                            continue;
                        var calleeName = FormatSymbol(callee);
                        nodes.Add(calleeName);
                        edges.Add((callerName, calleeName));
                    }
                }

                var localFunctions = root.DescendantNodes().OfType<LocalFunctionStatementSyntax>();
                foreach (var local in localFunctions)
                {
                    if (model.GetDeclaredSymbol(local) is not IMethodSymbol callerSymbol)
                        continue;
                    if (!options.IncludeFramework && !IsInSource(callerSymbol))
                        continue;

                    var callerName = FormatSymbol(callerSymbol);
                    nodes.Add(callerName);

                    foreach (var callee in CollectCallees(local, model))
                    {
                        if (!options.IncludeFramework && !IsInSource(callee))
                            continue;
                        var calleeName = FormatSymbol(callee);
                        nodes.Add(calleeName);
                        edges.Add((callerName, calleeName));
                    }
                }
            }
        }

        WriteDot(options.OutputPath, nodes, edges);
        Console.WriteLine(
            $"Wrote {options.OutputPath} ({nodes.Count} nodes, {edges.Count} edges)."
        );
        return 0;
    }

    private static Options? ParseOptions(IReadOnlyList<string> args)
    {
        if (args.Count == 0)
            return null;

        string? target = null;
        string output = Path.GetFullPath("callgraph.dot");
        bool includeFramework = false;

        for (int i = 0; i < args.Count; i++)
        {
            switch (args[i])
            {
                case "--output" when i + 1 < args.Count:
                    output = Path.GetFullPath(args[++i]);
                    break;
                case "--include-framework":
                    includeFramework = true;
                    break;
                default:
                    if (target == null)
                    {
                        target = Path.GetFullPath(args[i]);
                    }
                    else
                    {
                        return null;
                    }
                    break;
            }
        }

        if (target == null)
            return null;
        return new Options(target, output, includeFramework);
    }

    private static void PrintUsage()
    {
        Console.Error.WriteLine(
            "Usage: dotnet run --project CallGraphTool/CallGraphTool.csproj <solution|project> [--output <path>] [--include-framework]"
        );
    }

    private static async Task<IReadOnlyList<Project>> LoadProjectsAsync(
        MSBuildWorkspace workspace,
        string targetPath
    )
    {
        var extension = Path.GetExtension(targetPath);
        if (extension.Equals(".sln", StringComparison.OrdinalIgnoreCase))
        {
            var solution = await workspace.OpenSolutionAsync(targetPath);
            return solution.Projects.ToList();
        }

        if (extension.Equals(".csproj", StringComparison.OrdinalIgnoreCase))
        {
            var project = await workspace.OpenProjectAsync(targetPath);
            return new[] { project };
        }

        Console.Error.WriteLine("Target must be a .sln or .csproj file.");
        return Array.Empty<Project>();
    }

    private static IEnumerable<IMethodSymbol> CollectCallees(
        SyntaxNode callerNode,
        SemanticModel model
    )
    {
        var interesting = callerNode
            .DescendantNodes()
            .Where(n =>
                n
                    is InvocationExpressionSyntax
                        or ObjectCreationExpressionSyntax
                        or ImplicitObjectCreationExpressionSyntax
                        or ConstructorInitializerSyntax
                        or MemberAccessExpressionSyntax
                        or IdentifierNameSyntax
            );

        foreach (var node in interesting)
        {
            var target = GetMethodTarget(model, node);
            if (target != null)
                yield return target;
        }
    }

    private static IMethodSymbol? GetMethodTarget(SemanticModel model, SyntaxNode node)
    {
        var info = model.GetSymbolInfo(node);
        var symbol = info.Symbol ?? info.CandidateSymbols.FirstOrDefault();
        if (symbol == null)
            return null;

        return symbol switch
        {
            IMethodSymbol m => m.ReducedFrom ?? m,
            IPropertySymbol p when IsSetterContext(node) && p.SetMethod != null => p.SetMethod,
            IPropertySymbol p when p.GetMethod != null => p.GetMethod,
            IEventSymbol e when IsAddAccessor(node) && e.AddMethod != null => e.AddMethod,
            IEventSymbol e when IsRemoveAccessor(node) && e.RemoveMethod != null => e.RemoveMethod,
            _ => null,
        };
    }

    private static bool IsSetterContext(SyntaxNode node)
    {
        if (node.Parent is AssignmentExpressionSyntax assignment && assignment.Left == node)
        {
            return true;
        }

        return false;
    }

    private static bool IsAddAccessor(SyntaxNode node)
    {
        return node.Parent is AssignmentExpressionSyntax assignment
            && assignment.IsKind(SyntaxKind.AddAssignmentExpression)
            && assignment.Left == node;
    }

    private static bool IsRemoveAccessor(SyntaxNode node)
    {
        return node.Parent is AssignmentExpressionSyntax assignment
            && assignment.IsKind(SyntaxKind.SubtractAssignmentExpression)
            && assignment.Left == node;
    }

    private static bool IsInSource(ISymbol symbol) => symbol.Locations.Any(l => l.IsInSource);

    private static string FormatSymbol(IMethodSymbol symbol)
    {
        var text = symbol.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat);
        return text.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", " ");
    }

    private static void WriteDot(
        string outputPath,
        IEnumerable<string> nodes,
        IEnumerable<(string Caller, string Callee)> edges
    )
    {
        var sb = new StringBuilder();
        sb.AppendLine("digraph CallGraph {");
        sb.AppendLine("  rankdir=LR;");
        sb.AppendLine("  node [shape=box, style=rounded];");

        foreach (var node in nodes.OrderBy(n => n))
        {
            sb.Append("  \"").Append(node).AppendLine("\"");
        }

        foreach (var edge in edges.OrderBy(e => e.Caller).ThenBy(e => e.Callee))
        {
            sb.Append("  \"")
                .Append(edge.Caller)
                .Append("\" -> \"")
                .Append(edge.Callee)
                .AppendLine("\"");
        }

        sb.AppendLine("}");
        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(outputPath, sb.ToString());
    }

    private sealed class EdgeComparer : IEqualityComparer<(string Caller, string Callee)>
    {
        public bool Equals((string Caller, string Callee) x, (string Caller, string Callee) y)
        {
            return StringComparer.Ordinal.Equals(x.Caller, y.Caller)
                && StringComparer.Ordinal.Equals(x.Callee, y.Callee);
        }

        public int GetHashCode((string Caller, string Callee) obj)
        {
            return HashCode.Combine(
                StringComparer.Ordinal.GetHashCode(obj.Caller),
                StringComparer.Ordinal.GetHashCode(obj.Callee)
            );
        }
    }
}
