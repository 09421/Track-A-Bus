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
    /// <summary>
    /// Handles all acces to the MySQL database.
    /// </summary>
    public class DatabaseAcces
    {
        //selection binary semaphore.
        public static bool SelectWait = false;
        /// <summary>
        /// Handles a query operation on the MySQL database.
        /// </summary>
        /// <param name="rawQueryText">The raw query text to execute on the database</param>
        /// <param name="columns">A list of strings, containing the column names, in the order they are retrieved with the raw query</param>
        /// <returns>A list of string where each each column in each row lies sequentially. Example: [Column1Row1][Column2Row1][Column1Row2][Columm2Row2]...</returns>
        public static List<string> Query(string rawQueryText, List<string> columns)
        {
            //While the semahpore is closed.
            while(SelectWait)
            {
                Thread.Sleep(10);
            }
            //Take the semaphore
            SelectWait = true;
            //Create the connection from the connection string in the ConfigratatioManager
            using(MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["TrackABusConn"].ToString()))
            {
                //Create a command on this connection
                using(MySqlCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        List<string> returnList = new List<string>();
                        //Set commandtext
                        cmd.CommandText = rawQueryText;
                        //Open connection
                        conn.Open();
                        //Execute the command that returns a data reader.
                        MySqlDataReader reader = cmd.ExecuteReader();
                        //As long as there are rows to read
                        while (reader.Read())
                        {
                            //Add each column sequentially
                            foreach (string c in columns)
                            {
                                returnList.Add(reader[c].ToString());
                            }
                        }
                        //Close reader
                        reader.Close();
                        //Close connection
                        conn.Close();
                        //Release semaphore.
                        SelectWait = false;
                      
                        return returnList;
                    }
                    catch(Exception e)
                    {
                        //If error happens release semaphore and return null. This could happen on a timeout, or with a wrong query.
                        SelectWait = false;
                        return null;
                    }
                }
            } 
        }

        //Insertion/Updating binary semaphore semaphore.
        public static bool InsertWait = false;
        /// <summary>
        /// Handles and insert or update on the database.
        /// </summary>
        /// <param name="rawInsertionOrUpdatingText"></param>
        public static void InsertOrUpdate(string rawInsertionOrUpdatingText)
        {
            //Wait while semaphore is taken.
            while(InsertWait)
            {
                Thread.Sleep(10);
            }
            //Take semaphore
            InsertWait = true;
            //Create connection
            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["TrackABusConn"].ToString()))
            {
                //Create command
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    try
                    {
                        //Set command text
                        cmd.CommandText = rawInsertionOrUpdatingText;
                        conn.Open();
                        //Execute command text. Returns nothing, since they are insertions or updates.
                        cmd.ExecuteNonQuery();
                        conn.Close();
                    }
                    //If something goes wrong, release semaphore. Could be wrong MySQL statement or timeout.
                    catch (Exception e)
                    { InsertWait = false; }
                }
            }
            InsertWait = false;
        }


        /// <summary>
        /// Removes all GPS positions of the busses.
        /// </summary>
        public static void Truncate()
        {
            string query = "Truncate GPSPosition";
            DatabaseAcces.InsertOrUpdate(query);
        }
    }
}
