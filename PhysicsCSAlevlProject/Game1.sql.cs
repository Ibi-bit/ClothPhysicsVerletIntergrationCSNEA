using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace PhysicsCSAlevlProject;

public class Game1Database
{
    private readonly string _connectionString =
        "Host=localhost;Port=5432;Database=stick_simulation;Username=dev;Password=dev123";
    public ImGuiLogger logger;

    public Game1Database(ImGuiLogger logger)
    {
        this.logger = logger;
    }

    /// <summary>
    /// Opens a connection to the PostgreSQL database using the provided connection string. If the connection is successful, it returns an open NpgsqlConnection object that can be used for executing queries. If there is an error during the connection process, it logs the error message using the provided ImGuiLogger and rethrows the exception to be handled by the calling code. This method centralizes the database connection logic and ensures that any issues are properly logged for debugging purposes.
    /// </summary>
    /// <returns></returns>
    private NpgsqlConnection OpenConnection()
    {
        try
        {
            var conn = new NpgsqlConnection(_connectionString);
            conn.Open();
            return conn;
        }
        catch (Exception ex)
        {
            logger.AddLog(
                $"Error connecting to database: {ex.Message}",
                ImGuiLogger.LogTypes.Error
            );
            throw;
        }
    }

    public void TestConnection()
    {
        using var conn = OpenConnection();
    }

    /// <summary>
    /// Retrieves a list of student IDs from the database by executing a SQL query that selects the IDs of users with a role_id of 2 (indicating they are students). The method opens a connection to the database, executes the query, and reads the results into a list of integers. If there are any issues during the database connection or query execution, it logs the error message using the ImGuiLogger and rethrows the exception for further handling. This method provides a way to programmatically access student information from the database for use in the application.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Retrieves a list of teacher IDs from the database by executing a SQL query that selects the IDs of users with a role_id of 1 (indicating they are teachers). The method opens a connection to the database, executes the query, and reads the results into a list of integers. If there are any issues during the database connection or query execution, it logs the error message using the ImGuiLogger and rethrows the exception for further handling. This method provides a way to programmatically access teacher information from the database for use in the application.
    /// </summary>
    /// <returns></returns>
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

    /// <summary>
    /// Retrieves a list of teacher information from the database by executing a SQL query that selects the id, username, role_id, and password of users with a role_id of 1 (indicating they are teachers). The method opens a connection to the database, executes the query, and reads the results into a list of User objects containing the relevant information. If there are any issues during the database connection or query execution, it logs the error message using the ImGuiLogger and rethrows the exception for further handling. This method provides a way to programmatically access detailed teacher information from the database for use in the application.
    /// </summary>
    /// <returns></returns>
    public List<User> GetTeachersWithInfo()
    {
        var teachers = new List<User>();
        using var conn = OpenConnection();

        using var cmd = new NpgsqlCommand(
            "SELECT id, username, role_id, pass FROM Users WHERE role_id = 1",
            conn
        );
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            teachers.Add(
                new User
                {
                    Id = reader.GetInt32(0),
                    Username = reader.GetString(1),
                    RoleId = reader.GetInt32(2),
                    Password = reader.GetString(3),
                }
            );
        }

        return teachers;
    }

    /// <summary>
    /// Retrieves a dictionary of student structures from the database for a given student ID by executing a SQL query that selects the content of structures associated with the specified student. The method opens a connection to the database, executes the query, and reads the results into a dictionary where the key is "content" and the value is the JSON string representing the structure. If there are any issues during the database connection or query execution, it logs the error message using the ImGuiLogger and rethrows the exception for further handling. This method provides a way to programmatically access a student's saved structures from the database for use in the application.
    /// </summary>
    /// <param name="studentId"></param>
    /// <returns></returns>
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

    /// <summary>
    /// Retrieves a user from the database based on the provided username or user ID. If the input can be parsed as an integer, it treats it as a user ID and retrieves the user by ID. Otherwise, it treats the input as a username and retrieves the user by username. The method opens a connection to the database, executes the appropriate SQL query, and reads the results into a User object containing the relevant information. If there are any issues during the database connection or query execution, it logs the error message using the ImGuiLogger and rethrows the exception for further handling. This method provides a way to programmatically access user information from the database based on either username or user ID for use in the application.
    /// </summary>
    /// <param name="user"></param>
    /// <returns></returns>
    public User GetUser(string user)
    {
        if (!int.TryParse(user, out var userId))
        {
            using var conn = OpenConnection();
            using var cmd = new NpgsqlCommand(
                "SELECT id, username, role_id, pass FROM Users WHERE username = @username LIMIT 1",
                conn
            );
            cmd.Parameters.AddWithValue("username", user);

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
                Password = reader.GetString(3),
            };
        }

        return GetUserById(userId);
    }

    private User GetUserById(int userId)
    {
        using var conn = OpenConnection();
        using var cmd = new NpgsqlCommand(
            "SELECT id, username, role_id, pass FROM Users WHERE id = @id LIMIT 1",
            conn
        );
        cmd.Parameters.AddWithValue("id", userId);

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
            Password = reader.GetString(3),
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

    public int CreateAssignment(string title, string description, DateTime? dueDate, int teacherId)
    {
        using var conn = OpenConnection();
        using var cmd = new NpgsqlCommand(
            @"INSERT INTO Assignments (title, description, due_date, teacher_id)
              VALUES (@title, @description, @dueDate, @teacherId)
              RETURNING id",
            conn
        );
        cmd.Parameters.AddWithValue("title", title);
        cmd.Parameters.AddWithValue("description", description);
        cmd.Parameters.AddWithValue("dueDate", dueDate.HasValue ? dueDate.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("teacherId", teacherId);
        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<StructureInfo> GetStructuresForUser(int userId)
    {
        var structures = new List<StructureInfo>();
        using var conn = OpenConnection();

        using var cmd = new NpgsqlCommand(
            @"SELECT s.id, s.assignment_id, s.student_id, u.username, a.title, s.submitted_at
              FROM Structures s
              LEFT JOIN Users u ON s.student_id = u.id
              LEFT JOIN Assignments a ON s.assignment_id = a.id
              WHERE s.student_id = @userId
              ORDER BY s.submitted_at DESC",
            conn
        );
        cmd.Parameters.AddWithValue("userId", userId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            structures.Add(
                new StructureInfo
                {
                    Id = reader.GetInt32(0),
                    AssignmentId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    StudentName = reader.IsDBNull(3) ? "Unknown" : reader.GetString(3),
                    AssignmentTitle = reader.IsDBNull(4) ? "No Assignment" : reader.GetString(4),
                    SubmittedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                }
            );
        }

        return structures;
    }
    /// <summary>
    /// Retrieves the content of a structure from the database based on the provided structure ID. The method opens a connection to the database, executes a SQL query to select the content of the structure with the specified ID, and returns the content as a string. If there are any issues during the database connection or query execution, it logs the error message using the ImGuiLogger and rethrows the exception for further handling. This method provides a way to programmatically access the content of a specific structure from the database for use in the application.
    /// </summary>
    /// <param name="structureId"></param>
    /// <returns>StructureJson</returns>

    public string GetStructureContent(int structureId)
    {
        using var conn = OpenConnection();
        using var cmd = new NpgsqlCommand(
            "SELECT content::text FROM Structures WHERE id = @id",
            conn
        );
        cmd.Parameters.AddWithValue("id", structureId);

        var result = cmd.ExecuteScalar();
        return result?.ToString();
    }

    public int SaveStructureWithName(
        int studentId,
        string contentJson,
        string title = null,
        int? assignmentId = null
    )
    {
        using var conn = OpenConnection();

        int actualAssignmentId;
        if (assignmentId.HasValue)
        {
            actualAssignmentId = assignmentId.Value;
        }
        else
        {
            using var createCmd = new NpgsqlCommand(
                @"INSERT INTO Assignments (title, description, teacher_id) 
                  VALUES (@title, 'Personal save', @teacherId) 
                  RETURNING id",
                conn
            );
            createCmd.Parameters.AddWithValue(
                "title",
                title ?? $"Save_{DateTime.Now:yyyyMMdd_HHmmss}"
            );
            createCmd.Parameters.AddWithValue("teacherId", studentId);
            actualAssignmentId = Convert.ToInt32(createCmd.ExecuteScalar());
        }

        using var cmd = new NpgsqlCommand(
            @"INSERT INTO Structures (assignment_id, student_id, content, submitted_at)
              VALUES (@assignment_id, @student_id, @content::jsonb, NOW())
              ON CONFLICT (assignment_id, student_id) DO UPDATE
              SET content = EXCLUDED.content, submitted_at = EXCLUDED.submitted_at
              RETURNING id",
            conn
        );
        cmd.Parameters.AddWithValue("assignment_id", actualAssignmentId);
        cmd.Parameters.AddWithValue("student_id", studentId);
        cmd.Parameters.AddWithValue("content", contentJson);

        return Convert.ToInt32(cmd.ExecuteScalar());
    }

    public List<Assignment> GetAssignmentsForTeacher(int teacherId)
    {
        var assignments = new List<Assignment>();
        using var conn = OpenConnection();

        using var cmd = new NpgsqlCommand(
            "SELECT id, title, description FROM Assignments WHERE teacher_id = @teacherId",
            conn
        );
        cmd.Parameters.AddWithValue("teacherId", teacherId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            assignments.Add(
                new Assignment
                {
                    Id = reader.GetInt32(0),
                    Title = reader.GetString(1),
                    Description = reader.GetString(2),
                    TeacherId = teacherId,
                }
            );
        }

        return assignments;
    }

    public List<StructureInfo> GetStructuresForAssignment(int assignmentId)
    {
        var structures = new List<StructureInfo>();
        using var conn = OpenConnection();

        using var cmd = new NpgsqlCommand(
            @"SELECT s.id, s.assignment_id, s.student_id, u.username, a.title, s.submitted_at
              FROM Structures s
              LEFT JOIN Users u ON s.student_id = u.id
              LEFT JOIN Assignments a ON s.assignment_id = a.id
              WHERE s.assignment_id = @assignmentId
              ORDER BY s.submitted_at DESC",
            conn
        );
        cmd.Parameters.AddWithValue("assignmentId", assignmentId);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            structures.Add(
                new StructureInfo
                {
                    Id = reader.GetInt32(0),
                    AssignmentId = reader.IsDBNull(1) ? null : reader.GetInt32(1),
                    StudentId = reader.GetInt32(2),
                    StudentName = reader.IsDBNull(3) ? "Unknown" : reader.GetString(3),
                    AssignmentTitle = reader.IsDBNull(4) ? "Personal Save" : reader.GetString(4),
                    SubmittedAt = reader.IsDBNull(5) ? null : reader.GetDateTime(5),
                }
            );
        }

        return structures;
    }

    public class User
    {
        public int Id { get; set; }
        public string Username { get; set; }
        public int RoleId { get; set; }
        public string Password { get; set; }
    }

    public class StructureInfo
    {
        public int Id { get; set; }
        public int? AssignmentId { get; set; }
        public int StudentId { get; set; }
        public string StudentName { get; set; }
        public string AssignmentTitle { get; set; }
        public DateTime? SubmittedAt { get; set; }

        public string DisplayName =>
            $"{AssignmentTitle} - {StudentName} ({SubmittedAt?.ToString("g") ?? "No date"})";
    }

    public class Assignment
    {
        public int Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public int TeacherId { get; set; }
    }

    public class Roles
    {
        public static readonly int Teacher = 1;
        public static readonly int Student = 2;
    }
}
