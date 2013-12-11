using System;
namespace MapDrawRouteTool.Controllers
{
    interface IBusController
    {
        System.Web.Mvc.JsonResult GetAllBusses();
        System.Web.Mvc.JsonResult GetBussesNotOnRoute();
        System.Web.Mvc.JsonResult GetBussesOnRoute(string route);
        System.Web.Mvc.JsonResult GetRouteNames();
        int SaveBusChanges(System.Collections.Generic.List<string> NewBusList, System.Collections.Generic.List<string> AllBusses);
        int SaveChanges(System.Collections.Generic.List<string> bussesToAdd, string route, System.Collections.Generic.List<string> bussesToRemove);
    }
}
