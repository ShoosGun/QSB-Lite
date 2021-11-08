using System;
using System.Threading;
using SNet_Server.PacketCouriers;
using SNet_Server.Sockets;

namespace SNet_Server
{
    class Program
    {
        static int PORT = 2121;
        static int SERVER_TIME_STEP = 1000/60;
        
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

            Console.WriteLine("Aperte alguma tecla para fechar");
            //TODO Adicionar maneira de para essa thread por fora
            Server server = new Server(PORT);

            ServerInteraction serverInteraction = new ServerInteraction(server);
            EntityRepasser entityRepasser = new EntityRepasser(server, serverInteraction);
            
            while (!Console.KeyAvailable)
            {
                server.CheckReceivedData();
                Thread.Sleep(SERVER_TIME_STEP);
            }

            Console.ReadKey(true);
        }
    }
}
