using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackABusSim.Classes
{
    public class Logger
    {
        public event EventHandler<SendLogMessageEventArgs> LogUpdate;
        public void sendLogMessage(string msg)
        {
            var handler = LogUpdate;
            if (handler != null)
            {
                handler(this, new SendLogMessageEventArgs(msg));
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


}
