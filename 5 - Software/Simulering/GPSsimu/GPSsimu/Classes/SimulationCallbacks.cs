using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackABusSim.Classes
{
    public class SimulationCallbacks
    {
        public event EventHandler<SendLogMessageEventArgs> LogUpdate;
        public event EventHandler<StartingSimEventArgs> StartingSim;
        public void sendLogMessage(string msg)
        {

            var LogHandler = LogUpdate;
            if (LogHandler != null)
            {
                LogHandler(this, new SendLogMessageEventArgs(msg));
            }

        }

        public void StartSimMessage(int value)
        {
            var SimHandler = StartingSim;
            if (SimHandler != null)
            {
                SimHandler(this, new StartingSimEventArgs(value));
            }
        }
    }

    public class SendLogMessageEventArgs : EventArgs
    {
        public string msgData { get; private set; }
        public SendLogMessageEventArgs(string data)
        {
            msgData = data;
        }
    }

    public class StartingSimEventArgs : EventArgs
    {
        public int value { get; private set; }
        public StartingSimEventArgs(int val)
        {
            value = val;
        }


    }


}
