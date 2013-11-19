using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MapDrawRouteTool.Models;
using Newtonsoft.Json;
using System.Diagnostics;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace MapDrawRouteTool.Controllers
{
    public class RouteController : Controller
    {
        //
        // GET: /Route/

        public ActionResult Index()
        {
            return View();

        }

        [HttpGet]
        public JsonResult GetStops()
        {
            string ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TrackABus"].ConnectionString;
            using (var connection = new MySqlConnection(ConnectionString))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        var stops = new List<stop>();
                        connection.Open();
                        cmd.CommandText = "SELECT ID, StopName FROM BusStop;";
                        var read = cmd.ExecuteReader();

                        while (read.Read())
                        {
                            stops.Add(new stop() { ID = read.GetInt32(0), name = read.GetString(1) });
                        }

                        read.Close();
                        connection.Close();


                        return ConvertToJason(stops);
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                        connection.Close();
                        return null;
                    }
                }
            }
        }

        private JsonResult ConvertToJason(List<stop> stops)
        {
            JsonResult jr = new JsonResult();

            jr.Data = stops.ToList();
            jr.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jr;
        }

        private JsonResult ConvertToJason(List<StopPos> stops)
        {
            JsonResult jr = new JsonResult();

            jr.Data = stops.ToList();
            jr.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jr;
        }

        private JsonResult ConvertToJason(List<Points> stops)
        {
            JsonResult jr = new JsonResult();

            jr.Data = stops.ToList();
            jr.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jr;
        }

        public JsonResult GetRouteLatLng()
        {
            string ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TrackABus"].ConnectionString;

            using (var connection = new MySqlConnection(ConnectionString))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();

                        cmd.CommandText = "SELECT fk_RoutePoint FROM trackabus_dk_db.BusRoute_RoutePoint WHERE fk_BusRoute = 1";

                        var read = cmd.ExecuteReader();

                        List<int> ID = new List<int>();
                        while (read.Read())
                        {
                            ID.Add(read.GetInt32(0));
                        }
                        read.Close();

                        cmd.CommandText = "SELECT * FROM trackabus_dk_db.RoutePoint WHERE ID IN ( ";

                        for (var i = 0; i < ID.Count(); i++)
                        {
                            cmd.CommandText += ID[i] + ", ";                        
                        }
                        cmd.CommandText = cmd.CommandText.TrimEnd(", ".ToCharArray());
                        cmd.CommandText += ") ";

                        cmd.CommandText += "ORDER BY FIELD(ID, ";

                        for (var i = 0; i < ID.Count(); i++)
                        {
                            cmd.CommandText += ID[i] + ", ";
                        }
                        cmd.CommandText = cmd.CommandText.TrimEnd(", ".ToCharArray());
                        cmd.CommandText += ");";

                        var read2 = cmd.ExecuteReader();

                        List<Points> LatLng = new List<Points>();
                        while (read2.Read())
                        {
                            LatLng.Add(new Points() {ID=read2.GetInt32(0), Lat = read2.GetDecimal(1), Lng = read2.GetDecimal(2) });
                        }
                        read2.Close();
                        
                        connection.Close();
                        return ConvertToJason(LatLng);
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

        public JsonResult GetLatLng(List<string> StopNames)
        {
            string ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TrackABus"].ConnectionString;

            using (var connection = new MySqlConnection(ConnectionString))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();

                        cmd.CommandText = "SELECT l.Latitude, l.Longitude FROM RoutePoint AS l " +
                                          "INNER JOIN BusStop AS s ON l.ID = s.fk_RoutePoint " +
                                          "WHERE s.StopName = ";
                        for (var i = 0; i < StopNames.Count(); i++)
                        {
                            cmd.CommandText += "'" + StopNames[i] + "' OR s.StopName = ";
                        }
                        cmd.CommandText = cmd.CommandText.TrimEnd(" OR s.StopName = ".ToCharArray());

                        var read = cmd.ExecuteReader();

                        List<StopPos> LatLng = new List<StopPos>();
                        while (read.Read())
                        {
                            LatLng.Add(new StopPos() { Lat = read.GetDecimal(0), Lng = read.GetDecimal(1) });
                        }
                        read.Close();
                        connection.Clone();

                        return ConvertToJason(LatLng);

                    }
                    catch (Exception e)
                    {
                        connection.Clone();
                        Debug.WriteLine(e.Message);
                        return null;
                    }
                }
            }
        }
    }

    //public class stop
    //{
    //    public int ID;
    //    public string name;
    //}

    public class Points
    {
        public int ID;
        public Decimal Lat;
        public Decimal Lng;
    }
}
