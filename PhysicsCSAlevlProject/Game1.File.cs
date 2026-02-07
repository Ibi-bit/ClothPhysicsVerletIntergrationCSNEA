using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace PhysicsCSAlevlProject;

public partial class Game1
{
    private void SaveMeshToJSON(Mesh mesh, string Name, string filePath)
    {
        try
        {
            var settings = new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
                Formatting = Formatting.Indented,
            };
            var fileWriteableMesh = new FileWriteableMesh(mesh);
            System.IO.Directory.CreateDirectory(filePath);
            filePath = System.IO.Path.Combine(filePath, Name + ".json");
            string json = JsonConvert.SerializeObject(fileWriteableMesh, settings);
            System.IO.File.WriteAllText(filePath, json);
        }
        catch (Exception ex)
        {
            _logger.AddLog(
                $"Failed to save mesh '{Name}' to {filePath}: {ex.Message}",
                ImGuiLogger.LogTypes.Error
            );
        }
    }

    private Mesh LoadMeshFromJSON(string filePath)
    {
        try
        {
            var settings = new JsonSerializerSettings { TypeNameHandling = TypeNameHandling.All };

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
