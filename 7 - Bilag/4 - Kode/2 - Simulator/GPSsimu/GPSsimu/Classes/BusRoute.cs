using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackABusSim
{
    /// <summary>
    /// All the data regarding the busroutes. Implements IClonable since each bus has to have their own version of the busroute.
    /// </summary>
    class BusRoute : ICloneable
    {
        public string id;
        public string number;
        public List<Tuple<string, string>> points;
        public List<string> stops;
        public bool isFlipped = false;

        /// <summary>
        /// Sets the ID and routenumber of the busroute. Also gets the routepoints and stops regarding this route.
        /// </summary>
        /// <param name="ID"></param>
        /// <param name="Number"></param>
        public BusRoute(string ID, string Number)
        {
            id = ID;
            number = Number;
            points = new List<Tuple<string, string>>();
            stops = new List<string>();
            getPointsAndStops();
        }

        //Copy constructor. Usec for cloning purposes.
        public BusRoute(string IDCopy, string NumberCopy, List<Tuple<string, string>> PointsCopy, List<string> stopsCopy)
        {
            id = IDCopy;
            number = NumberCopy;
            points = PointsCopy;
            stops = stopsCopy;
        }

        /// <summary>
        /// Retrieves all points and stops of the current route.
        /// </summary>
        private void getPointsAndStops()
        {
            //Query to retrieve all points for the route defined by its ID.
            string GetPointsQuery =
                               "SELECT Latitude, Longitude FROM RoutePoint " +
                               "join BusRoute_RoutePoint on BusRoute_RoutePoint.fk_RoutePoint=RoutePoint.ID " +
                               "join BusRoute on BusRoute_RoutePoint.fk_BusRoute = BusRoute.ID " +
                               "where BusRoute.ID = " + id + " and " +
                               "RoutePoint.ID not in (select BusStop.fk_RoutePoint from BusStop) " +
                               " order by BusRoute_RoutePoint.ID asc";
            //List of strings containing latiude and longitude sequentially.
            List<string> nonSortedRoute = DatabaseAcces.Query(GetPointsQuery, new List<string>() { "Latitude", "longitude" });
            //Sort the list, so each point is its own tuple, with latitude and longitude.
            for (int i = 0; i < nonSortedRoute.Count; i = i + 2)
            {
                points.Add(new Tuple<string, string>(nonSortedRoute[i], nonSortedRoute[i + 1]));
            }
            //Query to get all busstops for the route defined by its ID.
            string GetStopsQuery = "Select BusStop.StopName from BusStop " +
                                              "join BusRoute_BusStop on BusStop.ID = BusRoute_BusStop.fk_BusStop " +
                                              "where BusRoute_BusStop.fk_BusRoute = " + id + " order by(BusRoute_BusStop.ID) asc";
            //Only one column. No need to sort.
            stops = DatabaseAcces.Query(GetStopsQuery, new List<string>() { "StopName" });
        }

        //Turns the route around. Setting the first point as the last, and the last as the first. Same with stops.
        public void TurnAround()
        {
            points.Reverse();
            stops.Reverse();
            isFlipped = !isFlipped;
        }

        /// <summary>
        /// Returns new instance of the route, from the old.
        /// </summary>
        /// <returns>Cloned route</returns>
        public object Clone()
        {
            return new BusRoute(id, number, points.ToList(), stops.ToList());
        }
    }
}
