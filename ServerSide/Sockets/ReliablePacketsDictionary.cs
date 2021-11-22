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

        public ClientDicitionary()
        {
            Clients = new Dictionary<string, Client>();
            IPEndpointToClientIDMap = new Dictionary<IPEndPoint, string>();
        }

        public void Add(string key, IPEndPoint clientIP)
        {
            Client client = new Client() { IpEndpoint = clientIP, ID = key };
            Clients.Add(key, client);
            IPEndpointToClientIDMap.Add(clientIP, key);
        }

        public void Remove(string key, out Client client)
        {
            Clients.Remove(key, out client);
            IPEndpointToClientIDMap.Remove(client.IpEndpoint);
        }
        public void Remove(IPEndPoint key, out Client client)
        {
            IPEndpointToClientIDMap.Remove(key, out string clientID);
            Clients.Remove(clientID, out client);
        }

        public Client GetClient(string key) => Clients[key];
        public Client GetClient(IPEndPoint key) => Clients[IPEndpointToClientIDMap[key]];

        public bool Contains(string key) => Clients.ContainsKey(key);
        public bool Contains(IPEndPoint key) => IPEndpointToClientIDMap.ContainsKey(key);

        public bool TryGet(string key, out Client client) => Clients.TryGetValue(key, out client);
        public bool TryGet(IPEndPoint key, out Client client)
        {
            if(IPEndpointToClientIDMap.TryGetValue(key, out string clientID))
            {
                client = Clients[clientID];
                return true;
            }
            client = new Client();
            return false;
        }

        public void ForEach(Action<Client> action)
        {
            foreach (var c in Clients)
                action(c.Value);
        }

        public int Count() => IPEndpointToClientIDMap.Count;

        public void Clear()
        {
            Clients.Clear();
            IPEndpointToClientIDMap.Clear();
        }
    }
    public class Client
    {
        public IPEndPoint IpEndpoint;
        public string ID;
        public DateTime TimeOfLastPacket;
        public bool IsConnected;

        public List<ReliablePacket> ReliablePacketsToReceive;
    }

    public class ReliablePacketHandler : KeyedCollection<int, ReliablePacket>
    {
        private int NextGeneratedID = int.MinValue;

        public ReliablePacket Add(byte[] packetData, out int packetID, params string[] clients)
        {
            packetID = NextGeneratedID;

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
        private List<string> ClientsLeftToReceive;

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
