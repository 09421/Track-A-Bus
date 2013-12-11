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
    public class DirController : Controller, IDirController
    {
        public ActionResult Index()
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
                    var routeNumberId = DBConnection.InsertRouteNumber(RouteNumber);
                    if (routeWayPoints != null)
                       DBConnection.InsertWaypoints(routeNumberId, ConvertLatLng(routeWayPoints));
                    pointList = ConvertLatLng(route);


                    pointIDList = DBConnection.InsertRoutePoints(pointList);
                    if (SubRoutes != null)
                    {
                        DBConnection.InsertSubRouteIntoBusRoute(RouteNumber, SubRoutes.Count());
                        if (SubrouteWaypoint != null)
                            DBConnection.InsertSubRouteWaypoints(SubrouteWaypoint, RouteNumber, SubRoutes.Count());
                        g = ConvertSubRoute(SubRoutes);
                        for (var i = 0; i < g.Count(); i++)
                        {
                            SubRoutesIDList.Add(DBConnection.InsertRoutePoints(g[i]));
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
                            RouteAndStops = RouteMath.CalculateBusStopsForRoute(stops, pointIDList, RouteNumber, i);
                            BusRouteWithStops = RouteAndStops[0];
                            BusStops = RouteAndStops[1];
                            DBConnection.InsertBusRoute_RoutePoint(routeNumberId, BusRouteWithStops);
                            DBConnection.InsertBusRoute_BusStop(routeNumberId, BusStops);
                        }
                        else // subroute
                        {
                            RouteAndStops = RouteMath.CalculateBusStopsForRoute(stops, SubRoutesIDList[i - 1], RouteNumber, i);
                            BusRouteWithStops = RouteAndStops[0];
                            BusStops = RouteAndStops[1];
                            DBConnection.InsertBusRoute_RoutePoint(routeNumberId + i, BusRouteWithStops);
                            DBConnection.InsertBusRoute_BusStop(routeNumberId + i, BusStops);
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

        private List<string> ConvertLatLng(List<string> latLng)
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

        private List<List<string>> ConvertSubRoute(List<string> subRoutes)
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
            return DBConnection.GetSelectedBusRoute(RouteName);
        }

        public JsonResult GetBusRoutesNames()
        {
            return DBConnection.GetAllRouteNames();
        }        

        public JsonResult GetStopsOnRoute(string RouteName)
        {
            return DBConnection.GetStopsOnRoute(RouteName);
        }

        public JsonResult GetStops()
        {
            return DBConnection.GetAllStops();
        }

        public JsonResult GetLatLng(List<string> StopNames)
        {
            return DBConnection.GetPosistionForBusstop(StopNames);
        }

        public int DeleteSelectedBusRoute(string RouteName)
        {
            return DBConnection.DeleteSelectedBusRouteFromDB(RouteName);
        }
    }
}