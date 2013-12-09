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

        public SimulationRunner()
        {
            simuConfig = new SimulationConfig();
            cb = new SimulationCallbacks();
        }

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

        public void StartStop(bool shouldRandom)
        {
            if (!isRunning)
            {
                string Check = CheckVal();
                if (!Check.Equals("ok"))
                {
                    cb.sendLogMessage(Check);
                    cb.StartSimMessage(-1);
                }
                else
                {
                    cb.sendLogMessage("Configuration OK\n");
                }
                isRunning = true;
                
                simuConfig.CreateAllBusRoutes();
                simuConfig.CreateBusses(simu, direction, bNumber, uSpeedParsed, minS, maxS, shouldRandom);
                if (shouldRandom)
                    cb.StartSimMessage(1);
                else
                    cb.StartSimMessage(3);
                foreach (Bus b in SimulationConfig.RunningBusses)
                {
                    b.startSim();
                }
            }
            else
            {
                isRunning = false;
                DatabaseAcces.SelectWait = false;
                DatabaseAcces.InsertWait = false;
                if (shouldRandom)
                    cb.StartSimMessage(0);
                else
                    cb.StartSimMessage(2);
                foreach (Bus b in SimulationConfig.RunningBusses)
                {
                    b.stopSim();
                }
            }
        }

        private string CheckVal()
        {

            if (bNumber == null)
            {
                return "*ERROR* - Busroute not chosen, or no busroutes in system\n";
            }

            if (simu == -1)
            {
                return "*ERROR* - No simulation mode chosen\n";
            }

            if (direction == -1)
            {
                return "*ERROR* - No direction chosen\n";
            }

            if (!int.TryParse(uSpeed, out uSpeedParsed))
            {
                return "*ERROR* - Please only use whole numbers as update speed\n";
            }
            if (uSpeedParsed <= 0)
            {
                return "*ERROR* - Update speed cannot be lower or equal to zero\n";
            }
            return "ok";
        }






        public static void MinValueChanged(int value)
        {
            if (SimulationConfig.RunningBusses == null)
                return;
            foreach (Bus b in SimulationConfig.RunningBusses)
            {
                b.minSpeed = value;
            }
        }

        public static void MaxValueChanged(int value)
        {
            if (SimulationConfig.RunningBusses == null)
                return;
            foreach (Bus b in SimulationConfig.RunningBusses)
            {
                b.maxSpeed = value;
            }
        }

        public void windowClosingForceStop()
        {
            foreach (Bus b in SimulationConfig.RunningBusses)
            {
                b.stopSim();
            }
        }

    }
}
