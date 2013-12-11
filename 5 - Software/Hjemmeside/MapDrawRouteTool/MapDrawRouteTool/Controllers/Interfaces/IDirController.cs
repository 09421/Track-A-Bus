using System;
namespace MapDrawRouteTool.Controllers
{
    interface IDirController
    {
        System.Collections.Generic.List<string> ConvertLatLng(System.Collections.Generic.List<string> latLng);
        System.Collections.Generic.List<string> ConvertLatLng(string latLng);
        System.Collections.Generic.List<System.Collections.Generic.List<string>> ConvertSubRoute(System.Collections.Generic.List<string> subRoutes);
        int DeleteSelectedBusRoute(string RouteName);
        System.Web.Mvc.JsonResult GetBusRoutesNames();
        System.Web.Mvc.JsonResult GetLatLng(System.Collections.Generic.List<string> StopNames);
        System.Web.Mvc.JsonResult GetSelectedBusRoute(string RouteName);
        System.Web.Mvc.JsonResult GetStops();
        System.Web.Mvc.JsonResult GetStopsOnRoute(string RouteName);
        void InsertBusRoute_BusStop(int routeNumberID, System.Collections.Generic.List<string> stops);
        void InsertBusRoute_RoutePoint(int routeNumberID, System.Collections.Generic.List<string> points);
        int InsertRouteNumber(string routeNumber);
        System.Collections.Generic.List<string> InsertRoutePoints(System.Collections.Generic.List<string> points);
        void InsertSubRouteIntoBusRoute(string routeNumber, int count);
        int Save(System.Collections.Generic.List<string> route, System.Collections.Generic.List<string> routeWayPoints, System.Collections.Generic.List<string> stops, System.Collections.Generic.List<string> SubRoutes, System.Collections.Generic.List<string> SubrouteWaypoint, string RouteNumber);
    }
}
