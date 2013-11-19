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
                        cmd.CommandText = "SELECT * FROM trackabus_dk_db.BusRoute;";
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
        public List<List<string>> GetBusRoute(string busNumber)
        {

            List<string> BusRouteLat = new List<string>();
            List<string> BusRouteLon = new List<string>();
            List<List<string>> totalRoute = new List<List<string>>();

            using (var connection = new MySqlConnection(GetConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();

                        cmd.CommandText = String.Format("SELECT t1.Latitude, t1.Longitude " +
                                                        "FROM RoutePoint AS t1 INNER JOIN BusRoute_RoutePoint AS t2 ON t1.ID = t2.fk_RoutePoint " +
                                                        "INNER JOIN BusRoute AS t3 ON t2.fk_BusRoute=t3.ID " +
                                                        "WHERE t3.RouteNumber = '{0}' AND " +
                                                        "t1.ID NOT IN (SELECT BusStop.fk_RoutePoint FROM BusStop)", busNumber);



                        MySqlDataReader dataReader = cmd.ExecuteReader();
                        while (dataReader.Read())
                        {
                            BusRouteLat.Add(dataReader["Latitude"] + "");
                            BusRouteLon.Add(dataReader["Longitude"] + "");
                        }
                        dataReader.Close();
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        var i = new List<string>();
                        i.Add(e.Message);
                        totalRoute.Add(i);
                        return totalRoute;
                    }
                }
            }

            totalRoute.Add(BusRouteLat);
            totalRoute.Add(BusRouteLon);
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
        public List<string> GetbusPos(string busNumber)
        {
            List<string> BusRouteLat = new List<string>();
            List<string> BusRouteLon = new List<string>();

            using (var connection = new MySqlConnection(GetConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();
                        cmd.CommandText = String.Format("SELECT t1.Latitude, t1.Longitude " +
                                 "FROM GPSPosition AS t1 INNER JOIN Bus AS t2 ON t1.fk_Bus=t2.ID " +
                                 "INNER JOIN BusRoute AS t3 on t2.fk_BusRoute = t3.ID " +
                                 "WHERE t3.RouteNumber='{0}';", busNumber);

                        MySqlDataReader dataReader = cmd.ExecuteReader();
                        while (dataReader.Read())
                        {
                            BusRouteLat.Add(dataReader["Latitude"] + "");
                            BusRouteLon.Add(dataReader["Longitude"] + "");
                        }

                        dataReader.Close();
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        return null;
                    }
                }
            }

            List<string> totalRoute = new List<string>();
            totalRoute.Add(BusRouteLat[BusRouteLat.Count() - 1]);
            totalRoute.Add(BusRouteLon[BusRouteLon.Count() - 1]);
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
}
