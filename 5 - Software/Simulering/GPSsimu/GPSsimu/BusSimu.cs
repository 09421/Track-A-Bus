using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows;
using MySql.Data.MySqlClient;
using System.Configuration;
namespace GPSsimu
{
    class BusSimu
    {
        private int bID;
        private List<BusRoute> routes = new List<BusRoute>();
        private RichTextBox logBox;
        private Window parent;
        private Random R;
        private int updateSpeed;
        private int initialPosIndex;
        private BusRoute initialRoute;
        private Tuple<double, double> currentPos = new Tuple<double, double>(0, 0);
        private Thread gpsPosCalcThread;
        private int indexCounter = -1;
        public int maxSpeed = 50;
        public int minSpeed = 30;




        public BusSimu(int busID, List<Tuple<string,string>> RoutesIDAndNumber, RichTextBox LogBox, Window w, Random Rand, int uSpeed, bool startDecending)
        {

            bID = busID;
            logBox = LogBox;
            foreach (Tuple<string,string> rID in RoutesIDAndNumber)
            {
                routes.Add(new BusRoute(rID.Item1, rID.Item2));
            }
            parent = w;
            R = Rand;
            updateSpeed = uSpeed;
            initialRoute = routes[Rand.Next(0, routes.Count)];


            if (startDecending)
            {
                initialRoute.TurnAround();
            }
            


            gpsPosCalcThread = new Thread(new ThreadStart(gpsCalc));
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)gpsPosCalcThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            gpsPosCalcThread.CurrentCulture = customCulture;

            InitializeBusDB(startDecending);
            SetInitialPos();
        }
        public void startSim()
        {
            gpsPosCalcThread.Start();
        }

        public void stopSim()
        {
            gpsPosCalcThread.Abort();
        }

        private delegate void invoker(string text);
        public void gpsCalc()
        {
            
            while (true)
            {
                double nextSpeed =R.Next(minSpeed, maxSpeed+1) ;
                //double nextModifier =R.NextDouble();
                double speed = nextSpeed; //* nextModifier;
                double travelLengthMeters = speed * (1000d / 3600d) * updateSpeed;
                double currentLength = 0;
                double nextLength = 0;
                double brng;
                string currPosMsg = "";
                if(indexCounter == -1)
                    indexCounter = initialPosIndex + 1;
                 

                while (currentLength < travelLengthMeters)
                {

                    if(indexCounter == initialRoute.points.Count - 1)
                    {
                        currentPos = new Tuple<double,double>(double.Parse(initialRoute.points[indexCounter].Item1),double.Parse(initialRoute.points[indexCounter].Item2)); 
                        UpdateBus();
                        break;
                    }

                    if (currentPos.Item1 != 0 && currentPos.Item2 != 0 && nextLength == 0)
                    {
                        nextLength = Haversine(currentPos.Item1.ToString(), currentPos.Item2.ToString(),
                                                   initialRoute.points[indexCounter].Item1, initialRoute.points[indexCounter].Item2);

                        brng = BearingDegs(currentPos.Item1.ToString(), currentPos.Item2.ToString(),
                                            initialRoute.points[indexCounter].Item1, initialRoute.points[indexCounter].Item2);
                    }
                    else
                    {
                        nextLength = Haversine(initialRoute.points[indexCounter - 1].Item1, initialRoute.points[indexCounter - 1].Item2,
                                                initialRoute.points[indexCounter].Item1, initialRoute.points[indexCounter].Item2);
                        brng = BearingDegs(initialRoute.points[indexCounter - 1].Item1, initialRoute.points[indexCounter - 1].Item2,
                                            initialRoute.points[indexCounter].Item1, initialRoute.points[indexCounter].Item2);

                    }
                    Console.WriteLine("Next: {0} ; Current: {1}", nextLength, currentLength);

                    //If The next line + the current calculated length of the route, is greater than the length the bus should drive
                    if (nextLength + currentLength > travelLengthMeters)
                    {
                        //The distance into the linepiece the busshould drive.
                        double missingLength = travelLengthMeters - currentLength;
                        //if (brng == 0)
                        //{
                        //    currPosMsg = "Bus " +bID.ToString() + ", new endpoint reached, missing length: " + (indexCounter+1).ToString();
                        //    parent.Dispatcher.BeginInvoke(new invoker(LogTextWrite), new object[] { currPosMsg });
                        //    indexCounter++;
                        //    continue;
                        //}
                        if (currentPos.Item1 != 0 && currentPos.Item2 != 0)
                        {
                            currentPos = finalGPSPosDeg(currentPos.Item1.ToString(), currentPos.Item2.ToString(),
                                           brng, missingLength);
                        }
                        else
                        {
                            currentPos = finalGPSPosDeg(initialRoute.points[indexCounter - 1].Item1, initialRoute.points[indexCounter - 1].Item2,
                                                                   brng, missingLength);
                        }
                        SetCurrentPos();

                        break ;
                        
                    }
                    else
                    {
                        currentLength += nextLength;
                    }
                    currPosMsg = "Bus " + bID.ToString() + ", new endpoint reached, index: " + (indexCounter + 1).ToString();
                    parent.Dispatcher.BeginInvoke(new invoker(LogTextWrite), new object[] { currPosMsg });
   
                    indexCounter++;

                }

                Thread.Sleep(updateSpeed * 1000);
            }
            
        }

        private void LogTextWrite(string text)
        {
            logBox.AppendText(text + "\n");
        }
        
        private void SetInitialPos()
        {
            int lengthOfRoute = initialRoute.points.Count;
            int fourthLength = lengthOfRoute/4;
            initialPosIndex = R.Next(0, lengthOfRoute - fourthLength + 1 );
            string initialPosLat = initialRoute.points[initialPosIndex].Item1;
            string initialPosLon = initialRoute.points[initialPosIndex].Item2;
            string initPosMsg = "ID " + bID + " initialPos: (" + initialPosLat + ", " + initialPosLon + "), index being " + initialPosIndex.ToString(); 
            parent.Dispatcher.BeginInvoke(new invoker(LogTextWrite), new object[] { initPosMsg });

            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["TrackABusConn"].ToString()))
            {
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    conn.Open();
                    string query = "Insert into GPSPosition (Longitude, Latitude, UpdateTime,fk_Bus)" +
                                   " values (" + initialPosLon + ", " + initialPosLat + ", '" +
                                   DateTime.Now.ToString("HH:mm:ss") + "', " + bID.ToString() + ")";
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        private void SetCurrentPos()
        {
            string currPosMsg = "BUS " + bID.ToString() + " NEW GPS DATA: (" + currentPos.Item1.ToString() + ", " + currentPos.Item2.ToString() + ")";
            parent.Dispatcher.BeginInvoke(new invoker(LogTextWrite), new object[] { currPosMsg });
            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["TrackABusConn"].ToString()))
            {
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    conn.Open();
                    string query = "Insert into GPSPosition (Longitude, Latitude, UpdateTime,fk_Bus)" +
                                   " values (" + currentPos.Item2.ToString() + ", " + currentPos.Item1.ToString() + ", '" +
                                   DateTime.Now.ToString("HH:mm:ss") + "', " + bID.ToString() + ")";
                    cmd.CommandText = query;

                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        private double Haversine(string lat1, string lon1, string lat2, string lon2)
        {
            double earthRadius = 6371; //kilometers
            double deltaLat = deg2rad(double.Parse(lat2) - double.Parse(lat1));
            double deltaLon = deg2rad(double.Parse(lon2) - double.Parse(lon1));
            double a = Math.Pow(Math.Sin(deltaLat / 2), 2) +
                       Math.Pow(Math.Sin(deltaLon / 2), 2) *
                       Math.Cos(deg2rad(double.Parse(lat1))) * Math.Cos(deg2rad(double.Parse(lat2)));
            double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1-a));
            return earthRadius * c * 1000; //meters
            
        }

        private double BearingDegs(string lat1, string lon1, string lat2, string lon2)
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

        private Tuple<double,double> finalGPSPosDeg(string lat, string lon, double bearingDegs, double dist_m)
        {
            double earthRadius = 6356.752;
            double latRad = deg2rad(double.Parse(lat));
            double lonRad = deg2rad(double.Parse(lon));
            double brngRad = deg2rad(bearingDegs);
            double angularDistance = dist_m / 1000 / earthRadius;

            double finalLatRad = Math.Asin(Math.Sin(latRad) * Math.Cos(angularDistance) +
                                 Math.Cos(latRad) * Math.Sin(angularDistance) * Math.Cos(brngRad));
            double finalLonRad;


            if (Math.Cos(finalLatRad) == 0)
            {
                finalLonRad = lonRad;
            }
            else
            {
     
                double atan1 = Math.Sin(brngRad)*Math.Sin(angularDistance)*Math.Cos(latRad);
                double atan2 = Math.Cos(angularDistance)-Math.Sin(latRad) * Math.Sin(finalLatRad);
                finalLonRad = lonRad + Math.Atan2(atan1, atan2);
            }
            double finalLatDeg = rad2deg(finalLatRad);
            double finalLonDeg = ((rad2deg(finalLonRad) + 360) % 360);
            return new Tuple<double, double>(finalLatDeg, finalLonDeg);
        }

        private double deg2rad(double deg)
        {
            return deg * double.Parse((Math.PI / 180).ToString());
        }

        private double rad2deg(double rad)
        {
            return rad * double.Parse((180 / Math.PI).ToString());
        }

        private void InitializeBusDB(bool desc)
        {
            string query = string.Format("Update Bus set Bus.fk_BusRoute = {0}, Bus.IsDescending = {1}", initialRoute.id, desc.ToString());
            using (MySqlConnection conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["TrackABusConn"].ToString()))
            {
                using (MySqlCommand cmd = conn.CreateCommand())
                {
                    conn.Open();
                    cmd.CommandText = query;
                    cmd.ExecuteNonQuery();
                    conn.Close();
                }
            }
        }

        private void UpdateBus()
        {
            if (routes.Count == 1)
            {
                initialRoute.TurnAround();
                indexCounter = 0;
            }
            else
            {
                List<BusRoute> possibleRoutes;
                string oldRouteStop= initialRoute.stops[initialRoute.stops.Count - 1];
                string atStop = initialRoute.stops[initialRoute.stops.Count - 1];
                possibleRoutes = routes.FindAll(R => (R.stops[R.stops.Count - 1] == atStop) || R.stops[0] == atStop);

                if (possibleRoutes.Count == 1)
                {
                    initialRoute.TurnAround();
                }

                else
                {
                    possibleRoutes.Remove(initialRoute);
                    initialRoute = possibleRoutes[R.Next(0, possibleRoutes.Count)];
                    if (initialRoute.stops[0] != oldRouteStop)
                    {
                        initialRoute.TurnAround();
                    }
                }
                indexCounter = 0;
                
   
            }
        }
        
    }
}
