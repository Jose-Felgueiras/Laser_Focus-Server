using System;
using System.Collections.Generic;
using System.Text;
using System.Data;
using System.Data.SQLite;
using Dapper;
using System.Data.Common;
namespace LaserFocusServer
{
    class SqliteDataAccess : IDisposable
    {
        private static SQLiteConnection dbConnection;


        public SqliteDataAccess()
        {
            Connect();
        }

        private static void Connect()
        {
            SQLiteConnection _connection = new SQLiteConnection(@"Data Source = E:\Work\Laser_Focus_Server\LaserFocusServer\LaserFocusServer\Users.db; Version=3");
            
            _connection.Open();
            if (_connection.State == ConnectionState.Connecting || _connection.State == ConnectionState.Closed)
            {
                Console.WriteLine("FAILED TO CONNECT TO DATABASE");
            }
            else
            {
                dbConnection = _connection;
            }
        }


        public PlayerInfo GetUserData(int _id)
        {
            PlayerInfo playerInfo = new PlayerInfo();
            using (SQLiteCommand fmd = dbConnection.CreateCommand())
            {
                SQLiteCommand sqlCommand = new SQLiteCommand(@"SELECT * FROM user WHERE id = ($id)", dbConnection);
                sqlCommand.Parameters.Add(new SQLiteParameter("$id", _id));
                SQLiteDataReader reader = sqlCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                while (reader.Read())
                {
                    if (reader.StepCount <= 0)
                    {
                        Console.WriteLine("Could not find user id in database");
                        return null; 
                    }
                    else
                    {
                        playerInfo.SetID((Int64)reader["id"]);
                        playerInfo.SetUsername((string)reader["username"]);
                    }
                }
            }
            return playerInfo;
        }

        public PlayerInfo GetUserData(string _username)
        {
            PlayerInfo playerInfo = new PlayerInfo();
            using (SQLiteCommand fmd = dbConnection.CreateCommand())
            {
                SQLiteCommand sqlCommand = new SQLiteCommand(@"SELECT * FROM user WHERE username = ($username)", dbConnection);
                sqlCommand.Parameters.Add(new SQLiteParameter("$username", _username));
                SQLiteDataReader reader = sqlCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                while (reader.Read())
                {
                    if (reader.StepCount <= 0)
                    {
                        Console.WriteLine("Could not find user username in database");
                        return null;
                    }
                    else
                    {
                        Console.WriteLine($"User { _username} found in database");
                        playerInfo.SetID((Int64)reader["id"]);
                        playerInfo.SetUsername((string)reader["username"]);
                    }
                }
            }
            return playerInfo;
        }

        public void AddUser(string _username)
        {
            using (SQLiteCommand fmd = dbConnection.CreateCommand())
            {
                SQLiteCommand sqlCommand = new SQLiteCommand(@"INSERT INTO user (username) VALUES ($username)", dbConnection);
                sqlCommand.Parameters.Add(new SQLiteParameter("$username", _username));
                try
                {
                    sqlCommand.ExecuteNonQuery();
                    Console.WriteLine($"Added user {_username} to database");

                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Unable to insert data into database: {_ex}");
                    throw;
                }
            }
        }

        public bool GetFriendRequest(int _fromUserID, int _toUserID)
        {
            using (SQLiteCommand fmd = dbConnection.CreateCommand())
            {
                SQLiteCommand sqlCommand = new SQLiteCommand(@"SELECT * FROM user_friend_requests WHERE userid = ($userID) AND friendid = ($friendid)", dbConnection);
                sqlCommand.Parameters.Add(new SQLiteParameter("$userID", _toUserID));
                sqlCommand.Parameters.Add(new SQLiteParameter("$friendid", _fromUserID));
                SQLiteDataReader reader = sqlCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                while (reader.Read())
                {
                    try
                    {
                        if (reader.StepCount > 0)
                        {
                            return true;
                        }
                    }
                    catch (Exception _ex)
                    {
                        Console.WriteLine($"Error trying to find a friend request: {_ex}");
                        throw;
                    }
                }
                Console.WriteLine($"Player {GetUserData(_toUserID).username} does not have a friend request from {GetUserData(_fromUserID).username}");
                return false;
            }
        }

        public bool GetFriend(int _friendID, int _userID)
        {
            using (SQLiteCommand fmd = dbConnection.CreateCommand())
            {
                SQLiteCommand sqlCommand = new SQLiteCommand(@"SELECT FROM user_friend WHERE userid = ($userID)", dbConnection);
                sqlCommand.Parameters.Add(new SQLiteParameter("$userID", _userID));
                SQLiteDataReader reader = sqlCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                while (reader.Read())
                {
                    try
                    {
                        if ((int)reader["id"] == _friendID)
                        {
                            return true;
                        }
                    }
                    catch (Exception _ex)
                    {
                        Console.WriteLine($"Error trying to find a friend: {_ex}");
                        throw;
                    }

                }
                Console.WriteLine($"Player {GetUserData(_userID).username} is not friends with {GetUserData(_friendID).username}");
                return false;
            }
        }

        public void SendFriendRequest(int _fromUserID, int _toUserID)
        {
            using (SQLiteCommand fmd = dbConnection.CreateCommand())
            {
                SQLiteCommand sqlCommand = new SQLiteCommand(@"INSERT INTO user_friend_requests (userid, friendid) VALUES ($userID, $friendID)", dbConnection);
                sqlCommand.Parameters.Add(new SQLiteParameter("$userID", _toUserID));
                sqlCommand.Parameters.Add(new SQLiteParameter("$friendID", _fromUserID));
                try
                {
                    sqlCommand.ExecuteNonQuery();
                    Console.WriteLine($"User {GetUserData(_fromUserID).username} sent a Friend Request to {GetUserData(_toUserID).username}");

                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Unable to insert data into database: {_ex}");
                    throw;
                }
            }
        }
        
        public void AcceptFriendRequest(int _fromUserID, int _toUserID)
        {
            using (SQLiteCommand fmd = dbConnection.CreateCommand())
            {
                SQLiteCommand sqlCommand = new SQLiteCommand(@"INSERT INTO user_friends (userid, friendid) VALUES ($userID, $friendID)", dbConnection);
                sqlCommand.Parameters.Add(new SQLiteParameter("$userID", _toUserID));
                sqlCommand.Parameters.Add(new SQLiteParameter("$friendID", _fromUserID));
                try
                {
                    sqlCommand.ExecuteNonQuery();
                    Console.WriteLine($"User {GetUserData(_fromUserID).username} added friend {GetUserData(_toUserID).username}");
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Unable to accept friend request: {_ex}");
                    throw;
                }
            }
        }

        public void RemoveFriendRequest(int _fromUserID, int _toUserID)
        {
            using (SQLiteCommand fmd = dbConnection.CreateCommand())
            {
                SQLiteCommand sqlCommand = new SQLiteCommand(@"DELETE FROM user_friend_requests WHERE userid = ($userID) AND friendid = ($friendID)", dbConnection);
                sqlCommand.Parameters.Add(new SQLiteParameter("$userID", _toUserID));
                sqlCommand.Parameters.Add(new SQLiteParameter("$friendID", _fromUserID));
                try
                {
                    sqlCommand.ExecuteNonQuery();
                }
                catch (Exception _ex)
                {
                    Console.WriteLine($"Unable to delete data from database: {_ex}");
                    throw;
                }
            }    
        }

        public void RemoveFriend(int _fromUserID, int _toUserID)
        {
            if (GetFriend(_fromUserID, _toUserID))
            {

            }
            Console.WriteLine($"Player {GetUserData(_fromUserID).username} is not friends with: ");

        }

        public List<Int64> GetAllFriends(int _fromID)
        {
            List<Int64> friendsIDs = new List<Int64>();
            using (SQLiteCommand fmd = dbConnection.CreateCommand())
            {
                SQLiteCommand sqlCommand = new SQLiteCommand(@"SELECT * FROM user_friends WHERE userid = ($userID)", dbConnection);
                sqlCommand.Parameters.Add(new SQLiteParameter("$userID", _fromID));
                SQLiteDataReader reader = sqlCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                while (reader.Read())
                {
                    if (reader.StepCount <= 0)
                    {
                        Console.WriteLine("User has no friends");
                        return null;
                    }
                    else
                    {
                        friendsIDs.Add((Int64)reader["friendid"]);
                    }
                }
            }
            return friendsIDs;
        }

        public List<Int64> GetAllFriendRequests(int _fromID)
        {
            List<Int64> friendsRequestsIDs = new List<Int64>();
            using (SQLiteCommand fmd = dbConnection.CreateCommand())
            {
                SQLiteCommand sqlCommand = new SQLiteCommand(@"SELECT * FROM user_friend_requests WHERE userid = ($userID)", dbConnection);
                sqlCommand.Parameters.Add(new SQLiteParameter("$userID", _fromID));
                SQLiteDataReader reader = sqlCommand.ExecuteReader(CommandBehavior.SequentialAccess);
                while (reader.Read())
                {
                    if (reader.StepCount <= 0)
                    {
                        Console.WriteLine("User has no friend requests");
                        return null;
                    }
                    else
                    {
                        Console.WriteLine($"Found {(Int64)reader["friendid"]}");
                        friendsRequestsIDs.Add((Int64)reader["friendid"]);
                    }
                }
            }
            return friendsRequestsIDs;
        }


        private bool disposed = false;

        protected virtual void Dispose(bool _disposing)
        {
            if (!disposed)
            {
                if (_disposing)
                {
                    dbConnection.Close();
                    dbConnection = null;
                }

                disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
