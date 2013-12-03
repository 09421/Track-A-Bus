using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using System.Configuration;

namespace MapDrawRouteTool.Models
{
    public class BusStops
    {
        public string busStopNames { get; set; }

        public int ID {get;set;}
        public string name { get; set; }
        public Decimal Lat { get; set; }
        public Decimal Lng { get; set; }

        //public BusStops(string busStopName)
        //{
        //    busStopNames = busStopName;
            
        //}

        public static List<BusStops> GetAllBusStops()
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
                        cmd.CommandText = "SELECT StopName FROM BusStop;";
                        var read = cmd.ExecuteReader();

                        while (read.Read())
                        {
                            stops.Add(new BusStops(){busStopNames = read.GetString(0)});
                        }
                        read.Close();
                        connection.Close();

                        return stops;
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

    }
}