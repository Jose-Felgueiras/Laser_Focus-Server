using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace LaserFocusServer
{
    class ServerSend
    {
        private static void SendTCPData(int _toClient, Packet _packet)
        {
            _packet.WriteLength();
            Server.clients[_toClient].tcp.SendData(_packet);
        }

        private static void SendUDPData(int _toClient, Packet _packet)
        {
            _packet.WriteLength();
            Server.clients[_toClient].udp.SendData(_packet);
        }


        private static void SendTCPDataToAll(Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.maxPlayers; i++)
            {
                Server.clients[i].tcp.SendData(_packet);
            }
        }

        private static void SendTCPDataToAll(int _exceptClient, Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.maxPlayers; i++)
            {
                if (i != _exceptClient)
                {
                    Server.clients[i].tcp.SendData(_packet);
                }
            }
        }

        private static void SendUDPDataToAll(Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.maxPlayers; i++)
            {
                Server.clients[i].udp.SendData(_packet);
            }
        }

        private static void SendUDPDataToAll(int _exceptClient, Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Server.maxPlayers; i++)
            {
                if (i != _exceptClient)
                {
                    Server.clients[i].udp.SendData(_packet);
                }
            }
        }

        private static void SendTCPDataToAllInRoom(int _roomID, Packet _packet)
        {
            _packet.WriteLength();
            foreach (Client client in Server.activeRooms[_roomID].GetRoomClients())
            {
                client.tcp.SendData(_packet);
            }
        }

        private static void SendTCPDataToAllInRoom(int _exceptClient, int _roomID, Packet _packet)
        {
            _packet.WriteLength();
            for (int i = 1; i <= Room.maxPlayers; i++)
            {
                if (i != _exceptClient)
                {
                    Server.activeRooms[_roomID].GetRoomClients()[i - 1].tcp.SendData(_packet);
                }
            }
        }

        public static void Welcome(int _toCLient, string _msg)
        {
            using (Packet _packet = new Packet((int)ServerPackets.welcome))
            {
                _packet.Write(_msg);
                _packet.Write(_toCLient);

                SendTCPData(_toCLient, _packet);
            }
        }

        public static void SendToRegisterMenu(int _toClient)
        {
            using (Packet _packet = new Packet((int)ServerPackets.sendToRegisterMenu))
            {
                _packet.Write("Unkwown user, please register");
                _packet.Write(_toClient);
                
                SendTCPData(_toClient, _packet);
            }
        }

        public static void SendToMainMenu(int _toClient)
        {
            using(Packet _packet = new Packet((int)ServerPackets.sendToMainMenu))
            {
                _packet.Write("Login Sucess");
                _packet.Write(_toClient);

                SendTCPData(_toClient, _packet);
            }
        }

        public static void RequestNewUsername(int _toClient)
        {
            using (Packet _packet = new Packet((int)ServerPackets.requestNewUsername))
            {
                _packet.Write("Username already taken!");

                SendTCPData(_toClient, _packet);
            }
        }

        public static void SendUserDataToClient(int _toClient, PlayerInfo _data)
        {
            using (Packet _packet = new Packet((int)ServerPackets.sendUserData))
            {
                _packet.Write(_data.username);
                _packet.Write((int)_data.id);

                SendTCPData(_toClient, _packet);
            }
        }

        public static void FriendshipRequestHandled(int _toClient, string _msg)
        {
            using (Packet _packet = new Packet((int)ServerPackets.handledFriendshipRequest))
            {
                _packet.Write(_msg);

                SendTCPData(_toClient, _packet);
            }
        }

        public static void FriendsListRequestHandled(int _toClient, PlayerInfo[] friendList)
        {
            using (Packet _packet = new Packet((int)ServerPackets.handledFriendshipListRequest))
            {
                _packet.Write(friendList.Length);
                for (int i = 0; i < friendList.Length; i++)
                {
                    _packet.Write((string)friendList[i].username);
                }
                for (int i = 0; i < friendList.Length; i++)
                {
                    _packet.Write(friendList[i].id);
                }
                Console.WriteLine("FriendsList: ");

                for (int i = 0; i < friendList.Length; i++)
                {
                    Console.WriteLine($"User ID: {friendList[i].id}; Username: {friendList[i].username}");
                }

                SendTCPData(_toClient, _packet);
            }
        }

        public static void FriendRequestsListRequestHandled(int _toClient, PlayerInfo[] friendList)
        {
            using (Packet _packet = new Packet((int)ServerPackets.handledFriendshipRequestListRequest))
            {
                _packet.Write(friendList.Length);
                for (int i = 0; i < friendList.Length; i++)
                {
                    _packet.Write((string)friendList[i].username);
                }
                for (int i = 0; i < friendList.Length; i++)
                {
                    _packet.Write(friendList[i].id);
                }

                Console.WriteLine("FriendRequestList: ");

                for (int i = 0; i < friendList.Length; i++)
                {
                    Console.WriteLine($"User ID: {friendList[i].id}; Username: friendList[i].username");
                }

                SendTCPData(_toClient, _packet);
            }
        }


        public static void CanceledMatchmaking(int _toClient)
        {
            using (Packet _packet = new Packet((int)ServerPackets.canceledMatchmaking))
            {
                SendTCPData(_toClient, _packet);
            }
        }

        public static void SendPlayersIntoGame(int _roomID)
        {
            using (Packet _packet = new Packet((int)ServerPackets.sendPlayerIntoGame))
            {
                SendTCPDataToAllInRoom(_roomID, _packet);
            }
        }

        public static void SendPlayerNumber(int _fromClient, PlayerID _number)
        {
            using (Packet _packet = new Packet((int)ServerPackets.sendPlayerNumber))
            {
                _packet.Write((int)_number);
                SendTCPData(_fromClient, _packet);
            }
        }

        public static void NextPlayerTurn(int _fromClient)
        {
            using (Packet _packet = new Packet((int)ServerPackets.startPlayerTurn))
            {
                SendTCPData(_fromClient, _packet);
            }
        }

        public static void PlaceTower(int _fromClient, int _roomID, Vector2 _pos,Quaternion _rot, int _towerID)
        {
            using (Packet _packet = new Packet((int)ServerPackets.placeTower))
            {
                _packet.Write(Server.clients[_fromClient].player.id);
                _packet.Write(_pos);
                _packet.Write(_rot);
                _packet.Write(_towerID);
                SendTCPDataToAllInRoom(Server.clients[_fromClient].player.currentRoomID, _packet);
            }
        }

        public static void OpponentDisconnected(int _fromClient, string _msg)
        {
            using (Packet _packet = new Packet((int)ServerPackets.opponentDisconnected))
            {
                _packet.Write(_msg);
                SendTCPDataToAllInRoom(Server.clients[_fromClient].player.currentRoomID, _packet);
            }
        }

        public static void OpponentForfeited(int _fromClient, string _msg)
        {
            using (Packet _packet = new Packet((int)ServerPackets.opponentForfeited))
            {
                _packet.Write(_msg);
                SendTCPDataToAllInRoom(Server.clients[_fromClient].player.currentRoomID, _packet);
            }
        }

        public static void SpawnPlayer(int _toClient, Player _player)
        {
            using (Packet _packet = new Packet((int)ServerPackets.spawnPlayer))
            {
                _packet.Write(_player.id);
                _packet.Write(_player.username);
                _packet.Write(_player.position);
                _packet.Write(_player.rotation);

                SendTCPData(_toClient, _packet);
            }
        }

        public static void PlayerPosition(Player _player)
        {
            using (Packet _packet = new Packet((int)ServerPackets.playerPosition))
            {
                _packet.Write(_player.id);
                _packet.Write(_player.position);

                SendUDPDataToAll(_packet);
            }
        }

        public static void PlayerRotation(Player _player)
        {
            using (Packet _packet = new Packet((int)ServerPackets.playerRotation))
            {
                _packet.Write(_player.id);
                _packet.Write(_player.rotation);

                SendUDPDataToAll(_player.id, _packet);
            }
        }
    }
}
