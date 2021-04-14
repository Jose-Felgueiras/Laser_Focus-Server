using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace LaserFocusServer
{
    class ServerHandle
    {
        public static void WelcomeReceived(int _fromClient, Packet _packet)
        {
            int _clientIdCheck = _packet.ReadInt();
            string _username = _packet.ReadString();

            Console.WriteLine($"{Server.clients[_fromClient].tcp.socket.Client.RemoteEndPoint} connected successfully as {_username}.");
            if (_fromClient != _clientIdCheck)
            {
                Console.WriteLine($"Player \"{_username}\" (ID: {_fromClient}) has assumed the wrong client ID ({_clientIdCheck})!");
            }

            Server.clients[_fromClient].SendIntoGame(_username);
        }

        public static void PlayerInput(int _fromClient, Packet _packet)
        {
            bool[] _inputs = new bool[_packet.ReadInt()];
            for (int i = 0; i < _inputs.Length; i++)
            {
                _inputs[i] = _packet.ReadBool();
            }
            Quaternion _rotation = _packet.ReadQuaternion();

            Server.clients[_fromClient].player.SetInput(_inputs, _rotation);
        }

        public static void CheckDatabase(int _fromClient, Packet _packet)
        {
            int _clientDBID = _packet.ReadInt();
            string _clientDBUsername = _packet.ReadString();
            using (SqliteDataAccess _db = new SqliteDataAccess())
            {
                PlayerInfo playerInfo = _db.GetUserData(_clientDBID);

                if (playerInfo.id == _clientDBID && playerInfo.username == _clientDBUsername)
                {
                    //SEND PLAYER TO MENU
                    Console.WriteLine($"User connected. Welcome back {_clientDBUsername}");

                    Server.clients[_fromClient].SendToMainMenu();
                }
                else
                {
                    //SEND PLAYER TO REGISTER
                    Console.WriteLine("New user. Waiting for registration...");
                    Server.clients[_fromClient].SendToRegisterMenu();
                }
            }
        }

        public static void CreateUser(int _fromClient, Packet _packet)
        {
            Console.WriteLine("Registering new User");

            string _username = _packet.ReadString();
            using (SqliteDataAccess _db = new SqliteDataAccess())
            {
                if (_db.GetUserData(_username).username != null)
                {
                    Console.WriteLine("Atempted to add user that already existed. Requesting new username.");
                    ServerSend.RequestNewUsername(_fromClient);
                }
                else
                {
                    _db.AddUser(_username);
                    Console.WriteLine($"Registered new user {_username}. Welcome to Laser Focus.");
                    Server.clients[_fromClient].ReturnNewUserData(_db.GetUserData(_username));
                }  
            }
        }

        public static void SendFriendRequest(int _fromClient, Packet _packet)
        {
            int _fromClientID = _packet.ReadInt();
            int _toClientID = _packet.ReadInt();

            using (SqliteDataAccess _db = new SqliteDataAccess())
            {
                if (!_db.GetFriendRequest(_fromClientID, _toClientID))
                {
                    _db.SendFriendRequest(_fromClientID, _toClientID);

                    ServerSend.FriendshipRequestHandled(_fromClient, "Friend request sent.");
                }
                else
                {
                    ServerSend.FriendshipRequestHandled(_fromClient, "The ID you inserted doesn't belong to anyone. Please try again.");
                }
            }
        }

        public static void AcceptFriendRequest(int _fromClient, Packet _packet)
        {
            int _toClientID = _packet.ReadInt(); //User that received request
            int _fromClientID = _packet.ReadInt(); //User that sent request

            using (SqliteDataAccess _db = new SqliteDataAccess())
            {
                if (_db.GetFriendRequest(_fromClientID, _toClientID))
                {
                    Console.WriteLine($"User {_db.GetUserData(_toClientID).username} accepted friend request from {_db.GetUserData(_fromClientID).username}");
                    _db.AcceptFriendRequest(_fromClientID, _toClientID);
                    _db.AcceptFriendRequest(_toClientID, _fromClientID);
                    _db.RemoveFriendRequest(_fromClientID, _toClientID);

                    ServerSend.FriendshipRequestHandled(_fromClient, "Friend added.");
                }
            }
        }

        public static void RejectFriendRequest(int _fromClient, Packet _packet)
        {
            int _toClientID = _packet.ReadInt();
            int _fromClientID = _packet.ReadInt();

            using (SqliteDataAccess _db = new SqliteDataAccess())
            {
                if (_db.GetFriendRequest(_fromClientID, _toClientID))
                {
                    _db.RemoveFriendRequest(_fromClientID, _toClientID);

                    ServerSend.FriendshipRequestHandled(_fromClient, "Friend request removed.");
                }
            }
        }

        public static void RequestAllFriendsList(int _fromClient, Packet _packet)
        {
            int _fromClientID = _packet.ReadInt();
            List<Int64> list;
            PlayerInfo[] playerInfoArray;
            using (SqliteDataAccess _db = new SqliteDataAccess())
            {
                Console.WriteLine($"Getting friends of {_db.GetUserData(_fromClientID).username}");


                list = _db.GetAllFriends(_fromClientID);
                playerInfoArray = new PlayerInfo[list.Count];

                for (int i = 0; i < list.Count; i++)
                {
                    playerInfoArray[i] = _db.GetUserData((int)list[i]);
                }

                ServerSend.FriendsListRequestHandled(_fromClient, playerInfoArray);
            }
        }

        public static void RequestAllFriendRequestsList(int _fromClient, Packet _packet)
        {
            int _fromClientID = _packet.ReadInt();

            List<Int64> list;
            PlayerInfo[] playerInfoArray;

            using (SqliteDataAccess _db = new SqliteDataAccess())
            {
                Console.WriteLine($"Getting friend requests for {_db.GetUserData(_fromClientID).username}");

                list = _db.GetAllFriendRequests(_fromClientID);
                playerInfoArray = new PlayerInfo[list.Count];

                for (int i = 0; i < list.Count; i++)
                {
                    playerInfoArray[i] = _db.GetUserData((int)list[i]);
                }

                ServerSend.FriendRequestsListRequestHandled(_fromClient, playerInfoArray);
            }
        }

        public static void JoinRandomRoom(int _fromClient, Packet _packet)
        {
            string _username = _packet.ReadString();            
            Server.clients[_fromClient].JoinRandomOrCreateRoom(_username);
        }

        public static void CancelMatchmaking(int _fromClient, Packet _packet)
        {
            Server.activeRooms[Server.clients[_fromClient].player.currentRoomID].LeaveRoom(Server.clients[_fromClient]);

            Server.clients[_fromClient].player.Dispose();
            Server.clients[_fromClient].player = null;

            ServerSend.CanceledMatchmaking(_fromClient);
        }

        public static void HandleReceiveDeck(int _fromClient, Packet _packet)
        {
            int[] deck = new int[_packet.ReadInt()];
            for (int i = 0; i < deck.Length; i++)
            {
                deck[i] = _packet.ReadInt();
            }
            Server.clients[_fromClient].player.SetDeck(deck);
        }

        public static void PlayerSuccessfullyLoadedRoom(int _fromClient, Packet _packet)
        {
            Server.clients[_fromClient].player.readyToStart = true;
            Server.activeRooms[Server.clients[_fromClient].player.currentRoomID].StartMatch();
        }

        public static void RequestPlayer(int _fromClient, Packet _packet)
        {
            PlayerID _player= Server.activeRooms[Server.clients[_fromClient].player.currentRoomID].GetPlayer(_fromClient);

            ServerSend.SendPlayerNumber(_fromClient, _player);
        }

        public static void PlaceTowerRequest(int _fromClient, Packet _packet)
        {
            Vector2 _pos = _packet.ReadVector2();
            Quaternion _rot = _packet.ReadQuaternion();
            int _towerID = _packet.ReadInt();

            ServerSend.PlaceTower(_fromClient, Server.clients[_fromClient].player.currentRoomID, _pos, _rot, _towerID);
            Server.activeRooms[Server.clients[_fromClient].player.currentRoomID].NextTurn();
        }

        public static void Forfeit(int _fromClient, Packet _packet)
        {
            Server.activeRooms[Server.clients[_fromClient].player.currentRoomID].Forfeit(_fromClient);
        }

        public static void SetWinner(int _fromClient, Packet _packet)
        {
            int _winner = _packet.ReadInt();
            Server.activeRooms[Server.clients[_fromClient].player.currentRoomID].SetWinner(_winner);
        }
    }
}
