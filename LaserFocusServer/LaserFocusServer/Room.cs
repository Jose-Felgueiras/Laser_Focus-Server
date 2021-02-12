using System;
using System.Collections.Generic;
using System.Text;

namespace LaserFocusServer
{
    public enum PlayerID
    {
        PLAYER1,
        PLAYER2
    }

    class Room
    {
        Random rnd = new Random();
        private int id;
        public static int maxPlayers = 2;

        public static List<Client> clients;

        public bool gameBegun = false;

        public bool gameEnded = false;

        public int winner = -1;

        public int playerTurn = -1;

        public Room(int _id)
        {
            gameBegun = false;
            gameEnded = false;
            winner = -1;
            playerTurn = -1;
            id = _id;
            clients = new List<Client>(maxPlayers);
        }

        public bool ConnectClient(Client _client)
        {
            if (clients.Count <= maxPlayers)
            {
                clients.Add(_client);
                return true;
            }
            Console.WriteLine($"Client atempted to join room {id} and failed. The room is full.");
            return false;
        }

        public void LeaveRoom(Client _client)
        {
            if (clients.Contains(_client))
            {
                clients.Remove(_client);
                if (gameBegun && !gameEnded)
                {
                    gameEnded = true;

                    //TELL REMAINING PLAYER OPONENT DISCONNECTED
                    //SEND TO WIN

                    winner = 0;

                    if (clients.Count > 0)
                    {
                        ServerSend.OpponentDisconnected(clients[0].id, "Opponent Connection Failed");
                        Console.WriteLine($"User {clients[winner].player.username} won the match.");
                    }
                }
                if (gameBegun && gameEnded)
                {
                    //TELL REMAINING PLAYER OPONENT FORFEITED
                    //SEND TO WIN

                    winner = 0;

                    if (clients.Count > 0)
                    {
                        ServerSend.OpponentDisconnected(clients[0].id, "Opponent Forfeited");
                        Console.WriteLine($"User {clients[winner].player.username} won the match.");
                    }
                }
                Console.WriteLine($"Player {_client.player.username} left room {id}.");
            }
            if (clients.Count <= 0)
            {
                CloseRoom();
            }
        }

        public bool HasGameStarted()
        {
            return gameBegun;
        }

        public bool HasPlayer(Client _client)
        {
            if (clients.Contains(_client))
            {
                return true;
            }
            return false;
        }

        public PlayerID GetPlayer(int _fromClient)
        {
            if (Server.clients[_fromClient] == clients[0])
            {
                return PlayerID.PLAYER1;
            }
            else
            {
                return PlayerID.PLAYER2;
            }
        }

        public void Forfeit(int _fromClient)
        {
            if (clients.Contains(Server.clients[_fromClient]))
            {
                gameEnded = true;
                LeaveRoom(Server.clients[_fromClient]);
                FinishMatch();
            }
        }

        public void LoadRoom()
        {
            ServerSend.SendPlayersIntoGame(id);
            Console.WriteLine($"Room {id} has enough players. Starting game between {clients[0].player.username} and {clients[1].player.username}");
            playerTurn = rnd.Next(0, maxPlayers);
        }

        private void CloseRoom()
        {
            clients.Clear();
            Server.activeRooms.Remove(id);
            Console.WriteLine($"Room {id} had no players and was closed");
        }

        public int GetMaxClient()
        {
            return maxPlayers;
        }

        public List<Client> GetRoomClients()
        {
            return clients;
        }

        public void StartMatch()
        {
            foreach (Client client in clients)
            {
                if (!client.player.readyToStart)
                {
                    Console.WriteLine($"Waiting for player {client.player.username} to finish loading");
                    return;
                }
            }
            gameBegun = true;
            Console.WriteLine($"All players finished loading. Ready to start.");
            NextTurn();
        }

        public void FinishMatch()
        {
            Console.WriteLine($"Finishing match...");
            CloseRoom();
        }

        public bool PlayerTurn(Client _client)
        {
            return playerTurn == clients.IndexOf(_client);
        }

        public void NextTurn()
        {
            playerTurn++;
            if (playerTurn >= maxPlayers)
            {
                playerTurn = 0;
            }
            ServerSend.NextPlayerTurn(clients[playerTurn].id);
        }

        public void SetWinner(int _winner)
        {
            winner = _winner;
            if (winner >= maxPlayers)
            {
                Console.WriteLine($"Match ended in a tie");
            }
            else
            {
                Console.WriteLine($"User {clients[winner].player.username} won the match in room {id}.");
            }
            FinishMatch();
        }
    }
}
