using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace wanderer.lan
{
    public interface IFrameSyncEvent
    {
        public Action<byte[]> OnGameInput { get; set; }
        void OnUserLeavedRoom(int roomId,int userId, IPEndPoint endPoint);
        void OnUserJoinedRoom(int roomId,int userId,IPEndPoint endPoint);
        void OnReceiveRoomInfo(int roomId, IPEndPoint remoteIPEndPoint);
        void OnUserInput(int logicFrame,int userId, byte[] data);
    }
}
