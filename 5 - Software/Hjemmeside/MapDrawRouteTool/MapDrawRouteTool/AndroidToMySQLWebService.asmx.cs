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
        public string HelloWorld()
        {
            return "Hello World";
        }

        [WebMethod]
        public List<string> GetBusList()
        {
            List<string> Buslist = new List<string>();

            var mystring = System.Configuration.ConfigurationManager.ConnectionStrings["TrackABus"].ConnectionString;
            
            using (var connection = new MySqlConnection(mystring))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();
                        cmd.CommandText = "SELECT * FROM trackabus_dk_db.BusNumbers;";
                        MySqlDataReader dataReader = cmd.ExecuteReader();
                        while (dataReader.Read())
                        {
                            Buslist.Add(dataReader["BusNumber"] + "");
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

            return Buslist;
        }

        [WebMethod]
        public List<List<string>> GetBusRoute(int busNumber)
        {
            
            List<string> BusRouteLat = new List<string>();
            List<string> BusRouteLon = new List<string>();

            var mystring = System.Configuration.ConfigurationManager.ConnectionStrings["TrackABus"].ConnectionString;

            using (var connection = new MySqlConnection(mystring))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();
                        cmd.CommandText = String.Format("SELECT t1.bus_lat, t1.bus_lon " +
                                                         "FROM BusRoutePoints AS t1 INNER JOIN BusNumberToRoutePoints AS t3 ON t1.id=t3.busRoutePointId "+
                                                         "INNER JOIN BusNumbers as t2 ON t3.busNumberId=t2.busNumberId "+
                                                         "WHERE t2.busNumber={0};", busNumber);
                        MySqlDataReader dataReader = cmd.ExecuteReader();
                        while (dataReader.Read())
                        {
                            BusRouteLat.Add(dataReader["bus_lat"] + "");
                            BusRouteLon.Add(dataReader["bus_lon"] + "");
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
            List<List<string>> totalRoute = new List<List<string>>();
            totalRoute.Add(BusRouteLat);
            totalRoute.Add(BusRouteLon);
            return totalRoute;
        }

        [WebMethod]
        public List<string> GetbusPos(int busNumber)
        {
            var mystring = System.Configuration.ConfigurationManager.ConnectionStrings["TrackABus"].ConnectionString;

            List<string> BusRouteLat = new List<string>();
            List<string> BusRouteLon = new List<string>();

            using (var connection = new MySqlConnection(mystring))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        connection.Open();
                        cmd.CommandText = String.Format("SELECT t1.pos_longitude, t1.pos_latitude " +
                                 "FROM GPSPos AS t1 INNER JOIN BusNumbers AS t2 ON t1.BusID=t2.busNumberId " +
                                 "WHERE t2.busNumber={0};", busNumber);

                        MySqlDataReader dataReader = cmd.ExecuteReader();
                        while (dataReader.Read())
                        {
                            BusRouteLat.Add(dataReader["pos_latitude"] + "");
                            BusRouteLon.Add(dataReader["pos_longitude"] + "");
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
            totalRoute.Add(BusRouteLat[BusRouteLat.Count()-1]);
            totalRoute.Add(BusRouteLon[BusRouteLon.Count()-1]);
            return totalRoute;

        }
    }
}
