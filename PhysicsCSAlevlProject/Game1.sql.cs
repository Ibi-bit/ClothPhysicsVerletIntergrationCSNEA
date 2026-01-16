using Npgsql;

class Game1Database
{
    private string connectionString =
        "Host=localhost;Username=postgres;Password=password;Database=Game1";

    public List<int> GetStudents()
    {
        var studentIds = new List<int>();
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        using var cmd = new NpgsqlCommand("SELECT content FROM Users WHERE role = 'student'", conn);
        cmd.Parameters.AddWithValue("id", studentId);
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

        using var cmd = new NpgsqlCommand("SELECT content FROM Users WHERE role = 'teacher'", conn);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            teacherIds.Add(reader.GetInt32(0));
        }
        return teacherIds;
    }
    public void GetStudentData(int studentId)
    {
        using var conn = new NpgsqlConnection(connectionString);
        conn.Open();

        using var cmd = new NpgsqlCommand("SELECT content FROM Structures WHERE student_id = @id", conn);
        cmd.Parameters.AddWithValue("id", studentId);
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            Console.WriteLine(reader.GetString(0));
        }
    }   
}
