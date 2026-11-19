using System;
using System.Collections.Generic;
using Microsoft.Data.SqlClient; 

namespace LabPCTracker.Backend
{
    public class Lab
    {
        public int id;
        public string name;

        // When this object is called, it will return the name of the lab
        public override string ToString()
        {
            return name;
        }
    }

    public static class Computers
    {
        public static List<Lab> GetBookedLabs()
        {
            var labs = new List<Lab>();

            using var conn = Db.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = @"
                SELECT lab_id, lab_name
                FROM Labs
                ORDER BY name;";
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

        //To create GetStatus. A method that lists the status of every computer whether working faulty or broken
    }


}
    