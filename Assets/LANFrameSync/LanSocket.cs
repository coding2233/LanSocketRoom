using System.Collections;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Net;

namespace wanderer.lan
{
    public class LanSocket:IDisposable
    {
        private int _port;
        private SynchronizationContext _mainSynchronizationContext;
        private UdpClient _udp;
        public Action<byte[], IPEndPoint> OnMainReceive;

        private IPEndPoint _broadcastIPEndPoint;

        public IPEndPoint SelfIPEndPoint
        {
            get
            {
                //return new IPEndPoint(IPAddress.Any, _port);
                return _udp.Client.LocalEndPoint as IPEndPoint;
            }
        }

        public LanSocket(int port)
        {
            _mainSynchronizationContext = SynchronizationContext.Current;
            if (_mainSynchronizationContext == null)
            {
                _mainSynchronizationContext = new SynchronizationContext();
            }

            try
            {
                _port = port;
                _udp = new UdpClient();
                _udp.EnableBroadcast = true;
                _udp.Client.Bind(new IPEndPoint(IPAddress.Parse("192.168.0.112"), port));
                _broadcastIPEndPoint = new IPEndPoint(IPAddress.Broadcast, port);

                //Task for receive.
                new Task(Receive).Start();
            }
            catch (Exception e)
            {
                Debug.LogWarning($"LanSocket udp create exception: {e.ToString()}");
            }
        }

        public void Dispose()
        {
            _udp.Close();
            _udp = null;
        }

        public void Send(byte[] buffer,IPEndPoint remote= null)
        {

            if (buffer == null || buffer.Length <= 0)
            {
                Debug.LogWarning("Sending data is null.");
                return;
            }
            SocketFlags flag = SocketFlags.None;
            if (remote == null)
            {
                remote = _broadcastIPEndPoint;
                flag = SocketFlags.Broadcast;
            }
            
            _udp.Send(buffer, buffer.Length, remote);
            //_udp.Client.SendTo(buffer, buffer.Length, flag, remote);
        }


        private void Receive()
        {
            IPEndPoint remoteEP=null;
            while (_udp != null)
            {
                var buffer =_udp.Receive(ref remoteEP);
                _mainSynchronizationContext.Post((obj)=> {
                    if (buffer != null&& buffer.Length>0)
                    {
                        OnMainReceive?.Invoke(buffer, remoteEP);
                    }
                }, buffer);
            }
        }

    }

 
}