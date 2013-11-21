using MySql.Data.MySqlClient;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GPSsimu
{
    class BusRoute
    {
        public string id;
        public string number;
        public List<Tuple<string,string>> points;
        public BusRoute(string ID, string Number)
        {
            id = ID;
            number = Number;
            
            getPoints();
        }

        private void getPoints()
        {
            List<Tuple<string, string>> rP = new List<Tuple<string, string>>();
            using(MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["TrackABusConn"].ToString()))
            {
                using(MySqlCommand cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText =
                               "SELECT Latitude, Longitude FROM RoutePoint " +
                               "join BusRoute_RoutePoint on BusRoute_RoutePoint.fk_RoutePoint=RoutePoint.ID " +
                               "join BusRoute on BusRoute_RoutePoint.fk_BusRoute = BusRoute.ID " +
                               "where BusRoute.ID = " + id + " and " +
                               "RoutePoint.ID not in (select BusStop.fk_RoutePoint from BusStop) " +
                               " order by BusRoute_RoutePoint.ID asc";
                    MySqlDataReader reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        points.Add(new Tuple<string, string>(reader["Latitude"].ToString(), reader["Longitude"].ToString()));
                    }
                    reader.Close();
                    conn.Close();
                }

            }
        }

        public void TurnAround()
        {
            points.Reverse();
        }
    }
}
