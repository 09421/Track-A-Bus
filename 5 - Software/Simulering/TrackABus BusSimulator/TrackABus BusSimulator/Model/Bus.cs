using System;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackABus_BusSimulator.Model
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
    public class Bus
    {

    }


   
}
