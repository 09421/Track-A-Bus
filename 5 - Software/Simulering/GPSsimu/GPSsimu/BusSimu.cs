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
        private int bnID;
        private List<Tuple<string, string>> LatLonRoute;
        private RichTextBox logBox;
        private int initialPosIndex;
        private Tuple<double, double> currentPos = new Tuple<double, double>(0, 0);
        private Thread gpsPosCalcThread;
        private Window parent;
        private Random R;
        private MySqlConnection conn;
        private int updateSpeed;
        bool isRunning = true;


        public BusSimu(int busID, int busNumberID,List<Tuple<string,string>> LatLonR, RichTextBox LogBox, Window w, Random Rand, int uSpeed)
        {
            bID = busID;
            bnID = busNumberID;
            logBox = LogBox;
            LatLonRoute = LatLonR;
            parent = w;
            R = Rand;
            updateSpeed = uSpeed;

            conn = new MySqlConnection(ConfigurationManager.ConnectionStrings["TrackABusConn"].ToString());


            gpsPosCalcThread = new Thread(new ThreadStart(gpsCalc));
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)gpsPosCalcThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            gpsPosCalcThread.CurrentCulture = customCulture;
            conn.Open();
            SetInitialPos();
            conn.Close();
        }
        public void startSim()
        {
            conn.Open();
            gpsPosCalcThread.Start();
        }

        public void stopSim()
        {
            gpsPosCalcThread.Abort();
            conn.Close();
        }


        public void test()
        {
            int counter = 0;
            while (counter < 150)
            {
                double brng = BearingDegs(LatLonRoute[counter].Item1, LatLonRoute[counter].Item2,
                        LatLonRoute[counter+1].Item1, LatLonRoute[counter+1].Item2);
                counter++;
            }
        }

        private delegate void invoker(string text);
        public void gpsCalc()
        {
            int indexCounter = 0;
            while (true)
            {
                double nextSpeed =R.Next(30, 50) ;
                double nextModifier =R.NextDouble();
                double speed = nextSpeed * nextModifier;
                double travelLengthMeters = speed * (1000d / 3600d) * updateSpeed;
                double currentLength = 0;
                double nextLength = 0;
                string currPosMsg = "";
                if(indexCounter == 0)
                    indexCounter = initialPosIndex + 1;
                 

                while (currentLength < travelLengthMeters)
                {
                    if (currentPos.Item1 != 0 && currentPos.Item2 != 0 && nextLength == 0)
                    {
                        nextLength = Haversine(currentPos.Item1.ToString(), currentPos.Item2.ToString(),
                                                   LatLonRoute[indexCounter].Item1, LatLonRoute[indexCounter].Item2);
                    }
                    else
                    {
                        nextLength = Haversine(LatLonRoute[indexCounter - 1].Item1, LatLonRoute[indexCounter - 1].Item2,
                                                LatLonRoute[indexCounter].Item1, LatLonRoute[indexCounter].Item2);
                    }
                    
                    if (nextLength + currentLength > travelLengthMeters)
                    {
                        double missingLength = travelLengthMeters - currentLength;
                        double brng = BearingDegs(LatLonRoute[indexCounter - 1].Item1, LatLonRoute[indexCounter - 1].Item2,
                                                LatLonRoute[indexCounter].Item1, LatLonRoute[indexCounter].Item2);
                        if (brng == 0)
                        {
                            currPosMsg = "Bus " +bID.ToString() + ", new endpoint reached, missing length: " + (indexCounter+1).ToString();
                            parent.Dispatcher.BeginInvoke(new invoker(LogTextWrite), new object[] { currPosMsg });
                            indexCounter++;
                            continue;
                        }
                        if (currentPos.Item1 != 0 && currentPos.Item2 != 0)
                        {
                            currentPos = finalGPSPosDeg(currentPos.Item1.ToString(), currentPos.Item2.ToString(),
                                           brng, missingLength);
                        }
                        else
                        {
                            currentPos = finalGPSPosDeg(LatLonRoute[indexCounter - 1].Item1, LatLonRoute[indexCounter - 1].Item2,
                                                                   brng, missingLength);
                        }
                        SetCurrentPos();
                        break;
                        
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
            int lengthOfRoute = LatLonRoute.Count;
            int fourthLength = lengthOfRoute/4;
            initialPosIndex = R.Next(0, lengthOfRoute - fourthLength);
            string initialPosLat = LatLonRoute[initialPosIndex].Item1;
            string initialPosLon = LatLonRoute[initialPosIndex].Item2;
            string initPosMsg = "ID " + bID + " initialPos: (" + initialPosLat + ", " + initialPosLon + "), index being " + initialPosIndex.ToString(); 
            parent.Dispatcher.BeginInvoke(new invoker(LogTextWrite), new object[] { initPosMsg });

            using (MySqlCommand cmd = new MySqlCommand())
            {
                string query = "Insert into GPSPos (pos_longitude, pos_latitude, UpdateTime,BusID)" +
                               " values (" + initialPosLon + ", " + initialPosLat + ", '" +
                               DateTime.Now.ToString("HH:mm:ss") + "', " + bID.ToString() + ")";
                cmd.CommandText = query;
                cmd.Connection = conn; 
                cmd.ExecuteNonQuery();
            }
        }

        private void SetCurrentPos()
        {
            string currPosMsg = "BUS " + bID.ToString() + " NEW GPS DATA: (" + currentPos.Item1.ToString() + ", " + currentPos.Item2.ToString() + ")";
            parent.Dispatcher.BeginInvoke(new invoker(LogTextWrite), new object[] { currPosMsg });
            using(MySqlCommand cmd = new MySqlCommand())
            {
                string query = "Insert into GPSPos (pos_longitude, pos_latitude, UpdateTime,BusID)" +
                               " values (" + currentPos.Item2.ToString() + ", " + currentPos.Item1.ToString() + ", '" +
                               DateTime.Now.ToString("HH:mm:ss") + "', " + bID.ToString() + ")";
                cmd.CommandText = query;
                cmd.Connection = conn;
                
                cmd.ExecuteNonQuery();
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
     
                //double asinParam = Math.Sin(brngRad) * Math.Sin(angularDistance) / Math.Cos(finalLatRad);
                //finalLonRad = ((lonRad - Math.Asin(asinParam)+Math.PI) % (2*Math.PI)) - Math.PI;
                double atan1 = Math.Sin(brngRad)*Math.Sin(angularDistance)*Math.Cos(latRad);
                double atan2 = Math.Cos(angularDistance)-Math.Sin(latRad) * Math.Sin(finalLatRad);
                finalLonRad = lonRad + Math.Atan2(atan1, atan2);
            }
            double finalLatDeg = rad2deg(finalLatRad);
            double finalLonDeg = ((rad2deg(finalLonRad) + 360) % 360);
            //double finalLonDeg = rad2deg(finalLonRad);
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


        
    }
}
