using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using MySql.Data.MySqlClient;
using System.Diagnostics;

namespace MapDrawRouteTool.Models
{
    public static class JConverter
    {
        //Convert any given string, to a Json result object
        public static JsonResult ConvertToJson<T>(List<T> list)
        {
            JsonResult jr = new JsonResult();

            jr.Data = list.ToList();
            jr.JsonRequestBehavior = JsonRequestBehavior.AllowGet;
            return jr;
        }
    }
}