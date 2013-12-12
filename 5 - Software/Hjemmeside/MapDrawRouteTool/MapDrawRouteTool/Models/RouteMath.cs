using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Diagnostics;

namespace MapDrawRouteTool.Models
{
    public static class RouteMath
    {
        public static double deg2Rad(decimal angle)
        {
            return (double)(((decimal)Math.PI) * angle / 180);
        }

        public static double Rad2Deg(decimal angle)
        {
            return (double)angle * (180 / Math.PI);
        }

        public static double Haversine(decimal Lat1, decimal Lat2, decimal Lon1, decimal Lon2)
        {
            int EarthRadiusKM = 6371;
            double deltaLat = RouteMath.deg2Rad(Lat2 - Lat1);
            double deltaLon = RouteMath.deg2Rad(Lon2 - Lon1);
            double lat1Rads = RouteMath.deg2Rad(Lat1);
            double lat2Rads = RouteMath.deg2Rad(Lat2);

            double a = (Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2)) + (Math.Sin(deltaLon / 2) * Math.Sin(deltaLon / 2) * Math.Cos(lat1Rads) * Math.Cos(lat2Rads));
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusKM * c * 1000;
        }

        public static decimal CalculateBusStopToRouteDist(decimal stopPosLat, decimal stopPosLon, decimal EP1Lat, decimal EP1Lon, decimal EP2Lat, decimal EP2Lon)
        {
            decimal scLat;
            decimal scLon;
            if ((EP1Lon - EP2Lon) == 0)
            {
                scLon = EP1Lon;
                scLat = stopPosLat;
            }
            else if ((EP1Lat - EP2Lat) == 0)
            {
                scLon = stopPosLon;
                scLat = EP1Lat;
            }
            else
            {
                decimal sA = (EP2Lat - EP1Lat) / (EP2Lon - EP1Lon);
                decimal sB = EP1Lat + (sA * (-EP1Lon));
                scLon = ((sA * stopPosLat) + stopPosLon - (sA * sB)) / ((sA * sA) + 1);
                scLat = ((sA * sA * stopPosLat) + (sA * stopPosLon) + sB) / ((sA * sA) + 1);
            }

            if ((scLat > EP1Lat && scLat > EP2Lat) || (scLat < EP1Lat && scLat < EP2Lat) ||
               (scLon > EP1Lon && scLon > EP2Lon) || (scLon < EP1Lon && scLon < EP2Lon))
            {
                if (RouteMath.Haversine(stopPosLat, EP1Lat, stopPosLon, EP1Lon) > 25 || RouteMath.Haversine(stopPosLat, EP2Lat, stopPosLon, EP2Lon) > 25)
                { return -1; }
            }
            return (decimal)(RouteMath.Haversine(stopPosLat, scLat, stopPosLon, scLon));

        }

        public static List<List<string>> CalculateBusStopsForRoute(List<string> stops, List<string> chosenRouteID, string routeNumber, int subRoute)
        {
            List<List<string>> RouteAndStops = new List<List<string>>();
            List<string> chosenRouteLatLng = new List<string>();
            List<string> RouteWithStopsID = new List<string>(chosenRouteID);
            List<string> StopOnRoute = new List<string>();
            int stopCounter = 0;

            chosenRouteLatLng = DBConnection.GetLatLngForRoute(chosenRouteID);

            decimal currDistToStartOfRoute;
            decimal currDistToEndOfRoute;
            decimal leastDistToStartOfRoute = -1;
            decimal leastDistToEndOfRoute = -1;
            int leastStartStopID = 0;
            int leastEndStopID = 0;
            string leastStartStopName = "";
            string leastEndStopName = "";
            foreach (string s in stops)
            {
                decimal leastDist = -1;
                decimal currentDist;
                BusStops stop = new BusStops();
                int pointBeforeStopIndex = 0;

                stop = DBConnection.GetIDLatLngForStop(s);

                for (int k = 0; k < chosenRouteLatLng.Count - 2; k = k + 2)
                {
                    currentDist = RouteMath.CalculateBusStopToRouteDist(stop.Lat, stop.Lng, (decimal.Parse(chosenRouteLatLng[k])), decimal.Parse(chosenRouteLatLng[k + 1]),
                          decimal.Parse(chosenRouteLatLng[k + 2]), decimal.Parse(chosenRouteLatLng[k + 3]));

                    if ((currentDist < leastDist || leastDist == -1) && currentDist <= 15 && currentDist != -1)
                    {
                        leastDist = currentDist;
                        pointBeforeStopIndex = k / 2;
                    }
                }
                if (stops.IndexOf(s) == 0 )
                {

                    RouteWithStopsID.Insert(0, stop.ID.ToString());
                    StopOnRoute.Add(s);
                    stopCounter++;
                    continue;
                }
                else if (stops.IndexOf(s) == stops.Count - 1 )
                {
                    RouteWithStopsID.Add(stop.ID.ToString());
                    StopOnRoute.Add(s); 
                    stopCounter++;
                    continue;
                }
                else if (leastDist != -1)
                {
                    RouteWithStopsID.Insert(pointBeforeStopIndex + stopCounter + 1, stop.ID.ToString());
                    StopOnRoute.Add(s);
                    stopCounter++;
                }
            }

            RouteAndStops.Add(RouteWithStopsID);
            RouteAndStops.Add(StopOnRoute);
            return RouteAndStops;
        }
    }
}