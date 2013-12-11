using System;
namespace MapDrawRouteTool.Controllers
{
    interface IDirController
    {
        int DeleteSelectedBusRoute(string RouteName);
        System.Web.Mvc.JsonResult GetBusRoutesNames();
        System.Web.Mvc.JsonResult GetLatLng(System.Collections.Generic.List<string> StopNames);
        System.Web.Mvc.JsonResult GetSelectedBusRoute(string RouteName);
        System.Web.Mvc.JsonResult GetStops();
        System.Web.Mvc.JsonResult GetStopsOnRoute(string RouteName);
        int Save(System.Collections.Generic.List<string> route, System.Collections.Generic.List<string> routeWayPoints, System.Collections.Generic.List<string> stops, System.Collections.Generic.List<string> SubRoutes, System.Collections.Generic.List<string> SubrouteWaypoint, string RouteNumber);
    }
}
