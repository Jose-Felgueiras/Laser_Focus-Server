using System;
using System.Collections.Generic;
using System.Text;

namespace LaserFocusServer
{
    class PlayerInfo
    {
        public Int64 id { get; private set; } = 0;
        public string username { get; private set; } = null;

        public void SetID(Int64 _id)
        {
            id = _id;
        }

        public void SetUsername(string _username)
        {
            username = _username;
        }
    }
}
