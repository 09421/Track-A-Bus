using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using System.Diagnostics;

namespace MapDrawRouteTool.Controllers
{
    public class BusController : Controller
    {
        //
        // GET: /Bus/

        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetRouteNames()
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

                        List<string> RouteNumber = new List<string>();
                        while (read.Read())
                        {
                            RouteNumber.Add(read.GetString(0));
                        }
                        connection.Close();
                        read.Close();

                        return ConvertToJason(RouteNumber);
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        return null;
                    }
                }
            }

        }

        public JsonResult GetBussesOnRoute(string route)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
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
                        return ConvertToJason(Busses);
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        return null;
                    }
                }
            }
        }
        public JsonResult GetBussesNotOnRoute()
        {
            using (var connection = new MySqlConnection(getConnectionString()))
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
                        return ConvertToJason(Busses);
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        return null;
                    }
                }
            }
        }

        public void SaveChanges(List<string> bussesToAdd, string route, List<string> bussesToRemove)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = "Update Bus " +
                                          "SET Bus.fk_BusRoute = (SELECT BusRoute.ID " +
                                                                  "FROM BusRoute " +
                                                                  "WHERE BusRoute.RouteNumber = '" + route + "') " +
                                          "WHERE Bus.ID = ";

                        for (var i = 0; i < bussesToAdd.Count(); i++)
                        {
                            cmd.CommandText += bussesToAdd[i];
                            cmd.CommandText += " OR Bus.ID = ";
                        }

                        cmd.CommandText = cmd.CommandText.TrimEnd(" OR Bus.ID = ".ToCharArray());
                        connection.Open();

                        cmd.ExecuteNonQuery();

                        cmd.CommandText = "UPDATE Bus " +
                                          "SET Bus.fk_BusRoute = null " +
                                          "WHERE Bus.ID = ";

                        for (var i = 0; i < bussesToRemove.Count(); i++)
                        {
                            cmd.CommandText += bussesToRemove[i];
                            cmd.CommandText += " OR Bus.ID = ";
                        }
                        cmd.CommandText = cmd.CommandText.TrimEnd(" OR Bus.ID = ".ToCharArray());
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

        public JsonResult GetAllBusses()
        {
            using (var connection = new MySqlConnection(getConnectionString()))
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
                        return ConvertToJason(Busses);
                    }
                    catch (Exception e)
                    {
                        connection.Close();
                        return null;
                    }
                }
            }
        }

        public string SaveNewBus(string BusName)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = String.Format("INSERT INTO Bus (ID, IsDescending, fk_BusRoute) VALUES({0}, null, null)", BusName);
                        connection.Open();
                        cmd.ExecuteNonQuery();
                        connection.Close();
                        return "Success";
                    }
                    catch (Exception e)
                    {
                        Debug.WriteLine(e.Message);
                        connection.Close();
                        return e.Message;
                    }
                }
            }
        }

        public void Deletebus(string BusName)
        {
            using (var connection = new MySqlConnection(getConnectionString()))
            {
                using (var cmd = connection.CreateCommand())
                {
                    try
                    {
                        cmd.CommandText = String.Format("DELETE FROM Bus WHERE ID = {0}", BusName);
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

        private static string getConnectionString()
        {
            return System.Configuration.ConfigurationManager.ConnectionStrings["TrackABus"].ConnectionString;
        }
        private JsonResult ConvertToJason<T>(List<T> RouteNumber)
        {
            JsonResult jr = new JsonResult();

            jr.Data = RouteNumber.ToList();
            jr.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jr;
        }
    }
}
