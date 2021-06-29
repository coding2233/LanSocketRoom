using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.IO;
using System.Threading.Tasks;

namespace wanderer.lan
{
    enum LanRoomKey : int
    {
        Create_Room = -100,
        Search_Room = -101,
        Join_Room_Request = -102,
        Room_Info=-103,
        Room_New_User=-104,
        Level_Room=-105,
        User_Level_Room=-106,
        Game_Input=-107,
        Game_Input_Logic=-108,
    }


    public class LanRoom:IDisposable
    {
        public bool InRoom { get; private set; }

        public bool RoomMaster { get; private set; }

        public int RoomId { get; private set; }

        public IPEndPoint Master { get; private set; }
        /// <summary>
        /// Master才有的数据
        /// </summary>
        public Dictionary<int, IPEndPoint> UserList { get; private set; } = new Dictionary<int, IPEndPoint>();

        private LanSocket _lanSocket;

        private LanStream _receiveStream;

        private LanStream _sendStream;

        private IFrameSyncEvent _frameSyncEvent;

        public int UserId { get; private set; }

        #region master
        public int LogicFrame { get; private set; }
        private bool _inGame;
        private readonly Dictionary<int, byte[]> _inputData = new Dictionary<int, byte[]>();
        #endregion

        public LanRoom(LanSocket lanSocket, IFrameSyncEvent frameSyncEvent)
        {
            _receiveStream = new LanStream();
            _sendStream = new LanStream();

            _frameSyncEvent = frameSyncEvent;
            _frameSyncEvent.OnGameInput += OnGameInput;

            _lanSocket = lanSocket;
            _lanSocket.OnMainReceive += OnReceive;

        }

        public void CreateRoom(int roomId=0)
        {
            if (InRoom)
            {
                return;
            }

            if (roomId == 0)
            {
                roomId = UnityEngine.Random.Range(1000, 9999);
            }
            RoomId = roomId;
            RoomMaster = true;
            InRoom = true;
            //Master
            Master = _lanSocket.SelfIPEndPoint;

            UserList.Clear();

            //Join room.
            UserId = Master.GetHashCode();
            OnMasterAccpetJoinedRoom(RoomId, Master);
            //_frameSyncEvent.OnJoinedRoom(RoomId, Master);
        }

        public void RunGame()
        {
            if (InRoom && RoomMaster)
            {
                new Task(MasterFrameLogic).Start();
            }
        }

        public void SearchRoom()
        {
            if (!InRoom)
            {
                _sendStream.Flush();
                _sendStream.Writer.Write((int)LanRoomKey.Search_Room);
                _sendStream.SetSeekOrigin(SeekOrigin.Begin);
                _lanSocket.Send(_sendStream.Reader.ReadBytes((int)_sendStream.Length));
            }
        }

        public void JoinRoom(int roomId,IPEndPoint remoteEndPoint)
        {
            if (!InRoom)
            {
                //RoomId = roomId;
                //RoomMaster = false;
                //InRoom = true;

                _sendStream.Flush();
                _sendStream.Writer.Write((int)LanRoomKey.Join_Room_Request);
                _sendStream.Writer.Write(roomId);
                _sendStream.SetSeekOrigin(SeekOrigin.Begin);
                _lanSocket.Send(_sendStream.Reader.ReadBytes((int)_sendStream.Length));
            }
        }

        public void LevelRoom()
        {
            if (RoomMaster)
            {
                _inGame = false;

                foreach (var item in UserList.Values)
                {
                    _sendStream.Flush();
                    _sendStream.Writer.Write((int)LanRoomKey.Level_Room);
                    _sendStream.Writer.Write(UserId);
                    _sendStream.SetSeekOrigin(SeekOrigin.Begin);
                    _lanSocket.Send(_sendStream.Reader.ReadBytes((int)_sendStream.Length), item);
                }
            }
            else
            {
                _sendStream.Flush();
                _sendStream.Writer.Write((int)LanRoomKey.Level_Room);
                _sendStream.Writer.Write(UserId);
                _sendStream.SetSeekOrigin(SeekOrigin.Begin);
                _lanSocket.Send(_sendStream.Reader.ReadBytes((int)_sendStream.Length), Master);
            }
            RoomMaster = false;
            Master = null;
            InRoom = false;
            UserList.Clear();
        }

        public void Dispose()
        {
            _receiveStream.Dispose();
            _sendStream.Dispose();
            _frameSyncEvent.OnGameInput -= OnGameInput;
            _lanSocket.OnMainReceive -= OnReceive;
            _lanSocket?.Dispose();

            _frameSyncEvent = null;
            _receiveStream = null;
            _sendStream = null;
            _lanSocket = null;
        }

        private void OnReceive(byte[] buffer, IPEndPoint remoteIPEndPoint)
        {
            _receiveStream.SetSeekOrigin(SeekOrigin.End);
            _receiveStream.Writer.Write(buffer,0, buffer.Length);
            if (_receiveStream.Length >= 4)
            {
                _receiveStream.SetSeekOrigin(SeekOrigin.Begin);
                LanRoomKey key = (LanRoomKey)_receiveStream.Reader.ReadInt32();
                switch (key)
                {
                    case LanRoomKey.Search_Room:
                        if (RoomMaster)
                        {
                            _sendStream.Flush();
                            _sendStream.Writer.Write((int)LanRoomKey.Room_Info);
                            _sendStream.Writer.Write(RoomId);
                            _sendStream.SetSeekOrigin(SeekOrigin.Begin);
                            _lanSocket.Send(_sendStream.Reader.ReadBytes((int)_sendStream.Length), remoteIPEndPoint);
                        }
                        break;
                    case LanRoomKey.Room_Info:
                        if (!InRoom)
                        {
                            int roomId = _receiveStream.Reader.ReadInt32();
                            _frameSyncEvent.OnReceiveRoomInfo(roomId, remoteIPEndPoint);
                        }
                        break;
                    case LanRoomKey.Join_Room_Request:
                        if (RoomMaster)
                        {
                            int roomId = _receiveStream.Reader.ReadInt32();
                            if (roomId == RoomId)
                            {
                                OnMasterAccpetJoinedRoom(roomId, remoteIPEndPoint);
                            }
                        }
                        break;
                    case LanRoomKey.Room_New_User:
                        {
                            int roomId = _receiveStream.Reader.ReadInt32();
                            int userId = _receiveStream.Reader.ReadInt32();
                            int port = _receiveStream.Reader.ReadInt32();
                            byte[] addressBuffer = _receiveStream.Reader.ReadBytes((int)_receiveStream.Length);
                            IPAddress address = new IPAddress(addressBuffer);
                            IPEndPoint newIPEndPoint = new IPEndPoint(address, port);
                            if (RoomMaster)
                            {

                            }
                            else
                            {
                                if (!InRoom)
                                {
                                    Master = remoteIPEndPoint;
                                    RoomMaster = false;
                                    InRoom = true;
                                    UserId = userId;
                                }
                            }
                            _frameSyncEvent.OnUserJoinedRoom(roomId, userId, newIPEndPoint);
                        }
                        break;
                    case LanRoomKey.Level_Room:
                        {
                            if (RoomMaster)
                            {
                                int userId = _receiveStream.Reader.ReadInt32();
                                UserList.Remove(userId);
                                foreach (var item in UserList.Values)
                                {
                                    _sendStream.Flush();
                                    _sendStream.Writer.Write((int)LanRoomKey.User_Level_Room);
                                    _sendStream.Writer.Write(RoomId);
                                    _sendStream.Writer.Write(userId);
                                    _sendStream.Writer.Write(remoteIPEndPoint.Port);
                                    _sendStream.Writer.Write(remoteIPEndPoint.Address.GetAddressBytes());
                                    _sendStream.SetSeekOrigin(SeekOrigin.Begin);
                                    _lanSocket.Send(_sendStream.Reader.ReadBytes((int)_sendStream.Length), item);
                                }
                            }
                            else 
                            {
                                Master = null;
                                UserList.Clear();
                                InRoom = false;
                            }
                        }
                        break;
                    case LanRoomKey.User_Level_Room:
                        {
                            int roomId = _receiveStream.Reader.ReadInt32();
                            int userId = _receiveStream.Reader.ReadInt32();
                            int port = _receiveStream.Reader.ReadInt32();
                            byte[] addressBuffer = _receiveStream.Reader.ReadBytes((int)_receiveStream.Length);
                            IPAddress address = new IPAddress(addressBuffer);
                            IPEndPoint delIPEndPoint = new IPEndPoint(address, port);
                            _frameSyncEvent.OnUserLeavedRoom(roomId, userId, delIPEndPoint);
                        }
                        break;
                    case LanRoomKey.Game_Input:
                        {
                            if (RoomMaster)
                            {
                                if (_inGame)
                                {
                                    int userId = _receiveStream.Reader.ReadInt32();
                                    byte[] inputBuffer = _receiveStream.Reader.ReadBytes((int)_receiveStream.Length);
                                    lock (_inputData)
                                    {
                                        _inputData[userId] = inputBuffer;
                                    }
                                }
                            }
                        }
                        break;
                    case LanRoomKey.Game_Input_Logic:
                        {
                            int logicFrame = _receiveStream.Reader.ReadInt32();
                            if (!RoomMaster)
                            {
                                LogicFrame = logicFrame;
                            }
                            while (_receiveStream.Length > 0)
                            {
                                int userId = _receiveStream.Reader.ReadInt32();
                                int size = _receiveStream.Reader.ReadInt32();
                                byte[] inputBuffer = _receiveStream.Reader.ReadBytes(size);
                                _frameSyncEvent.OnUserInput(logicFrame, userId, inputBuffer);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            _receiveStream.Flush();
        }


        private void OnMasterAccpetJoinedRoom(int roomId,IPEndPoint endPoint)
        {
            foreach (var item in UserList)
            {
                _sendStream.Flush();
                _sendStream.Writer.Write((int)LanRoomKey.Room_New_User);
                _sendStream.Writer.Write(RoomId);
                _sendStream.Writer.Write(item.Key);
                _sendStream.Writer.Write(item.Value.Port);
                _sendStream.Writer.Write(item.Value.Address.GetAddressBytes());
                _sendStream.SetSeekOrigin(SeekOrigin.Begin);
                _lanSocket.Send(_sendStream.Reader.ReadBytes((int)_sendStream.Length), endPoint);
            }

            int userId= endPoint.GetHashCode();
            UserList.Add(userId,endPoint);
            foreach (var item in UserList.Values)
            {
                _sendStream.Flush();
                _sendStream.Writer.Write((int)LanRoomKey.Room_New_User);
                _sendStream.Writer.Write(RoomId);
                _sendStream.Writer.Write(userId);
                _sendStream.Writer.Write(endPoint.Port);
                _sendStream.Writer.Write(endPoint.Address.GetAddressBytes());
                _sendStream.SetSeekOrigin(SeekOrigin.Begin);
                byte[] sendBuffer = _sendStream.Reader.ReadBytes((int)_sendStream.Length);
                _lanSocket.Send(sendBuffer, item);
            }
        }


        private void MasterFrameLogic()
        {
            _inGame = true;
            LogicFrame = 0;
            float lastTime = Time.realtimeSinceStartup;
            float logicTime = 1.0f / 15.0f;
            _inputData.Clear();
            while (InRoom&& RoomMaster)
            {
                lastTime = Time.realtimeSinceStartup - lastTime;
                if (lastTime >= logicTime)
                {
                    lock (_inputData)
                    {
                        _sendStream.Flush();
                        _sendStream.Writer.Write((int)LanRoomKey.Game_Input_Logic);
                        _sendStream.Writer.Write(LogicFrame);
                        foreach (var item in _inputData)
                        {
                            _sendStream.Writer.Write(item.Key);
                            _sendStream.Writer.Write(item.Value.Length);
                            _sendStream.Writer.Write(item.Value);
                        }
                        _sendStream.SetSeekOrigin(SeekOrigin.Begin);
                        byte[] sendBuffer = _sendStream.Reader.ReadBytes((int)_sendStream.Length);
                        foreach (var item in UserList.Values)
                        {
                            _lanSocket.Send(sendBuffer, item);
                        }

                        _inputData.Clear();
                    }

                    LogicFrame++;
                }
                lastTime = Time.realtimeSinceStartup;
            }
            _inGame = false;
        }


        private void OnGameInput(byte[] data)
        {
            if (InRoom&&Master!=null)
            {
                _sendStream.Flush();
                _sendStream.Writer.Write((int)LanRoomKey.Game_Input);
                _sendStream.Writer.Write(UserId);
                _sendStream.Writer.Write(data);
                _sendStream.SetSeekOrigin(SeekOrigin.Begin);
                _lanSocket.Send(_sendStream.Reader.ReadBytes((int)_sendStream.Length), Master);
            }
        }

    }
}