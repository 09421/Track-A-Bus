using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using System.Diagnostics;
using MapDrawRouteTool.Models;

namespace MapDrawRouteTool.Controllers
{
    public class BusController : Controller, IBusController
    {
        //
        // GET: /Bus/

        public ActionResult Index()
        {
            return View();
        }

        public JsonResult GetRouteNames()
        {
            return DBConnection.GetAllRouteNames();
        }

        public JsonResult GetBussesOnRoute(string route)
        {
            return DBConnection.GetBussesOnRoute(route);
        }

        public JsonResult GetBussesNotOnRoute()
        {
            return DBConnection.GetBussesNotOnRoute();
        }

        public JsonResult GetAllBusses()
        {
            return DBConnection.GetAllBusses();
        }

        public int SaveChanges(List<string> bussesToAdd, string route, List<string> bussesToRemove)
        {
            return DBConnection.SaveChangesToBus(bussesToAdd, route, bussesToRemove);
        }       

        public int SaveBusChanges(List<string> NewBusList, List<string> AllBusses)
        {
            List<string> BussesToAdd = new List<string>();
            List<string> BussesToRemove = new List<string>();

            foreach (var bus in NewBusList)
            {
                if (!AllBusses.Contains(bus))
                    BussesToAdd.Add(bus);
            }

            foreach (var bus in AllBusses)
            {
                if (!NewBusList.Contains(bus))
                    BussesToRemove.Add(bus);
            }

            removeBusses(BussesToRemove);
            return addBusses(BussesToAdd);
        }

        private void removeBusses(List<string> bussesToRemove)
        {
            DBConnection.removeBussesFromDB(bussesToRemove);
        }

        private int addBusses(List<string> bussesToAdd)
        {
            return DBConnection.addBussesToDB(bussesToAdd);
        }
    }
}
