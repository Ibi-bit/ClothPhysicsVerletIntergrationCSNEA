using System.Collections.Generic;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    private string _selectedToolName = "Drag";
    private Dictionary<string, Tool> _tools;
    private float dragRadius = 20f;

    private void InitializeTools()
    {
        _tools = new Dictionary<string, Tool>
        {
            { "Drag", new Tool("Drag", null, null) },
            { "Pin", new Tool("Pin", null, null) },
            { "Cut", new Tool("Cut", null, null) },
            { "Wind", new Tool("Wind", null, null) },
            { "DragOne", new Tool("DragOne", null, null) },
            { "PhysicsDrag", new Tool("PhysicsDrag", null, null) },
            { "LineCut", new Tool("LineCut", null, null) },
        };

        foreach (var tool in _tools.Values)
        {
            tool.Properties = new Dictionary<string, object>();
        }

        _tools["Drag"].Properties["Radius"] = 20f;
        _tools["Drag"].Properties["MaxParticles"] = (int)20;
        _tools["Drag"].Properties["InfiniteParticles"] = true;

        _tools["Cut"].Properties["Radius"] = 10f;
    }

    private void DrawToolMenuItems()
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

            if (ImGui.MenuItem(toolName))
            {
                _selectedToolName = toolName;
            }

            if (isSelected)
            {
                ImGui.PopStyleColor();
            }
        }
    }

    private void DrawToolButtons()
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

    private void DrawSelectedToolSettings()
    {
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
    }
}
