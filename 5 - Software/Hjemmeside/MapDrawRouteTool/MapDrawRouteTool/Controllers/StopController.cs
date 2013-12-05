using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using System.Configuration;
using MapDrawRouteTool.Models;

namespace MapDrawRouteTool.Controllers
{
    public class StopController : Controller
    {
        //
        // GET: /Stop/

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult EditStops()
        {
            return View();
        }

        public ActionResult AddStops()
        {
            return View();
        }

        public int Save(string c, string n)
        {
            var cords = formatCord(c);
            var names = formatNames(n);

            for (var i = 0; i < cords.Count; i = i + 2)
            {
                using (var connection = new MySqlConnection(getConnectionString()))
                {
                    using (var cmd = connection.CreateCommand())
                    {
                        try
                        {
                            if (cords.Count == (names.Count * 2))
                            {

                                cmd.CommandText = string.Format("INSERT INTO RoutePoint (Latitude, Longitude) VALUES({0}, {1});", cords[i], cords[i + 1]);

                                connection.Open();
                                cmd.ExecuteNonQuery();
                                connection.Close();
                                cmd.CommandText = "SELECT ID FROM RoutePoint WHERE Latitude = " + cords[i] + ";";

                                connection.Open();
                                MySqlDataReader read = cmd.ExecuteReader();
                                var ID = 0;
                                while (read.Read())
                                {
                                    ID = read.GetInt32(0);
                                }
                                read.Close();
                                connection.Close();


                                cmd.CommandText = string.Format("INSERT INTO BusStop (StopName, fk_RoutePoint) VALUES('{0}', {1});", names[i / 2], ID);
                                connection.Open();
                                cmd.ExecuteNonQuery();
                                connection.Close();
                                
                            }
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.ToString());
                            connection.Close();
                            return -1;
                        }
                    }
                }
            }
            return 0;
        }

        public int Delete(string stop)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        int fkID = 0;
                        cmd.CommandText = string.Format("SELECT fk_RoutePoint FROM BusStop WHERE StopName = '{0}'", stop);
                        connection.Open();
                        var read = cmd.ExecuteReader();
                        while (read.Read())
                        {
                            fkID = read.GetInt32(0);
                        }
                        read.Close();
                        connection.Close();

                        cmd.CommandText = string.Format("DELETE FROM BusStop WHERE StopName = '{0}'", stop);
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();

                        cmd.CommandText = string.Format("DELETE FROM RoutePoint WHERE ID = {0}", fkID);
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();
                        return 0;
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        Debug.WriteLine(e.Message);
                        return -1;
                    }
                }
            }
        }

        public int Rename(string oldName, string newName, string NewPos)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {

                        cmd.CommandText = string.Format("UPDATE BusStop SET StopName = '{0}'  WHERE StopName = '{1}'", newName, oldName);
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();

                        cmd.CommandText = string.Format("UPDATE RoutePoint SET Latitude = {0}, Longitude = {1} WHERE ID = " +
                                          "(SELECT fk_RoutePoint FROM BusStop WHERE StopName = '{2}')", NewPos.Split(',')[0].TrimStart('('), NewPos.Split(',')[1].TrimEnd(')'), newName);
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();

                        return 0;

                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                        connection.Close();
                        return -1;
                    }
                }
            }
        }

        public JsonResult GetAllStops()
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        List<string> names = new List<string>();
                        cmd.CommandText = "SELECT StopName FROM BusStop";
                        connection.Open();

                        var read = cmd.ExecuteReader();                        
                        while (read.Read())
                        {
                            names.Add(read.GetString(0));
                        }
                        read.Close();
                        connection.Close();

                        return JConverter.ConvertToJson(names);
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        Debug.WriteLine(e.Message);
                        return null;
                    }
                }
            }
        }

        public JsonResult GetPosistion(string stopName)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        List<string> LatLng = new List<string>();
                        cmd.CommandText = string.Format("SELECT Latitude, Longitude FROM RoutePoint " +
                                          "INNER JOIN BusStop ON RoutePoint.ID = BusStop.fk_RoutePoint " +
                                          "WHERE BusStop.StopName = '{0}'", stopName);

                        connection.Open();
                        var read = cmd.ExecuteReader();
                        while (read.Read())
                        {
                            LatLng.Add(read.GetDecimal(0).ToString());
                            LatLng.Add(read.GetDecimal(1).ToString());
                        }
                        read.Close();
                        connection.Close();
                        return JConverter.ConvertToJson(LatLng);
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        Debug.WriteLine(e.Message);
                        return null;
                    }
                }
            }

        }

        private List<string> formatNames(string name)
        {
            var t = name.Split(',');
            List<string> names = new List<string>();
            foreach (var y in t)
            {
                var j = y;
                j = j.TrimEnd(']');
                j = j.TrimStart('[');
                j = j.TrimEnd('"');
                j = j.TrimStart('"');
                j = j.TrimEnd(')');
                j = j.TrimStart('(');
                j = j.Trim();
                names.Add(j);
            }
            return names;
        }

        private List<string> formatCord(string cord)
        {
            var t = cord.Split(',');
            List<string> cords = new List<string>();
            foreach (var y in t)
            {
                var j = y;
                j = j.TrimEnd(']');
                j = j.TrimStart('[');
                j = j.TrimEnd('"');
                j = j.TrimStart('"');
                j = j.TrimEnd(')');
                j = j.TrimStart('(');
                j = j.Trim();
                cords.Add(j);
            }
            return cords;
        }

        private static string getConnectionString()
        {
            return System.Configuration.ConfigurationManager.ConnectionStrings["TrackABus"].ConnectionString;
        }
    }
}
