using System.Collections;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using System.Net;
using System.Net.Sockets;

namespace wanderer.lan
{
    public class LANFrameSync 
    {
        public static bool Enable
        {
            get
            {
                return Lan != null;
            }
        }

        public static LanRoom Lan { get; private set; }

        public static void Run(IFrameSyncEvent frameSyncEvent, int port=1099)
        {
            var lanSocket = new LanSocket(port);
            Lan = new LanRoom(lanSocket,frameSyncEvent);
        }

        public static void ShutDown()
        {
            Lan.Dispose();
            Lan = null;
        }
    }
}