using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Numerics;

namespace LaserFocusServer
{
    class Client
    {
        public static int dataBufferSize = 4096;

        public int id;
        public Player player;
        public TCP tcp;
        public UDP udp;
        public Client(int _clientID)
        {
            id = _clientID;
            tcp = new TCP(id);
            udp = new UDP(id);
        }


        public class TCP
        {
            public TcpClient socket;

            private readonly int id;
            private NetworkStream stream;
            private Packet receiveData;
            private byte[] receiveBuffer;

            public TCP(int _id)
            {
                id = _id;
            }
            
            public void Connect(TcpClient _socket)
            {
                socket = _socket;
                socket.ReceiveBufferSize = dataBufferSize;
                socket.SendBufferSize = dataBufferSize;

                stream = socket.GetStream();

                receiveData = new Packet();
                receiveBuffer = new byte[dataBufferSize];

                stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                ServerSend.Welcome(id, "Welcome to Laser Focus");
                Console.WriteLine("Connected sucessfully");
            }

            public void SendData(Packet _packet)
            {
                try
                {
                    if (socket != null)
                    {
                        stream.BeginWrite(_packet.ToArray(), 0, _packet.Length(), null, null);
                    }
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error sending data to player {id} via TCP: {_ex}");
                    throw;
                }
            }

            private void ReceiveCallback(IAsyncResult _result)
            {
                try
                {
                    int _byteLenght = stream.EndRead(_result);
                    if (_byteLenght <= 0)
                    {
                        Server.clients[id].Disconnect();

                        return;
                    }

                    byte[] _data = new byte[_byteLenght];
                    Array.Copy(receiveBuffer, _data, _byteLenght);

                    receiveData.Reset(HandleData(_data));
                    stream.BeginRead(receiveBuffer, 0, dataBufferSize, ReceiveCallback, null);
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Error receiving TCP data: {_ex}");
                    Server.clients[id].Disconnect();
                }
            }

            private bool HandleData(byte[] _data)
            {
                int _packetLenght = 0;

                receiveData.SetBytes(_data);

                if (receiveData.UnreadLength() >= 4)
                {
                    _packetLenght = receiveData.ReadInt();
                    if (_packetLenght <= 0)
                    {
                        return true;
                    }
                }

                while (_packetLenght > 0 && _packetLenght <= receiveData.UnreadLength())
                {
                    byte[] _packetBytes = receiveData.ReadBytes(_packetLenght);
                    ThreadManager.ExecuteOnMainThread(() =>
                    {
                        using (Packet _packet = new Packet(_packetBytes))
                        {
                            int _packetId = _packet.ReadInt();
                            Server.packetHandlers[_packetId](id, _packet);
                        }

                    });

                    _packetLenght = 0;
                    if (receiveData.UnreadLength() >= 4)
                    {
                        _packetLenght = receiveData.ReadInt();
                        if (_packetLenght <= 0)
                        {
                            return true;
                        }
                    }
                }
                if (_packetLenght <= 1)
                {
                    return true;
                }

                return false;
            }

            public void Disconnect()
            {
                socket.Close();
                stream = null;
                receiveData = null;
                receiveBuffer = null;
                socket = null;
            }
        }

        public class UDP
        {
            public IPEndPoint endPoint;

            private int id;

            public UDP(int _id)
            {
                id = _id;
            }

            public void Connect(IPEndPoint _endPoint)
            {
                endPoint = _endPoint;
            }

            public void SendData(Packet _packet)
            {
                Server.SendUDPData(endPoint, _packet);
            }

            public void HandleData(Packet _packetData)
            {
                int _packetLength = _packetData.ReadInt();
                byte[] _packetBytes = _packetData.ReadBytes(_packetLength);

                ThreadManager.ExecuteOnMainThread(() =>
                {
                    using (Packet _packet = new Packet(_packetBytes))
                    {
                        int _packetId = _packet.ReadInt();
                        Server.packetHandlers[_packetId](id, _packet);
                    }
                });
            }

            public void Disconnect()
            {
                endPoint = null;
            }
        }

        public void SendIntoGame(string _playerName)
        {
            //player = new Player(id, _playerName, new Vector3(0, 0, 0));

            foreach (Client _client in Server.clients.Values)
            {
                if (_client.player != null)
                {
                    if (_client.id != id)
                    {
                        ServerSend.SpawnPlayer(id, _client.player);
                    }
                }
            }

            foreach (Client _client in Server.clients.Values)
            {
                if (_client.player != null)
                {
                    ServerSend.SpawnPlayer(_client.id, player);
                }
            }
        }

        public void SendToMainMenu()
        {
            ServerSend.SendToMainMenu(id);
        }

        public void SendToRegisterMenu()
        {
            ServerSend.SendToRegisterMenu(id);
        }

        public void JoinRoom(int _roomID)
        {
            if (Server.activeRooms[_roomID].ConnectClient(Server.clients[id]))
            {
                Console.WriteLine($"Player {player.username} joined room {_roomID}");
                return;
            }
            Console.WriteLine($"Player {player.username} attempted to join room {_roomID} but failed");
        }



        public void JoinRandomOrCreateRoom(string _username)
        {
            for (int i = 1; i <= Server.activeRooms.Count; i++)
            {
                if (!Server.activeRooms[i].HasGameStarted())
                {
                    if (Server.activeRooms[i].ConnectClient(Server.clients[id]))
                    {
                        int _id;
                        using (SqliteDataAccess _db = new SqliteDataAccess())
                        {
                            _id = (int)_db.GetUserData(_username).id;
                        }
                        player = new Player(_id, _username, i);
                        Console.WriteLine($"Player {player.username} joined room {i}");
                        if (Server.activeRooms[i].GetRoomClients().Count == Server.activeRooms[i].GetMaxClient())
                        {
                            Server.activeRooms[i].LoadRoom();
                        }
                        return;
                    }
                }
            }
            Server.activeRooms.Add(Server.activeRooms.Count + 1, new Room(Server.activeRooms.Count + 1));
            Console.WriteLine($"No room available. Created new room {Server.activeRooms.Count}");
            JoinRandomOrCreateRoom(_username);
        }

        public void LeaveCurrentRoom()
        {
            for (int i = 1; i <= Server.activeRooms.Count; i++)
            {
                if (Server.activeRooms[i].HasPlayer(Server.clients[id]))
                {
                    Server.activeRooms[i].LeaveRoom(Server.clients[id]);
                    return;
                }
            }
        }


        public void ReturnNewUserData(PlayerInfo _data)
        {
            ServerSend.SendUserDataToClient(id, _data);
        }

        private void Disconnect()
        {
            LeaveCurrentRoom();
            Console.WriteLine($"{tcp.socket.Client.RemoteEndPoint} has disconnected");
            player = null;
            tcp.Disconnect();
            udp.Disconnect();
        }

    }
}
