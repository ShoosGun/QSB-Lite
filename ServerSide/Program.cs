using System;

using SNet_Server.PacketCouriers;
using SNet_Server.Sockets;
using SNet_Server.Utils;

namespace SNet_Server
{
    class Program
    {
        static int PORT = 2121;
        static int SERVER_TIME_STEP = 60;
        
        static void Main(string[] args)
        {
            if(args.Length > 0)
            {
                if(byte.TryParse(args[0],out byte CustomChoosenPort))
                {
                    Console.WriteLine("Using Choosen Port {0}", CustomChoosenPort);
                    PORT = CustomChoosenPort;
                }
            }
            if(args.Length > 1)
            {
                if (int.TryParse(args[1], out int CustomChoosenServerTimeStep))
                {
                    Console.WriteLine("Using Choosen Server Time Step {0} ms", CustomChoosenServerTimeStep);
                    SERVER_TIME_STEP = CustomChoosenServerTimeStep;
                }
            }

            Console.WriteLine("Aperte S , P ou ENTER para fechar");
            Server server = new Server(PORT);

            ServerInteraction serverInteraction = new ServerInteraction(server);
            EntityRepasser entityRepasser = new EntityRepasser(server, serverInteraction);

            bool shouldServerBeOn = true;
            object shouldServerBeOn_LOCK = new object();

            bool areWeSupposedToLoop = true;

            Util.RepeatDelayedAction(1000 / SERVER_TIME_STEP, 1000 / SERVER_TIME_STEP, () =>
            {
                server.CheckReceivedData();
                lock (shouldServerBeOn_LOCK) { return shouldServerBeOn; }
            });

            while (areWeSupposedToLoop)
            {
                if (Console.KeyAvailable)
                {
                    ConsoleKeyInfo keyPressed = Console.ReadKey(true);

                    switch (keyPressed.Key)
                    {
                        case ConsoleKey.P:
                        case ConsoleKey.S:
                        case ConsoleKey.Enter:
                            lock (shouldServerBeOn_LOCK) { shouldServerBeOn = false; }
                            server.Stop();
                            areWeSupposedToLoop = false;
                            break;
                        case ConsoleKey.C: //Crash Button
                            throw new Exception("CRASH TEST");
                        case ConsoleKey.R: //Return Button
                            return;
                    }
                }
            }
        }
    }
}
