using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    /// <summary>
    /// Gets consistent JSON serializer settings for mesh serialization/deserialization.
    /// This ensures oscillating particles and other types are properly handled.
    /// </summary>
    private static JsonSerializerSettings GetMeshJsonSettings()
    {
        return new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.Objects,
            TypeNameAssemblyFormatHandling = TypeNameAssemblyFormatHandling.Simple,
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore,
        };
    }

    private void SaveMeshToJSON(Mesh mesh, string Name, string filePath)
    {
        try
        {
            var settings = GetMeshJsonSettings();
            var fileWriteableMesh = new FileWriteableMesh(mesh);
            System.IO.Directory.CreateDirectory(filePath);
            filePath = System.IO.Path.Combine(filePath, Name + ".json");
            string json = JsonConvert.SerializeObject(fileWriteableMesh, settings);
            System.IO.File.WriteAllText(filePath, json);
            _logger.AddLog(
                $"Saved mesh '{Name}' with {mesh.Particles.Count} particles and {mesh.Sticks.Count} sticks"
            );
        }
        catch (Exception ex)
        {
            _logger.AddLog(
                $"Failed to save mesh '{Name}' to {filePath}: {ex.Message}",
                ImGuiLogger.LogTypes.Error
            );
        }
    }

    private string SaveMeshToJsonString(Mesh mesh)
    {
        try
        {
            var settings = GetMeshJsonSettings();
            var fileWriteableMesh = new FileWriteableMesh(mesh);
            string json = JsonConvert.SerializeObject(fileWriteableMesh, settings);
            return json;
        }
        catch (Exception ex)
        {
            _logger.AddLog(
                $"Failed to serialize mesh to JSON string: {ex.Message}",
                ImGuiLogger.LogTypes.Error
            );
            return string.Empty;
        }
    }

    private Mesh LoadMeshFromJSON(string filePath)
    {
        try
        {
            var settings = GetMeshJsonSettings();
            string json = System.IO.File.ReadAllText(filePath);
            FileWriteableMesh fileWriteableMesh = JsonConvert.DeserializeObject<FileWriteableMesh>(
                json,
                settings
            );

            Mesh mesh = fileWriteableMesh.ToMesh();
            mesh.RestoreStickReferences();

            return mesh;
        }
        catch (Exception ex)
        {
            _logger.AddLog(
                $"Failed to load mesh from {filePath}: {ex.Message}",
                ImGuiLogger.LogTypes.Error
            );
            return _activeMesh;
        }
    }

    private Mesh LoadMeshFromJsonString(string json)
    {
        try
        {
            var settings = GetMeshJsonSettings();
            FileWriteableMesh fileWriteableMesh = JsonConvert.DeserializeObject<FileWriteableMesh>(
                json,
                settings
            );

            Mesh mesh = fileWriteableMesh.ToMesh();
            mesh.RestoreStickReferences();

            return mesh;
        }
        catch (Exception ex)
        {
            _logger.AddLog(
                $"Failed to load mesh from JSON string: {ex.Message}",
                ImGuiLogger.LogTypes.Error
            );
            return _activeMesh;
        }
    }

    protected override bool BeginDraw()
    {
        return base.BeginDraw();
    }

    private Dictionary<string, Mesh> LoadAllMeshesFromDirectory(string directoryPath)
    {
        if (!System.IO.Directory.Exists(directoryPath))
        {
            _logger.AddLog(
                $"Mesh directory not found: {directoryPath}",
                ImGuiLogger.LogTypes.Warning
            );
            return new Dictionary<string, Mesh>();
        }

        string[] jsonFiles = System.IO.Directory.GetFiles(directoryPath, "*.json");
        Dictionary<string, Mesh> meshes = new Dictionary<string, Mesh>();

        foreach (string filePath in jsonFiles)
        {
            try
            {
                Mesh mesh = LoadMeshFromJSON(filePath);
                string fileName = System.IO.Path.GetFileNameWithoutExtension(
                    System.IO.Path.GetFileName(filePath)
                );
                meshes[fileName] = mesh;
            }
            catch (Exception ex)
            {
                _logger.AddLog(
                    $"Failed to load mesh from {filePath}: {ex.Message}",
                    ImGuiLogger.LogTypes.Error
                );
            }
        }

        return meshes;
    }
}
