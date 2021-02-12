using System;
using System.Threading;

namespace LaserFocusServer
{
    class Program
    {
        private static bool isRunning = false;

        static void Main(string[] args)
        {
            Console.Title = "Laser Focus Server";
            isRunning = true;

            Thread mainThread = new Thread(new ThreadStart(MainThread));
            mainThread.Start();
            Server.Start(10, 5000);
        }

        private static void MainThread()
        {
            Console.WriteLine($"Main thread started. Running at {Constants.TICKS_PER_SECOND} ticks per second");
            DateTime _nextLoop = DateTime.Now;

            while (isRunning)
            {
                while (_nextLoop < DateTime.Now)
                {
                    GameLogic.Update();
                    _nextLoop = _nextLoop.AddMilliseconds(Constants.MS_PER_TICK);

                    if (_nextLoop > DateTime.Now)
                    {
                        Thread.Sleep(_nextLoop - DateTime.Now);
                    }
                }

                //if (Console.ReadLine() == "pause" && isRunning)
                //{
                //    isRunning = false;
                //    Console.WriteLine("Server has paused recieving updates");
                //}
                //if (Console.ReadLine() == "start" && !isRunning)
                //{
                //    isRunning = true;
                //    Console.WriteLine("Server has resumed recieving updates");

                //}
            }
        }
    }


    

}
