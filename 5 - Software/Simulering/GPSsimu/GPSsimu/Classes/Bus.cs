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
using TrackABusSim.Classes;

namespace TrackABusSim
{
    class Bus
    {
        private int bID;
        private int updateSpeed;
        private int initialPosIndex;
        private int indexCounter = -1;
        public int maxSpeed = 50;
        public int minSpeed = 30;
        public Logger log = new Logger();

        private Random R;
        private BusRoute initialRoute;
        private Tuple<double, double> currentPos = new Tuple<double, double>(0, 0);
        private List<BusRoute> routes;
        private Thread gpsPosCalcThread;
       
        private SimulationMath sMath = new SimulationMath();

        public Bus(int busID, List<BusRoute> Routes, Random Rand, int uSpeed, bool startDecending, int startingMinSpeed, int startingMaxSpeed)
        {
            routes = Routes;
            bID = busID;
            R = Rand;
            updateSpeed = uSpeed;
            initialRoute = Routes[Rand.Next(0, routes.Count)];

            if (startDecending)
            {
                initialRoute.TurnAround();
            }
            UpdateBusDB();
            
            gpsPosCalcThread = new Thread(new ThreadStart(gpsCalc));
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)gpsPosCalcThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            gpsPosCalcThread.CurrentCulture = customCulture;
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
                double speed = nextSpeed; 
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
                        nextLength = sMath.Haversine(currentPos.Item1.ToString(), currentPos.Item2.ToString(),
                                                   initialRoute.points[indexCounter].Item1, initialRoute.points[indexCounter].Item2);

                        brng = sMath.BearingDegs(currentPos.Item1.ToString(), currentPos.Item2.ToString(),
                                            initialRoute.points[indexCounter].Item1, initialRoute.points[indexCounter].Item2);
                    }
                    else
                    {
                        nextLength = sMath.Haversine(initialRoute.points[indexCounter - 1].Item1, initialRoute.points[indexCounter - 1].Item2,
                                                initialRoute.points[indexCounter].Item1, initialRoute.points[indexCounter].Item2);
                        brng = sMath.BearingDegs(initialRoute.points[indexCounter - 1].Item1, initialRoute.points[indexCounter - 1].Item2,
                                            initialRoute.points[indexCounter].Item1, initialRoute.points[indexCounter].Item2);

                    }
                  
                    //If The next line + the current calculated length of the route, is greater than the length the bus should drive
                    if (nextLength + currentLength > travelLengthMeters)
                    {
                        //The distance into the linepiece the busshould drive.
                        double missingLength = travelLengthMeters - currentLength;
    
                        if (currentPos.Item1 != 0 && currentPos.Item2 != 0)
                        {
                            currentPos = sMath.finalGPSPosDeg(currentPos.Item1.ToString(), currentPos.Item2.ToString(),
                                           brng, missingLength);
                        }
                        else
                        {
                            currentPos = sMath.finalGPSPosDeg(initialRoute.points[indexCounter - 1].Item1, initialRoute.points[indexCounter - 1].Item2,
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
                    LogTextWrite(currPosMsg);
                    indexCounter++;
                }

                Thread.Sleep(updateSpeed * 1000);
            }
            
        }
        
        private void SetInitialPos()
        {
            int lengthOfRoute = initialRoute.points.Count;
            int fourthLength = lengthOfRoute/4;
            initialPosIndex = R.Next(0, lengthOfRoute - fourthLength + 1 );
            string initialPosLat = initialRoute.points[initialPosIndex].Item1;
            string initialPosLon = initialRoute.points[initialPosIndex].Item2;
            
            string query = "Insert into GPSPosition (Longitude, Latitude, UpdateTime,fk_Bus)" +
                            " values (" + initialPosLon + ", " + initialPosLat + ", '" +
                            DateTime.Now.ToString("HH:mm:ss") + "', " + bID.ToString() + ")";

           

            string initPosMsg = "ID " + bID + " initialPos: (" + initialPosLat + ", " + initialPosLon + "), index being " + initialPosIndex.ToString();
            LogTextWrite(initPosMsg);
        }

        private void SetCurrentPos()
        {
            string query = "Insert into GPSPosition (Longitude, Latitude, UpdateTime,fk_Bus)" +
               " values (" + currentPos.Item2.ToString() + ", " + currentPos.Item1.ToString() + ", '" +
               DateTime.Now.ToString("HH:mm:ss") + "', " + bID.ToString() + ")";

            DatabaseAcces.InsertOrUpdate(query);
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
            UpdateBusDB();
        }

        private void UpdateBusDB()
        {
            string query = "Update Bus set fk_BusRoute = " +
                        initialRoute.id + ", IsDescending = " + initialRoute.isFlipped.ToString() +
                        " where Bus.ID = " + bID.ToString();
            DatabaseAcces.InsertOrUpdate(query);
        }

        private void LogTextWrite(string text)
        {
            log.sendLogMessage(text + "\n");
        }
        
    }
}
