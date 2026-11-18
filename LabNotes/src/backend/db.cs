using System;
using System.IO;
using Microsoft.Data.SqlClient;

namespace LabPCTracker.Backend
{
    public static class Db
    {
        private const string DbName = "MyDb";

        private const string ServerPart =
            @"Server=(localdb)\MSSQLLocalDB;Integrated Security=true;TrustServerCertificate=True;";

        private static readonly string MasterConn = ServerPart + "Database=master;";
        private static readonly string AppConn    = ServerPart + $"Database={DbName};";

        private static readonly string SchemaPath =
            Path.Combine(AppContext.BaseDirectory, "database", "schema.sql");

        // Opens a connection to MyDb. Foreign keys are always enforced in SQL Server,
        // so there is nothing to enable here.
        public static SqlConnection Open()
        {
            var conn = new SqlConnection(AppConn);
            conn.Open();
            return conn;
        }

        // Call once at startup from main.cs. Creates MyDb and its tables if missing.
        public static void Initialize()
        {
            bool created = false;

            using (var conn = new SqlConnection(MasterConn))
            {
                conn.Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = $@"
                    IF DB_ID('{DbName}') IS NULL
                    BEGIN
                        CREATE DATABASE [{DbName}];
                        SELECT 1;
                    END
                    ELSE SELECT 0;";
                created = (int)cmd.ExecuteScalar() == 1;
            }

            if (created && File.Exists(SchemaPath))
            {
                using var conn = Open();
                using var cmd = conn.CreateCommand();
                cmd.CommandText = File.ReadAllText(SchemaPath);
                cmd.ExecuteNonQuery();
            }
        }
    }
}