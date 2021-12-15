using System.Collections.Generic;
using System.Net;
using System;

namespace SNet_Server.Sockets
{
    public class Client
    {
        public IPEndPoint IpEndpoint;
        public string ID;
        public DateTime TimeOfLastPacket;

        public Dictionary<int, ReliablePacket> ReliablePacketsToReceive;

        public Client()
        { 
            ReliablePacketsToReceive = new Dictionary<int, ReliablePacket>();
            TimeOfLastPacket = DateTime.UtcNow;
        }
    }
    public class ReliablePacket
    {
        public readonly int PacketID;
        public readonly byte[] Data;
        public List<IPEndPoint> ClientsLeftToReceive;

        public ReliablePacket(int PacketID, byte[] Data)
        {
            this.PacketID = PacketID;
            this.Data = Data;
            ClientsLeftToReceive = new List<IPEndPoint>();
        }
    }
}
