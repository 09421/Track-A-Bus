using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackABus_BusSimulator.Model
{
    class BusRoute 
    {
        public string id;
        public string number;
        public List<Tuple<string, string>> points;
        public List<string> stops;
        public bool isFlipped = false;

        public BusRoute(string ID, string Number)
        {
            id = ID;
            number = Number;
            points = new List<Tuple<string, string>>();
            stops = new List<string>();
            getPointsAndStops();
        }

        private void getPointsAndStops()
        {
            string GetPointsQuery =
                               "SELECT Latitude, Longitude FROM RoutePoint " +
                               "join BusRoute_RoutePoint on BusRoute_RoutePoint.fk_RoutePoint=RoutePoint.ID " +
                               "join BusRoute on BusRoute_RoutePoint.fk_BusRoute = BusRoute.ID " +
                               "where BusRoute.ID = " + id + " and " +
                               "RoutePoint.ID not in (select BusStop.fk_RoutePoint from BusStop) " +
                               " order by BusRoute_RoutePoint.ID asc";
            List<string> nonSortedRoute = DatabaseAcces.Query(GetPointsQuery, new List<string>() { "Latitude", "longitude" });
            for (int i = 0; i < nonSortedRoute.Count; i = i + 2)
            {
                points.Add(new Tuple<string, string>(nonSortedRoute[i], nonSortedRoute[i + 1]));
            }

            string GetStopsQuery = "Select BusStop.StopName from BusStop " +
                                              "join BusRoute_BusStop on BusStop.ID = BusRoute_BusStop.fk_BusStop " +
                                              "where BusRoute_BusStop.fk_BusRoute = " + id + " order by(BusRoute_BusStop.ID) asc";
            List<string> stops = DatabaseAcces.Query(GetPointsQuery, new List<string>() { "StopName" });
        }

        public void TurnAround()
        {
            points.Reverse();
            stops.Reverse();
            isFlipped = !isFlipped;
        }
    }
}
