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
    //Controller class for the bus view
    public class BusController : Controller, IBusController
    {
        //
        // GET: /Bus/

        public ActionResult Index()
        {
            return View();
        }

        //Get the name for all routes
        public JsonResult GetRouteNames()
        {
            return DBConnection.GetAllRouteNames();
        }

        //Get ID for all busses on a given route
        public JsonResult GetBussesOnRoute(string route)
        {
            return DBConnection.GetBussesOnRoute(route);
        }

        //Get ID for all busses not on any routes
        public JsonResult GetBussesNotOnRoute()
        {
            return DBConnection.GetBussesNotOnRoute();
        }

        //Get ID for all busses
        public JsonResult GetAllBusses()
        {
            return DBConnection.GetAllBusses();
        }

        //Save changes to busses
        public int SaveChanges(List<string> bussesToAdd, string route, List<string> bussesToRemove)
        {
            return DBConnection.SaveChangesToBus(bussesToAdd, route, bussesToRemove);
        }
        
        //Finds what busses to delete, and what busses to save
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

        //Delete busses from database
        private void removeBusses(List<string> bussesToRemove)
        {
            DBConnection.removeBussesFromDB(bussesToRemove);
        }

        //Save busses to database
        private int addBusses(List<string> bussesToAdd)
        {
            return DBConnection.addBussesToDB(bussesToAdd);
        }
    }
}
