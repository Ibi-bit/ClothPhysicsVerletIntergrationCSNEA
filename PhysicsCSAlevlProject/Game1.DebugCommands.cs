using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace PhysicsCSAlevlProject;


class ConsoleCommandAttribute : Attribute
{
    public string CommandPath { get; }

    public ConsoleCommandAttribute(string commandPath)
    {
        CommandPath = commandPath;
    }
}

class CommandRegistry
{
    private readonly ImGuiLogger _logger;
    private readonly Dictionary<
        string,
        (object instance, MethodInfo method, string commandPath)
    > _commands = new();

    public CommandRegistry(ImGuiLogger logger)
    {
        _logger = logger;
    }

    public void RegisterType(object instance, Type type, bool logRegistration = true)
    {
        var methods = type.GetMethods(
            BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.DeclaredOnly
        );

        foreach (var method in methods)
        {
            var attr = method.GetCustomAttribute<ConsoleCommandAttribute>();
            if (attr != null)
            {
                string commandKey = attr.CommandPath.ToLower();
                _commands[commandKey] = (instance, method, attr.CommandPath);
                if (logRegistration)
                {
                    _logger.AddLog($"Registered command: {attr.CommandPath}");
                }
            }
        }
    }

    public IEnumerable<string> GetRegisteredCommandPaths()
    {
        return _commands
            .Select(entry => entry.Value.commandPath)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(commandPath => commandPath, StringComparer.OrdinalIgnoreCase);
    }

    public bool TryInvokeCommand(ImGuiLogger.Command command)
    {
        string fullPath = string.Join(".", command.CommandPath) + "." + command.ExCommand;
        string key = fullPath.ToLower();

        if (_commands.TryGetValue(key, out var entry))
        {
            try
            {
                var (instance, method, _) = entry;
                object[] parameters = [command.Parameters];
                method.Invoke(instance, parameters);
                return true;
            }
            catch (Exception ex)
            {
                _logger.AddLog(
                    $"Error invoking command {fullPath}: {ex.InnerException?.Message ?? ex.Message}",
                    ImGuiLogger.LogTypes.Error
                );
                return true;
            }
        }

        return false;
    }
}

public partial class Game1
{
    private CommandRegistry _commandRegistry;
    private Mesh _commandRegistryMeshBinding;

    private void RefreshMeshCommandBindings()
    {
        if (_commandRegistry == null || _activeMesh == null)
        {
            return;
        }

        if (!ReferenceEquals(_commandRegistryMeshBinding, _activeMesh))
        {
            _commandRegistry.RegisterType(_activeMesh, typeof(Mesh), false);
            _commandRegistryMeshBinding = _activeMesh;
        }
    }

    private void ProcessDebugCommands()
    {
        RefreshMeshCommandBindings();

        if (_logger.HasPendingCommands)
        {
            _logger.TryDequeueCommand(out var command);

            if (command.CommandPath[0] == "Exit")
            {
                Exit();
                return;
            }

            if (!_commandRegistry.TryInvokeCommand(command))
            {
                _logger.AddLog(
                    $"Unknown command: {string.Join(".", command.CommandPath)}.{command.ExCommand}",
                    ImGuiLogger.LogTypes.Error
                );
            }
        }
    }

    [ConsoleCommand("Commands.List")]
    private void ListCommands(string[] parameters)
    {
        if (_commandRegistry == null)
        {
            _logger.AddLog("Command registry is not ready yet.", ImGuiLogger.LogTypes.Warning);
            return;
        }

        var commands = _commandRegistry.GetRegisteredCommandPaths().ToList();
        _logger.AddLog($"Available commands ({commands.Count}):", ImGuiLogger.LogTypes.Info);

        foreach (var command in commands)
        {
            _logger.AddLog($"- {command}", ImGuiLogger.LogTypes.Info);
        }

        if (parameters.Length > 0)
        {
            _logger.AddLog("Commands.List does not take parameters.", ImGuiLogger.LogTypes.Warning);
        }
    }

    [ConsoleCommand("Mesh.AddTire")]
    private void AddTire(string[] parameters)
    {
        if (parameters.Length < 5)
        {
            _logger.AddLog(
                $"Not enough parameters for Mesh.AddTire(centerX, centerY, OuterStickCount, radius, spokeLength) Expected 5, got {parameters.Length}",
                ImGuiLogger.LogTypes.Error
            );
            return;
        }

        try
        {
            _activeMesh.CreateHubSpokeTire([
                parameters[0],
                parameters[1],
                parameters[2],
                parameters[3],
                parameters[4],
            ]);

            _logger.AddLog(
                $"Added tire to active mesh at ({parameters[0]}, {parameters[1]}) with outer radius {parameters[3]} and inner radius {parameters[4]} and {parameters[2]} outer sticks"
            );
        }
        catch (Exception ex)
        {
            _logger.AddLog(
                $"Invalid parameters for Mesh.AddTire Could not parse floats/ints from '{parameters[0]}', '{parameters[1]}', '{parameters[2]}', '{parameters[3]}', or '{parameters[4]}' - {ex.Message}",
                ImGuiLogger.LogTypes.Error
            );
        }
    }

    [ConsoleCommand("Particle.Add")]
    private void AddParticle(string[] parameters)
    {
        if (parameters.Length < 3)
        {
            _logger.AddLog(
                $"Not enough parameters for Particle.Add(x, y, mass) Expected 3, got {parameters.Length}",
                ImGuiLogger.LogTypes.Error
            );
            return;
        }

        if (
            float.TryParse(parameters[0], out float x)
            && float.TryParse(parameters[1], out float y)
            && float.TryParse(parameters[2], out float mass)
        )
        {
            int id = _activeMesh.AddParticle(new Vector2(x, y), mass, false, Color.White);
            _logger.AddLog($"Added particle at ({x}, {y}) with mass {mass} (ID {id})");
        }
        else
        {
            _logger.AddLog(
                $"Invalid parameters for Particle.Add Could not parse floats from '{parameters[0]}', '{parameters[1]}', or '{parameters[2]}'",
                ImGuiLogger.LogTypes.Error
            );
        }
    }

    [ConsoleCommand("Stick.Add")]
    private void AddStick(string[] parameters)
    {
        if (parameters.Length < 2)
        {
            _logger.AddLog(
                $"Not enough parameters for Stick.Add(id1, id2) Expected 2, got {parameters.Length}",
                ImGuiLogger.LogTypes.Error
            );
            return;
        }

        if (int.TryParse(parameters[0], out int p1Id) && int.TryParse(parameters[1], out int p2Id))
        {
            if (_activeMesh.Particles.ContainsKey(p1Id) && _activeMesh.Particles.ContainsKey(p2Id))
            {
                _activeMesh.AddStickBetween(p1Id, p2Id);
                _logger.AddLog($"Added stick between particles {p1Id} and {p2Id}");
            }
            else
            {
                _logger.AddLog(
                    $"One or both particle IDs not found: {p1Id}, {p2Id}",
                    ImGuiLogger.LogTypes.Error
                );
            }
        }
        else
        {
            _logger.AddLog(
                $"Invalid parameters for Stick.Add Could not parse floats from '{parameters[0]}' and '{parameters[1]}'",
                ImGuiLogger.LogTypes.Error
            );
        }
    }

    [ConsoleCommand("Database.TestConnection")]
    private void TestConnection(string[] parameters)
    {
        if (parameters.Length > 0)
        {
            _logger.AddLog(
                "Database.TestConnection does not take parameters.",
                ImGuiLogger.LogTypes.Warning
            );
        }

        try
        {
            _database.TestConnection();
            _logger.AddLog("Database connection successful");
        }
        catch (Exception ex)
        {
            _logger.AddLog(
                $"Database connection failed: {ex.Message}",
                ImGuiLogger.LogTypes.Error
            );
        }
    }
}

public class ImGuiLogger
{
    public enum LogTypes
    {
        Info,
        Warning,
        Error,
    };

    class MessageLog
    {
        public string Message;
        public int Count;
        public LogTypes Type;
    }

    public class Command
    {
        public string[] CommandPath;
        public string ExCommand;
        public string[] Parameters;
    }

    readonly Queue<MessageLog> _logs = new();
    readonly Queue<Command> _commands = new();
    private string _commandInput = "";
    private List<string> _logStringHistory = new();
    private readonly Dictionary<string, Func<string>> _envVars = new();

    public void RegisterEnvVar(string name, Func<string> resolver)
    {
        _envVars[name] = resolver;
    }

    private string ResolveEnvVars(string command)
    {
        return Regex.Replace(
            command,
            @"\$(\w+)",
            match =>
            {
                string varName = match.Groups[1].Value;
                if (_envVars.TryGetValue(varName, out var resolver))
                    return resolver();
                return match.Value;
            }
        );
    }

    private readonly List<string> _commandHistory = new();
    private int _historyIndex = -1;
    private string _savedInput = "";

    public bool autoScrollLogs = true;
    public bool wrapText = true;
    public bool supportDebugConsole = true;

    // public bool clearLogs = false;
    readonly Dictionary<LogTypes, bool> _logTypeVisibility = new Dictionary<LogTypes, bool>
    {
        { LogTypes.Info, true },
        { LogTypes.Warning, true },
        { LogTypes.Error, true },
    };

    public void AddLog(string message, LogTypes type = LogTypes.Info)
    {
        var lastLog = _logs.LastOrDefault();
        if (lastLog != null && lastLog.Message == message && lastLog.Type == type)
        {
            lastLog.Count++;
        }
        else
        {
            _logs.Enqueue(
                new MessageLog
                {
                    Message = message,
                    Count = 1,
                    Type = type,
                }
            );
            var log = _logs.Peek();
            string displayMessage = log.Count > 1 ? $"{log.Message} (x{log.Count})" : log.Message;
            _logStringHistory.Add(displayMessage);
        }
    }

    public void AddCommand(string command)
    {
        command = command?.Trim();
        if (string.IsNullOrWhiteSpace(command))
        {
            return;
        }

        command = ResolveEnvVars(command);

        string commandWithoutParams = command.Split('(')[0];
        string[] pathParts = commandWithoutParams.Split('.');

        if (pathParts.Length == 0)
            return;

        string[] parameters = Array.Empty<string>();
        if (command.Contains('(') && command.Contains(')'))
        {
            int start = command.IndexOf('(') + 1;
            int end = command.LastIndexOf(')');
            if (end > start)
            {
                string paramStr = command.Substring(start, end - start);
                parameters = paramStr.Split(
                    ',',
                    StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries
                );
            }
        }

        string exCommand = pathParts[^1];
        string[] commandPath = pathParts.Length > 1 ? pathParts[..^1] : Array.Empty<string>();

        _commands.Enqueue(
            new Command
            {
                CommandPath = commandPath,
                ExCommand = exCommand,
                Parameters = parameters,
            }
        );

        AddLog(
            $"Command queued: {string.Join(".", commandPath)}.{exCommand}({string.Join(", ", parameters)})"
        );
    }

    public bool TryDequeueCommand(out Command command)
    {
        return _commands.TryDequeue(out command);
    }

    public bool HasPendingCommands => _commands.Count > 0;

    public void DrawLogs(ref bool openWindow)
    {
        if (!ImGui.Begin("Logs", ref openWindow))
        {
            ImGui.End();
            return;
        }
        bool infoVisible = _logTypeVisibility[LogTypes.Info];
        if (ImGui.Checkbox("Info", ref infoVisible))
            _logTypeVisibility[LogTypes.Info] = infoVisible;
        ImGui.SameLine();
        bool errorVisible = _logTypeVisibility[LogTypes.Error];
        if (ImGui.Checkbox("Error", ref errorVisible))
            _logTypeVisibility[LogTypes.Error] = errorVisible;
        ImGui.SameLine();
        bool warningVisible = _logTypeVisibility[LogTypes.Warning];
        if (ImGui.Checkbox("Warning", ref warningVisible))
            _logTypeVisibility[LogTypes.Warning] = warningVisible;

        ImGui.Separator();
        ImGui.Checkbox("Auto Scroll Logs", ref autoScrollLogs);

        ImGui.Checkbox("Wrap Text", ref wrapText);

        ImGui.SeparatorText("Logs");
        if (supportDebugConsole)
        {
            ImGui.Separator();
            ImGui.Text("Debug Console (Up/Down for history):");
            ImGui.SetNextItemWidth(ImGui.GetContentRegionAvail().X - 80);

            if (ImGui.IsKeyPressed(ImGuiKey.UpArrow) && _commandHistory.Count > 0)
            {
                if (_historyIndex == -1)
                {
                    _savedInput = _commandInput;
                    _historyIndex = _commandHistory.Count - 1;
                }
                else if (_historyIndex > 0)
                {
                    _historyIndex--;
                }
                _commandInput = _commandHistory[_historyIndex];
            }
            else if (ImGui.IsKeyPressed(ImGuiKey.DownArrow))
            {
                if (_historyIndex >= 0)
                {
                    _historyIndex++;
                    if (_historyIndex >= _commandHistory.Count)
                    {
                        _historyIndex = -1;
                        _commandInput = _savedInput;
                    }
                    else
                    {
                        _commandInput = _commandHistory[_historyIndex];
                    }
                }
            }

            bool enterPressed = ImGui.InputText(
                "##Command",
                ref _commandInput,
                256,
                ImGuiInputTextFlags.EnterReturnsTrue
            );
            ImGui.SameLine();
            if (
                (ImGui.Button("Execute") || enterPressed)
                && !string.IsNullOrWhiteSpace(_commandInput)
            )
            {
                if (_commandHistory.Count == 0 || _commandHistory[^1] != _commandInput)
                {
                    _commandHistory.Add(_commandInput);
                }
                _historyIndex = -1;
                _savedInput = "";

                AddCommand(_commandInput);
                _commandInput = "";
            }
        }
        ImGui.BeginChild("LogScrollArea", new System.Numerics.Vector2(0, 0));
        if (wrapText)
            ImGui.PushTextWrapPos(0.0f);
        foreach (var log in _logs)
        {
            string displayMessage = log.Count > 1 ? $"{log.Message} (x{log.Count})" : log.Message;
            _logStringHistory.Add(displayMessage);
            var color = log.Type switch
            {
                LogTypes.Info => new System.Numerics.Vector4(1f, 1f, 1f, 1f),
                LogTypes.Warning => new System.Numerics.Vector4(1f, 1f, 0f, 1f),
                LogTypes.Error => new System.Numerics.Vector4(1f, 0f, 0f, 1f),
                _ => new System.Numerics.Vector4(1f, 1f, 1f, 1f),
            };
            if (_logTypeVisibility[log.Type])
                ImGui.TextColored(color, displayMessage);
        }
        if (wrapText)
            ImGui.PopTextWrapPos();
        if (autoScrollLogs)
        {
            ImGui.SetScrollHereY(1.0f);
        }
        ImGui.EndChild();

        ImGui.End();
    }

    public string GetLogsAsString()
    {
        string historyString = string.Join(Environment.NewLine, _logStringHistory);
        return historyString;
    }

    public void SaveLogsToFile(string filePath)
    {
        try
        {
            System.IO.File.WriteAllText(filePath, GetLogsAsString());
            AddLog($"Logs saved to {filePath}");
        }
        catch (Exception ex)
        {
            AddLog($"Failed to save logs to {filePath}: {ex.Message}", LogTypes.Error);
        }
    }
}
