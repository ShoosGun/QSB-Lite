using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Net;
using System;

namespace SNet_Server.Sockets
{
    public class ClientDicitionary
    {
        Dictionary<string, Client> Clients;
        Dictionary<IPEndPoint, string> IPEndpointToClientIDMap;

        private ReliablePacketHandler ReliablePackets;

        private object Clients_LOCK = new object();

        public ClientDicitionary()
        {
            Clients = new Dictionary<string, Client>();
            IPEndpointToClientIDMap = new Dictionary<IPEndPoint, string>();

            ReliablePackets = new ReliablePacketHandler();
        }

        public void Add(string key, IPEndPoint clientIP)
        {
            lock (Clients_LOCK)
            {
                Client client = new Client() { IpEndpoint = clientIP, ID = key };
                Clients.Add(key, client);
                IPEndpointToClientIDMap.Add(clientIP, key);
            }
        }

        public bool Remove(string key, out Client client)
        {
            lock (Clients_LOCK)
            {
                bool result = Clients.Remove(key, out client);
                IPEndpointToClientIDMap.Remove(client.IpEndpoint);

                return result;
            }
        }
        public bool Remove(IPEndPoint key, out Client client)
        {
            lock (Clients_LOCK)
            {
                IPEndpointToClientIDMap.Remove(key, out string clientID);
                bool result =  Clients.Remove(clientID, out client);

                foreach (var p in client.ReliablePacketsToReceive)
                    p.Value.ClientReceived(clientID);

                return result;
            }
        }

        public Client GetClient(string key) { lock(Clients_LOCK){ return Clients[key]; } }
        public Client GetClient(IPEndPoint key) { lock (Clients_LOCK){ return Clients[IPEndpointToClientIDMap[key]]; } }

        public bool Contains(string key) { lock (Clients_LOCK) { return Clients.ContainsKey(key); } }
        public bool Contains(IPEndPoint key) { lock (Clients_LOCK) { return IPEndpointToClientIDMap.ContainsKey(key); } }

        public bool TryGet(string key, out Client client) { lock (Clients_LOCK) { return Clients.TryGetValue(key, out client); } }
        public bool TryGet(IPEndPoint key, out Client client)
        {
            lock (Clients_LOCK)
            {
                if (IPEndpointToClientIDMap.TryGetValue(key, out string clientID))
                {
                    client = Clients[clientID];
                    return true;
                }
                client = new Client();
                return false;
            }
        }
        public void TrySetDateTime(IPEndPoint key, DateTime time)
        {
            lock (Clients_LOCK)
            {
                if (IPEndpointToClientIDMap.TryGetValue(key, out string clientID))
                    Clients[clientID].TimeOfLastPacket = time;
            }
        }

        public void ClientForEach(Action<Client> action)
        {
            lock (Clients_LOCK)
            {
                foreach (var c in Clients)
                    action(c.Value);
            }
        }
        public void ReliablePacketForEach(Action<ReliablePacket> action)
        {
            lock (Clients_LOCK)
            {
                foreach (var p in ReliablePackets)
                    action(p);
            }
        }

        public void FreeLockedManipulation(Action<Dictionary<string, Client>, Dictionary<IPEndPoint, string>, ReliablePacketHandler> action)
        {
            lock (Clients_LOCK)
            {
                action(Clients, IPEndpointToClientIDMap, ReliablePackets);
            }
        }

        public bool TryManipulateClient(IPEndPoint key, Action<Client> action)
        {
            lock (Clients_LOCK)
            {
                if (IPEndpointToClientIDMap.TryGetValue(key, out string clientID))
                {
                    action(Clients[clientID]);
                    return true;
                }
                return false;
            }
        }
        public bool TryManipulateClient(string key, Action<Client> action)
        {
            lock (Clients_LOCK)
            {
                if (Clients.TryGetValue(key, out Client client))
                {
                    action(client);
                    return true;
                }
                return false;
            }
        }

        public int Count() { lock (Clients_LOCK) { return IPEndpointToClientIDMap.Count; } }

        public void Clear()
        {
            lock (Clients_LOCK)
            {
                Clients.Clear();
                IPEndpointToClientIDMap.Clear();
                ReliablePackets.Clear();
            }
        }
    }
    public class Client
    {
        public IPEndPoint IpEndpoint;
        public string ID;
        public DateTime TimeOfLastPacket;
        public bool IsConnected;

        public Dictionary<int, ReliablePacket> ReliablePacketsToReceive;

        public Client()
        { 
            ReliablePacketsToReceive = new Dictionary<int, ReliablePacket>();
            IsConnected = false;
            TimeOfLastPacket = DateTime.UtcNow;
        }
    }

    public class ReliablePacketHandler : KeyedCollection<int, ReliablePacket>
    {
        private int NextGeneratedID = int.MinValue;

        public ReliablePacket Add(byte[] packetData, params string[] clients)
        {
            int packetID = NextGeneratedID;

            if (NextGeneratedID == int.MaxValue)
                NextGeneratedID = int.MinValue;
            else
                NextGeneratedID += 1;

            var rP = new ReliablePacket(this, packetID, packetData);
            rP.AddClients(clients);

            Add(rP);
            return rP;
        }

        protected override int GetKeyForItem(ReliablePacket reliablePacket) => reliablePacket.PacketID;
    }
    public class ReliablePacket
    {
        private ReliablePacketHandler owner;

        public readonly int PacketID;
        public readonly byte[] Data;
        public List<string> ClientsLeftToReceive;

        public ReliablePacket(ReliablePacketHandler owner, int PacketID, byte[] Data)
        {
            this.owner = owner;

            this.PacketID = PacketID;
            this.Data = Data;
            ClientsLeftToReceive = new List<string>();
        }

        public void AddClient( string client) => ClientsLeftToReceive.Add(client);
        public void AddClients(IEnumerable<string> clients) => ClientsLeftToReceive.AddRange(clients);

        public void ClientReceived(string clientID)
        {
            ClientsLeftToReceive.Remove(clientID);

            if(ClientsLeftToReceive.Count <= 0)
                owner.Remove(this);
        }
    }
}
