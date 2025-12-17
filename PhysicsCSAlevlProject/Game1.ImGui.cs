using System.Collections.Generic;
using System.Linq;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using VectorGraphics;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    private bool _showPhysicsControlsWindow = false;
    private bool _showConfigurationWindow = false;
    private bool _showReadMeWindow = false;
    private ImGuiLogger _Logger = new ImGuiLogger();
    private bool _showLoggerWindow = false;

    private void ImGuiDraw(GameTime gameTime)
    {
        _guiRenderer.BeginLayout(gameTime);

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

        _guiRenderer.EndLayout();
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
                _currentMode = MeshMode.Cloth;
                _activeMesh = _clothInstance;
                _modeIndex = 0;
                ;
            }
            if (ImGui.MenuItem("Buildable", null, _currentMode == MeshMode.Buildable))
            {
                _currentMode = MeshMode.Buildable;
                _activeMesh = _buildableMeshInstance;
                _modeIndex = 1;
            }
            if (ImGui.MenuItem("Polygon Builder", null, _currentMode == MeshMode.PolygonBuilder))
            {
                _currentMode = MeshMode.PolygonBuilder;
                _activeMesh = _buildableMeshInstance;
                _modeIndex = 2;
            }
            leftPressed = false;
            windDirectionArrow = null;
            cutLine = null;
            particlesInDragArea.Clear();
            buildableMeshParticlesInDragArea.Clear();
            ImGui.EndMenu();
        }
        if (ImGui.BeginMenu("Quick Settings"))
        {
            ImGui.Checkbox("Use Constraint Solver", ref _useConstraintSolver);
            ImGui.SliderInt("Constraint Iterations", ref _constraintIterations, 1, 20);

            ImGui.BeginDisabled(_useConstraintSolver);
            ImGui.SliderFloat("Spring Constant", ref _springConstant, 0.1f, 10E3f);
            ImGui.EndDisabled();

            ImGui.EndMenu();
        }
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
        ImGui.SliderInt("Constraint Iterations", ref _constraintIterations, 1, 20);

        ImGui.BeginDisabled(_useConstraintSolver);
        ImGui.SliderFloat("Spring Constant", ref _springConstant, 0.1f, 10E3f);
        ImGui.EndDisabled();

        ImGui.Separator();

        ImGui.Text("Tools:");
        if (_currentMode != MeshMode.PolygonBuilder)
        {
            foreach (var toolName in _tools.Keys)
            {
                bool isSelected = _selectedToolName == toolName;
                if (isSelected)
                {
                    ImGui.PushStyleColor(
                        ImGuiCol.Button,
                        new System.Numerics.Vector4(0.2f, 0.6f, 0.2f, 1f)
                    );
                }
                if (ImGui.Button(toolName))
                {
                    _selectedToolName = toolName;
                }

                if (isSelected)
                {
                    ImGui.PopStyleColor();
                }

                ImGui.SameLine();
            }
            ImGui.NewLine();
        }

        ImGui.Separator();
        ImGui.Text($"Settings for {_selectedToolName} Tool:");
        if (_selectedToolName == "Drag")
        {
            float radius = (float)_tools["Drag"].Properties["Radius"];
            if (ImGui.SliderFloat("Radius", ref radius, 5f, 100f))
            {
                _tools["Drag"].Properties["Radius"] = radius;
            }

            bool infiniteParticles = (bool)_tools["Drag"].Properties["InfiniteParticles"];
            if (ImGui.Checkbox("Infinite Particles", ref infiniteParticles))
            {
                _tools["Drag"].Properties["InfiniteParticles"] = infiniteParticles;
            }

            int maxParticles = (int)_tools["Drag"].Properties["MaxParticles"];
            string maxParticlesLabel = infiniteParticles ? "Max Particles: ∞" : "Max Particles";

            ImGui.BeginDisabled(infiniteParticles);
            if (ImGui.SliderInt(maxParticlesLabel, ref maxParticles, 1, 100))
            {
                _tools["Drag"].Properties["MaxParticles"] = maxParticles;
            }
            ImGui.EndDisabled();
        }

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

        ImGui.Checkbox("Keep Aspect Ratio", ref keepAspectRatio);
        float aspectRatio =
            changedBounds.Height > 0 ? changedBounds.Width / (float)changedBounds.Height : 1f;

        ImGui.InputInt("Width", ref changedBounds.Width);
        if (keepAspectRatio)
        {
            changedBounds.Height = (int)(changedBounds.Width / aspectRatio);
        }
        ImGui.InputInt("Height", ref changedBounds.Height);
        if (keepAspectRatio)
        {
            changedBounds.Width = (int)(changedBounds.Height * aspectRatio);
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
