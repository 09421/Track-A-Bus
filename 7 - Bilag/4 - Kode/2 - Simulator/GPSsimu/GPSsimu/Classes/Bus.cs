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
    /// <summary>
    /// Contains all information regarding the bus. Also handles the driving simulation.
    /// </summary>
    class Bus
    {
        private bool shouldRandomize;
        private int bID;
        private int updateSpeed;
        private int initialPosIndex;
        private List<BusRoute> routes;
        private int indexCounter = -1;

        public int maxSpeed = 50;
        public int minSpeed = 30;
        public SimulationCallbacks log = new SimulationCallbacks();

 
        private BusRoute initialRoute;
        private Tuple<double, double> currentPos = new Tuple<double, double>(0, 0);
        private Thread gpsPosCalcThread;
        private SimulationMath sMath = new SimulationMath();

        /// <summary>
        /// Constructor for the bus. 
        /// </summary>
        /// <param name="busID">ID of the bus</param>
        /// <param name="Routes">All the routes the bus can drive on. Is multiple routes when complex</param>
        /// <param name="uSpeed">Update speed</param>
        /// <param name="startDecending">Direction of the route. True = Last to first stop.</param>
        /// <param name="startingMinSpeed">Initial minimum speed of the bus</param>
        /// <param name="startingMaxSpeed">Initial maximum speed of the bus</param>
        /// <param name="randomize">True if the bus should have a random position on the route initially.</param>
        public Bus(int busID, List<BusRoute> Routes, int uSpeed, bool startDecending, int startingMinSpeed, int startingMaxSpeed, bool randomize)
        {
            
            routes = Routes;
            bID = busID;
            updateSpeed = uSpeed;
            //If complex, pick a route on random.
            initialRoute = Routes[SimulationConfig.rand.Next(0, routes.Count)];
            shouldRandomize = randomize;
            //If the bus should travel from first to last stop initially, flip the route.
            if (startDecending)
            {
                initialRoute.TurnAround();
            }
            //Update the current route and direction of the bus on the MySQL database.
            UpdateBusDB();

            maxSpeed = startingMaxSpeed;
            minSpeed = startingMinSpeed;
            
            //Set floating point for the bus thread to be "." instead of ",".
            gpsPosCalcThread = new Thread(new ThreadStart(gpsCalc));
            System.Globalization.CultureInfo customCulture = (System.Globalization.CultureInfo)gpsPosCalcThread.CurrentCulture.Clone();
            customCulture.NumberFormat.NumberDecimalSeparator = ".";
            gpsPosCalcThread.CurrentCulture = customCulture;
            //Set the intial position of the bus on the route.
            SetInitialPos();
        }

        /// <summary>
        /// Starts the thread, and hereby the simulation of this bus.
        /// </summary>
        public void startSim()
        {
            gpsPosCalcThread.Start();
        }


        /// <summary>
        /// Stops the thread, and hereby the simulation of this bus
        /// </summary>
        public void stopSim()
        {
            gpsPosCalcThread.Abort();
            //Wait until the thread is completly dead.
            while (gpsPosCalcThread.IsAlive)
                Thread.Sleep(10);
        }

        /// <summary>
        /// Main part of the simulation. Calculates the new position of the bus. Also the function set in the thread.
        /// </summary>
        private void gpsCalc()
        {
            
            while (true)
            {
                //Pick a speed between the minimum and the maximum speed. Maximum speed is +1 since both values can't be equal.
                double nextSpeed =SimulationConfig.rand.Next(minSpeed, maxSpeed+1) ;
                //Calculates the distance the bus should travel. 
                double travelLengthMeters = nextSpeed * (1000d / 3600d) * updateSpeed;
                //Reset all variables.
                double currentLength = 0;
                double nextLength = 0;
                double brng;
                //If not yet set, set indexCounter to the initial position +1, since this is the next point the bus, should reach.
                if(indexCounter == -1)
                    indexCounter = initialPosIndex + 1;

                //As long as the current length calculated is less than the distance the bus should travel.
                while (currentLength < travelLengthMeters)
                {
                    //If the bus has reached the last point on the route, set the bus to this position,
                    //Update the route the bus is driving on (if complex route), aswell as the direction.
                    if(indexCounter == initialRoute.points.Count - 1)
                    {
                        currentPos = new Tuple<double,double>(double.Parse(initialRoute.points[indexCounter].Item1),double.Parse(initialRoute.points[indexCounter].Item2));
                        UpdateBus();
                        break;
                    }

                    //If this is not the first calculation and this is the first calculation for this update
                    //Calculate distance and bearing with the two points being the bus and the next point the bus reaches.
                    if (currentPos.Item1 != 0 && currentPos.Item2 != 0 && nextLength == 0)
                    {
                        nextLength = sMath.Haversine(currentPos.Item1.ToString(), currentPos.Item2.ToString(),
                                                   initialRoute.points[indexCounter].Item1, initialRoute.points[indexCounter].Item2);

                        brng = sMath.BearingDegs(currentPos.Item1.ToString(), currentPos.Item2.ToString(),
                                            initialRoute.points[indexCounter].Item1, initialRoute.points[indexCounter].Item2);
                    }
                    //Otherwise calculate distance and bearing from this point and the next.
                    else
                    {
                        nextLength = sMath.Haversine(initialRoute.points[indexCounter - 1].Item1, initialRoute.points[indexCounter - 1].Item2,
                                                initialRoute.points[indexCounter].Item1, initialRoute.points[indexCounter].Item2);
                        brng = sMath.BearingDegs(initialRoute.points[indexCounter - 1].Item1, initialRoute.points[indexCounter - 1].Item2,
                                            initialRoute.points[indexCounter].Item1, initialRoute.points[indexCounter].Item2);

                    }
                  
                    //If the current calculated distance + the total calculated distance, is greater than the distance the bus should drive
                    if (nextLength + currentLength > travelLengthMeters)
                    {
                        //The distance the bus should drive from the current point. Distance the bus should drive - the distance that has been calculated.
                        double missingLength = travelLengthMeters - currentLength;

                        //If this is not the first calculation and this is the first calculation for this update
                        //Calculate new point, with the current position of the bus as the point of origin.
                        if (currentPos.Item1 != 0 && currentPos.Item2 != 0 && currentLength == 0)
                        {
                            currentPos = sMath.finalGPSPosDeg(currentPos.Item1.ToString(), currentPos.Item2.ToString(),
                                           brng, missingLength);
                        }
                        //Otherwise calculate new point, wit hthe last previous point being the point of origin.
                        else
                        {
                            Console.WriteLine("This is from other point");
                            currentPos = sMath.finalGPSPosDeg(initialRoute.points[indexCounter - 1].Item1, initialRoute.points[indexCounter - 1].Item2,
                                                                   brng, missingLength);
                        }
                        //Set the new position and break out of the calculations
                        SetCurrentPos();

                        break ;
                    }
                    //Otherwise increment total distance with the newly calculated distance.
                    else
                    {
                        currentLength += nextLength;
                    }
                    //Send a log message that a new routepoint has been reached, and increment indexCounter.
                    string currPosMsg = "Bus " + bID.ToString() + ", new endpoint reached, index: " + (indexCounter + 1).ToString();
                    LogTextWrite(currPosMsg);
                    indexCounter++;
                }
                //Sleep for the amount of seconds that has been defined as update speed.
                Thread.Sleep(updateSpeed * 1000);
            }
            
        }
        
        /// <summary>
        /// sets the first position of the bus on the route.
        /// </summary>
        private void SetInitialPos()
        {
 
            string initialPosLat = "";
            string initialPosLon = "";
            string query;

            //If first position should be random.
            if (shouldRandomize)
            {
                //Pick a point from the last first 3/4 parts of the route, so it doesnt turn around right away.
                int lengthOfRoute = initialRoute.points.Count;
                int fourthLength = lengthOfRoute / 4;
                initialPosIndex = SimulationConfig.rand.Next(0, lengthOfRoute - fourthLength + 1);
                initialPosLat = initialRoute.points[initialPosIndex].Item1;
                initialPosLon = initialRoute.points[initialPosIndex].Item2;

                query = "Insert into GPSPosition (Longitude, Latitude, UpdateTime,fk_Bus)" +
                                " values (" + initialPosLon + ", " + initialPosLat + ", '" +
                                DateTime.Now.ToString("HH:mm:ss") + "', " + bID.ToString() + ")";
            }
            //Otherwise pick the first point on the route.
            else
            {
                initialPosIndex = 0;
                initialPosLat = initialRoute.points[0].Item1;
                initialPosLat = initialRoute.points[0].Item2;
                query = "Insert into GPSPosition (Longitude, Latitude, UpdateTime,fk_Bus)" +
                " values (" + initialPosLon + ", " + initialPosLat + ", '" +
                DateTime.Now.ToString("HH:mm:ss") + "', " + bID.ToString() + ")";
            }
            //Update the position of the bus on the database, and write a message to the log window.
            DatabaseAcces.InsertOrUpdate(query);
            string initPosMsg = "ID " + bID + " initialPos: (" + initialPosLat + ", " + initialPosLon + "), index being " + initialPosIndex.ToString();
            LogTextWrite(initPosMsg);
        }

        /// <summary>
        /// Sets the current position of the bus on the database.
        /// </summary>
        private void SetCurrentPos()
        {
            string query = "Insert into GPSPosition (Longitude, Latitude, UpdateTime,fk_Bus)" +
               " values (" + currentPos.Item2.ToString() + ", " + currentPos.Item1.ToString() + ", '" +
               DateTime.Now.ToString("HH:mm:ss") + "', " + bID.ToString() + ")";

            DatabaseAcces.InsertOrUpdate(query);
        }

        /// <summary>
        /// Update the route and direction the bus is driving here.
        /// </summary>
        private void UpdateBus()
        {
            //If simple route, simply flip the route, and reset the counter.
            if (routes.Count == 1)
            {
                initialRoute.TurnAround();
                indexCounter = 0;
            }
            //Otherwise pick a route on random from the list, where it cant be this route, unless its the only one possible.
            else
            {
                List<BusRoute> possibleRoutes;
                //The name of the stop, the bus is at at the moment.
                string atStop = initialRoute.stops[initialRoute.stops.Count - 1];
                //Find all possible routes where this stop is either of final stations.
                possibleRoutes = routes.FindAll(R => (R.stops[R.stops.Count - 1] == atStop) || R.stops[0] == atStop);

                //If only one remains it can only be the current route, and so reverse it
                if (possibleRoutes.Count == 1)
                {
                    initialRoute.TurnAround();
                }

                else
                {
                    //Otherwise remove the old route, and pick a new one.
                    possibleRoutes.Remove(initialRoute);
                    //If only one remains pick this.
                    if (possibleRoutes.Count == 1)
                        initialRoute = possibleRoutes[0];
                    //Otherwise pick one at random.
                    else
                        initialRoute = possibleRoutes[SimulationConfig.rand.Next(0, possibleRoutes.Count)];
                    //If the first stop on the route is not the same as the stop the bus is at, flip the route.
                    if (initialRoute.stops[0] != atStop)
                    {
                        initialRoute.TurnAround();
                    }   

                }
                indexCounter = 0;   
               
            }
            //Update the configurations of the bus on the database.
            UpdateBusDB();
        }

        /// <summary>
        /// Update the bus on the database.
        /// </summary>
        private void UpdateBusDB()
        {
            string query = "Update Bus set fk_BusRoute = " +
                        initialRoute.id + ", IsDescending = " + initialRoute.isFlipped.ToString() +
                        " where Bus.ID = " + bID.ToString();
            DatabaseAcces.InsertOrUpdate(query);
        }

        /// <summary>
        /// Send a log message to the view.
        /// </summary>
        /// <param name="text">The message to send</param>
        private void LogTextWrite(string text)
        {
            log.sendLogMessage(text + "\n");
        }
        
    }
}
