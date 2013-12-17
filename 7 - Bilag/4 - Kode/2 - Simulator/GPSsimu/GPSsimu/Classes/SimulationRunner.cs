using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackABusSim.Classes
{
    class SimulationRunner
    {
        private bool isRunning = false;
        private string bNumber;
        private string uSpeed;
        private int uSpeedParsed;
        private int simu;
        private int direction;
        private int minS;
        private int maxS;
        private SimulationConfig simuConfig;
        public SimulationCallbacks cb;

        /// <summary>
        /// Constructor for SimulationRunner.
        /// </summary>
        public SimulationRunner()
        {
            simuConfig = new SimulationConfig();
            cb = new SimulationCallbacks();
        }

        /// <summary>
        /// Sets the values gotten from the view.
        /// </summary>
        /// <param name="BusNumber">The chosen route</param>
        /// <param name="UpdateSpeed">The chosen update speed</param>
        /// <param name="SimuMode">the chosen simulation mode</param>
        /// <param name="DirectionMode">The chosen direction mode</param>
        /// <param name="MinSpeed">The initial minimum speed of the busses</param>
        /// <param name="MaxSpeed">The initial maximum speed of the busses</param>
        public void SetVal(object BusNumber, string UpdateSpeed, int SimuMode, int DirectionMode, int MinSpeed, int MaxSpeed)
        {
 

            if (BusNumber != null)
                bNumber = BusNumber.ToString();
            else
                bNumber = null;
            uSpeed = UpdateSpeed;
            simu = SimuMode;
            direction = DirectionMode;
            minS = MinSpeed;
            maxS = MaxSpeed;
        }

        /// <summary>
        /// Starts or stops the simulator based on what state the simulator is in.
        /// </summary>
        /// <param name="shouldRandom">True if start random button is clicked, false if start first button is clicked</param>
        public void StartStop(bool shouldRandom)
        {
            //Starting the simulator if not running.
            if (!isRunning)
            {
                //Checks to see if the configuration of the simulator is okay.
                string Check = CheckVal();
                //If it is not okay then send log message detailing what is going on.
                if (!Check.Equals("ok"))
                {
                    cb.sendLogMessage(Check);
                    cb.StartSimMessage(-1);
                    return;
                }
                //Otherwise send an "okay" log message.
                else
                {
                    cb.sendLogMessage("Configuration OK\n");
                }
                isRunning = true;
                //Creates all routes.
                simuConfig.CreateAllBusRoutes();
                //Creates all busses.
                simuConfig.CreateBusses(simu, direction, bNumber, uSpeedParsed, minS, maxS, shouldRandom);
                //Set state of simulator based on if position of busses should be randomized or not.
                if (shouldRandom)
                    cb.StartSimMessage(1);
                else
                    cb.StartSimMessage(3);
                //Start the simulation of all busses created.
                foreach (Bus b in SimulationConfig.RunningBusses)
                {
                    b.startSim();
                }
            }
            //If simulator allready running.
            else
            {
                isRunning = false;
                //Release semaphores.
                DatabaseAcces.SelectWait = false;
                DatabaseAcces.InsertWait = false;
                //Set state of simulator based on if position of busses should be randomized or not.
                if (shouldRandom)
                    cb.StartSimMessage(0);
                else
                    cb.StartSimMessage(2);
                //Stop simulation of all running busses.
                foreach (Bus b in SimulationConfig.RunningBusses)
                {
                    b.stopSim();
                }
            }
        }

        /// <summary>
        /// Checks to see, if the configurations is okay.
        /// </summary>
        /// <returns>Value of the error message, or "ok" if configuration passed.</returns>
        private string CheckVal()
        {
            //Checks to see if routenumber hasnt been set.
            if (bNumber == null)
            {
                return "*ERROR* - Busroute not chosen, or no busroutes in system\n";
            }
            //Checks to see if simulation mode hasnt been set.
            if (simu == -1)
            {
                return "*ERROR* - No simulation mode chosen\n";
            }
            //Checks to see if direction mode hasnt been set.
            if (direction == -1)
            {
                return "*ERROR* - No direction chosen\n";
            }
            //Checks to see if the update speed has been set, and if the value is in the correct format.
            if (!int.TryParse(uSpeed, out uSpeedParsed))
            {
                return "*ERROR* - Please only use whole numbers as update speed\n";
            }
            //checks to see if the update speed is less than zero.
            if (uSpeedParsed <= 0)
            {
                return "*ERROR* - Update speed cannot be lower or equal to zero\n";
            }
            return "ok";
        }

        /// <summary>
        /// Changes the minimum speed of each bus.
        /// </summary>
        /// <param name="value">The new minimum speed</param>
        public static void MinValueChanged(int value)
        {
            if (SimulationConfig.RunningBusses == null)
                return;
            foreach (Bus b in SimulationConfig.RunningBusses)
            {
                b.minSpeed = value;
            }
        }

        /// <summary>
        /// Changes the maximum speed of each bus.
        /// </summary>
        /// <param name="value">The new maximum speed</param>
        public static void MaxValueChanged(int value)
        {
            if (SimulationConfig.RunningBusses == null)
                return;
            foreach (Bus b in SimulationConfig.RunningBusses)
            {
                b.maxSpeed = value;
            }
        }
        
        /// <summary>
        /// Forces each bus to stop. Only used when simulator is closing, and each thread should be stopped.
        /// </summary>
        public void windowClosingForceStop()
        {
            foreach (Bus b in SimulationConfig.RunningBusses)
            {
                b.stopSim();
            }
        }

    }
}
