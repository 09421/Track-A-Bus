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
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            var routeNumberId = InsertRouteNumber(RouteNumber);
            List<List<string>> g = new List<List<string>>();
            List<string> pointList;
            int counter;
            if (route != null && stops != null)
            {
                pointList = ConvertLatLng(route);
                InsertRoutePoints(pointList);
                if (SubRoutes != null)
                {
                    InsertSubRouteIntoBusRoute(RouteNumber, SubRoutes.Count());
                    g = ConvertSubRoute(SubRoutes);
                    for (var i = 0; i < g.Count(); i++)
                    {
                        InsertRoutePoints(g[i]);
                    }
                    counter = SubRoutes.Count();
                }
                else
                    counter = 0;

                List<List<String>> RouteAndStops;
                List<string> BusRouteWithStops = new List<string>();
                List<string> BusStops = new List<string>();
                for (int i = 0; i <= counter; i++)
                {
                    if (i == 0)
                    {
                        RouteAndStops = CalculateBusStopsForRoute(stops, pointList, RouteNumber, i);
                        BusRouteWithStops = RouteAndStops[0];
                        BusStops = RouteAndStops[1];
                        InsertBusRoute_RoutePoint(routeNumberId, BusRouteWithStops);
                        InsertBusRoute_BusStop(routeNumberId, BusStops);
                    }
                    else
                    {
                        RouteAndStops = CalculateBusStopsForRoute(stops, g[i - 1], RouteNumber, i);
                        BusRouteWithStops = RouteAndStops[0];
                        BusStops = RouteAndStops[1];
                        InsertBusRoute_RoutePoint(routeNumberId+i, BusRouteWithStops);
                        InsertBusRoute_BusStop(routeNumberId + i, BusStops);
                    }
                    
                }
            }
        }

        private List<List<string>> CalculateBusStopsForRoute(List<string> stops, List<string> chosenRoute, string routeNumber, int subRoute)
        {
            int routeID;
            using(var connection = new MySqlConnection(getConnectionString()))
            {
                using(var cmd = connection.CreateCommand())
                {
                    connection.Open();
                    cmd.CommandText = string.Format("Select BusRoute.ID from BusRoute where BusRoute.RouteNumber = '{0}' and BusRoute.SubRoute = {1}", routeNumber, subRoute);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while(reader.Read())
                    {
                        routeID = int.Parse(reader["ID"].ToString());
                    }
                    connection.Close();
                }
            }
            
            List<List<string>> RouteAndStops = new List<List<string>>();
            List<string> RouteWithStopsID = new List<string>();
            List<string> StopOnRoute = new List<string>();
            int stopCounter = 0;

            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {

                    cmd.CommandText = "Select RoutePoint.ID from RoutePoint where ";
                    for (int j = 0; j < chosenRoute.Count - 2; j = j + 2)
                    {
                        cmd.CommandText += string.Format("(RoutePoint.Latitude={0} and RoutePoint.Longitude={1}) or ", chosenRoute[j], chosenRoute[j + 1]);
                    }
                    cmd.CommandText = cmd.CommandText.TrimEnd(" or ".ToCharArray());
                    cmd.CommandText += " Order by RoutePoint.ID asc";
                    connection.Open();
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        RouteWithStopsID.Add(reader["ID"].ToString());
                    }
                    reader.Close();
                    connection.Close();
                }
            }
            decimal currDistToStartOfRoute;
            decimal currDistToEndOfRoute;
            decimal leastDistToStartOfRoute = -1;
            decimal leastDistToEndOfRoute = -1;
            int leastStartStopID = 0;
            int leastEndStopID = 0 ;
            string leastStartStopName = "";
            string leastEndStopName = "";
            foreach(string s in stops)
            {
                int stopID = 0;
                decimal stopLat = 0;
                decimal stopLon = 0;
                decimal leastDist = -1;
                decimal currentDist;
                decimal distToEp;
                
                int pointBeforeStopIndex = 0;
                
                using (var connection = new MySqlConnection(getConnectionString()))
                {
                    using (var cmd = connection.CreateCommand())
                    {

                        cmd.CommandText = string.Format("Select RoutePoint.ID,RoutePoint.Latitude, RoutePoint.Longitude from RoutePoint "+ 
                                                        "inner join BusStop on RoutePoint.ID = BusStop.fk_RoutePoint " +
                                                        "where BusStop.StopName='{0}'", s);
                        connection.Open();
                        MySqlDataReader reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            stopID = int.Parse(reader["ID"].ToString());
                            stopLat = decimal.Parse(reader["Latitude"].ToString());
                            stopLon = decimal.Parse(reader["Longitude"].ToString());
                        }
                        reader.Close();
                        connection.Close();
                    }
                }

                for (int k = 0; k < chosenRoute.Count - 2; k = k + 2)
                {
                    if (k == 0)
                    {
                        currDistToStartOfRoute = (decimal)Haversine(stopLat, decimal.Parse(chosenRoute[0]), stopLon, decimal.Parse(chosenRoute[1]));
                        currDistToEndOfRoute = (decimal)Haversine(stopLat, decimal.Parse(chosenRoute[chosenRoute.Count - 2]), stopLon, decimal.Parse(chosenRoute[chosenRoute.Count - 1]));
                        if (currDistToStartOfRoute < leastDistToStartOfRoute || leastDistToStartOfRoute == -1)
                        {
                            leastStartStopID = stopID;
                            leastStartStopName = s;
                            leastDistToStartOfRoute = currDistToStartOfRoute;
                        }
                        if (currDistToEndOfRoute < leastDistToEndOfRoute || leastDistToEndOfRoute == -1)
                        {
                            leastEndStopID = stopID;
                            leastEndStopName = s;
                            leastDistToEndOfRoute = currDistToEndOfRoute;
                        }

                    }
                    currentDist = CalculateBusStopToRouteDist(stopLat, stopLon, (decimal.Parse(chosenRoute[k])), decimal.Parse(chosenRoute[k + 1]),
                          decimal.Parse(chosenRoute[k + 2]), decimal.Parse(chosenRoute[k + 3]));
                    
                    if ((currentDist < leastDist || leastDist == -1) && currentDist <= 15 && currentDist != -1)
                    {
                        leastDist = currentDist;
                        pointBeforeStopIndex = k/2;
                    }
                }
                if (stops.IndexOf(s) == 0 && leastDist != -1)
                {

                    RouteWithStopsID.Insert(0, stopID.ToString());
                    StopOnRoute.Add(s);
                    stopCounter++;
                    continue;
                }
                else if (stops.IndexOf(s) == stops.Count - 1 && leastDist != -1)
                {
                    RouteWithStopsID.Add(stopID.ToString());
                    StopOnRoute.Add(s);
                    stopCounter++;
                    continue;
                }
                else if (leastDist != -1)
                {
                    RouteWithStopsID.Insert(pointBeforeStopIndex + stopCounter + 1, stopID.ToString());
                    StopOnRoute.Add(s);
                    stopCounter++;
                }
            }
            RouteWithStopsID.Remove(leastStartStopID.ToString());
            RouteWithStopsID.Remove(leastEndStopID.ToString());
            RouteWithStopsID.Insert(0, leastStartStopID.ToString());
            RouteWithStopsID.Add(leastEndStopID.ToString());
            StopOnRoute.Remove(leastStartStopName);
            StopOnRoute.Remove(leastEndStopName);
            StopOnRoute.Insert(0, leastStartStopName);
            StopOnRoute.Add(leastEndStopName);

            RouteAndStops.Add(RouteWithStopsID);
            RouteAndStops.Add(StopOnRoute);
            return RouteAndStops;
        }

        private decimal CalculateBusStopToRouteDist(decimal stopPosLat, decimal stopPosLon, decimal EP1Lat, decimal EP1Lon, decimal EP2Lat, decimal EP2Lon)
        {
            decimal scLat;
            decimal scLon;
            if((EP1Lon - EP2Lon) == 0)
            {
                scLon = EP1Lon;
                scLat = stopPosLat;
            }
            else if ((EP1Lat - EP2Lat) == 0)
            {
                scLon = stopPosLon;
                scLat = EP1Lat;
            }
            else
            {
                decimal sA = (EP2Lat - EP1Lat) / (EP2Lon - EP1Lon);
                decimal sB = EP1Lat + (sA * (-EP1Lon));
                scLon = ((sA * stopPosLat) + stopPosLon - (sA * sB)) / ((sA * sA) + 1);
                scLat = ((sA * sA * stopPosLat) + (sA * stopPosLon) + sB) / ((sA * sA) + 1);
            }

            if ((scLat > EP1Lat && scLat > EP2Lat) || (scLat < EP1Lat && scLat < EP2Lat) ||
               (scLon > EP1Lon && scLon > EP2Lon) || (scLon < EP1Lon && scLon < EP2Lon))
            {
                if(Haversine(stopPosLat,EP1Lat,stopPosLon,EP1Lon) > 25 || Haversine(stopPosLat,EP2Lat,stopPosLon,EP2Lon) > 25)
                { return -1; }
            }
            return (decimal)(Haversine(stopPosLat, scLat, stopPosLon, scLon));

        }

        private double Haversine(decimal Lat1, decimal Lat2, decimal Lon1, decimal Lon2)
        {
            int EarthRadiusKM = 6371;
            double deltaLat = deg2Rad(Lat2 - Lat1);
            double deltaLon = deg2Rad(Lon2 - Lon1);
            double lat1Rads = deg2Rad(Lat1);
            double lat2Rads = deg2Rad(Lat2);

            double a = (Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2)) + (Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2) * Math.Cos(lat1Rads) * Math.Cos(lat2Rads));
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKM * c * 1000;
        }

        private double deg2Rad(decimal angle)
        {
            return (double)(((decimal)Math.PI) * angle / 180);
        }

        private double Rad2Deg(decimal angle)
        {
            return (double) angle * (180 / Math.PI);
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


                        cmd.CommandText = "INSERT INTO BusRoute_RoutePoint (fk_BusRoute, fk_RoutePoint) VALUES";
                        for (var i = 0; i < points.Count(); i++)
                        {
                            cmd.CommandText += "(" + routeNumberID + " , " + points[i] + "),";
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
