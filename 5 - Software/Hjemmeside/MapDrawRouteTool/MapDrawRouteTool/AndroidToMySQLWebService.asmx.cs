using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Services;
using MySql.Data.MySqlClient;
using MapDrawRouteTool.Models;

namespace MapDrawRouteTool
{
    [WebService(Namespace = "http://TrackABus.dk/Webservice/")]
    [WebServiceBinding(ConformsTo = WsiProfiles.BasicProfile1_1)]
    [System.ComponentModel.ToolboxItem(false)]

    public class AndroidToMySQLWebService : System.Web.Services.WebService
    {
        [WebMethod]
        public List<string> GetBusList()
        {
            List<string> Buslist = new List<string>();

            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
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
                        connection.Close();
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

            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();
                        cmd.CommandText = String.Format("SELECT BusRoute.ID, BusRoute.SubRoute FROM BusRoute WHERE RouteNumber = '{0}'", busNumber);
                        List<Tuple<string, string>> SubrouteNumbers = new List<Tuple<string, string>>();

                        MySqlDataReader dataReader = cmd.ExecuteReader();
                        while (dataReader.Read())
                        {
                            SubrouteNumbers.Add(new Tuple<string, string>(dataReader["ID"].ToString(), dataReader["SubRoute"].ToString()));
                        }
                        dataReader.Close();


                        foreach (var subroutenumber in SubrouteNumbers)
                        {
                            var route = new Route();

                            cmd.CommandText = String.Format("SELECT t1.ID, t1.Latitude, t1.Longitude, t2.ID " +
                                                           "FROM RoutePoint AS t1 " +
                                                           "INNER JOIN BusRoute_RoutePoint AS t2 ON t1.ID = t2.fk_RoutePoint " +
                                                           "INNER JOIN BusRoute AS t3 ON t2.fk_BusRoute = t3.ID " +
                                                           "WHERE t3.RouteNumber = '{0}' AND t3.SubRoute = {1} AND " +
                                                           "t1.ID NOT IN (SELECT BusStop.fk_routePoint FROM BusStop)", busNumber, subroutenumber.Item2);

                            MySqlDataReader dataReader1 = cmd.ExecuteReader();

                            while (dataReader1.Read())
                            {
                                Point pt = new Point();
                                pt.ID = dataReader1.GetInt32(0).ToString();
                                pt.Lat = dataReader1["Latitude"].ToString();
                                pt.Lng = dataReader1["Longitude"].ToString();
                                route.RoutePoint.Add(pt);
                                route.BusRoute_RoutePoint.Add(dataReader1.GetInt32(3).ToString());
                            }
                            route.ID = subroutenumber.Item1;
                            route.SubRoute = subroutenumber.Item2;
                            route.RouteNr = busNumber;
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
                        r.ID = e.Message;
                        totalRoute.Add(r);
                        return totalRoute;
                    }
                }
            }
            return totalRoute;
        }

        [WebMethod]
        public List<BusStop> GetBusStops(string busNumber)
        {

            List<BusStop> totalBusStops = new List<BusStop>();

            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();

                        cmd.CommandText = String.Format("SELECT t1.ID, t1.Latitude, t1.Longitude,t2.ID, t2.StopName, t3.ID, t4.ID " +
                                                        "FROM RoutePoint AS t1 " +
                                                        "INNER JOIN BusStop t2 ON t1.ID = t2.fk_RoutePoint " +
                                                        "INNER JOIN BusRoute_BusStop AS t3 ON t2.ID = t3.fk_BusStop " +
                                                        "INNER JOIN BusRoute AS t4 ON t3.fk_BusRoute = t4.ID " +
                                                        "WHERE t4.RouteNumber = '{0}'", busNumber);

                        MySqlDataReader dataReader = cmd.ExecuteReader();
                        BusStop bs;
                        while (dataReader.Read())
                        {
                            bs = new BusStop();
                            Point pt = new Point();
                            pt.ID = dataReader.GetInt32(0).ToString();
                            pt.Lat = dataReader["Latitude"].ToString();
                            pt.Lng = dataReader["Longitude"].ToString();

                            bs.ID = dataReader.GetInt32(3).ToString();
                            bs.Pos = pt;
                            bs.RouteID = dataReader.GetInt32(6).ToString().ToString();
                            bs.Name = dataReader["StopName"].ToString();
                            bs.BusRouteBusStopID = dataReader.GetInt32(5).ToString();
                            totalBusStops.Add(bs);
                        }
                        dataReader.Close();
                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        totalBusStops = new List<BusStop>();
                        totalBusStops.Add(new BusStop() { ID = e.Message });
                        return totalBusStops;
                    }
                }
            }
            return totalBusStops;
        }

        [WebMethod]
        public List<Point> GetbusPos(string busNumber)
        {
            List<Point> totalRoute = new List<Point>();
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
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
                            cmd.CommandText = String.Format("SELECT t1.Latitude, t1.Longitude, t1.ID " +
                                                            "FROM GPSPosition AS t1 " +
                                                            "INNER JOIN Bus AS t2 ON t1.fk_Bus=t2.ID " +
                                                            "INNER JOIN BusRoute AS t3 on t2.fk_BusRoute = t3.ID " +
                                                            "WHERE t3.RouteNumber='{0}' AND t1.fk_Bus = {1} ORDER BY t1.ID DESC LIMIT 1;", busNumber, NumberOfBussesOnRoute[i]);

                            MySqlDataReader dataReader2 = cmd.ExecuteReader();
                            while (dataReader2.Read())
                            {
                                Point pt = new Point();
                                pt.ID = dataReader2.GetInt32(0).ToString();
                                pt.Lat = dataReader2["Latitude"].ToString();
                                pt.Lng = dataReader2["Longitude"].ToString();
                                totalRoute.Add(pt);
                            }

                            dataReader2.Close();
                        }

                        connection.Close();
                    }
                    catch (Exception e)
                    {
                        totalRoute = new List<Point>();
                        Point pt = new Point();
                        pt.ID = e.Message;
                        pt.Lat = null;
                        pt.Lng = null;
                        totalRoute.Add(pt);
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
            using (var connection = new MySqlConnection(DBConnection.getConnectionString()))
            {
                using (var cmd = new MySqlCommand("CalcBusToStopTime", connection))
                {
                    int i = 0;
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

                        cmd.Parameters.Add(new MySqlParameter("?busIDAsc", MySqlDbType.Int32));
                        cmd.Parameters["?busIDAsc"].Direction = System.Data.ParameterDirection.Output;

                        cmd.Parameters.Add(new MySqlParameter("?busIDDesc", MySqlDbType.Int32));
                        cmd.Parameters["?busIDDesc"].Direction = System.Data.ParameterDirection.Output;

                        cmd.Parameters.Add(new MySqlParameter("?EndBusStopAsc", MySqlDbType.VarChar));
                        cmd.Parameters["?EndBusStopAsc"].Direction = System.Data.ParameterDirection.Output;

                        cmd.Parameters.Add(new MySqlParameter("?EndBusStopDesc", MySqlDbType.VarChar));
                        cmd.Parameters["?EndBusStopDesc"].Direction = System.Data.ParameterDirection.Output;

                        connection.Open();
                        cmd.ExecuteNonQuery();
                        i = 7;
                        if (string.IsNullOrEmpty(cmd.Parameters["?TimeToStopSecAsc"].Value.ToString())
                            || int.Parse(cmd.Parameters["?TimeToStopSecAsc"].Value.ToString()) > 10000)
                        {

                            results.Add("-1");
                            results.Add("");
                        }
                        else
                        {
                            results.Add(cmd.Parameters["?TimeToStopSecAsc"].Value.ToString());
                            results.Add(cmd.Parameters["?EndBusStopAsc"].Value.ToString());
                        }

                        if (string.IsNullOrEmpty(cmd.Parameters["?TimeToStopSecDesc"].Value.ToString())
                            || int.Parse(cmd.Parameters["?TimeToStopSecDesc"].Value.ToString()) > 10000)
                        {
                            results.Add("-1");
                            results.Add("");
                        }
                        else
                        {
                            results.Add(cmd.Parameters["?TimeToStopSecDesc"].Value.ToString());
                            results.Add(cmd.Parameters["?EndBusStopDesc"].Value.ToString());
                        }

                        connection.Close();
                        return results;
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        results.Add("ERROR! : " + e.Message + ", at: " + i.ToString());
                        return results;
                    }
                }
            }

        }
    }

    public class Route
    {
        public Route()
        {
            RoutePoint = new List<Point>();
            BusRoute_RoutePoint = new List<string>();
        }
        public List<Point> RoutePoint;
        public List<string> BusRoute_RoutePoint;
        public string SubRoute;
        public string RouteNr;
        public string ID;
    }

    public class Point
    {
        public Point()
        {
        }
        public string Lat;
        public string Lng;
        public string ID;
    }

    public class BusStop
    {
        public BusStop()
        { }
        public Point Pos;
        public string Name;
        public string ID;
        public string RouteID;
        public string BusRouteBusStopID;
    }

}
