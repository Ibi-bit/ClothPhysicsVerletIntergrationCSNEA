using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    private bool _showPhysicsControlsWindow;
    private bool _showConfigurationWindow;
    private bool _showReadMeWindow;
    private bool _showStructureWindow;
    private bool _showSaveWindow;
    private bool _showSignInWindow;
    private ImGuiLogger _logger = new ImGuiLogger();
    private bool _showLoggerWindow;
    private string _meshName = "MyMesh";
    private string _structurePath = Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory,
        "..",
        "..",
        "..",
        "JSONStructures"
    );

    private bool _ctrlHeld;
    private bool _shiftHeld;
    private bool _capsActive;
    private bool _altHeld;

    private int _signedInUserId = -1;

    private void ImGuiDraw(GameTime gameTime)
    {
        _guiRenderer.BeginLayout(gameTime);
        _ctrlHeld = ImGui.GetIO().KeyCtrl;
        _shiftHeld = ImGui.GetIO().KeyShift;
        _altHeld = ImGui.GetIO().KeyAlt;

        if (ImGui.IsKeyPressed(ImGuiKey.CapsLock))
        {
            _capsActive = !_capsActive;
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
        if (_showStructureWindow)
        {
            DrawStructureWindow();
        }
        if (_showSaveWindow)
        {
            DrawSaveWindow();
        }
        if (_showLoggerWindow)
        {
            _logger.DrawLogs(ref _showLoggerWindow);
        }
        if (_showSignInWindow)
        {
            DrawSignInWindow();
        }
        if (inspectedParticles.Count > 0)
        {
            DrawInspectParticleWindow();
        }

        ModeSwitchingImGui();

        _guiRenderer.EndLayout();
    }

    private void DrawInspectParticleWindow()
    {
        if (!ImGui.Begin("Inspect Particle"))
        {
            ImGui.End();
            return;
        }
        ImGui.Text("Particle Info:");
        if (ImGui.Button("Clear All Inspected Particles"))
        {
            inspectedParticles.Clear();
        }
        if (ImGui.Button("Delete All Inspected Particles With Sticks"))
        {
            foreach (var index in inspectedParticles)
            {
                _activeMesh.RemoveParticle(index);
            }
            inspectedParticles.Clear();
            ImGui.End();
            return;
        }
        if (ImGui.Button("Close Window"))
        {
            ImGui.End();
            return;
        }

        ImGui.BeginChild("ParticleInfoScrollArea", new System.Numerics.Vector2(0, -30));
        foreach (var index in inspectedParticles)
        {
            if (!_activeMesh.Particles.TryGetValue(index, out var p))
            {
                continue;
            }

            ImGui.Text($"Particle Index: {index}");
            ImGui.Text($"Position: {p.Position}");
            ImGui.Text($"Previous Position: {p.PreviousPosition}");
            ImGui.Text($"Is Fixed: {p.IsPinned}");
            if (ImGui.Button(p.IsPinned ? "Unpin Particle" : "Pin Particle"))
            {
                p.IsPinned = !p.IsPinned;
            }

            if (ImGui.Button("Remove Particle From Inspect"))
            {
                inspectedParticles.Remove(index);
                break;
            }
            ImGui.Separator();
        }
        ImGui.EndChild();
        ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.8f, 0.2f, 0.2f, 1f));
        if (ImGui.Button("Close and Clear All"))
        {
            inspectedParticles.Clear();
        }
        ImGui.PopStyleColor();

        ImGui.End();
    }

    private void DrawSignInWindow()
    {
        if (!ImGui.Begin("Sign In", ref _showSignInWindow))
        {
            ImGui.End();
            return;
        }

        if (_signedInUserId == -1)
        {
            ImGui.Text("Enter User ID to Sign In:");
            string userIdInput = "";
            ImGui.InputText("User ID", ref userIdInput, 20);
            if (ImGui.Button("Sign In"))
            {
                if (int.TryParse(userIdInput, out int userId))
                {
                    _signedInUserId = userId;
                    _showSignInWindow = false;
                }
            }
        }
        else
        {
            ImGui.Text($"Signed in as User ID: {_signedInUserId}");
            if (ImGui.Button("Sign Out"))
            {
                _signedInUserId = -1;
            }
        }

        ImGui.End();
    }

    private void ModeSwitchingImGui()
    {
        bool isMac = OperatingSystem.IsMacOS();
        bool backwardModifierHeld = isMac ? _altHeld : _ctrlHeld;

        if (_shiftHeld && ImGui.IsKeyPressed(ImGuiKey.Tab))
        {
            int delta = backwardModifierHeld ? -1 : 1;
            int modeCount = Enum.GetValues(typeof(MeshMode)).Length;
            int newModeIndex = ((int)_currentMode + delta + modeCount) % modeCount;
            SetMode((MeshMode)newModeIndex);
        }

        if (_shiftHeld)
        {
            var drawList = ImGui.GetForegroundDrawList();
            drawList.AddText(
                new System.Numerics.Vector2(10, 30),
                ImGui.GetColorU32(ImGuiCol.Text),
                "Mode:"
            );
            var modes = Enum.GetValues(typeof(MeshMode));
            for (int i = 0; i < modes.Length; i++)
            {
                var mode = (MeshMode)modes.GetValue(i);
                string text = mode == _currentMode ? $"> {mode} <" : $"  {mode}";
                uint color =
                    mode == _currentMode
                        ? ImGui.GetColorU32(new System.Numerics.Vector4(0.2f, 1f, 0.2f, 1f))
                        : ImGui.GetColorU32(ImGuiCol.Text);
                drawList.AddText(new System.Numerics.Vector2(10, 48 + i * 18), color, text);
            }
        }
    }

    private void SetMode(MeshMode mode)
    {
        _currentMode = mode;

        switch (_currentMode)
        {
            case MeshMode.Cloth:
                _activeMesh = _clothInstance;
                EnsureSelectedToolValid();
                break;
            case MeshMode.Interact:
                _activeMesh = _defaultBuildableMesh;
                EnsureSelectedToolValid();
                break;
            case MeshMode.Edit:
                _activeMesh = _defaultBuildableMesh;
                EnsureSelectedToolValid();
                break;
        }

        leftPressed = false;
        windDirectionArrow = null;
        cutLine = null;
        particlesInDragArea.Clear();
        buildableMeshParticlesInDragArea.Clear();
        _stickToolFirstParticleId = null;
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
            if (ImGui.MenuItem("Open"))
            {
                _showStructureWindow = true;
            }
            if (ImGui.MenuItem("Save"))
            {
                _showSaveWindow = true;
            }
            if (ImGui.MenuItem("Sign In/Sign Out"))
            {
                _showSignInWindow = true;
            }

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
                SetMode(MeshMode.Cloth);
            }
            if (ImGui.MenuItem("Interact", null, _currentMode == MeshMode.Interact))
            {
                SetMode(MeshMode.Interact);
            }
            if (ImGui.MenuItem("Edit", null, _currentMode == MeshMode.Edit))
            {
                SetMode(MeshMode.Edit);
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
            DrawToolMenuItems();

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

    private void DrawStructureWindow()
    {
        if (!ImGui.Begin("Structure", ref _showStructureWindow))
        {
            ImGui.End();
            return;
        }
        ImGui.Text("Available Meshes:");
        ImGui.Separator();

        var meshes = LoadAllMeshesFromDirectory(_structurePath);

        if (meshes.Count == 0)
        {
            ImGui.TextDisabled("No saved meshes found.");
        }

        foreach (var meshEntry in meshes)
        {
            if (ImGui.Button($"Load {meshEntry.Key}"))
            {
                _activeMesh = meshEntry.Value;
                _defaultBuildableMesh = _activeMesh;
                SetMode(MeshMode.Interact);
                _showStructureWindow = false;
            }
        }

        ImGui.End();
    }

    private void DrawSaveWindow()
    {
        if (!ImGui.Begin("Save Mesh", ref _showSaveWindow))
        {
            ImGui.End();
            return;
        }
        ImGui.InputText("Mesh Name", ref _meshName, 100);
        if (ImGui.Button("Save"))
        {
            SaveMeshToJSON(_activeMesh, _meshName, _structurePath);
        }
        ImGui.End();
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

        DrawToolButtons();

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
            (_currentMode == MeshMode.Interact || _currentMode == MeshMode.Edit)
            && ImGui.Button("Reset Buildable Mesh")
        )
        {
            _activeMesh.ResetMesh();
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

    Queue<MessageLog> _logs;
    public bool autoScrollLogs = true;
    public bool wrapText = true;

    // public bool clearLogs = false;
    Dictionary<LogTypes, bool> _logTypeVisibility = new Dictionary<LogTypes, bool>
    {
        { LogTypes.Info, true },
        { LogTypes.Warning, true },
        { LogTypes.Error, true },
    };

    public ImGuiLogger()
    {
        _logs = new Queue<MessageLog>();
    }

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
