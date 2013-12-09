using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackABusSim.Classes
{
    class SimulationConfig
    {
        
        public SimulationConfig()
        {
        }

        public static List<BusRoute> AllRoutes
        {
            
            private set;
            get;
        }
        public static List<Bus> RunningBusses
        {
            private set;
            get;
        }

        public static Random rand = new Random();
        

        public void CreateAllBusRoutes()
        {
            AllRoutes = new List<BusRoute>();

            string query = "select distinct BusRoute.ID, BusRoute.RouteNumber from BusRoute" +
                           " inner join Bus on Bus.fk_BusRoute = BusRoute.ID " +
                            "inner join BusRoute_RoutePoint on BusRoute.ID = BusRoute_RoutePoint.fk_BusRoute";
            List<string> AllRoutesNonCreated = DatabaseAcces.Query(query, new List<string>() { "ID", "Routenumber" });
            for (int i = 0; i < AllRoutesNonCreated.Count; i = i + 2)
                AllRoutes.Add(new BusRoute(AllRoutesNonCreated[i], AllRoutesNonCreated[i + 1]));
        }

        public void CreateBusses(int SimuMode, int DirectionMode, string BusNumber, int UpdateSpeed, int minSpeed, int maxSpeed , bool ShouldRandomize)
        {
            bool desc = false;
            List<Bus> bs = new List<Bus>();
            List<BusRoute> SimuRoutes = AllRoutes.FindAll(R => R.number == BusNumber);
            List<BusRoute> routesToPass = new List<BusRoute>();

            DatabaseAcces.Truncate();
            
            string singleBusQuery = "Select ID from Bus where ";
            string allBusOnRouteQuery = "Select ID from Bus where ";
            string AllBusQuery = "select distinct Bus.ID, Bus.fk_BusRoute from Bus "
                                  + " inner join BusRoute on Bus.fk_BusRoute = BusRoute.ID"
                                  + " inner join BusRoute_RoutePoint on BusRoute_RoutePoint.fk_BusRoute = BusRoute.ID";
            foreach (BusRoute br in  AllRoutes.FindAll(R => R.number == BusNumber))
            {
                singleBusQuery += "fk_busRoute = " + br.id + " or ";
                allBusOnRouteQuery += "fk_busRoute = " + br.id + " or ";
            }

            singleBusQuery = singleBusQuery.TrimEnd(" or ".ToCharArray()) + " limit 1";
            allBusOnRouteQuery = allBusOnRouteQuery.TrimEnd(" or ".ToCharArray());
            switch (SimuMode)
            {
                case 0:
                    int id = int.Parse(DatabaseAcces.Query(singleBusQuery, new List<string>() { "ID" })[0].ToString());
                   
                    switch (DirectionMode)
                    {
                        case 0:
                            routesToPass = SimuRoutes.Clone<BusRoute>();
                            bs.Add(new Bus(id, routesToPass, UpdateSpeed, false,minSpeed, maxSpeed, ShouldRandomize));
                            break;
                        case 1:
                            routesToPass = SimuRoutes.Clone<BusRoute>();
                            bs.Add(new Bus(id, routesToPass, UpdateSpeed, true, minSpeed, maxSpeed, ShouldRandomize));
                            break;
                        default:
                            break;
                    }
                    break;
                case 1:
                    List<string> IDList = DatabaseAcces.Query(allBusOnRouteQuery, new List<string>() { "ID" });

                    switch (DirectionMode)
                    {

                        case 0:
                            for (int i = 0; i < IDList.Count; i++)
                            {
                                routesToPass = SimuRoutes.Clone();
                                bs.Add(new Bus(int.Parse(IDList[i]), routesToPass, UpdateSpeed, false,minSpeed, maxSpeed, ShouldRandomize));
                            }
                            break;
                        case 1:
                            for (int i = 0; i < IDList.Count; i++)
                            {
                                routesToPass = SimuRoutes.Clone();
                                bs.Add(new Bus(int.Parse(IDList[i]), routesToPass, UpdateSpeed, true, minSpeed, maxSpeed, ShouldRandomize));
                            }
                            break;
                        case 2:
                            for (int i = 0; i < IDList.Count; i++)
                            {
                                int test = rand.Next(1, 3);
                                if (test == 1)
                                    desc = true;
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
                case 2:
                    List<string> IDFKList = DatabaseAcces.Query(AllBusQuery, new List<string>() { "ID", "fk_BusRoute" });
                    switch (DirectionMode)
                    {
                        case 0:
                            for (int i = 0; i < IDFKList.Count; i = i + 2)
                            {
                                if (IDFKList[i + 1].ToString() == BusNumber)
                                    desc = false;
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

        public static List<string> GetBusRoutesInSystem()
        {

            string query = "select distinct BusRoute.RouteNumber from BusRoute " +
                           "inner join Bus on Bus.fk_BusRoute = BusRoute.ID " +
                           "inner join BusRoute_RoutePoint on BusRoute.ID = BusRoute_RoutePoint.fk_BusRoute";
            return DatabaseAcces.Query(query, new List<string>() { "RouteNumber" });
        }


    }
}
