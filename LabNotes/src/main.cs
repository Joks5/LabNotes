using System;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Microsoft.Data.SqlClient;
using LabPCTracker.Backend;

// ---- TEMPORARY TEST (delete when everything passes) ----
static void RunDbTest()
{
    var log = new StringBuilder();
    int failures = 0;
    SqlConnection conn = null;

    void Info(string s) => log.AppendLine("       " + s);

    bool Step(string title, Action action)
    {
        try
        {
            action();
            log.AppendLine($"[PASS] {title}");
            return true;
        }
        catch (Exception ex)
        {
            failures++;
            log.AppendLine($"[FAIL] {title}");
            log.AppendLine("       Error: " + ex.Message);
            string hint = Explain(ex);
            if (hint != null) log.AppendLine("       Likely cause: " + hint);
            return false;
        }
    }

    // 1. Is schema.sql where the app looks for it?
    string schemaPath = Path.Combine(AppContext.BaseDirectory, "database", "schema.sql");
    Step("1. schema.sql is next to the app", () =>
    {
        Info("Looking in: " + schemaPath);
        if (!File.Exists(schemaPath))
            throw new FileNotFoundException("schema.sql was not found at that path.");
    });

    // 2. Create the database and tables if needed
    Step("2. Db.Initialize() runs", () => Db.Initialize());

    // 3. Connect and show where we are
    bool connected = Step("3. Connected to MyDb", () =>
    {
        conn = Db.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            SELECT @@SERVERNAME, DB_NAME(),
                   (SELECT TOP 1 physical_name FROM sys.master_files
                    WHERE database_id = DB_ID() AND type_desc = 'ROWS')";
        using var r = cmd.ExecuteReader();
        if (r.Read())
        {
            Info("Server:   " + r.GetString(0));
            Info("Database: " + r.GetString(1));
            Info("File:     " + r.GetString(2));
        }
    });

    bool tablesOk = false;

    if (!connected)
    {
        log.AppendLine("[SKIP] Steps 4-7 need a connection, so they were skipped.");
    }
    else
    {
        // 4. Do all the tables exist?
        string[] expectedTables = { "users", "labs", "computers", "software", "pc_software", "schedules" };
        tablesOk = Step("4. All 6 tables exist", () =>
        {
            var found = new System.Collections.Generic.HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT TABLE_NAME FROM INFORMATION_SCHEMA.TABLES";
            using var r = cmd.ExecuteReader();
            while (r.Read()) found.Add(r.GetString(0));

            var missing = expectedTables.Where(t => !found.Contains(t)).ToList();
            if (missing.Count > 0)
                throw new Exception("Missing tables: " + string.Join(", ", missing) +
                    ". The schema did not run. Check step 1, then drop MyDb in SSMS and run again.");
        });

        if (!tablesOk)
        {
            log.AppendLine("[SKIP] Steps 5-7 need the tables, so they were skipped.");
        }
        else
        {
            // 5. Do the row counts match the schema?
            Step("5. Row counts are correct", () =>
            {
                var expected = new (string table, int count)[]
                {
                    ("users", 3), ("labs", 5), ("computers", 125),
                    ("software", 16), ("pc_software", 625), ("schedules", 10)
                };
                var problems = new System.Collections.Generic.List<string>();

                foreach (var (table, count) in expected)
                {
                    using var cmd = conn.CreateCommand();
                    cmd.CommandText = $"SELECT COUNT(*) FROM {table}";
                    int n = (int)cmd.ExecuteScalar();
                    Info($"{table}: {n} (expected {count})");
                    if (n != count) problems.Add($"{table} has {n}, expected {count}");
                }

                if (problems.Count > 0)
                    throw new Exception(string.Join("; ", problems) +
                        ". Too many rows usually means a duplicate INSERT block is still in schema.sql.");
            });

            // 6. Do JOINs between tables work?
            Step("6. JOIN across schedules, labs and users", () =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT TOP 3 l.lab_name, u.full_name, s.start_time, s.end_time
                    FROM schedules s
                    JOIN labs l  ON s.lab_id  = l.lab_id
                    JOIN users u ON s.user_id = u.user_id
                    ORDER BY s.start_time";
                using var r = cmd.ExecuteReader();
                int rows = 0;
                while (r.Read())
                {
                    rows++;
                    Info($"{r.GetString(0)} | {r.GetString(1)} | {r.GetDateTime(2):MMM d HH:mm}-{r.GetDateTime(3):HH:mm}");
                }
                if (rows == 0) throw new Exception("The JOIN returned no rows, so the schedules data is missing or doesn't link up.");
            });

            // 7. Do parameterized queries work?
            Step("7. Parameter query (software on LAB1-PC01)", () =>
            {
                using var cmd = conn.CreateCommand();
                cmd.CommandText = @"
                    SELECT sw.software_name
                    FROM computers c
                    JOIN pc_software ps ON c.computer_id = ps.computer_id
                    JOIN software sw    ON ps.software_id = sw.software_id
                    WHERE c.pc_name = @pc";
                cmd.Parameters.AddWithValue("@pc", "LAB1-PC01");
                using var r = cmd.ExecuteReader();
                int rows = 0;
                while (r.Read()) { rows++; Info("- " + r.GetString(0)); }
                if (rows != 5) throw new Exception($"Expected 5 programs, got {rows}.");
            });
        }
    }

    conn?.Dispose();

    log.AppendLine();
    log.AppendLine(failures == 0 ? "ALL CHECKS PASSED" : $"{failures} CHECK(S) FAILED. Fix the first [FAIL] first; later ones are often caused by it.");
    MessageBox.Show(log.ToString(), failures == 0 ? "DB test: PASSED" : "DB test: FAILED");
}

// Turns common errors into plain English
static string Explain(Exception ex)
{
    if (ex is FileNotFoundException)
        return "schema.sql wasn't copied next to the app. The 'database' folder (singular) must sit beside the .csproj, and the .csproj needs the <None Update=\"database\\schema.sql\" ...> line.";

    if (ex is SqlException sql)
    {
        switch (sql.Number)
        {
            case 208:  return "A table is missing. MyDb exists but the schema never ran. Drop MyDb in SSMS and run again.";
            case 2714: return "A table already exists. The schema ran twice or partly. Drop MyDb in SSMS and run again.";
            case 1801: return "schema.sql still has a 'CREATE DATABASE MyDb' line. Delete it, plus the 'USE MyDb' line.";
            case 102:
            case 156:
            case 170:  return "Syntax error in schema.sql. Check that it is T-SQL and that GO sits alone on its own line.";
            case 2627: return "Duplicate key. An INSERT block probably appears twice in schema.sql.";
            case 547:  return "A foreign key points to a row that doesn't exist. Check the insert order in schema.sql (parent tables first).";
            case 4060:
            case 911:  return "The database MyDb can't be opened or doesn't exist.";
            case -1:
            case 2:
            case 53:   return "Can't reach the server. Run 'sqllocaldb info' in a terminal and check the server name in db.cs.";
            case 18456: return "Login failed. Check Integrated Security in the connection string.";
        }
    }
    return null;
}


try
{
    RunDbTest();
}
catch (Exception ex)
{
    MessageBox.Show("Unexpected error: " + ex.Message, "DB test: ERROR");
}