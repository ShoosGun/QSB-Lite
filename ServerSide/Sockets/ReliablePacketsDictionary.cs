using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Net;
using System;

namespace SNet_Server.Sockets
{
    //TODO adicionar as funcionalidades aqui e trocar as estruturas no Listener
    public class ClientDicitionary
    {
        Dictionary<string, Client> Clients;
        Dictionary<IPEndPoint, string> IPEndpointToClientIDMap;

        public void Add(string key, IPEndPoint clientIP)
        {
            Client client = new Client() { IpEndpoint = clientIP };
            Clients.Add(key, client);
            IPEndpointToClientIDMap.Add(clientIP, key);
        }
    }
    public struct Client
    {
        public IPEndPoint IpEndpoint;
        public DateTime TimeOfLastPacket;
    }

    public class ReliablePacketHandler : KeyedCollection<int, ReliablePacket>
    {
        private int NextGeneratedID = int.MinValue;

        public void Add(byte[] packetData, out int packetID, params string[] clients)
        {
            packetID = NextGeneratedID;

            if (NextGeneratedID == int.MaxValue)
                NextGeneratedID = int.MinValue;
            else
                NextGeneratedID += 1;

            var rP = new ReliablePacket(packetID, packetData);
            rP.ClientsLeftToReceive.AddRange(clients);

            Add(rP);
        }

        protected override int GetKeyForItem(ReliablePacket reliablePacket) => reliablePacket.PacketID;

        public void ClientReceivedData(int PacketID, string client)
        {
            if (TryGetValue(PacketID, out ReliablePacket reliablePacket))
            {
                reliablePacket.ClientsLeftToReceive.Remove(client);

                if (reliablePacket.ClientsLeftToReceive.Count <= 0)
                    Remove(reliablePacket);
            }
        }
    }
    public struct ReliablePacket
    {
        public int PacketID;
        public byte[] Data;
        public List<string> ClientsLeftToReceive;
        public ReliablePacket(int PacketID, byte[] Data)
        {
            this.PacketID = PacketID;
            this.Data = Data;
            ClientsLeftToReceive = new List<string>();
        }
    }
}
