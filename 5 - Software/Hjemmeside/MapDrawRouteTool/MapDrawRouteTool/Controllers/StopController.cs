using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using System.Configuration;
using MapDrawRouteTool.Models;

namespace MapDrawRouteTool.Controllers
{
    public class StopController : Controller, IStopController
    {
        public ActionResult Index()
        {
            return View();
        }

        public int Save(string c, string n)
        {
            return DBConnection.SaveBusstop(formatCord(c), formatNames(n));
        }

        public int Delete(string stop)
        {
            return DBConnection.DeleteBusstop(stop);
        }

        public int SaveChangeToStop(string oldName, string newName, string NewPos)
        {
            return DBConnection.RenameBusstop(oldName, newName, NewPos);
        }

        public JsonResult GetAllStops()
        {
            return DBConnection.GetAllStops();
        }

        public JsonResult GetPosistion(string stopName)
        {
            var stopname = new List<string>();
            stopname.Add(stopName);
            return DBConnection.GetPosistionForBusstop(stopname);
        }

        private List<string> formatNames(string name)
        {
            var t = name.Split(',');
            List<string> names = new List<string>();
            foreach (var y in t)
            {
                var j = y;
                j = j.TrimEnd(']');
                j = j.TrimStart('[');
                j = j.TrimEnd('"');
                j = j.TrimStart('"');
                j = j.TrimEnd(')');
                j = j.TrimStart('(');
                j = j.Trim();
                names.Add(j);
            }
            return names;
        }

        private List<string> formatCord(string cord)
        {
            var t = cord.Split(',');
            List<string> cords = new List<string>();
            foreach (var y in t)
            {
                var j = y;
                j = j.TrimEnd(']');
                j = j.TrimStart('[');
                j = j.TrimEnd('"');
                j = j.TrimStart('"');
                j = j.TrimEnd(')');
                j = j.TrimStart('(');
                j = j.Trim();
                cords.Add(j);
            }
            return cords;
        }
    }
}
