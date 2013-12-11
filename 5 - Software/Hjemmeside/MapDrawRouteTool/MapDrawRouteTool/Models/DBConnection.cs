using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using System.Diagnostics;

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
                        List<string> names = new List<string>();
                        cmd.CommandText = "SELECT ID, StopName FROM BusStop";
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
    }
}