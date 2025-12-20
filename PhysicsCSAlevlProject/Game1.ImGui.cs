using System;
using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    private bool _showPhysicsControlsWindow = false;
    private bool _showConfigurationWindow = false;
    private bool _showReadMeWindow = false;
    private ImGuiLogger _Logger = new ImGuiLogger();
    private bool _showLoggerWindow = false;

    bool ctrlHeld = false;
    bool shiftHeld = false;

    // Track CapsLock as a toggle for better macOS reliability
    bool capsActive = false;
    bool altHeld = false;

    const float ModeSwitchDisplayDuration = 1.5f;

    private void ImGuiDraw(GameTime gameTime)
    {
        _guiRenderer.BeginLayout(gameTime);
        ctrlHeld = ImGui.GetIO().KeyCtrl;
        shiftHeld = ImGui.GetIO().KeyShift;
        altHeld = ImGui.GetIO().KeyAlt;
        if (ImGui.IsKeyPressed(ImGuiKey.CapsLock))
        {
            capsActive = !capsActive;
        }

        DrawMainMenuBar();
        if (_showPhysicsControlsWindow)
        {
            DrawPhysicsControlsWindow();
        }

        if (_showConfigurationWindow)
        {
            DrawConfigurationWindow();
        }
        if (_showReadMeWindow)
        {
            DrawReadMeWindow();
        }
        if (_showLoggerWindow)
        {
            _Logger.DrawLogs(ref _showLoggerWindow);
        }

        ModeSwitchingImGui();

        _guiRenderer.EndLayout();
    }

    private void ModeSwitchingImGui()
    {
        bool isMac = System.OperatingSystem.IsMacOS();
        bool backwardModifierHeld = isMac ? altHeld : ctrlHeld;

        if (shiftHeld && ImGui.IsKeyPressed(ImGuiKey.Tab))
        {
            int delta = backwardModifierHeld ? -1 : 1;
            _modeIndex = (_modeIndex + delta + _modes.Length) % _modes.Length;
            ApplyModeIndex();
        }

        if (shiftHeld)
        {
            var drawList = ImGui.GetForegroundDrawList();
            drawList.AddText(
                new System.Numerics.Vector2(10, 30),
                ImGui.GetColorU32(ImGuiCol.Text),
                "Mode:"
            );
            for (int i = 0; i < _modes.Length; i++)
            {
                string text = i == _modeIndex ? $"> {_modes[i]} <" : $"  {_modes[i]}";
                uint color =
                    i == _modeIndex
                        ? ImGui.GetColorU32(new System.Numerics.Vector4(0.2f, 1f, 0.2f, 1f))
                        : ImGui.GetColorU32(ImGuiCol.Text);
                drawList.AddText(new System.Numerics.Vector2(10, 48 + i * 18), color, text);
            }
        }
    }

    private void ApplyModeIndex()
    {
        switch (_modeIndex)
        {
            case 0:
                _currentMode = MeshMode.Cloth;
                _activeMesh = _clothInstance;
                break;
            case 1:
                _currentMode = MeshMode.Buildable;
                _activeMesh = _buildableMeshInstance;
                break;
            case 2:
                _currentMode = MeshMode.PolygonBuilder;
                _activeMesh = _buildableMeshInstance;
                break;
        }

        leftPressed = false;
        windDirectionArrow = null;
        cutLine = null;
        particlesInDragArea.Clear();
        buildableMeshParticlesInDragArea.Clear();
    }

    private void DrawMainMenuBar()
    {
        if (!ImGui.BeginMainMenuBar())
        {
            return;
        }

        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("New")) { }
            if (ImGui.MenuItem("Open")) { }
            if (ImGui.MenuItem("Save")) { }
            ImGui.Separator();
            if (ImGui.MenuItem("Exit"))
            {
                Exit();
            }
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("View"))
        {
            if (ImGui.MenuItem("Reset Camera")) { }
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Edit"))
        {
            if (ImGui.MenuItem("Undo")) { }
            if (ImGui.MenuItem("Redo")) { }
            ImGui.EndMenu();
        }
        if (ImGui.BeginMenu("Mode"))
        {
            if (ImGui.MenuItem("Cloth", null, _currentMode == MeshMode.Cloth))
            {
                _modeIndex = 0;
                ApplyModeIndex();
            }
            if (ImGui.MenuItem("Buildable", null, _currentMode == MeshMode.Buildable))
            {
                _modeIndex = 1;
                ApplyModeIndex();
            }
            if (ImGui.MenuItem("Polygon Builder", null, _currentMode == MeshMode.PolygonBuilder))
            {
                _modeIndex = 2;
                ApplyModeIndex();
            }
            ImGui.EndMenu();
        }
        if (ImGui.BeginMenu("Quick Settings"))
        {
            ImGui.Checkbox("Use Constraint Solver", ref _useConstraintSolver);
            ImGui.BeginDisabled(!_useConstraintSolver);
            ImGui.SliderInt("Constraint Iterations", ref _constraintIterations, 1, 20);
            ImGui.EndDisabled();
            ImGui.BeginDisabled(_useConstraintSolver);
            ImGui.SliderFloat("Spring Constant", ref _springConstant, 0.1f, 10E3f);
            ImGui.EndDisabled();

            ImGui.EndMenu();
        }
        if (ImGui.BeginMenu("Tools"))
        {
            ImGui.BeginDisabled(_currentMode == MeshMode.PolygonBuilder);
            DrawToolMenuItems();
            ImGui.EndDisabled();
            ImGui.EndMenu();
        }
        if (ImGui.MenuItem(Paused ? "Resume (Esc)" : "Pause (Esc)"))
        {
            Paused = !Paused;
        }
        ImGui.SameLine();
        if (ImGui.BeginMenu("Show"))
        {
            ImGui.MenuItem("Physics Controls", null, ref _showPhysicsControlsWindow);
            ImGui.MenuItem("Configuration", null, ref _showConfigurationWindow);
            ImGui.MenuItem("Logger", null, ref _showLoggerWindow);
            ImGui.MenuItem("ReadMe", null, ref _showReadMeWindow);

            ImGui.EndMenu();
        }
        if (ImGui.BeginMenu("Help"))
        {
            if (ImGui.MenuItem("About", null, ref _showReadMeWindow)) { }
            ImGui.EndMenu();
        }

        ImGui.EndMainMenuBar();
    }

    private void DrawReadMeWindow()
    {
        if (!ImGui.Begin("ReadMe", ref _showReadMeWindow))
        {
            ImGui.End();
            return;
        }

        ImGui.TextWrapped("Hello World");

        ImGui.End();
    }

    private void DrawPhysicsControlsWindow()
    {
        if (!ImGui.Begin("Physics Controls", ref _showPhysicsControlsWindow))
        {
            ImGui.End();
            return;
        }

        ImGui.Text($"Current Mode: {_currentMode}");
        ImGui.Separator();

        ImGui.Checkbox("Use Constraint Solver", ref _useConstraintSolver);
        ImGui.BeginDisabled(!_useConstraintSolver);
        ImGui.SliderInt("Constraint Iterations", ref _constraintIterations, 1, 20);
        ImGui.EndDisabled();
        ImGui.BeginDisabled(_useConstraintSolver);
        ImGui.SliderFloat("Spring Constant", ref _springConstant, 0.1f, 10E3f);
        ImGui.EndDisabled();

        ImGui.Separator();

        ImGui.Text("Tools:");
        if (_currentMode != MeshMode.PolygonBuilder)
        {
            DrawToolButtons();
        }

        ImGui.Separator();
        DrawSelectedToolSettings();

        ImGui.Separator();

        if (ImGui.Button(Paused ? "Resume (Esc)" : "Pause (Esc)"))
        {
            Paused = !Paused;
        }
        ImGui.SameLine();
        var red = new System.Numerics.Vector4(0.8f, 0.2f, 0.2f, 1f);
        ImGui.PushStyleColor(ImGuiCol.Button, red);
        if (
            (_currentMode == MeshMode.Buildable || _currentMode == MeshMode.PolygonBuilder)
            && ImGui.Button("Reset Buildable Mesh")
        )
        {
            _buildableMeshInstance.ResetMesh();
        }
        ImGui.PopStyleColor();
        ImGui.End();
    }

    private void DrawConfigurationWindow()
    {
        if (!ImGui.Begin("Configuration", ref _showConfigurationWindow))
        {
            ImGui.End();
            return;
        }

        ImGui.Text("Window Size:");
        if (changedBounds.Width <= 0 || changedBounds.Height <= 0)
        {
            changedBounds = _windowBounds;
        }

        bool ratioToggled = ImGui.Checkbox("Keep Aspect Ratio", ref keepAspectRatio);
        if (ratioToggled && keepAspectRatio)
        {
            _lockedAspectRatio =
                changedBounds.Height > 0 ? changedBounds.Width / (float)changedBounds.Height : 1f;
        }
        else if (keepAspectRatio && _lockedAspectRatio <= 0.0001f)
        {
            _lockedAspectRatio =
                changedBounds.Height > 0 ? changedBounds.Width / (float)changedBounds.Height : 1f;
        }

        int newWidth = changedBounds.Width;
        int newHeight = changedBounds.Height;
        bool widthChanged = ImGui.InputInt("Width", ref newWidth);

        ImGui.BeginDisabled(keepAspectRatio);
        bool heightChanged = ImGui.InputInt("Height", ref newHeight);
        ImGui.EndDisabled();

        if (keepAspectRatio)
        {
            float aspect =
                _lockedAspectRatio > 0.0001f
                    ? _lockedAspectRatio
                    : (
                        changedBounds.Height > 0
                            ? changedBounds.Width / (float)changedBounds.Height
                            : 1f
                    );

            if (widthChanged && newWidth > 0)
            {
                changedBounds.Width = newWidth;
                changedBounds.Height = Math.Max(1, (int)Math.Round(newWidth / aspect));
            }
            else if (heightChanged && newHeight > 0)
            {
                changedBounds.Height = newHeight;
                changedBounds.Width = Math.Max(1, (int)Math.Round(newHeight * aspect));
            }
        }
        else
        {
            if (widthChanged)
                changedBounds.Width = Math.Max(1, newWidth);
            if (heightChanged)
                changedBounds.Height = Math.Max(1, newHeight);
        }

        if (ImGui.Button("Apply Size"))
        {
            SetWindowSize(changedBounds.Width, changedBounds.Height);
        }

        ImGui.Separator();
        ImGui.Text("Base Force:");

        ImGui.SliderFloat("X", ref BaseForce.X, -1000f, 1000f);
        ImGui.SameLine();
        if (ImGui.Button("Set X to 0"))
        {
            BaseForce.X = 0;
        }

        ImGui.SliderFloat("Y", ref BaseForce.Y, -1000f, 1000f);
        ImGui.SameLine();
        if (ImGui.Button("Set Y to 0"))
        {
            BaseForce.Y = 0;
        }
        if (ImGui.Button("Reset Base Force"))
        {
            BaseForce = new Vector2(0, 980f);
        }

        ImGui.Text($"Current Base Force: {BaseForce}");
        ImGui.Text("Note: The Y axis is inverted, so positive Y values point downwards.");

        ImGui.Separator();
        float drag = _activeMesh.drag;
        if (ImGui.SliderFloat("Drag (1.0 = no friction)", ref drag, 0.9f, 1.0f))
        {
            _activeMesh.drag = drag;
            _clothInstance.drag = drag;
        }

        ImGui.End();
    }
}

class ImGuiLogger
{
    public enum logTypes
    {
        Info,
        Warning,
        Error,
    };

    class messageLog
    {
        public string Message;
        public int Count;
        public logTypes Type;
    }

    Queue<messageLog> logs;
    public bool autoScrollLogs = true;
    Dictionary<logTypes, bool> logTypeVisibility = new Dictionary<logTypes, bool>
    {
        { logTypes.Info, true },
        { logTypes.Warning, true },
        { logTypes.Error, true },
    };

    public ImGuiLogger()
    {
        logs = new Queue<messageLog>();
    }

    public void AddLog(string message, logTypes type = logTypes.Info)
    {
        var lastLog = logs.LastOrDefault();
        if (lastLog != null && lastLog.Message == message && lastLog.Type == type)
        {
            lastLog.Count++;
        }
        else
        {
            logs.Enqueue(
                new messageLog
                {
                    Message = message,
                    Count = 1,
                    Type = type,
                }
            );
        }
    }

    public void DrawLogs(ref bool OpenWindow)
    {
        if (!ImGui.Begin("Logs", ref OpenWindow))
        {
            ImGui.End();
            return;
        }
        bool infoVisible = logTypeVisibility[logTypes.Info];
        if (ImGui.Checkbox("Info", ref infoVisible))
            logTypeVisibility[logTypes.Info] = infoVisible;
        ImGui.SameLine();
        bool errorVisible = logTypeVisibility[logTypes.Error];
        if (ImGui.Checkbox("Error", ref errorVisible))
            logTypeVisibility[logTypes.Error] = errorVisible;
        ImGui.SameLine();
        bool warningVisible = logTypeVisibility[logTypes.Warning];
        if (ImGui.Checkbox("Warning", ref warningVisible))
            logTypeVisibility[logTypes.Warning] = warningVisible;

        ImGui.Separator();
        ImGui.Checkbox("Auto Scroll Logs", ref autoScrollLogs);

        ImGui.SeparatorText("Logs");
        ImGui.BeginChild("LogScrollArea", new System.Numerics.Vector2(0, 0));
        foreach (var log in logs)
        {
            string displayMessage = log.Count > 1 ? $"{log.Message} (x{log.Count})" : log.Message;
            switch (log.Type)
            {
                case logTypes.Info:
                    if (logTypeVisibility[logTypes.Info])
                        ImGui.TextColored(
                            new System.Numerics.Vector4(1f, 1f, 1f, 1f),
                            displayMessage
                        );
                    break;
                case logTypes.Warning:
                    if (logTypeVisibility[logTypes.Warning])
                        ImGui.TextColored(
                            new System.Numerics.Vector4(1f, 1f, 0f, 1f),
                            displayMessage
                        );
                    break;
                case logTypes.Error:
                    if (logTypeVisibility[logTypes.Error])
                        ImGui.TextColored(
                            new System.Numerics.Vector4(1f, 0f, 0f, 1f),
                            displayMessage
                        );
                    break;
            }
        }
        if (autoScrollLogs && ImGui.GetScrollY() >= ImGui.GetScrollMaxY())
        {
            ImGui.SetScrollHereY(1.0f);
        }
        ImGui.EndChild();
        ImGui.End();
    }
}
