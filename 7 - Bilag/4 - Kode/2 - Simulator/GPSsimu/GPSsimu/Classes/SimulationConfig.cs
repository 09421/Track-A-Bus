using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackABusSim.Classes
{
    /// <summary>
    /// This class handles the create of new busses and routes. 
    /// </summary>
    class SimulationConfig
    {
        
        public SimulationConfig()
        {
        }

        /// <summary>
        /// All routes retrieved. Can only be set when class is instantiated but can be retrieved statically
        /// </summary>
        public static List<BusRoute> AllRoutes
        {
            
            private set;
            get;
        }

        /// <summary>
        /// All busses. Can only be set when class is instantiated but can be retrieved statically
        /// </summary>
        public static List<Bus> RunningBusses
        {
            private set;
            get;
        }

        //The Random object used throughout the system. One value means more spread between random values.
        public static Random rand = new Random();
        

        /// <summary>
        /// Retrieves all busroutes and their numbers, with a bus coupled to it, from the MySQL database, and creates the routes from the retrieved data.
        /// </summary>
        public void CreateAllBusRoutes()
        {
            //Reset allroutes.
            AllRoutes = new List<BusRoute>();

            //Retrieve all routes with a bus coupled to it. Raw query.
            string query = "select distinct BusRoute.ID, BusRoute.RouteNumber from BusRoute" +
                           " inner join Bus on Bus.fk_BusRoute = BusRoute.ID " +
                            "inner join BusRoute_RoutePoint on BusRoute.ID = BusRoute_RoutePoint.fk_BusRoute";
            //Acces data access layer to retrieve the routes.
            List<string> AllRoutesNonCreated = DatabaseAcces.Query(query, new List<string>() { "ID", "Routenumber" });
            //Create all the routes.
            for (int i = 0; i < AllRoutesNonCreated.Count; i = i + 2)
                AllRoutes.Add(new BusRoute(AllRoutesNonCreated[i], AllRoutesNonCreated[i + 1]));
        }

        /// <summary>
        /// Creates the busses based on route.
        /// 
        /// </summary>
        /// <param name="SimuMode">Simulation mode</param>
        /// <param name="DirectionMode">Direction mode of the busses</param>
        /// <param name="BusNumber">Route number</param>
        /// <param name="UpdateSpeed">Update speed in seconds</param>
        /// <param name="minSpeed">Initial minimum speed of the busses</param>
        /// <param name="maxSpeed">Initial maximum speed of the busses</param>
        /// <param name="ShouldRandomize">If true then randomizes the position of the busses on the route, otherwise position at the beginning </param>
        public void CreateBusses(int SimuMode, int DirectionMode, string BusNumber, int UpdateSpeed, int minSpeed, int maxSpeed , bool ShouldRandomize)
        {
            bool desc = false;
            List<Bus> bs = new List<Bus>();
            //List of routes with the given routenumber.
            List<BusRoute> SimuRoutes = AllRoutes.FindAll(R => R.number == BusNumber);
            List<BusRoute> routesToPass = new List<BusRoute>();

            //truncate all positions
            DatabaseAcces.Truncate();
            
            //Create the needed raw queries to pull the relevant busses from the database.
            string singleBusQuery = "Select ID from Bus where ";
            string allBusOnRouteQuery = "Select ID from Bus where ";
            string AllBusQuery = "select distinct Bus.ID, Bus.fk_BusRoute from Bus "
                                  + " inner join BusRoute on Bus.fk_BusRoute = BusRoute.ID"
                                  + " inner join BusRoute_RoutePoint on BusRoute_RoutePoint.fk_BusRoute = BusRoute.ID";

            //Limit busroute to pick a bus from, by whatever the routenumber is set to. Used in cased of complex routes.
            foreach (BusRoute br in  AllRoutes.FindAll(R => R.number == BusNumber))
            {
                singleBusQuery += "fk_busRoute = " + br.id + " or ";
                allBusOnRouteQuery += "fk_busRoute = " + br.id + " or ";
            }

            //Retrieve only one bus if this query is needed.
            singleBusQuery = singleBusQuery.TrimEnd(" or ".ToCharArray()) + " limit 1";
            allBusOnRouteQuery = allBusOnRouteQuery.TrimEnd(" or ".ToCharArray());
            switch (SimuMode)
            {
                //If only one bus should be used.
                case 0:
                    //get single bus ID
                    int id = int.Parse(DatabaseAcces.Query(singleBusQuery, new List<string>() { "ID" })[0].ToString());
                    //If direction is from first to last stop.
                    switch (DirectionMode)
                    {
                        //If direction is from first to last stop.
                        case 0:
                            //Clone routes and give them to the new bus.
                            routesToPass = SimuRoutes.Clone<BusRoute>();
                            //Create one bus and add it to the list. startDescending set to false 
                            bs.Add(new Bus(id, routesToPass, UpdateSpeed, false,minSpeed, maxSpeed, ShouldRandomize));
                            break;
                        //If direction is from  to last to first stop.
                        case 1:
                            routesToPass = SimuRoutes.Clone<BusRoute>();
                            //Create one bus and add it to the list. startDescending set to true 
                            bs.Add(new Bus(id, routesToPass, UpdateSpeed, true, minSpeed, maxSpeed, ShouldRandomize));
                            break;
                        default:
                            break;
                    }
                    break;
                //All busses on route.
                case 1:
                    //Get all busses driving on the chosen route.
                    List<string> IDList = DatabaseAcces.Query(allBusOnRouteQuery, new List<string>() { "ID" });

                    switch (DirectionMode)
                    {
                        //All busses are driving from first to last stop.
                        case 0:
                            //Create all busses and set startDescending to false
                            for (int i = 0; i < IDList.Count; i++)
                            {
                                routesToPass = SimuRoutes.Clone();
                                bs.Add(new Bus(int.Parse(IDList[i]), routesToPass, UpdateSpeed, false,minSpeed, maxSpeed, ShouldRandomize));
                            }
                            break;
                        //All busses are driving from last to first stop.
                        case 1:
                            //Create all busses and set startDescending to ¨true
                            for (int i = 0; i < IDList.Count; i++)
                            {
                                routesToPass = SimuRoutes.Clone();
                                bs.Add(new Bus(int.Parse(IDList[i]), routesToPass, UpdateSpeed, true, minSpeed, maxSpeed, ShouldRandomize));
                            }
                            break;
                        //Randomize direction of the busses.
                        case 2:
                            //Create all busses on route.
                            for (int i = 0; i < IDList.Count; i++)
                            {
                                //Random value. Can either be 1 or 2.
                                int test = rand.Next(1, 3);
                                //If 1 set startDescending to true.
                                if (test == 1)
                                    desc = true;
                                //If 2 set startDescending to true.
                                else
                                    desc = false;
                                routesToPass = SimuRoutes.Clone();
                                bs.Add(new Bus(int.Parse(IDList[i]), routesToPass, UpdateSpeed, desc, minSpeed, maxSpeed, ShouldRandomize));
                            }

                            break;
                        default:
                            break;
                    }
                    break;
                //Get all busses in system
                case 2:
                    List<string> IDFKList = DatabaseAcces.Query(AllBusQuery, new List<string>() { "ID", "fk_BusRoute" });
                   
                    switch (DirectionMode)
                    {
                        case 0:
                            //All busses on routes driving from first to last stop.
                            for (int i = 0; i < IDFKList.Count; i = i + 2)
                            {
                                //if the current bus, is the simulated route, set startDescending to false.
                                if (IDFKList[i + 1].ToString() == BusNumber)
                                    desc = false;
                                //Otherwise randomize direction
                                else
                                {
                                    if (rand.Next(1, 3) == 1)
                                        desc = true;
                                    else
                                        desc = false;
                                }
                                //Find all routes that is coupled to the routenumber of the current bus. Used with complex routes.
                                List<BusRoute> l = AllRoutes.FindAll(R => R.id == IDFKList[i + 1].ToString());
                                routesToPass = l.Clone<BusRoute>();
                                bs.Add(new Bus(int.Parse(IDFKList[i]), routesToPass, UpdateSpeed, desc, minSpeed, maxSpeed, ShouldRandomize));
                            }
                            break;
                        //All busses on routes driving from last to first stop.
                        case 1:
                            for (int i = 0; i < IDFKList.Count; i = i + 2)
                            {
                                if (IDFKList[i + 1].ToString() == BusNumber)
                                    desc = true;
                                else
                                {
                                    if (rand.Next(1, 3) == 1)
                                        desc = true;
                                    else
                                        desc = false;
                                }
                                List<BusRoute> l = AllRoutes.FindAll(R => R.id == IDFKList[i + 1].ToString());
                                routesToPass = l.Clone<BusRoute>();
                                bs.Add(new Bus(int.Parse(IDFKList[i]), routesToPass, UpdateSpeed, desc, minSpeed, maxSpeed, ShouldRandomize));
                            }

                            break;
                        //Ranomize direction 
                        case 2:
                            for (int i = 0; i < IDFKList.Count; i = i + 2)
                            {
                                if (rand.Next(1, 3) == 1)
                                    desc = true;
                                else
                                    desc = false;
                                List<BusRoute> l = AllRoutes.FindAll(R => R.id == IDFKList[i + 1].ToString());
                                routesToPass = l.Clone<BusRoute>();
                                bs.Add(new Bus(int.Parse(IDFKList[i]), routesToPass, UpdateSpeed, desc, minSpeed, maxSpeed, ShouldRandomize));
                            }
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
            
            RunningBusses = bs;
        }

        /// <summary>
        /// Returns all routenumber, that has a bus coupled to it.
        /// </summary>
        /// <returns></returns>
        public static List<string> GetBusRoutesInSystem()
        {

            string query = "select distinct BusRoute.RouteNumber from BusRoute " +
                           "inner join Bus on Bus.fk_BusRoute = BusRoute.ID " +
                           "inner join BusRoute_RoutePoint on BusRoute.ID = BusRoute_RoutePoint.fk_BusRoute";
            return DatabaseAcces.Query(query, new List<string>() { "RouteNumber" });
        }


    }
}
