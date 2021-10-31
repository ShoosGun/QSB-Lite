using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Threading;

using ServerSide.Sockets.Clients;
using ServerSide.Utils;

namespace ServerSide.Sockets.Servers
{
    public class Server
    {
        private Listener l;
        private List<Client> clients;
        private Dictionary<string, Client> clientsLookUpTable;

        private List<Client> NewClientsCache = new List<Client>();
        private readonly object NCC_lock = new object();

        private const int MAX_DATA_PER_CLIENT_LOOP = 3;
        private Dictionary<string, Queue<byte[]>> ReceivedDataCache = new Dictionary<string, Queue<byte[]>>();
        private readonly object RDC_lock = new object();

        private List<string> DisconnecedClientsCache = new List<string>();
        private readonly object DCC_lock = new object();

        
        public DynamicPacketIO DynamicPacketIO { get; private set; }
        
        public Server(int port, AllowedConnections allowedConnections = AllowedConnections.ANY)
        {
            DynamicPacketIO = new DynamicPacketIO();
                        
            clients = new List<Client>();
            clientsLookUpTable = new Dictionary<string, Client>();
            l = new Listener(port, allowedConnections);
            l.SocketAccepted += L_SocketAccepted;
            l.Start();
        }

        /// <summary>
        /// Handles new connections on the in game loop and creates the in-game representation of the client
        /// </summary>
        /// <returns></returns>
        private void NewConnections(Client client)
        {
            clientsLookUpTable.Add(client.ID, client);
            clients.Add(client);
            lock (RDC_lock)
            {
                ReceivedDataCache.Add(client.ID, new Queue<byte[]>(MAX_DATA_PER_CLIENT_LOOP));
                Console.WriteLine(client.ID);
            }

            string clientsString = "";
            foreach (Client c in clients)
            {
                clientsString += string.Format("\n{0}\n===================", c.ID);
            }

            Console.WriteLine("Lista dos clientes conectados ate agora: {0}", clientsString);

            NewConnection?.Invoke();
            NewConnectionID?.Invoke(client.ID);
        }
        private void NewConnections(List<Client> newClients)
        {
            foreach (var c in newClients)
            {
                NewConnections(c);
            }
        }
        /// <summary>
        /// Handles disconnections on the in game loop and delets the in-game representation of the client
        /// </summary>
        /// <returns></returns>
        private void Disconnections(string clientID)
        {
            Client c = clientsLookUpTable[clientID];
            Console.WriteLine("{0} se desconectou!", c.ID);

            Disconnection?.Invoke();
            DisconnectionID?.Invoke(clientID);

            c.Close();
            clientsLookUpTable.Remove(c.ID);
            clients.Remove(c);

            string clientsString = "";
            foreach (Client temp in clients)
            {
                clientsString = clientsString + "\n" + temp.ID + "\n===================";
            }
            Console.WriteLine("Lista dos clientes conectados ate agora: {0}", clientsString);
        }
        private void Disconnections(List<string> clientsIDs)
        {
            foreach (var c in clientsIDs)
            {
                Disconnections(c);
            }
        }
        /// <summary>
        /// Handles new data sent by the clients on the in-game loop
        /// </summary>
        /// <returns></returns>
        private void ReceivedData(string clientID, byte[] data)
        {
            if (data.Length > 0)
            {
                PacketReader packet = new PacketReader(data);
                try
                {
                    DynamicPacketIO.ReadReceivedPacket(ref packet, clientID);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erro ao ler dados de {0}: {1} {2}", clientID , ex.Source, ex.Message);
                }
            }
        }

        /// <summary>
        ///  Handles the new packets sent by all the clients on the in-game loop
        /// </summary>
        /// <param name="receivedData">Holds the packets in an array separated by the clients ids</param>
        /// <param name="resetDataArrays"></param>
        private void ReceivedData(Dictionary<string, Queue<byte[]>> receivedData, bool resetDataArrays = true)
        {
            //Ideias: 
            // 1 - Fazer a leitura de dados no async (X)
            // 2 - Fazer a leitura de dados em uma Coroutine da unity (X)
            // 3 - No lugar de enviar byte[]'s enviamos PacketReaders um pouco tratadas (X)
            for (int i = 0; i < clients.Count; i++)
            {
                string clientID = clients[i].ID;
                
                foreach (byte[] data in receivedData[clientID])
                {
                    if (data.Length > 0)
                        ReceivedData(clientID, data);
                }
                if(resetDataArrays)
                    receivedData[clientID].Clear();
            }
        }
        
        public void CheckReceivedData()
        {
            //Global Data
            byte[] globalDataBuffer = DynamicPacketIO.GetGlobalPacketWriterData();
            if (globalDataBuffer.Length > 0)
                SendAll(globalDataBuffer);

            //Client Specific Data
            for (int i =0; i< clients.Count; i++)
            {
                byte[] clientSpecificBuffer = DynamicPacketIO.GetClientSpecificPacketWriterData(clients[i].ID);
                if (clientSpecificBuffer.Length > 0)
                    Send(clientSpecificBuffer, clients[i].ID);
            }
            DynamicPacketIO.ResetClientSpecificDataHolder();

            bool NCC_NotLoked = Monitor.TryEnter(NCC_lock, 10);
            try
            {
                if (NCC_NotLoked && NewClientsCache.Count > 0)
                {
                    NewConnections(NewClientsCache);
                    NewClientsCache.Clear();
                }
            }
            finally
            {
                Monitor.Exit(NCC_lock);
            }

            bool RDC_NotLoked = Monitor.TryEnter(RDC_lock, 10);
            try
            {
                if (RDC_NotLoked)
                {
                    ReceivedData(ReceivedDataCache);
                }
            }
            finally
            {
                Monitor.Exit(RDC_lock);
            }

            bool DCC_NotLoked = Monitor.TryEnter(DCC_lock, 10);
            try
            {
                if (DCC_NotLoked && DisconnecedClientsCache.Count > 0)
                {
                    Disconnections(DisconnecedClientsCache);
                    DisconnecedClientsCache.Clear();
                }
            }
            finally
            {
                Monitor.Exit(DCC_lock);
            }
        }

        private void L_SocketAccepted(Socket e)
        {
            Client client = new Client(e);
            client.Received += Client_Received;
            client.Disconnected += Client_Disconnected;
            lock (NCC_lock)
            {
                NewClientsCache.Add(client);
            }
        }

        private void Client_Disconnected(Client sender)
        {
            lock (DCC_lock)
            {
                DisconnecedClientsCache.Add(sender.ID);
            }
        }

        private void Client_Received(Client sender, byte[] data)
        {
            lock (RDC_lock)
            {
                if (!ReceivedDataCache.ContainsKey(sender.ID))
                    return;

                if (ReceivedDataCache[sender.ID].Count < MAX_DATA_PER_CLIENT_LOOP)
                    ReceivedDataCache[sender.ID].Enqueue(data);
            }
        }

        /// <summary>
        /// Sends a buffer of information to an especified Client
        /// </summary>
        /// <param name="shade">The client's in-game representation</param>
        /// <param name="buffer"></param>
        public void Send(byte[] buffer, string clientID)
        {
            if (clientsLookUpTable.ContainsKey(clientID))
                clientsLookUpTable[clientID].Send(buffer);
        }
        /// <summary>
        /// Sends a buffer of information to an array of especified Clients. It is faster, to use SendAll if you want to send some information to all connected clients.
        /// </summary>
        /// <param name="shades"></param>
        /// <param name="buffer"></param>
        public void Send(byte[] buffer, params string[] clientIDs)
        {
            foreach (string id in clientIDs)
            {
                Send(buffer, id);
            }
        }
        /// <summary>
        /// Sends a buffer of information to all Clients. By iterating through all the Clients and sending with their sockets, it can be faster compared to searching for all ClientID's and then using Send().
        /// </summary>
        /// <param name="Exceptions"> The ids of the ones you don't want to send to</param>
        /// <param name="buffer"></param>
        public void SendAll(byte[] buffer)
        {
            foreach (Client c in clients)
                c.Send(buffer);
        }

        public void Stop()
        {
            for (int i = 0; i < clients.Count; i++)
                clients[i].Close();

            l.Stop();
        }

        public event NewConnectionHandler NewConnection;
        public delegate void NewConnectionHandler();

        public event NewConnectionIDHandler NewConnectionID;
        public delegate void NewConnectionIDHandler(string clientID);

        public event DisconnectionHandler Disconnection;
        public delegate void DisconnectionHandler();

        public event DisconnectionHandlerID DisconnectionID;
        public delegate void DisconnectionHandlerID(string clientID);
    }

    public struct ClientEssentials
    {
        public string ClientID;
        public byte[] Data;

        public ClientEssentials(string clientID, byte[] data)
        {
            ClientID = clientID;
            Data = data;
        }
    }
}
