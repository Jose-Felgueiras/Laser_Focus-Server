using System;
using System.Collections.Generic;
using System.Text;
using System.Numerics;

namespace LaserFocusServer
{
    class Player : IDisposable
    {
        public int id;
        public string username;

        public int currentRoomID;

        public bool readyToStart;

        public Vector3 position;
        public Quaternion rotation;

        private float moveSpeed = 5f / Constants.TICKS_PER_SECOND;
        private bool[] inputs;

        public int[] deck;

        public Player(int _id, string _username, int _roomID)
        {
            id = _id;
            username = _username;

            currentRoomID = _roomID;

            inputs = new bool[4];
            deck = new int[8];
        }

        public void Update()
        {
            Vector2 _inputDirection = Vector2.Zero;
            if (inputs[0])
            {
                _inputDirection.Y += 1;
            }
            if (inputs[1])
            {
                _inputDirection.Y -= 1;
            }
            if (inputs[2])
            {
                _inputDirection.X += 1;
            }
            if (inputs[3])
            {
                _inputDirection.X -= 1;
            }

            Move(_inputDirection);

        }

        private void Move(Vector2 _inputDirection)
        {
            Vector3 _forward = Vector3.Transform(new Vector3(0, 0, 1), rotation);
            Vector3 _right = Vector3.Normalize(Vector3.Cross(_forward, new Vector3(0, 1, 0)));

            Vector3 _moveDirection = _right * _inputDirection.X + _forward * _inputDirection.Y;
            position += _moveDirection * moveSpeed;

            ServerSend.PlayerPosition(this);
            ServerSend.PlayerRotation(this);
        }

        public void SetInput(bool[] _inputs, Quaternion _rotation)
        {
            inputs = _inputs;
            rotation = _rotation;
        }

        public void SetDeck(int[] _deck)
        {
            deck = _deck;
        }

        private bool disposed = false;

        protected virtual void Dispose(bool _disposing)
        {
            if (!disposed)
            {
                if (_disposing)
                {
                    id = -1;
                    username = null;

                    position = Vector3.Zero;
                    rotation = Quaternion.Identity;

                    moveSpeed = -1;
                    inputs = null;
                    deck = null;
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
