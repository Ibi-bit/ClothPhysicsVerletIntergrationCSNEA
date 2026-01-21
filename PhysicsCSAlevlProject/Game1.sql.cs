using System;
using System.Collections.Generic;
using System.Diagnostics;
using Npgsql;

namespace PhysicsCSAlevlProject;

public class Game1Database
{
    private string connectionString =
        "Host=localhost;Username=postgres;Password=password;Database=stick_simulation";

    public bool TestConnection()
    {
        try
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();
            Debug.WriteLine("Database connection successful!");
            return true;
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Database connection failed: {ex.Message}");
            return false;
        }
    }

    public List<int> GetStudents()
    {
        var studentIds = new List<int>();
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        using var cmd = new NpgsqlCommand("SELECT id FROM Users WHERE role_id = 2", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            studentIds.Add(reader.GetInt32(0));
        }
        return studentIds;
    }

    public List<int> GetTeachers()
    {
        var teacherIds = new List<int>();
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        using var cmd = new NpgsqlCommand("SELECT id FROM Users WHERE role_id = 1", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            teacherIds.Add(reader.GetInt32(0));
        }
        return teacherIds;
    }

    public Dictionary<string, string> GetStudentData(int studentId )
    {
        var data = new Dictionary<string, string>();
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        using var cmd = new NpgsqlCommand(
            "SELECT content FROM Structures WHERE student_id = @id",
            conn
        );
        cmd.Parameters.AddWithValue("id", studentId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            data.Add("content", reader.GetString(0));
            
        }
        return data;
    }
}
