using MySql.Data.MySqlClient;
using System;
using System.Configuration;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;

namespace TrackABusSim
{
    public class DatabaseAcces
    {
        private static bool SelectWait = false;
        public static List<string> Query(string rawQueryText, List<string> columns)
        {
            while(SelectWait)
            {
                Thread.Sleep(10);
            }
            SelectWait = true;
            using(MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["TrackABusConn"].ToString()))
            {
                using(MySqlCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        List<string> returnList = new List<string>();
                        cmd.CommandText = rawQueryText;
                        conn.Open();
                        MySqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            foreach (string c in columns)
                            {
                                returnList.Add(reader[c].ToString());
                            }
                        }
                        reader.Close();
                        conn.Close();
                        SelectWait = false;
                        return returnList;
                    }
                    catch(Exception e)
                    {
                        SelectWait = false;
                        return null;
                    }
                }
            }
            
        }

        private static bool InsertWait = false;
        public static void InsertOrUpdate(string rawInsertionOrUpdatingText)
        {
            while(InsertWait)
            {
                Thread.Sleep(10);
            }
            InsertWait = true;
            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["TrackABusConn"].ToString()))
            {
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = rawInsertionOrUpdatingText;
                        conn.Open();
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                    catch (Exception e)
                    { }
                }
            }
            InsertWait = false;
        }
    }
}
