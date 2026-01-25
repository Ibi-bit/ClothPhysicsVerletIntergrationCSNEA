using System;
using System.Collections.Generic;
using System.Diagnostics;
using Npgsql;

namespace PhysicsCSAlevlProject;

public class Game1Database
{
    private readonly string _connectionString =
        "Host=localhost;Port=5432;Database=stick_simulation;Username=dev;Password=dev123";

    private NpgsqlConnection OpenConnection()
    {
        var conn = new NpgsqlConnection(_connectionString);
        conn.Open();
        return conn;
    }

    public bool TestConnection()
    {
        try
        {
            using var conn = OpenConnection();
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
        using var conn = OpenConnection();

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
        using var conn = OpenConnection();

        using var cmd = new NpgsqlCommand("SELECT id FROM Users WHERE role_id = 1", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            teacherIds.Add(reader.GetInt32(0));
        }

        return teacherIds;
    }

    public Dictionary<string, string> GetStudentStructures(int studentId)
    {
        var data = new Dictionary<string, string>();
        using var conn = OpenConnection();

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

    public User GetUserByUsername(string username)
    {
        using var conn = OpenConnection();
        using var cmd = new NpgsqlCommand(
            "SELECT id, username, role_id FROM Users WHERE username = @username LIMIT 1",
            conn
        );
        cmd.Parameters.AddWithValue("username", username);

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
        {
            return null;
        }

        return new User
        {
            Id = reader.GetInt32(0),
            Username = reader.GetString(1),
            RoleId = reader.GetInt32(2),
        };
    }

    public int CreateUser(string username, int roleId)
    {
        using var conn = OpenConnection();
        using var cmd = new NpgsqlCommand(
            "INSERT INTO Users (username, role_id) VALUES (@username, @role_id) RETURNING id",
            conn
        );
        cmd.Parameters.AddWithValue("username", username);
        cmd.Parameters.AddWithValue("role_id", roleId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public void SaveStructure(int assignmentId, int studentId, string contentJson)
    {
        using var conn = OpenConnection();
        using var cmd = new NpgsqlCommand(
            @"INSERT INTO Structures (assignment_id, student_id, content, submitted_at)
              VALUES (@assignment_id, @student_id, @content, NOW())
              ON CONFLICT (assignment_id, student_id) DO UPDATE
              SET content = EXCLUDED.content, submitted_at = EXCLUDED.submitted_at",
            conn
        );
        cmd.Parameters.AddWithValue("assignment_id", assignmentId);
        cmd.Parameters.AddWithValue("student_id", studentId);
        cmd.Parameters.AddWithValue("content", contentJson);
        cmd.ExecuteNonQuery();
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public int RoleId { get; set; }
    }
}
