using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Data;
using System.Data.SqlClient;


namespace LaserFocusServer
{
    class Server
    {
        public static int maxPlayers { get; private set; }
        public static int port { get; private set; }

        public static Dictionary<int, Client> clients = new Dictionary<int, Client>();
        public delegate void PacketHandler(int _fromClient, Packet _packet);
        public static Dictionary<int, PacketHandler> packetHandlers;

        public static Dictionary<int, Room> activeRooms = new Dictionary<int, Room>();


        private static TcpListener tcpListner;
        private static UdpClient udpListner;

        public static void Start(int _maxPlayers, int _port)
        {
            maxPlayers = _maxPlayers;
            port = _port;

            Console.WriteLine("Starting Server...");
            InitializeServerData();

            tcpListner = new TcpListener(IPAddress.Any, port);
            tcpListner.Start();
            tcpListner.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);

            udpListner = new UdpClient(port);
            udpListner.BeginReceive(UDPReceiveCallback, null);

            Console.WriteLine($"Server started on {port}.");

        }

 

        public static void Stop()
        {
            Console.WriteLine("Stopping Server...");
            udpListner.Close();
            tcpListner.Stop();

            clients.Clear();
            packetHandlers.Clear();
        }

        private static void TCPConnectCallback(IAsyncResult _result)
        {
            TcpClient _client = tcpListner.EndAcceptTcpClient(_result);
            tcpListner.BeginAcceptTcpClient(new AsyncCallback(TCPConnectCallback), null);
            Console.WriteLine($"Incoming connection form {_client.Client.RemoteEndPoint}...");

            for (int i = 1; i <= maxPlayers; i++)
            {
                if (clients[i].tcp.socket == null)
                {
                    clients[i].tcp.Connect(_client);
                    return;
                }
            }
            Console.WriteLine($"{ _client.Client.RemoteEndPoint} failed to connect: Server is full.");
        }

        private static void UDPReceiveCallback(IAsyncResult _result)
        {
            try
            {
                IPEndPoint _clientEndPoint = new IPEndPoint(IPAddress.Any, 0);
                byte[] _data = udpListner.EndReceive(_result, ref _clientEndPoint);
                udpListner.BeginReceive(UDPReceiveCallback, null);

                if (_data.Length < 4)
                {
                    return;
                }

                using (Packet _packet = new Packet(_data))
                {
                    int _clientId = _packet.ReadInt();

                    if (_clientId == 0)
                    {
                        return;
                    }

                    if (clients[_clientId].udp.endPoint == null)
                    {
                        clients[_clientId].udp.Connect(_clientEndPoint);
                        return;
                    }

                    if (clients[_clientId].udp.endPoint.ToString() == _clientEndPoint.ToString())
                    {
                        clients[_clientId].udp.HandleData(_packet);
                    }
                }
            }
            catch (Exception _ex)
            {
                Console.WriteLine($"Error receiving UDP data: {_ex}");
                throw;
            }
        }

        public static void SendUDPData(IPEndPoint _clientEndPoint, Packet _packet)
        {
            try
            {
                if (_clientEndPoint != null)
                {
                    udpListner.BeginSend(_packet.ToArray(), _packet.Length(), _clientEndPoint, null, null);
                }
            }
            catch (Exception _ex)
            {
                Console.WriteLine($"Error sending data to {_clientEndPoint} via UDP: {_ex}");
                throw;
            }
        }

        private static void InitializeServerData()
        {
            for (int i = 1; i <= maxPlayers; i++)
            {
                clients.Add(i, new Client(i));
            }

            packetHandlers = new Dictionary<int, PacketHandler>()
            {
                { (int)ClientPackets.welcomeReceived, ServerHandle.WelcomeReceived},
                { (int)ClientPackets.checkDatabase, ServerHandle.CheckDatabase},
                { (int)ClientPackets.createUser, ServerHandle.CreateUser},
                { (int)ClientPackets.sendFriendRequest, ServerHandle.SendFriendRequest},
                { (int)ClientPackets.acceptFriendRequest, ServerHandle.AcceptFriendRequest},
                { (int)ClientPackets.rejectFriendRequest, ServerHandle.RejectFriendRequest},
                { (int)ClientPackets.friendsListRequest, ServerHandle.RequestAllFriendsList},
                { (int)ClientPackets.friendRequestsListRequest, ServerHandle.RequestAllFriendRequestsList},

                { (int)ClientPackets.joinRandomRoom, ServerHandle.JoinRandomRoom},
                { (int)ClientPackets.cancelMatchmaking, ServerHandle.CancelMatchmaking},
                { (int)ClientPackets.sendDeckToServer, ServerHandle.HandleReceiveDeck},
                { (int)ClientPackets.successfullyLoadedGame, ServerHandle.PlayerSuccessfullyLoadedRoom},
                { (int)ClientPackets.requestPlayer, ServerHandle.RequestPlayer},
                { (int)ClientPackets.placeTowerRequest, ServerHandle.PlaceTowerRequest},
                { (int)ClientPackets.playerForfeited, ServerHandle.Forfeit},
                { (int)ClientPackets.sendWinner, ServerHandle.SetWinner},

                { (int)ClientPackets.playerInput, ServerHandle.PlayerInput}
                



            };
            Console.WriteLine("Initialized packets.");
        }

        #region Rooms

        public static int FindRoomWithClient(Client _client)
        {
            for (int i = 1; i <= activeRooms.Count; i++)
            {
                if (activeRooms[i].HasPlayer(_client))
                {
                    return i;
                }
            }
            return -1;
        }

        #endregion
    }
}
