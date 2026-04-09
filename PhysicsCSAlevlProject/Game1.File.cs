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
    /// <summary>
    /// saves the given mesh to a JSOn file at the given filepath
    /// </summary>
    /// <param name="mesh"></param>
    /// <param name="Name"></param>
    /// <param name="filePath"></param>
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
    /// <summary>
    /// returns a json string of the mesh used to save to the database
    /// </summary>
    /// <param name="mesh"></param>
    /// <returns>JSON String</returns>
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
    /// <summary>
    /// attempts to load a Mesh from the file path provided and converts the file writable mesh to a regular mesh if it fails it returns active mesh
    /// </summary>
    /// <param name="filePath"></param>
    /// <returns></returns>

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
    /// <summary>
    /// converts a json string intoo a mesh used to laod from the database if it fails it returns active mesh
    /// </summary>
    /// <param name="json"></param>
    /// <returns></returns>

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

    /// <summary>
    /// loads all mesh from a a directory path and returns a dictionary  of the meshes with the key being the filename
    /// </summary>
    /// <param name="directoryPath"></param>
    /// <returns>Dictionary of FileName and Mesh</returns>
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
