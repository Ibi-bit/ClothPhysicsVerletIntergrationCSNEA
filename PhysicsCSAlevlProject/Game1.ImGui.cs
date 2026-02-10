using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    private bool _showConfigurationWindow;
    private bool _showReadMeWindow;
    private bool _showStructureWindow;
    private bool _showSaveWindow;
    private bool _showSignInWindow;
    private bool _showLoggerWindow;
    private bool _showConfirmNewMeshPopup;

    private ImGuiLogger _logger;
    private string _meshName;
    private string _structurePath;
    private string _quickStructureName;
    private Collider _cursorCollider;
    private Dictionary<string, Collider> _cursorColliderStore;
    private Dictionary<string, Mesh> _quickMeshes;
    private Dictionary<string, Func<Mesh>> _template;

    private int _buttonSteps;

    private bool _ctrlHeld;
    private bool _shiftHeld;
    private bool _capsActive;
    private bool _altHeld;

    private int _signedInUserId;

    private void InitializeImGui()
    {
        _showConfigurationWindow = false;
        _showReadMeWindow = false;
        _showStructureWindow = false;
        _showSaveWindow = false;
        _showSignInWindow = false;
        _showLoggerWindow = false;
        _showConfirmNewMeshPopup = false;

        _buttonSteps = 10;

        _logger = new ImGuiLogger();
        _meshName = "MyMesh";
        _structurePath = Path.Combine(
            AppDomain.CurrentDomain.BaseDirectory,
            "..",
            "..",
            "..",
            "JSONStructures"
        );
        _quickStructureName = "QuickStructure";

        _cursorColliderStore = new Dictionary<string, Collider>
        {
            { "Rectangle", new RectangleCollider(new Rectangle(0, 0, 10, 10)) },
            { "Circle", new CircleCollider(Vector2.Zero, 5f) },
        };
        _cursorCollider = _cursorColliderStore["Rectangle"];
        _quickMeshes = LoadAllMeshesFromDirectory(_structurePath);
        _template = new Dictionary<string, Func<Mesh>>
        {
            {
                "Cloth 20x20",
                () =>
                    Mesh.CreateClothMesh(
                        new Vector2(220, 20),
                        new Vector2(420, 320),
                        10f,
                        null,
                        _activeMesh.springConstant,
                        _activeMesh.drag,
                        _activeMesh.mass
                    )
            },
            {
                "Cloth 30x20",
                () =>
                    Mesh.CreateClothMesh(
                        new Vector2(220, 20),
                        new Vector2(520, 220),
                        10f,
                        null,
                        _activeMesh.springConstant,
                        _activeMesh.drag,
                        _activeMesh.mass
                    )
            },
        };

        _signedInUserId = -1;
    }

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
        if (_inspectedParticles.Count > 0)
        {
            DrawInspectParticleWindow();
        }
        DrawPopUps();

        ModeSwitchingImGui();
        ToolSwitchingImGui();

        _guiRenderer.EndLayout();
    }

    private void DrawPopUps()
    {
        if (_showConfirmNewMeshPopup)
        {
            ImGui.OpenPopup("ConfirmNewMeshPopup");
            _showConfirmNewMeshPopup = false;
        }

        if (ImGui.BeginPopupModal("ConfirmNewMeshPopup"))
        {
            ImGui.Text("Are you sure you want to create a new mesh? Unsaved changes will be lost.");
            if (ImGui.Button("Yes"))
            {
                _activeMesh = new Mesh();
                _defaultMesh = _activeMesh;
                SetMode(MeshMode.Interact);
                ImGui.CloseCurrentPopup();
            }
            ImGui.SameLine();
            if (ImGui.Button("No"))
            {
                ImGui.CloseCurrentPopup();
            }
            ImGui.EndPopup();
        }
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
            _inspectedParticles.Clear();
        }
        if (ImGui.Button("Delete All Inspected Particles With Sticks"))
        {
            foreach (var index in _inspectedParticles)
            {
                _activeMesh.RemoveParticle(index);
            }
            _inspectedParticles.Clear();
            ImGui.End();
            return;
        }
        if (ImGui.Button("Close Window"))
        {
            ImGui.End();
            return;
        }
        ImGui.Text("Press a particle index to inspect");
        float windowWidth = ImGui.GetContentRegionAvail().X;

        float cursorX = 0f;
        ImGui.BeginChild("ParticleIndexList", new System.Numerics.Vector2(0, 80));
        foreach (int pID in _inspectedParticles)
        {
            bool isOpen = _openedInspectedParticles.Contains(pID);
            float itemWidth = ImGui.CalcTextSize(pID.ToString()).X + 30f;

            if (cursorX + itemWidth > windowWidth && cursorX > 0f)
            {
                cursorX = 0f;
            }
            else if (cursorX > 0f)
            {
                ImGui.SameLine();
            }

            if (ImGui.Checkbox(pID.ToString(), ref isOpen))
            {
                if (isOpen)
                {
                    _openedInspectedParticles.Add(pID);
                }
                else
                {
                    _openedInspectedParticles.Remove(pID);
                }
            }
            cursorX += itemWidth + ImGui.GetStyle().ItemSpacing.X;
        }
        ImGui.EndChild();

        ImGui.BeginChild("ParticleInfoScrollArea", new System.Numerics.Vector2(0, -100));
        foreach (var index in _openedInspectedParticles)
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
                _inspectedParticles.Remove(index);
                break;
            }
            ImGui.Separator();
        }
        ImGui.EndChild();
        ImGui.PushStyleColor(ImGuiCol.Button, new System.Numerics.Vector4(0.8f, 0.2f, 0.2f, 1f));
        if (ImGui.Button("Close and Clear All"))
        {
            _inspectedParticles.Clear();
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

        if (_shiftHeld && !_ctrlHeld)
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
                var mode = (MeshMode)modes.GetValue(i)!;
                string text = mode == _currentMode ? $"> {mode} <" : $"  {mode}";
                uint color =
                    mode == _currentMode
                        ? ImGui.GetColorU32(new System.Numerics.Vector4(0.2f, 1f, 0.2f, 1f))
                        : ImGui.GetColorU32(ImGuiCol.Text);
                drawList.AddText(new System.Numerics.Vector2(10, 48 + i * 18), color, text);
            }
        }
    }

    private void ToolSwitchingImGui()
    {
        if (_ctrlHeld)
        {
            List<string> toolNames = _currentToolSet.Keys.ToList();
            int currentIndex = toolNames.IndexOf(_selectedToolName);
            if (ImGui.IsKeyPressed(ImGuiKey.T))
            {
                int nextIndex = (currentIndex + 1) % toolNames.Count;
                _selectedToolName = toolNames[nextIndex];
                _logger.AddLog($"Switched to tool: {_selectedToolName}");
            }
            else if (_shiftHeld && ImGui.IsKeyPressed(ImGuiKey.T))
            {
                int nextIndex = (currentIndex - 1 + toolNames.Count) % toolNames.Count;
                _selectedToolName = toolNames[nextIndex];
                _logger.AddLog($"Switched to tool: {_selectedToolName}");
            }
            for (int i = 0; i < toolNames.Count; i++)
            {
                var toolName = toolNames[i];
                uint color =
                    toolName == _selectedToolName
                        ? ImGui.GetColorU32(new System.Numerics.Vector4(0.2f, 1f, 0.2f, 1f))
                        : ImGui.GetColorU32(ImGuiCol.Text);
                ImGui
                    .GetForegroundDrawList()
                    .AddText(new System.Numerics.Vector2(10, 60 + i * 18), color, toolName);
            }
        }
    }

    private void SetMode(MeshMode mode)
    {
        _currentMode = mode;

        _activeMesh ??= _defaultMesh;
        EnsureSelectedToolValid();
        if (_currentMode == MeshMode.Edit)
        {
            _paused = true;
        }

        MeshHistoryPush();
        _leftPressed = false;
        _windDirectionArrow = null;
        _cutLine = null;
        _meshParticlesInDragArea.Clear();
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
            if (ImGui.MenuItem("New"))
            {
                _showConfirmNewMeshPopup = true;
            }
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

        if (ImGui.BeginMenu("Time"))
        {
            if (ImGui.Button(_paused ? "Resume (Esc)" : "Pause (Esc)"))
            {
                _paused = !_paused;
            }
            ImGui.SameLine();
            ImGui.PushStyleColor(
                ImGuiCol.Button,
                new System.Numerics.Vector4(0.8f, 0.2f, 0.2f, 1f)
            );
            if (ImGui.Button("Reset Simulation (R)"))
            {
                _activeMesh.ResetMesh();
            }
            ImGui.PopStyleColor();
            ImGui.BeginDisabled(!_paused);

            if (ImGui.Button("Step Forward (T)"))
            {
                _stepsToStep += _buttonSteps;
            }
            ImGui.SameLine();
            ImGui.InputInt("Steps to Step", ref _buttonSteps, 1, 100);

            ImGui.EndDisabled();
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

        if (ImGui.BeginMenu("Templates"))
        {
            foreach (var meshEntry in _template)
            {
                if (ImGui.MenuItem(meshEntry.Key))
                {
                    var mesh = meshEntry.Value();
                    _activeMesh = mesh;
                    _defaultMesh = mesh;
                    SetMode(MeshMode.Interact);
                }
            }
            ImGui.EndMenu();
        }
        if (ImGui.BeginMenu("Quick Structures"))
        {
            if (ImGui.Button("Refresh List"))
            {
                _quickMeshes = LoadAllMeshesFromDirectory(_structurePath);
            }

            if (ImGui.Button("Save Current Mesh"))
            {
                SaveMeshToJSON(_activeMesh, _quickStructureName, _structurePath);
                _quickMeshes = LoadAllMeshesFromDirectory(_structurePath);
            }
            ImGui.SameLine();
            ImGui.InputText("Structure Name", ref _quickStructureName, 100);
            ImGui.BeginDisabled(_quickMeshes.Count == 0);
            foreach (var meshEntry in _quickMeshes)
            {
                if (ImGui.MenuItem(meshEntry.Key))
                {
                    var mesh = meshEntry.Value;
                    _activeMesh = mesh;
                    _defaultMesh = mesh;
                    _quickStructureName = meshEntry.Key;
                    SetMode(MeshMode.Interact);
                }
            }
            ImGui.EndDisabled();
            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Quick Settings"))
        {
            ImGui.InputFloat("Global Mass", ref _activeMesh.mass);
            ImGui.InputFloat("Spring Constant", ref _activeMesh.springConstant);
            ImGui.SliderInt("Physics Substeps", ref _subSteps, 1, 600);

            float drag = _activeMesh.drag;
            if (ImGui.SliderFloat("Drag (1.0 = no friction)", ref drag, 0.9f, 1.0f))
            {
                _activeMesh.drag = drag;
            }

            ImGui.SliderFloat("Base Force X", ref _baseForce.X, -1000f, 1000f);
            ImGui.SliderFloat("Base Force Y", ref _baseForce.Y, -1000f, 1000f);
            ImGui.EndMenu();
        }
        if (ImGui.BeginMenu("Tool Menu"))
        {
            DrawToolMenuItems();
            ImGui.Separator();
            DrawSelectedToolSettings();
            ImGui.EndMenu();
        }
        if (ImGui.MenuItem(_paused ? "Resume (Esc)" : "Pause (Esc)"))
        {
            _paused = !_paused;
        }
        ImGui.SameLine();
        if (ImGui.BeginMenu("Show"))
        {
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
                _defaultMesh = _activeMesh;
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

    private void DrawConfigurationWindow()
    {
        if (!ImGui.Begin("Configuration", ref _showConfigurationWindow))
        {
            ImGui.End();
            return;
        }

        ImGui.Text("Window Size:");
        if (_changedBounds.Width <= 0 || _changedBounds.Height <= 0)
        {
            _changedBounds = _windowBounds;
        }

        bool ratioToggled = ImGui.Checkbox("Keep Aspect Ratio", ref _keepAspectRatio);
        if (ratioToggled && _keepAspectRatio)
        {
            _lockedAspectRatio =
                _changedBounds.Height > 0
                    ? _changedBounds.Width / (float)_changedBounds.Height
                    : 1f;
        }
        else if (_keepAspectRatio && _lockedAspectRatio <= 0.0001f)
        {
            _lockedAspectRatio =
                _changedBounds.Height > 0
                    ? _changedBounds.Width / (float)_changedBounds.Height
                    : 1f;
        }

        int newWidth = _changedBounds.Width;
        int newHeight = _changedBounds.Height;
        bool widthChanged = ImGui.InputInt("Width", ref newWidth);

        ImGui.BeginDisabled(_keepAspectRatio);
        bool heightChanged = ImGui.InputInt("Height", ref newHeight);
        ImGui.EndDisabled();

        if (_keepAspectRatio)
        {
            float aspect =
                _lockedAspectRatio > 0.0001f
                    ? _lockedAspectRatio
                    : (
                        _changedBounds.Height > 0
                            ? _changedBounds.Width / (float)_changedBounds.Height
                            : 1f
                    );

            if (widthChanged && newWidth > 0)
            {
                _changedBounds.Width = newWidth;
                _changedBounds.Height = Math.Max(1, (int)Math.Round(newWidth / aspect));
            }
            else if (heightChanged && newHeight > 0)
            {
                _changedBounds.Height = newHeight;
                _changedBounds.Width = Math.Max(1, (int)Math.Round(newHeight * aspect));
            }
        }
        else
        {
            if (widthChanged)
                _changedBounds.Width = Math.Max(1, newWidth);
            if (heightChanged)
                _changedBounds.Height = Math.Max(1, newHeight);
        }

        if (ImGui.Button("Apply Size"))
        {
            SetWindowSize(_changedBounds.Width, _changedBounds.Height);
        }
        ImGui.Checkbox("Draw Particles", ref _drawParticles);
        ImGui.Checkbox("Draw Constraints", ref _drawConstraints);
        ImGui.SliderFloat("Constraint Thickness", ref _activeMesh.stickDrawThickness, 1f, 10f);

        ImGui.Separator();
        ImGui.Text("Physics Controls:");

        ImGui.InputFloat("Global Mass", ref _activeMesh.mass);
        ImGui.InputFloat("Global Spring Constant", ref _activeMesh.springConstant);
        ImGui.SliderInt("Physics Substeps", ref _subSteps, 1, 600);

        float drag = _activeMesh.drag;
        if (ImGui.SliderFloat("Drag (1.0 = no friction)", ref drag, 0.9f, 1.0f))
        {
            _activeMesh.drag = drag;
        }

        ImGui.Separator();
        ImGui.Text("Base Force:");

        ImGui.SliderFloat("X", ref _baseForce.X, -1000f, 1000f);
        ImGui.SameLine();
        if (ImGui.Button("Set X to 0"))
        {
            _baseForce.X = 0;
        }

        ImGui.SliderFloat("Y", ref _baseForce.Y, -1000f, 1000f);
        ImGui.SameLine();
        if (ImGui.Button("Set Y to 0"))
        {
            _baseForce.Y = 0;
        }
        if (ImGui.Button("Reset Base Force"))
        {
            _baseForce = new Vector2(0, 980f);
        }

        ImGui.Text($"Current Base Force: {_baseForce}");
        ImGui.Text("Note: The Y axis is inverted, so positive Y values point downwards.");

        ImGui.Separator();

        if (ImGui.Button(_paused ? "Resume (Esc)" : "Pause (Esc)"))
        {
            _paused = !_paused;
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
}


