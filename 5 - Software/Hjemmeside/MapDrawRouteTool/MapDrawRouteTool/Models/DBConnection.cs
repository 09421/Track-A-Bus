using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace MapDrawRouteTool.Models
{
    public static class DBConnection
    {
        public static string getConnectionString()
        {
            return System.Configuration.ConfigurationManager.ConnectionStrings["TrackABus"].ConnectionString;
        }
    }
}