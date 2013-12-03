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

        public ActionResult CreateRoute()
        {
            return View();
        }

        public ActionResult EditRoute()
        {
            return View();
        }

        public int Save(List<string> route, List<string> routeWayPoints, List<string> stops, List<string> SubRoutes, List<string> SubrouteWaypoint, string RouteNumber)
        {
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)System.Threading.Thread.CurrentThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            System.Threading.Thread.CurrentThread.CurrentCulture = customCulture;

            List<List<string>> g = new List<List<string>>();
            List<List<string>> SubRoutesIDList = new List<List<string>>();
            List<string> pointList;
            List<string> pointIDList;

            int counter;
            if (route != null && stops != null)
            {
                try
                {
                    var routeNumberId = InsertRouteNumber(RouteNumber);
                    if (routeWayPoints != null)
                        InsertWaypoints(routeNumberId, ConvertLatLng(routeWayPoints));
                    pointList = ConvertLatLng(route);


                    pointIDList = InsertRoutePoints(pointList);
                    if (SubRoutes != null)
                    {
                        InsertSubRouteIntoBusRoute(RouteNumber, SubRoutes.Count());
                        if (SubrouteWaypoint != null)
                            InsertSubRouteWaypoints(SubrouteWaypoint, RouteNumber, SubRoutes.Count());
                        g = ConvertSubRoute(SubRoutes);
                        for (var i = 0; i < g.Count(); i++)
                        {
                            SubRoutesIDList.Add(InsertRoutePoints(g[i]));
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
                        if (i == 0) // main route
                        {
                            RouteAndStops = CalculateBusStopsForRoute(stops, pointIDList, RouteNumber, i);
                            BusRouteWithStops = RouteAndStops[0];
                            BusStops = RouteAndStops[1];
                            InsertBusRoute_RoutePoint(routeNumberId, BusRouteWithStops);
                            InsertBusRoute_BusStop(routeNumberId, BusStops);
                        }
                        else // subroute
                        {
                            RouteAndStops = CalculateBusStopsForRoute(stops, SubRoutesIDList[i - 1], RouteNumber, i);
                            BusRouteWithStops = RouteAndStops[0];
                            BusStops = RouteAndStops[1];
                            InsertBusRoute_RoutePoint(routeNumberId + i, BusRouteWithStops);
                            InsertBusRoute_BusStop(routeNumberId + i, BusStops);
                        }
                    }
                }
                catch (Exception e)
                {
                    Debug.WriteLine(e.Message);
                    return -42;
                }
            }
            else
                return -1;
            return 0;
        }

        private void InsertSubRouteWaypoints(List<string> SubrouteWaypoints, string routeNumber, int p)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = string.Format("SELECT ID FROM BusRoute WHERE RouteNumber = '{0}' AND SubRoute > 0", routeNumber);
                        connection.Open();
                        var read1 = cmd.ExecuteReader();
                        List<int> ID = new List<int>();
                        while (read1.Read())
                        {
                            ID.Add(read1.GetInt32(0));
                        }
                        read1.Close();
                        connection.Close();

                        cmd.CommandText = string.Format("SELECT ID FROM Waypoint WHERE fk_BusRoute IN (SELECT ID FROM BusRoute WHERE RouteNumber = '{0}' AND SubRoute > 0)", routeNumber);
                        connection.Open();
                        var read2 = cmd.ExecuteReader();
                        List<int> WayID = new List<int>();
                        while (read2.Read())
                        {
                            WayID.Add(read2.GetInt32(0));
                        }
                        read2.Close();
                        connection.Close();
                        if (WayID.Count() > 0)
                        {
                            cmd.CommandText = string.Format("SET SQL_SAFE_UPDATES=0; DELETE FROM Waypoint WHERE fk_BusRoute = {0}", WayID.First());
                            connection.Open();
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }

                        cmd.CommandText = "INSERT INTO Waypoint (Latitude, Longitude, fk_BusRoute) VALUES ";
                        for (int i = 0; i < ID.Count(); i++)
                        {
                            var points = ConvertLatLng(SubrouteWaypoints[i]);
                            for (var j = 0; j < points.Count(); j = j + 2)
                            {
                                cmd.CommandText += "(" + points[j] + " , " + points[j + 1] + " , " + ID[i] + "),";
                            }

                        }
                        cmd.CommandText = cmd.CommandText.TrimEnd(',');
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

        private void InsertWaypoints(int routeNumberId, List<string> routeWayPoints)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = string.Format("SELECT ID FROM Waypoint WHERE fk_BusRoute = {0}", routeNumberId);

                        connection.Open();
                        var read1 = cmd.ExecuteReader();
                        List<int> ID = new List<int>();
                        while (read1.Read())
                        {
                            ID.Add(read1.GetInt32(0));
                        }
                        read1.Close();
                        connection.Close();
                        if (ID.Count > 0)
                        {
                            cmd.CommandText = string.Format("SET SQL_SAFE_UPDATES=0; DELETE FROM Waypoint WHERE fk_BusRoute = {0}", routeNumberId);
                            connection.Open();
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }

                        cmd.CommandText = "INSERT INTO Waypoint (Latitude, Longitude, fk_BusRoute) VALUES ";
                        for (var i = 0; i < routeWayPoints.Count(); i = i + 2)
                        {
                            cmd.CommandText += "(" + routeWayPoints[i] + " , " + routeWayPoints[i + 1] + " , " + routeNumberId + "),";
                        }

                        cmd.CommandText = cmd.CommandText.TrimEnd(',');

                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                        connection.Close();
                    }
                }
            }
        }

        private List<List<string>> CalculateBusStopsForRoute(List<string> stops, List<string> chosenRouteID, string routeNumber, int subRoute)
        {
            int routeID;
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    connection.Open();
                    cmd.CommandText = string.Format("Select BusRoute.ID from BusRoute where BusRoute.RouteNumber = '{0}' and BusRoute.SubRoute = {1}", routeNumber, subRoute);
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        routeID = int.Parse(reader["ID"].ToString());
                    }
                    connection.Close();
                }
            }

            List<List<string>> RouteAndStops = new List<List<string>>();
            List<string> chosenRouteLatLng = new List<string>();
            List<string> RouteWithStopsID = new List<string>(chosenRouteID);
            List<string> StopOnRoute = new List<string>();
            int stopCounter = 0;

            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {

                    cmd.CommandText = "Select RoutePoint.Latitude,RoutePoint.Longitude from RoutePoint where ";
                    for (int j = 0; j < chosenRouteID.Count; j++)
                    {
                        cmd.CommandText += string.Format("(RoutePoint.ID = {0}) or ", chosenRouteID[j]);
                    }
                    cmd.CommandText = cmd.CommandText.TrimEnd(" or ".ToCharArray());
                    cmd.CommandText += " Order by RoutePoint.ID asc";
                    connection.Open();
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        chosenRouteLatLng.Add(reader["Latitude"].ToString());
                        chosenRouteLatLng.Add(reader["Longitude"].ToString());
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
            int leastEndStopID = 0;
            string leastStartStopName = "";
            string leastEndStopName = "";
            foreach (string s in stops)
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

                        cmd.CommandText = string.Format("Select RoutePoint.ID,RoutePoint.Latitude, RoutePoint.Longitude from RoutePoint " +
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

                for (int k = 0; k < chosenRouteLatLng.Count - 2; k = k + 2)
                {
                    if (k == 0)
                    {
                        currDistToStartOfRoute = (decimal)Haversine(stopLat, decimal.Parse(chosenRouteLatLng[0]), stopLon, decimal.Parse(chosenRouteLatLng[1]));
                        currDistToEndOfRoute = (decimal)Haversine(stopLat, decimal.Parse(chosenRouteLatLng[chosenRouteLatLng.Count - 2]), stopLon, decimal.Parse(chosenRouteLatLng[chosenRouteLatLng.Count - 1]));
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
                    currentDist = CalculateBusStopToRouteDist(stopLat, stopLon, (decimal.Parse(chosenRouteLatLng[k])), decimal.Parse(chosenRouteLatLng[k + 1]),
                          decimal.Parse(chosenRouteLatLng[k + 2]), decimal.Parse(chosenRouteLatLng[k + 3]));

                    if ((currentDist < leastDist || leastDist == -1) && currentDist <= 15 && currentDist != -1)
                    {
                        leastDist = currentDist;
                        pointBeforeStopIndex = k / 2;
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
            if ((EP1Lon - EP2Lon) == 0)
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
                if (Haversine(stopPosLat, EP1Lat, stopPosLon, EP1Lon) > 25 || Haversine(stopPosLat, EP2Lat, stopPosLon, EP2Lon) > 25)
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
            return (double)angle * (180 / Math.PI);
        }

        public int InsertRouteNumber(string routeNumber)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {

                        cmd.CommandText = String.Format("SELECT ID FROM BusRoute WHERE RouteNumber = '{0}' AND SubRoute = 0", routeNumber);
                        connection.Open();
                        var read1 = cmd.ExecuteReader();

                        List<int> BusRouteID = new List<int>();
                        while (read1.Read())
                        {
                            BusRouteID.Add(read1.GetInt32(0));
                        }
                        read1.Close();
                        connection.Close();
                        if (BusRouteID.Count == 0)
                            cmd.CommandText = string.Format("INSERT INTO BusRoute (RouteNumber, SubRoute) VALUES('{0}', 0);select last_insert_id();", routeNumber);
                        else return BusRouteID.First();
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
                        cmd.CommandText = String.Format("SELECT ID FROM BusRoute WHERE RouteNumber = '{0}' AND SubRoute > 0", routeNumber);
                        connection.Open();
                        var read1 = cmd.ExecuteReader();

                        List<int> BusRouteID = new List<int>();
                        while (read1.Read())
                        {
                            BusRouteID.Add(read1.GetInt32(0));
                        }
                        read1.Close();
                        connection.Close();

                        for (var i = 0; i < count; i++)
                        {
                            if (BusRouteID.Count() >= i + 1)
                            {
                                cmd.CommandText = string.Format("UPDATE BusRoute (RouteNumber, SubRoute) VALUES('{0}', {1})", routeNumber, i + 1);
                                connection.Open();
                                cmd.ExecuteNonQuery();
                                connection.Close();
                            }
                            else
                            {
                                cmd.CommandText = string.Format("INSERT INTO BusRoute (RouteNumber, SubRoute) VALUES('{0}', {1})", routeNumber, i + 1);
                                connection.Open();
                                cmd.ExecuteNonQuery();
                                connection.Close();
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                    }
                }
            }
        }

        public List<string> InsertRoutePoints(List<string> points)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                List<string> idOFInserted = new List<string>();
                using (var cmd = connection.CreateCommand())
                {
                    long FirstID = 0;
                    int LastID = 0;

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

                        cmd.CommandText = "SELECT Last_insert_ID();";
                        var LastReader = cmd.ExecuteReader();
                        while (LastReader.Read())
                        {
                            FirstID = LastReader.GetInt64(0);
                        }
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        Debug.WriteLine(e.Message);
                    }
                    try
                    {
                        cmd.CommandText = "SELECT ID from RoutePoint order by(RoutePoint.ID) desc limit 1";
                        connection.Open();
                        var reader = cmd.ExecuteReader();
                        while (reader.Read())
                        {
                            //FirstID = int.Parse(reader["Last_insert_ID()"].ToString());
                            LastID = int.Parse(reader["ID"].ToString());
                        }
                        connection.Close();
                        for (var i = FirstID; i <= LastID; i++)
                        {
                            idOFInserted.Add(i.ToString());
                        }

                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        Debug.WriteLine(e.Message);
                    }
                }
                return idOFInserted;
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

                        List<int> ID = new List<int>();
                        foreach (var stop in stops)
                        {
                            cmd.CommandText = string.Format("SELECT ID FROM BusStop WHERE StopName = '{0}'", stop);

                            connection.Open();
                            var read = cmd.ExecuteReader();
                            while (read.Read())
                            {
                                ID.Add(read.GetInt32(0));
                            }
                            read.Close();
                            connection.Close();

                        }

                        foreach (var id in ID)
                        {
                            cmd.CommandText = "INSERT INTO BusRoute_BusStop (fk_BusRoute, fk_BusStop) VALUES" +
                                              "(" + routeNumberID + " , " + id + ")";

                            connection.Open();
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }
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
                LatLng.Add(t[0].TrimStart('(').Trim());
                LatLng.Add(t[1].TrimEnd(')').Trim());

            }

            return LatLng;
        }

        public List<string> ConvertLatLng(string latLng)
        {
            List<string> LatLng = new List<string>();
            var s = latLng.Split(',');

            for (var i = 0; i < s.Count(); i = i + 2)
            {
                LatLng.Add(s[i].TrimStart('(').Trim());
                LatLng.Add(s[i + 1].TrimEnd(')').Trim());
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

        public JsonResult GetSelectedBusRoute(string RouteName)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = String.Format("SELECT fk_BusRoute, Latitude, Longitude FROM Waypoint " +
                                          "WHERE fk_BusRoute IN (SELECT ID FROM BusRoute WHERE RouteNumber = '{0}')", RouteName);

                        connection.Open();

                        var read = cmd.ExecuteReader();
                        List<Waypoint> waypoints = new List<Waypoint>();
                        while (read.Read())
                        {
                            waypoints.Add(new Waypoint() { ID = read.GetInt32(0), Lat = read.GetDecimal(1), Lng = read.GetDecimal(2) });
                        }
                        read.Close();

                        connection.Close();
                        return ConvertToJason(waypoints);
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

        public JsonResult GetBusRoutesNames()
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = "SELECT RouteNumber FROM BusRoute WHERE SubRoute = 0";
                        connection.Open();

                        var read = cmd.ExecuteReader();
                        List<string> names = new List<string>();
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

        public int DeleteSelectedBusRoute(string RouteName)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = string.Format("SELECT fk_RoutePoint FROM BusRoute_RoutePoint WHERE fk_BusRoute IN (SELECT ID FROM BusRoute WHERE RouteNumber = '{0}')", RouteName);
                        connection.Open();
                        var read1 = cmd.ExecuteReader();
                        List<int> RouteID = new List<int>();
                        while (read1.Read())
                        {
                            RouteID.Add(read1.GetInt32(0));
                        }
                        read1.Close();
                        connection.Close();

                        cmd.CommandText = string.Format("SELECT ID FROM Bus WHERE fk_BusRoute IN (SELECT ID FROM BusRoute WHERE RouteNumber = '{0}')", RouteName);
                        connection.Open();
                        var read = cmd.ExecuteReader();
                        List<int> BusID = new List<int>();
                        while (read.Read())
                        {
                            BusID.Add(read.GetInt32(0));
                        }
                        read.Close();
                        if (BusID.Count > 0)
                            return -1;
                        connection.Close();

                        cmd.CommandText = String.Format("DELETE FROM BusRoute_RoutePoint WHERE fk_BusRoute IN (SELECT ID FROM BusRoute WHERE RouteNumber = '{0}')", RouteName);

                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();

                        cmd.CommandText = String.Format("DELETE FROM Waypoint WHERE fk_BusRoute IN (SELECT ID FROM BusRoute WHERE RouteNumber = '{0}')", RouteName);
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();

                        cmd.CommandText = String.Format("DELETE FROM BusRoute_BusStop WHERE fk_BusRoute IN (SELECT ID FROM BusRoute WHERE RouteNumber = '{0}')", RouteName);
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();

                        cmd.CommandText = String.Format("DELETE FROM BusRoute WHERE RouteNumber = '{0}'", RouteName);
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();

                        cmd.CommandText = "DELETE FROM RoutePoint WHERE (ID = ";

                        for (int i = 0; i < RouteID.Count(); i++)
                        {
                            cmd.CommandText += RouteID[i] + " OR ID = ";
                        }
                        cmd.CommandText = cmd.CommandText.TrimEnd("OR ID = ".ToCharArray());
                        cmd.CommandText += ") AND RoutePoint.ID NOT IN (SELECT BusStop.fk_routePoint FROM BusStop)";
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();
                        return 42;
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        return -2;
                    }
                }
            }

        }

        public JsonResult GetStopsOnRoute(string RouteName)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        List<BusStops> LatLng = new List<BusStops>();
                        cmd.CommandText = string.Format("SELECT l.Latitude, l.Longitude FROM RoutePoint AS l " +
                                                        "INNER JOIN BusStop AS Stops ON l.ID = Stops.fk_RoutePoint " +
                                                        "INNER JOIN BusRoute_BusStop AS brbs ON Stops.ID = brbs.fk_BusStop " +
                                                        "INNER JOIN BusRoute AS br ON brbs.fk_BusRoute = br.ID " +
                                                        "WHERE br.RouteNumber = '{0}'", RouteName);

                        connection.Open();
                        var read = cmd.ExecuteReader();
                        while (read.Read())
                        {
                            LatLng.Add(new BusStops() { Lat = read.GetDecimal(0), Lng = read.GetDecimal(1) });
                        }
                        read.Close();
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

        public JsonResult GetStops()
        {
            string ConnectionString = System.Configuration.ConfigurationManager.ConnectionStrings["TrackABus"].ConnectionString;
            using (var connection = new MySqlConnection(ConnectionString))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        var stops = new List<BusStops>();
                        connection.Open();
                        cmd.CommandText = "SELECT ID, StopName FROM BusStop;";
                        var read = cmd.ExecuteReader();

                        while (read.Read())
                        {
                            stops.Add(new BusStops() { ID = read.GetInt32(0), name = read.GetString(1) });
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

                        List<BusStops> LatLng = new List<BusStops>();
                        while (read.Read())
                        {
                            LatLng.Add(new BusStops() { Lat = read.GetDecimal(0), Lng = read.GetDecimal(1) });
                        }
                        read.Close();
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

        private JsonResult ConvertToJason<T>(List<T> ListOfItems)
        {
            JsonResult jr = new JsonResult();

            jr.Data = ListOfItems.ToList();
            jr.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jr;
        }

        private static string getConnectionString()
        {
            return System.Configuration.ConfigurationManager.ConnectionStrings["TrackABus"].ConnectionString;
        }
    }
}