using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    private void ProcessDebugCommands()
    {
        if (_logger.HasPendingCommands)
        {
            _logger.TryDequeueCommand(out var command);
            switch (command.CommandPath[0])
            {
                case "Particle":
                    ProccessParticleCommands(command);
                    break;
                case "Stick":
                    ProccessStickCommands(command);
                    break;
                case "Get":
                    ProccessGetCommands(command);
                    break;
                default:
                    _logger.AddLog(
                        $"Unknown command path: {string.Join(".", command.CommandPath)}",
                        ImGuiLogger.LogTypes.Error
                    );
                    break;
            }
        }
    }

    private void ProccessParticleCommands(ImGuiLogger.Command command)
    {
        if (command.ExCommand == "Add")
        {
            if (command.Parameters.Length < 3)
            {
                _logger.AddLog(
                    $"Not enough parameters for Particle.Add(x, y, mass) Expected 3, got {command.Parameters.Length}",
                    ImGuiLogger.LogTypes.Error
                );
                return;
            }

            if (
                float.TryParse(command.Parameters[0], out float x)
                && float.TryParse(command.Parameters[1], out float y)
                && float.TryParse(command.Parameters[2], out float mass)
            )
            {
                int id = _activeMesh.AddParticle(new Vector2(x, y), mass, false, Color.White);
                _logger.AddLog($"Added particle at ({x}, {y}) with mass {mass} (ID {id})");
            }
            else
            {
                _logger.AddLog(
                    $"Invalid parameters for Particle.Add Could not parse floats from '{command.Parameters[0]}', '{command.Parameters[1]}', or '{command.Parameters[2]}'",
                    ImGuiLogger.LogTypes.Error
                );
            }
        }
    }

    private void ProccessStickCommands(ImGuiLogger.Command command)
    {
        if (command.ExCommand == "Add")
        {
            if (command.Parameters.Length < 2)
            {
                _logger.AddLog(
                    $"Not enough parameters for Stick.Add(id1, id2) Expected 2, got {command.Parameters.Length}",
                    ImGuiLogger.LogTypes.Error
                );
                return;
            }

            if (
                int.TryParse(command.Parameters[0], out int p1Id)
                && int.TryParse(command.Parameters[1], out int p2Id)
            )
            {
                if (
                    _activeMesh.Particles.ContainsKey(p1Id)
                    && _activeMesh.Particles.ContainsKey(p2Id)
                )
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
                    $"Invalid parameters for Stick.Add Could not parse floats from '{command.Parameters[0]}' and '{command.Parameters[1]}'",
                    ImGuiLogger.LogTypes.Error
                );
            }
        }
        else
        {
            _logger.AddLog(
                $"Unknown command: Stick.{command.ExCommand}",
                ImGuiLogger.LogTypes.Error
            );
        }
    }

    private void ProccessGetCommands(ImGuiLogger.Command command)
    {
        
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

    // Command history
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
        }
    }

    public void AddCommand(string command)
    {
        // Example: mesh.particle.add(100, 200)

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
                // Add to history (avoid duplicates at the end)
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
}
