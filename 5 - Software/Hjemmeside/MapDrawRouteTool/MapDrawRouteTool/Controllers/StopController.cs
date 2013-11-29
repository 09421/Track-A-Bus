using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using System.Configuration;

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

        public void Save(string c, string n)
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
                                connection.Open();

                                cmd.CommandText = "INSERT INTO RoutePoint (Latitude, Longitude) VALUES(?Lat, ?lng);";
                                cmd.Parameters.Add("?Lat", MySqlDbType.Decimal).Value = decimal.Parse(cords[i]);
                                cmd.Parameters.Add("?lng", MySqlDbType.Decimal).Value = decimal.Parse(cords[i + 1]);
                                cmd.ExecuteNonQuery();

                                cmd.CommandText = "SELECT ID FROM RoutePoint WHERE Latitude = " + cords[i] + ";";
                                MySqlDataReader read = cmd.ExecuteReader();
                                var ID = 0;
                                while (read.Read())
                                {
                                    ID = read.GetInt32(0);
                                }
                                read.Close();

                                cmd.CommandText = "INSERT INTO BusStop (StopName, fk_RoutePoint) VALUES(?name, ?routePoint);";
                                cmd.Parameters.Add("?name", MySqlDbType.String).Value = names[i / 2];
                                cmd.Parameters.Add("?routePoint", MySqlDbType.Int32).Value = ID;
                                cmd.ExecuteNonQuery();
                            }
                            connection.Close();
                        }
                        catch (Exception e)
                        {
                            Debug.WriteLine(e.ToString());
                            connection.Close();
                        }
                    }
                }
            }
        }

        public void Delete(string stop)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = string.Format("DELETE FROM BusStop WHERE StopName = '{0}'", stop);
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        Debug.WriteLine(e.Message);
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

                        return ConvertToJason(names);
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

        private JsonResult ConvertToJason(List<string> names)
        {
            JsonResult jr = new JsonResult();

            jr.Data = names.ToList();
            jr.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jr;
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
