using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using MySql.Data.MySqlClient;

namespace MapDrawRouteTool
{
    /// <summary>
    /// Summary description for AndroidToMySQLWebService
    /// </summary>
    [WebService(Namespace = "http://TrackABus.dk/Webservice/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]
    // To allow this Web Service to be called from script, using ASP.NET AJAX, uncomment the following line. 
    // [System.Web.Script.Services.ScriptService]
    public class AndroidToMySQLWebService : System.Web.Services.WebService
    {
        [WebMethod]
        public List<string> GetBusList()
        {
            List<string> Buslist = new List<string>();

            using (var connection = new MySqlConnection(GetConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();
                        cmd.CommandText = "SELECT * FROM trackabus_dk_db.BusRoute WHERE SubRoute = 0;";
                        MySqlDataReader dataReader = cmd.ExecuteReader();
                        while (dataReader.Read())
                        {
                            Buslist.Add(dataReader["RouteNumber"] + "");
                        }
                        dataReader.Close();
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        Buslist.Add(e.Message);
                        return Buslist;
                    }
                }
            }

            return Buslist;
        }

        [WebMethod]
        public List<Route> GetBusRoute(string busNumber)
        {
            List<Route> totalRoute = new List<Route>();

            using (var connection = new MySqlConnection(GetConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();
                        cmd.CommandText = String.Format("SELECT SubRoute FROM BusRoute WHERE RouteNumber = '{0}'", busNumber);
                        List<string> SubrouteNumbers = new List<string>();
                        MySqlDataReader dataReader = cmd.ExecuteReader();
                        while (dataReader.Read())
                        {
                            SubrouteNumbers.Add(dataReader["SubRoute"].ToString());
                        }
                        dataReader.Close();


                        foreach (var subroutenumber in SubrouteNumbers)
                        {
                            var route = new Route();

                            cmd.CommandText = String.Format("SELECT t1.Latitude, t1.Longitude " +
                                                           "FROM RoutePoint AS t1 " +
                                                           "INNER JOIN BusRoute_RoutePoint AS t2 ON t1.ID = t2.fk_RoutePoint " +
                                                           "INNER JOIN BusRoute AS t3 ON t2.fk_BusRoute = t3.ID " +
                                                           "WHERE t3.RouteNumber = '{0}' AND t3.SubRoute = {1} AND " +
                                                           "t1.ID NOT IN (SELECT BusStop.fk_routePoint FROM BusStop)", busNumber, subroutenumber);
                            MySqlDataReader dataReader1 = cmd.ExecuteReader();

                            while (dataReader1.Read())
                            {
                                route.Lat.Add(dataReader1["Latitude"] + "");
                                route.Lng.Add(dataReader1["Longitude"] + "");
                            }
                            dataReader1.Close();
                            totalRoute.Add(route);
                        }

                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        totalRoute = new List<Route>();
                        var r = new Route();
                        r.Lat.Add(e.Message);
                        totalRoute.Add(r);
                        return totalRoute;
                    }
                }
            }
            return totalRoute;
        }

        [WebMethod]
        public List<List<string>> GetBusStops(string busNumber)
        {
            List<string> BusStopLat = new List<string>();
            List<string> BusStopLon = new List<string>();
            List<string> BusStopName = new List<string>();
            List<List<string>> totalBusStops = new List<List<string>>();

            using (var connection = new MySqlConnection(GetConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();

                        cmd.CommandText = String.Format("SELECT t1.Latitude, t1.Longitude,t2.StopName " +
                                                        "FROM RoutePoint AS t1 " +
                                                        "INNER JOIN BusStop t2 ON t1.ID = t2.fk_RoutePoint " +
                                                        "INNER JOIN BusRoute_BusStop AS t3 ON t2.ID = t3.fk_BusStop " +
                                                        "INNER JOIN BusRoute AS t4 ON t3.fk_BusRoute = t4.ID " +
                                                        "WHERE t4.RouteNumber = '{0}'", busNumber);

                        MySqlDataReader dataReader = cmd.ExecuteReader();
                        while (dataReader.Read())
                        {
                            BusStopLat.Add(dataReader["Latitude"] + "");
                            BusStopLon.Add(dataReader["Longitude"] + "");
                            BusStopName.Add(dataReader["StopName"] + "");
                        }
                        dataReader.Close();
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        var i = new List<string>();
                        i.Add(e.Message);
                        totalBusStops.Add(i);
                        return totalBusStops;
                    }
                }
            }
            totalBusStops.Add(BusStopLat);
            totalBusStops.Add(BusStopLon);
            totalBusStops.Add(BusStopName);
            return totalBusStops;

        }

        [WebMethod]
        public List<Point> GetbusPos(string busNumber)
        {
            List<Point> totalRoute = new List<Point>();
            using (var connection = new MySqlConnection(GetConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();
                        cmd.CommandText = String.Format("SELECT Bus.ID FROM Bus " +
                                                        "INNER JOIN BusRoute ON Bus.fk_BusRoute = BusRoute.ID " +
                                                        "WHERE BusRoute.RouteNumber = '{0}'", busNumber);

                        MySqlDataReader dataReader = cmd.ExecuteReader();
                        List<int> NumberOfBussesOnRoute = new List<int>(); ;
                        while (dataReader.Read())
                        {
                            NumberOfBussesOnRoute.Add((int)dataReader["ID"]);
                        }

                        dataReader.Close();

                        for (int i = 0; i < NumberOfBussesOnRoute.Count(); i++)
                        {
                            cmd.CommandText = String.Format("SELECT t1.Latitude, t1.Longitude " +
                                                            "FROM GPSPosition AS t1 " +
                                                            "INNER JOIN Bus AS t2 ON t1.fk_Bus=t2.ID " +
                                                            "INNER JOIN BusRoute AS t3 on t2.fk_BusRoute = t3.ID " +
                                                            "WHERE t3.RouteNumber='{0}' AND t1.fk_Bus = {1} ORDER BY t1.ID DESC LIMIT 1;", busNumber, NumberOfBussesOnRoute[i]);

                            MySqlDataReader dataReader2 = cmd.ExecuteReader();
                            while (dataReader2.Read())
                            {
                                totalRoute.Add(new Point() { Lat = dataReader2["Latitude"] + "", Lng = dataReader2["Longitude"] + "" });
                            }

                            dataReader2.Close();
                        }

                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        totalRoute = new List<Point>();
                        totalRoute.Add(new Point() { Lat = e.Message });
                        return totalRoute;
                    }
                }
            }
            return totalRoute;
        }

        [WebMethod]
        public List<string> GetBusTime(string StopName, string RouteNumber)
        {
            List<string> results = new List<string>();
            using (var connection = new MySqlConnection(GetConnectionString()))
            {
                using (var cmd = new MySqlCommand("CalcBusToStopTime", connection))
                {
                    try
                    {
                        string TimeToStopSecAsc = "";
                        string TimeToStopSecDesc = "";
                        string EndStopNameAsc = "";
                        string EndStopNameDesc = "";
                        cmd.CommandType = System.Data.CommandType.StoredProcedure;

                        cmd.Parameters.Add("?stopName", MySqlDbType.VarChar);
                        cmd.Parameters["?stopName"].Value = StopName;
                        cmd.Parameters["?stopName"].Direction = System.Data.ParameterDirection.Input;

                        cmd.Parameters.Add("?routeNumber", MySqlDbType.VarChar);
                        cmd.Parameters["?routeNumber"].Value = RouteNumber;
                        cmd.Parameters["?routeNumber"].Direction = System.Data.ParameterDirection.Input;

                        cmd.Parameters.Add(new MySqlParameter("?TimeToStopSecAsc", MySqlDbType.Int32));
                        cmd.Parameters["?TimeToStopSecAsc"].Direction = System.Data.ParameterDirection.Output;

                        cmd.Parameters.Add(new MySqlParameter("?TimeToStopSecDesc", MySqlDbType.Int32));
                        cmd.Parameters["?TimeToStopSecDesc"].Direction = System.Data.ParameterDirection.Output;

                        cmd.Parameters.Add(new MySqlParameter("?EndStopNameAsc", MySqlDbType.VarChar));
                        cmd.Parameters["?EndStopNameAsc"].Direction = System.Data.ParameterDirection.Output;

                        cmd.Parameters.Add(new MySqlParameter("?EndStopNameDesc", MySqlDbType.VarChar));
                        cmd.Parameters["?EndStopNameDesc"].Direction = System.Data.ParameterDirection.Output;


                        connection.Open();
                        cmd.ExecuteNonQuery();

                        if (string.IsNullOrEmpty(cmd.Parameters["?TimeToStopSecAsc"].Value.ToString()))
                        {
                            results.Add("-1");
                        }
                        else
                        {
                            results.Add(cmd.Parameters["?TimeToStopSecAsc"].Value.ToString());
                        }

                        if (string.IsNullOrEmpty(cmd.Parameters["?TimeToStopSecDesc"].Value.ToString()))
                        {
                            results.Add("-1");
                        }
                        else
                        {
                            results.Add(cmd.Parameters["?TimeToStopSecDesc"].Value.ToString());
                        }
                        connection.Close();

                        return results;
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        results.Add("ERROR! : " + e.Message);
                        return results;
                    }
                }
            }

        }

        private static string GetConnectionString()
        {
            return System.Configuration.ConfigurationManager.ConnectionStrings["TrackABus"].ConnectionString;
        }
    }

    public class Route
    {
        public Route()
        {
            Lat = new List<string>();
            Lng = new List<string>();
        }
        public List<string> Lat;
        public List<string> Lng;
    }

    public class Point
    {
        public Point()
        {
        }
        public string Lat;
        public string Lng;
    }

}
