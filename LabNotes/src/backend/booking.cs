using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient;

namespace LabPCTracker.Backend
{
    public class Teacher
    {
        public int id;
        public string name;

        // When this object is called, it will return the name of the teacher
        public override string ToString()
        {
            return name;
        }
    }

    public static class Schedules
    {
        // Creates a list of teachers from the database and returns it
        public static List<Teacher> GetTeachers()
        {
            var teachers = new List<Teacher>();

            using var conn = Db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT id, name
                FROM Teachers
                ORDER BY name;";
            var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                var teacher = new Teacher
                {
                    id = reader.GetInt32(0),
                    name = reader.GetString(1)
                };
                teachers.Add(teacher);
            }

            return teachers;
        }
    
        // Creates a list of labs from the database and returns it
        // P.S class Lab is defined in logic.cs, but it shares namespace so it can be used here as well
        public static List<Lab> GetBookedLabs(DateTime start, DateTime end)
        {
            var labs = new List<Lab>();

            using var conn = Db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT l.lab_id, l.lab_name
                FROM labs l
                JOIN schedules s ON s.lab_id = l.lab_id
                WHERE s.start_time < @end AND s.end_time > @start
                ORDER BY l.lab_name;";
            cmd.Parameters.AddWithValue("@start", start);  
            cmd.Parameters.AddWithValue("@end", end);    
            var reader = cmd.ExecuteReader();

            while(reader.Read())
            {
                var lab = new Lab
                {
                    id = reader.GetInt32(0),
                    name = reader.GetString(1)
                };
                labs.Add(lab);
            }

            return labs;
        }

        public static void AddSchedule (DateTime start, DateTime end, int teacherId, int labId)
        {
            using var conn = Db.Open();
            using var cmd = conn.CreateCommand();

            cmd.CommandText = @"
                INSERT INTO Schedules (start_time, end_time, teacher_id, lab_id)
                VALUES (@start, @end, @teacherId, @labId);";

            cmd.Parameters.AddWithValue("@start", start);
            cmd.Parameters.AddWithValue("@end", end);
            cmd.Parameters.AddWithValue("@teacherId", teacherId);
            cmd.Parameters.AddWithValue("@labId", labId);
            cmd.ExecuteNonQuery();
        }
    }
}