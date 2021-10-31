using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerSide.Sockets.Servers
{
    public class Listener
    {
        private static DateTime TimeFromLastClose;
        public bool Listening
        {
            get;
            private set;
        }

        public int Port
        {
            get;
            private set;
        }        

        private AllowedConnections AllowedConnections;
        private Socket s;

        //TODO refazer a implementação das sockets usando UDP!
        private const ProtocolType listenerProtocolType = ProtocolType.Tcp;

        public Listener(int port, AllowedConnections allowedConnections)
        {
            Port = port;
            AllowedConnections = allowedConnections;
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, listenerProtocolType);
        }
        public void Start()
        {
            if (Listening)
                return;

            //TODO descobrir maneira de resolver o problema de usar o mesmo port num tempo menor que 120s
            //1 - fazer ele esperar para resolver
            //2 - mudar o protocolo das sockets para ProtocolType.Udp
            //https://stackoverflow.com/questions/3229860/what-is-the-meaning-of-so-reuseaddr-setsockopt-option-linux/3233022#3233022, achar forma de resolver isso ai

            if (AllowedConnections == AllowedConnections.ANY)
            {
                s.Bind(new IPEndPoint(0, Port));

                string localIPv4 = GetLocalIPAddress();
                if (localIPv4 != null)
                    Console.WriteLine("Server IP = {0} <<", localIPv4);
                else
                    Console.WriteLine("Não conseguimos o IPv4");
            }
            else if (AllowedConnections == AllowedConnections.ONLY_HOST)
                s.Bind(new IPEndPoint(IPAddress.Parse("127.1.0.0"), Port));

            s.Listen(0);
            s.BeginAccept(callback, null);
            Listening = true;
        }

        public void Stop()
        {
            if (!Listening)
                return;

            s.Close();
            
            TimeFromLastClose = DateTime.UtcNow;
            s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, listenerProtocolType);

        }

        private void callback(IAsyncResult ar)
        {
            try
            {
                Socket s = this.s.EndAccept(ar);

                SocketAccepted?.Invoke(s);
                //TODO adicionar maneira de controlar a quantidade de conecções feitas
                this.s.BeginAccept(callback, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro no callback: {0}", ex.Message);
            }
        }
        public event SocketAcceptedHandler SocketAccepted;
        public delegate void SocketAcceptedHandler(Socket e);

        /// <summary>
        /// Returns the local IP
        /// </summary>
        /// <param name="addressFamily">Defaults to IPv4</param>
        /// <returns>If no IP from that AddressFamily is found, returns an empty string</returns>
        public static string GetLocalIPAddress(AddressFamily addressFamily = AddressFamily.InterNetwork)
        {
            IPAddress[] IPArray = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            foreach (var ip in IPArray)
            {
                if (ip.AddressFamily == addressFamily)
                {
                    return ip.ToString();
                }
            }
            return "";
        }
    }
    public enum AllowedConnections : byte
    {
        ONLY_HOST,
        ANY,
    }
} 