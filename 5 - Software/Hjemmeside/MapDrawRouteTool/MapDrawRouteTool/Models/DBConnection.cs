using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Threading;

namespace MapDrawRouteTool.Models
{
    public static class DBConnection
    {
        public static string getConnectionString()
        {
            return System.Configuration.ConfigurationManager.ConnectionStrings["TrackABus"].ConnectionString;
        }

        public static JsonResult GetAllRouteNames()
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = "SELECT RouteNumber FROM BusRoute WHERE SubRoute = 0";
                        connection.Open();
                        var read = cmd.ExecuteReader();

                        List<string> RouteNumber = new List<string>();
                        while (read.Read())
                        {
                            RouteNumber.Add(read.GetString(0));
                        }
                        connection.Close();
                        read.Close();

                        return JConverter.ConvertToJson(RouteNumber);
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

        public static JsonResult GetAllBusses()
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = "SELECT ID FROM Bus ORDER BY ID";
                        List<int> Busses = new List<int>();
                        connection.Open();
                        var read = cmd.ExecuteReader();
                        while (read.Read())
                        {
                            Busses.Add(read.GetInt32(0));
                        }
                        read.Close();
                        connection.Close();
                        return JConverter.ConvertToJson(Busses);
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

        public static JsonResult GetBussesOnRoute(string route)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = "SELECT Bus.ID FROM Bus " +
                                          "INNER JOIN BusRoute on BusRoute.ID = Bus.fk_BusRoute " +
                                          "WHERE BusRoute.RouteNumber = '" + route + "' " +
                                          "ORDER BY Bus.ID;";
                        connection.Open();
                        var read = cmd.ExecuteReader();
                        List<int> Busses = new List<int>();
                        while (read.Read())
                        {
                            Busses.Add(read.GetInt32(0));
                        }
                        read.Close();
                        connection.Close();
                        return JConverter.ConvertToJson(Busses);
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

        public static JsonResult GetBussesNotOnRoute()
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = "SELECT ID FROM Bus " +
                                          "WHERE fk_BusRoute IS NULL;";
                        connection.Open();
                        var read = cmd.ExecuteReader();
                        List<int> Busses = new List<int>();
                        while (read.Read())
                        {
                            Busses.Add(read.GetInt32(0));
                        }
                        read.Close();
                        connection.Close();
                        return JConverter.ConvertToJson(Busses);
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

        public static JsonResult GetAllStops()
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        var stops = new List<BusStops>();
                        cmd.CommandText = "SELECT ID, StopName FROM BusStop";
                        connection.Open();

                        var read = cmd.ExecuteReader();
                        while (read.Read())
                        {
                            stops.Add(new BusStops() { ID = read.GetInt32(0), name = read.GetString(1) });
                        }
                        read.Close();
                        connection.Close();

                        return JConverter.ConvertToJson(stops);
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

        public static JsonResult GetPosistionForBusstop(List<string> StopNames)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
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

        public static JsonResult GetStopsOnRoute(string RouteName)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        List<BusStops> LatLng = new List<BusStops>();
                        cmd.CommandText = string.Format("SELECT l.Latitude, l.Longitude, Stops.StopName FROM RoutePoint AS l " +
                                                        "INNER JOIN BusStop AS Stops ON l.ID = Stops.fk_RoutePoint " +
                                                        "INNER JOIN BusRoute_BusStop AS brbs ON Stops.ID = brbs.fk_BusStop " +
                                                        "INNER JOIN BusRoute AS br ON brbs.fk_BusRoute = br.ID " +
                                                        "WHERE br.RouteNumber = '{0}'", RouteName);

                        connection.Open();
                        var read = cmd.ExecuteReader();
                        while (read.Read())
                        {
                            LatLng.Add(new BusStops() { Lat = read.GetDecimal(0), Lng = read.GetDecimal(1), name = read.GetString(2) });
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

        public static JsonResult GetSelectedBusRoute(string RouteName)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
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
                        return JConverter.ConvertToJson(waypoints);
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

        public static int RenameBusstop(string oldName, string newName, string NewPos)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
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

        public static int DeleteBusstop(string stop)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
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

        public static int SaveBusstop(List<string> cords, List<string> names)
        {
            for (var i = 0; i < cords.Count; i = i + 2)
            {
                using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
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

        public static int SaveChangesToBus(List<string> bussesToAdd, string route, List<string> bussesToRemove)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        if (bussesToAdd != null)
                        {

                            cmd.CommandText = "Update Bus " +
                                              "SET Bus.fk_BusRoute = (SELECT BusRoute.ID " +
                                                                      "FROM BusRoute " +
                                                                      "WHERE BusRoute.RouteNumber = '" + route + "' AND SubRoute = 0) " +
                                              "WHERE Bus.ID = ";

                            for (var i = 0; i < bussesToAdd.Count(); i++)
                            {
                                cmd.CommandText += bussesToAdd[i];
                                cmd.CommandText += " OR Bus.ID = ";
                            }

                            cmd.CommandText = cmd.CommandText.TrimEnd(" OR Bus.ID = ".ToCharArray());
                            connection.Open();
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }
                        if (bussesToRemove != null)
                        {
                            cmd.CommandText = "UPDATE Bus " +
                                              "SET Bus.fk_BusRoute = null " +
                                              "WHERE Bus.ID = ";

                            for (var i = 0; i < bussesToRemove.Count(); i++)
                            {
                                cmd.CommandText += bussesToRemove[i];
                                cmd.CommandText += " OR Bus.ID = ";
                            }
                            cmd.CommandText = cmd.CommandText.TrimEnd(" OR Bus.ID = ".ToCharArray());
                            connection.Open();
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }
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

        public static void removeBussesFromDB(List<string> bussesToRemove)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        foreach (var bus in bussesToRemove)
                        {
                            cmd.CommandText = string.Format("DELETE FROM Bus WHERE ID = {0}", bus);
                            connection.Open();
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }

                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                        connection.Close();
                    }
                }
            }
        }

        public static int addBussesToDB(List<string> bussesToAdd)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        foreach (var bus in bussesToAdd)
                        {
                            cmd.CommandText = string.Format("INSERT INTO Bus (ID, IsDescending, fk_BusRoute) " +
                                                            "VALUES({0}, null, null)", bus);

                            connection.Open();
                            cmd.ExecuteNonQuery();
                            connection.Close();
                        }
                        return 0;
                    }
                    catch (Exception e)
                    {
                        if (connection.State == System.Data.ConnectionState.Open)
                            connection.Close();
                        Debug.WriteLine(e.Message);
                        if (e.Message == "Unable to connect to any of the specified MySQL hosts.")
                            return -3;
                        else
                            return -1;
                    }
                }
            }
        }

        public static int DeleteSelectedBusRouteFromDB(string RouteName)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
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
                        Debug.WriteLine(e.Message);
                        return -2;
                    }
                }
            }
        }

        public static void InsertWaypoints(int routeNumberId, List<string> routeWayPoints)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
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

        public static int InsertRouteNumber(string routeNumber)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
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
                        Debug.WriteLine(e.Message);
                        return 0;
                    }
                }
            }
        }

        public static void InsertBusRoute_RoutePoint(int routeNumberID, List<string> points)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
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

        public static void InsertSubRouteIntoBusRoute(string routeNumber, int count)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
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
                        Debug.WriteLine(e.Message);
                    }
                }
            }
        }

        public static List<string> InsertRoutePoints(List<string> points)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
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

        public static void InsertBusRoute_BusStop(int routeNumberID, List<string> stops)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
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

        public static void InsertSubRouteWaypoints(List<string> SubrouteWaypoints, string routeNumber, int p)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
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

        public static List<string> GetLatLngForRoute(List<string> chosenRouteID)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    List<string> chosenRouteLatLng = new List<string>();
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

                    return chosenRouteLatLng;
                }
            }
        }

        public static BusStops GetIDLatLngForStop(string stopname)
        {
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    BusStops stop = new BusStops();
                    cmd.CommandText = string.Format("Select RoutePoint.ID,RoutePoint.Latitude, RoutePoint.Longitude from RoutePoint " +
                                                    "inner join BusStop on RoutePoint.ID = BusStop.fk_RoutePoint " +
                                                    "where BusStop.StopName='{0}'", stopname);
                    connection.Open();
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        stop.ID = int.Parse(reader["ID"].ToString());
                        stop.Lat = decimal.Parse(reader["Latitude"].ToString());
                        stop.Lng = decimal.Parse(reader["Longitude"].ToString());
                    }
                    reader.Close();
                    connection.Close();
                    return stop;
                }
            }
        }

        private static List<string> ConvertLatLng(string latLng)
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
    }
}