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
    public class DirController : Controller
    {
        //
        // GET: /Dir/
        public ActionResult Index()
        {
            return View();
        }

        public void Save(List<string> route, List<string> stops, List<string> SubRoutes, string RouteNumber)
        {
            var routeNumberId = InsertRouteNumber(RouteNumber);
            if (route != null && stops != null)
            {
                var pointList = ConvertLatLng(route);
                InsertRoutePoints(pointList);
                InsertBusRoute_RoutePoint(routeNumberId, pointList);
                if (SubRoutes != null)
                {
                    InsertSubRouteIntoBusRoute(RouteNumber, SubRoutes.Count());
                    var g = ConvertSubRoute(SubRoutes);
                    for (var i = 0; i < g.Count(); i++)
                    {
                        InsertRoutePoints(g[i]);
                        InsertBusRoute_RoutePoint(routeNumberId+i+1, g[i]);
                    }
                }

                InsertBusRoute_BusStop(routeNumberId, stops);

            }
        }

        public int InsertRouteNumber(string routeNumber)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = string.Format("INSERT INTO BusRoute (RouteNumber, SubRoute) VALUES('{0}', 0);select last_insert_id();", routeNumber);
                        connection.Open();
                        var read = cmd.ExecuteReader();

                        int ID = 0;
                        while (read.Read())
                        {
                            ID = read.GetInt32(0);
                        }
                        connection.Close();

                        return ID;
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        return 0;
                    }
                }
            }
        }

        public void InsertSubRouteIntoBusRoute(string routeNumber, int count)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        for (var i = 0; i < count; i++)
                            cmd.CommandText = string.Format("INSERT INTO BusRoute (RouteNumber, SubRoute) VALUES('{0}', {1})", routeNumber, i + 1);
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                    }
                }
            }
        }

        public void InsertRoutePoints(List<string> points)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();
                        cmd.CommandText = "INSERT INTO RoutePoint (Latitude, Longitude) VALUES";
                        for (var i = 0; i < points.Count(); i = i + 2)
                        {
                            cmd.CommandText += "(" + points[i] + " , " + points[i + 1] + "),";
                        }

                        cmd.CommandText = cmd.CommandText.TrimEnd(',');
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

        public void InsertBusRoute_RoutePoint(int routeNumberID, List<string> points)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();
                        cmd.CommandText = "SELECT ID FROM RoutePoint WHERE";

                        for (var i = 0; i < points.Count(); i = i + 2)
                        {
                            cmd.CommandText += "(Latitude = " + points[i] + " AND Longitude = " + points[i + 1] + ") OR";
                        }
                        cmd.CommandText = cmd.CommandText.Trim(" OR".ToCharArray());

                        var read = cmd.ExecuteReader();

                        List<int> ID = new List<int>();
                        while (read.Read())
                        {
                            ID.Add(read.GetInt32(0));
                        }
                        read.Close();

                        cmd.CommandText = "INSERT INTO BusRoute_RoutePoint (fk_BusRoute, fk_RoutePoint) VALUES";
                        for (var i = 0; i < ID.Count(); i++)
                        {
                            cmd.CommandText += "(" + routeNumberID + " , " + ID[i] + "),";
                        }
                        cmd.CommandText = cmd.CommandText.TrimEnd(',');
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

        public void InsertBusRoute_BusStop(int routeNumberID, List<string> stops)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();
                        cmd.CommandText = "SELECT ID FROM BusStop WHERE StopName = ";

                        for (var i = 0; i < stops.Count(); i++)
                        {
                            cmd.CommandText += "'" + stops[i] + "' OR StopName = ";
                        }

                        cmd.CommandText = cmd.CommandText.TrimEnd(" OR StopName = ".ToCharArray());

                        var read = cmd.ExecuteReader();

                        List<int> ID = new List<int>();
                        while (read.Read())
                        {
                            ID.Add(read.GetInt32(0));
                        }
                        read.Close();
                        connection.Clone();


                        cmd.CommandText = "INSERT INTO BusRoute_BusStop (fk_BusRoute, fk_BusStop) VALUES";
                        for (var i = 0; i < ID.Count(); i++)
                        {
                            cmd.CommandText += "(" + routeNumberID + " , " + ID[i] + "),";
                        }
                        cmd.CommandText = cmd.CommandText.TrimEnd(',');
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

        public List<string> ConvertLatLng(List<string> latLng)
        {
            List<string> LatLng = new List<string>();

            foreach (var point in latLng)
            {
                var t = point.Split(',');
                LatLng.Add(t[0].TrimStart('('));
                LatLng.Add(t[1].TrimEnd(')'));

            }

            return LatLng;
        }

        public List<List<string>> ConvertSubRoute(List<string> subRoutes)
        {
            List<List<string>> result = new List<List<string>>();
            foreach (var route in subRoutes)
            {
                List<string> LatLng = new List<string>();
                var t = route.Split(',');
                for (var i = 0; i < t.Length; i = i + 2)
                {
                    LatLng.Add(t[i].TrimStart('('));
                    LatLng.Add(t[i + 1].TrimEnd(')'));
                }
                result.Add(LatLng);
            }

            return result;
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

        public JsonResult GetLatLng(List<string> StopNames)
        {

            using (var connection = new MySqlConnection(getConnectionString()))
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

        private JsonResult ConvertToJason(List<StopPos> stops)
        {
            JsonResult jr = new JsonResult();

            jr.Data = stops.ToList();
            jr.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jr;
        }

        private JsonResult ConvertToJason(List<stop> stops)
        {
            JsonResult jr = new JsonResult();

            jr.Data = stops.ToList();
            jr.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jr;
        }

        private static string getConnectionString()
        {
            return System.Configuration.ConfigurationManager.ConnectionStrings["TrackABus"].ConnectionString;
        }

    }

    public class stop
    {
        public int ID;
        public string name;
    }
    public class StopPos
    {
        public Decimal Lat;
        public Decimal Lng;
    }

    public class testclass
    {
        public List<string> ob;
        public List<string> pb;
    }
}
