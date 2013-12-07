using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackABusSim
{
    class SimulationMath
    {
        public double Haversine(string lat1, string lon1, string lat2, string lon2)
        {
            double earthRadius = 6371; //kilometers
            double deltaLat = deg2rad(double.Parse(lat2) - double.Parse(lat1));
            double deltaLon = deg2rad(double.Parse(lon2) - double.Parse(lon1));
            double a = Math.Pow(Math.Sin(deltaLat / 2), 2) +
                       Math.Pow(Math.Sin(deltaLon / 2), 2) *
                       Math.Cos(deg2rad(double.Parse(lat1))) * Math.Cos(deg2rad(double.Parse(lat2)));
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return earthRadius * c * 1000; 
        }

        public double BearingDegs(string lat1, string lon1, string lat2, string lon2)
        {
            double deltaLon = deg2rad(double.Parse(lon2) - double.Parse(lon1));
            double lat1Rad = deg2rad(double.Parse(lat1));
            double lat2Rad = deg2rad(double.Parse(lat2));
            double x = (Math.Cos(lat1Rad) * Math.Sin(lat2Rad)) -
                        Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(deltaLon);
            double y = Math.Sin(deltaLon) * Math.Cos(lat2Rad);
            double Bearing = rad2deg(Math.Atan2(y, x));
            return ((Bearing + 360) % 360);
        }

        public Tuple<double, double> finalGPSPosDeg(string lat, string lon, double bearingDegs, double dist_m)
        {
            double earthRadius = 6371;
            double latRad = deg2rad(double.Parse(lat));
            double lonRad = deg2rad(double.Parse(lon));
            double brngRad = deg2rad(bearingDegs);
            double angularDistance = dist_m / 1000 / earthRadius;

            double finalLatRad = Math.Asin(Math.Sin(latRad) * Math.Cos(angularDistance) +
                                 Math.Cos(latRad) * Math.Sin(angularDistance) * Math.Cos(brngRad));
            double atan1 = Math.Sin(brngRad) * Math.Sin(angularDistance) * Math.Cos(latRad);
            double atan2 = Math.Cos(angularDistance) - Math.Sin(latRad) * Math.Sin(finalLatRad);
            double finalLonRad = lonRad + Math.Atan2(atan1, atan2);
            
            double finalLatDeg = rad2deg(finalLatRad);
            double finalLonDeg = ((rad2deg(finalLonRad) + 360) % 360);
            return new Tuple<double, double>(finalLatDeg, finalLonDeg);
        }

        public double deg2rad(double deg)
        {
            return deg * double.Parse((Math.PI / 180).ToString());
        }

        public double rad2deg(double rad)
        {
            return rad * double.Parse((180 / Math.PI).ToString());
        }
    }
}
