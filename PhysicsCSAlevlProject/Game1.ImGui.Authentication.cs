using System;
using System.Collections.Generic;
using ImGuiNET;

namespace PhysicsCSAlevlProject;

/// <summary>
/// Sign-in, authentication, and assignment/teacher window functionality.
/// </summary>
public partial class Game1
{
    private void DrawSignInWindow()
    {
        if (!ImGui.Begin("Sign In", ref _showSignInWindow))
        {
            ImGui.End();
            return;
        }

        if (_currentUser == null)
        {
            ImGui.Text("Enter User ID to Sign In:");

            ImGui.InputText("User ID", ref _userInputUserId, 20);
            ImGui.InputText("Password", ref _password, 20);

            if (ImGui.Button("Sign In"))
            {
                _currentUser = _database.GetUser(_userInputUserId);
                if (_currentUser != null)
                {
                    if (_currentUser.Password == _password)
                    {
                        _logger.AddLog(
                            $"Signed in {_currentUser.RoleId}: {_currentUser.Username} with the ID:{_currentUser.Id}"
                        );
                    }
                    else
                    {
                        _logger.AddLog("Incorrect password", ImGuiLogger.LogTypes.Error);
                        _currentUser = null;
                        _password = "";
                    }
                }
                else
                {
                    _logger.AddLog("Failed to Sign in", ImGuiLogger.LogTypes.Error);
                }
            }
        }
        else
        {
            ImGui.Text($"Signed in as User ID:{_currentUser.Id}");
            if (ImGui.Button("Sign Out"))
            {
                _currentUser = null;
                _logger.AddLog("Signed out");
            }
        }

        ImGui.End();
    }

    private void DrawAssignmentMenuItems()
    {
        if (_currentUser == null)
        {
            ImGui.TextDisabled("Sign in to view assignments.");
            return;
        }

        var teachers = _database.GetTeachers();
        var assignments = new Dictionary<int, List<Game1Database.Assignment>>();
        foreach (var teacher in teachers)
        {
            var teacherAssignments = _database.GetAssignmentsForTeacher(teacher);
            if (teacherAssignments.Count > 0)
            {
                assignments[teacher] = teacherAssignments;
            }
        }
        if (assignments.Count == 0)
        {
            ImGui.TextDisabled("No assignments found.");
            return;
        }

        foreach (var teacherAssignmentPair in assignments)
        {
            if (ImGui.BeginMenu($"Teacher {teacherAssignmentPair.Key}"))
            {
                foreach (var assignment in teacherAssignmentPair.Value)
                {
                    if (ImGui.MenuItem(assignment.Title))
                    {
                        _selectedAssignment = assignment;
                        _showSaveAssignmentPopup = true;
                    }
                }
                ImGui.EndMenu();
            }
        }
    }

    private void DrawTeacherAssignmentsWindow()
    {
        if (
            !ImGui.Begin(
                "Teacher Assignments",
                ref _showTeacherAssignmentsWindow,
                ImGuiWindowFlags.NoCollapse
            )
        )
        {
            ImGui.End();
            return;
        }
        if (_currentUser == null || _currentUser.RoleId != Game1Database.Roles.Teacher)
        {
            ImGui.TextDisabled("Only teachers can view this window.");
            ImGui.End();
            return;
        }

        ImGui.Text("Create Assignment");
        ImGui.InputText("Title", ref _newAssignmentTitle, 100);
        ImGui.InputTextMultiline(
            "Description",
            ref _newAssignmentDescription,
            500,
            new System.Numerics.Vector2(-1, 80)
        );
        ImGui.InputText("Due Date (optional, yyyy-MM-dd HH:mm)", ref _newAssignmentDueDate, 40);

        if (ImGui.Button("Create Assignment"))
        {
            if (string.IsNullOrWhiteSpace(_newAssignmentTitle))
            {
                _logger.AddLog("Assignment title is required.", ImGuiLogger.LogTypes.Warning);
            }
            else
            {
                try
                {
                    DateTime? dueDate = null;
                    if (!string.IsNullOrWhiteSpace(_newAssignmentDueDate))
                    {
                        if (
                            DateTime.TryParse(_newAssignmentDueDate, out var parsedDueDate)
                            || DateTime.TryParseExact(
                                _newAssignmentDueDate,
                                "yyyy-MM-dd HH:mm",
                                null,
                                System.Globalization.DateTimeStyles.None,
                                out parsedDueDate
                            )
                        )
                        {
                            dueDate = parsedDueDate;
                        }
                        else
                        {
                            _logger.AddLog(
                                "Invalid due date. Use yyyy-MM-dd HH:mm.",
                                ImGuiLogger.LogTypes.Warning
                            );
                            ImGui.End();
                            return;
                        }
                    }

                    int assignmentId = _database.CreateAssignment(
                        _newAssignmentTitle.Trim(),
                        _newAssignmentDescription.Trim(),
                        dueDate,
                        _currentUser.Id
                    );

                    _logger.AddLog(
                        $"Created assignment '{_newAssignmentTitle}' (ID: {assignmentId})."
                    );

                    _newAssignmentTitle = "";
                    _newAssignmentDescription = "";
                    _newAssignmentDueDate = "";
                }
                catch (Exception ex)
                {
                    _logger.AddLog(
                        $"Failed to create assignment: {ex.Message}",
                        ImGuiLogger.LogTypes.Error
                    );
                }
            }
        }

        ImGui.Separator();
        ImGui.Text("Existing Assignments");
        var assignments = _database.GetAssignmentsForTeacher(_currentUser.Id);
        ImGui.BeginTabBar("TeacherAssignments");
        foreach (var assignment in assignments)
        {
            if (ImGui.BeginTabItem(assignment.Title))
            {
                List<Game1Database.StructureInfo> structures = _database.GetStructuresForAssignment(
                    assignment.Id
                );
                ImGui.BeginTable(
                    "AssignmentStructures" + assignment.Id,
                    2,
                    ImGuiTableFlags.RowBg | ImGuiTableFlags.Borders
                );
                ImGui.TableSetupColumn("Structure Name", ImGuiTableColumnFlags.WidthStretch);
                ImGui.TableSetupColumn("Actions", ImGuiTableColumnFlags.WidthFixed, 150);
                ImGui.TableHeadersRow();
                foreach (var structure in structures)
                {
                    ImGui.TableNextRow();
                    ImGui.TableNextColumn();
                    ImGui.Text(structure.DisplayName);
                    ImGui.TableNextColumn();
                    if (ImGui.Button($"Load##{structure.Id}"))
                    {
                        try
                        {
                            string json = _database.GetStructureContent(structure.Id);
                            if (!string.IsNullOrEmpty(json))
                            {
                                _activeMesh = LoadMeshFromJsonString(json);
                                _defaultMesh = _activeMesh;
                                SetMode(MeshMode.Interact);
                                _logger.AddLog($"Loaded structure: {structure.DisplayName}");
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.AddLog(
                                $"Failed to load structure: {ex.Message}",
                                ImGuiLogger.LogTypes.Error
                            );
                        }
                    }
                }
                ImGui.EndTable();
                ImGui.EndTabItem();
            }
        }
        ImGui.EndTabBar();

        ImGui.End();
    }

    private void PopUpSaveProjectToAssignment(Game1Database.Assignment assignment)
    {
        if (ImGui.BeginPopupModal("SaveToAssignmentPopup"))
        {
            ImGui.Text("Save current mesh to an assignment:");
            ImGui.Text($"{assignment.Title}");
            ImGui.Text($"{assignment.Description}");
            ImGui.InputText("Name", ref _currentAssignmentTitle, 100);
            if (ImGui.Button("Save"))
            {
                string jMesh = SaveMeshToJsonString(_activeMesh);
                _database.SaveStructureWithName(
                    _currentUser.Id,
                    jMesh,
                    _currentAssignmentTitle,
                    assignment.Id
                );
                ImGui.CloseCurrentPopup();
            }
            if (ImGui.Button("Cancel"))
            {
                ImGui.CloseCurrentPopup();
            }

            ImGui.EndPopup();
        }
    }
}
