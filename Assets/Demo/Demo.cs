using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using UnityEngine;

namespace wanderer.lan
{
    public class Demo : MonoBehaviour,IFrameSyncEvent
    {
        private Dictionary<int, IPEndPoint> _roomList = new Dictionary<int, IPEndPoint>();

        [SerializeField]
        private GameObject _userPrefab;

        private Dictionary<int, GameObject> _userInstance=new Dictionary<int, GameObject>();

        private float _speed = 5.0f;

        // Start is called before the first frame update
        void Start()
        {
            LANFrameSync.Run(this);
        }

        private void OnDestroy()
        {
            LANFrameSync.ShutDown();
        }

        // Update is called once per frame
        void Update()
        {
            if (LANFrameSync.Enable && LANFrameSync.Lan.InRoom)
            {
                if (Input.GetKey(KeyCode.W))
                {
                    byte[] buffer = BitConverter.GetBytes((int)KeyCode.W);
                    OnGameInput.Invoke(buffer);
                }
                if (Input.GetKey(KeyCode.A))
                {
                    byte[] buffer = BitConverter.GetBytes((int)KeyCode.A);
                    OnGameInput.Invoke(buffer);
                }
                if (Input.GetKey(KeyCode.S))
                {
                    byte[] buffer = BitConverter.GetBytes((int)KeyCode.S);
                    OnGameInput.Invoke(buffer);
                }
                if (Input.GetKey(KeyCode.D))
                {
                    byte[] buffer = BitConverter.GetBytes((int)KeyCode.D);
                    OnGameInput.Invoke(buffer);
                }
            }
        }

        void OnGUI()
        {
            if (LANFrameSync.Enable)
            {
                if (!LANFrameSync.Lan.InRoom)
                {
                    if (GUILayout.Button("Create Room"))
                    {
                        LANFrameSync.Lan.CreateRoom();
                    }
                    if (GUILayout.Button("Search Room"))
                    {
                        _roomList.Clear();
                    }
                    if (_roomList.Count > 0)
                    {
                        GUILayout.BeginVertical("box");
                        GUILayout.Label("[Room List]");
                        foreach (var item in _roomList)
                        {
                            GUILayout.BeginHorizontal("box");
                            GUILayout.Label(item.Key.ToString());
                            if (GUILayout.Button("Join"))
                            {
                                LANFrameSync.Lan.JoinRoom(item.Key,item.Value);
                            }
                            GUILayout.EndHorizontal();
                        }
                        GUILayout.EndVertical();
                    }
                }
                else
                {
                    GUILayout.Label($" [In Room: {LANFrameSync.Lan.RoomId}] ");
                    GUILayout.Label($" [Is Master: {LANFrameSync.Lan.RoomMaster}] ");
                    GUILayout.Label($" [Frame: {LANFrameSync.Lan.LogicFrame}] ");

                    if (GUILayout.Button("KeyCode.W"))
                    {
                        byte[] buffer = BitConverter.GetBytes((int)KeyCode.W);
                        OnGameInput.Invoke(buffer);
                    }
                    if (GUILayout.Button("KeyCode.A"))
                    {
                        byte[] buffer = BitConverter.GetBytes((int)KeyCode.A);
                        OnGameInput.Invoke(buffer);
                    }
                    if (GUILayout.Button("KeyCode.S"))
                    {
                        byte[] buffer = BitConverter.GetBytes((int)KeyCode.S);
                        OnGameInput.Invoke(buffer);
                    }
                    if (GUILayout.Button("KeyCode.D"))
                    {
                        byte[] buffer = BitConverter.GetBytes((int)KeyCode.D);
                        OnGameInput.Invoke(buffer);
                    }
                }
            }
            
        }

        #region IFrameSyncEvent
        public Action<byte[]> OnGameInput { get; set; }

        public void OnReceiveRoomInfo(int roomId, IPEndPoint remoteIPEndPoint)
        {
            _roomList.Add(roomId, remoteIPEndPoint);
        }

        public void OnUserInput(int logicFrame, int userId, byte[] data)
        {
            if (_userInstance.TryGetValue(userId, out GameObject @object))
            {
                KeyCode code = (KeyCode)(BitConverter.ToInt32(data,0));
                if (Input.GetKey(KeyCode.W))
                {
                    @object.transform.Translate(transform.forward * Time.deltaTime* _speed, Space.Self);

                }
                if (Input.GetKey(KeyCode.A))
                {
                    @object.transform.Translate(-transform.right * Time.deltaTime * _speed, Space.Self);
                }
                if (Input.GetKey(KeyCode.S))
                {
                    @object.transform.Translate(-transform.forward * Time.deltaTime * _speed, Space.Self);
                }
                if (Input.GetKey(KeyCode.D))
                {
                    @object.transform.Translate(transform.right * Time.deltaTime * _speed, Space.Self);
                }
            }
        }

        public void OnUserJoinedRoom(int roomId, int userId, IPEndPoint endPoint)
        {
            var clone = GameObject.Instantiate(_userPrefab);
            clone.transform.localScale = Vector3.one;
            clone.transform.position = Vector3.zero;
           var tm = clone.transform.Find("Name").GetComponent<TextMesh>();
            tm.text = userId.ToString();
            tm.color = new Color(UnityEngine.Random.Range(0.1f, 0.9f), UnityEngine.Random.Range(0.1f, 0.9f), UnityEngine.Random.Range(0.1f, 0.9f));

            _userInstance.Add(userId, clone);
        }

        public void OnUserLeavedRoom(int roomId, int userId, IPEndPoint endPoint)
        {
        }
        #endregion

      
    }
}