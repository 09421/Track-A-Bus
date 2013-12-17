using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackABusSim.Classes
{
    /// <summary>
    /// Handles events raised from logic layer.
    /// </summary>
    public class SimulationCallbacks
    {
        public event EventHandler<SendLogMessageEventArgs> LogUpdate;
        public event EventHandler<StartingSimEventArgs> StartingSim;
        /// <summary>
        /// Raises an event with the purpose of logging a message.
        /// </summary>
        /// <param name="msg">The message to log</param>
        public void sendLogMessage(string msg)
        {

            var LogHandler = LogUpdate;
            //If handler is bound.
            if (LogHandler != null)
            {
                LogHandler(this, new SendLogMessageEventArgs(msg));
            }

        }
        /// <summary>
        /// Raises an event with the purpose of setting the state of the simulator on the view.
        /// </summary>
        /// <param name="value">The state of the view</param>
        public void StartSimMessage(int value)
        {
            var SimHandler = StartingSim;
            //If handler is bound.
            if (SimHandler != null)
            {
                SimHandler(this, new StartingSimEventArgs(value));
            }
        }
    }

    /// <summary>
    /// Event argument for logging data
    /// </summary>
    public class SendLogMessageEventArgs : EventArgs
    {
        public string msgData { get; private set; }
        public SendLogMessageEventArgs(string data)
        {
            msgData = data;
        }
    }

    /// <summary>
    /// Event argument for simulator state change
    /// </summary>
    public class StartingSimEventArgs : EventArgs
    {
        public int value { get; private set; }
        public StartingSimEventArgs(int val)
        {
            value = val;
        }


    }


}
