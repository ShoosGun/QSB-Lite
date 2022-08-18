using System;
using System.Net;
using System.Threading;

namespace SNet_Client.Sockets
{
    public struct ReliablePacket
    {
        public int PacketID;
        public byte[] Data;
        public ReliablePacket(int PacketID, byte[] Data)
        {
            this.PacketID = PacketID;
            this.Data = Data;
        }
    }

    public class Server
    {
        private readonly object _lock = new object();

        private EndPoint ServerEndPoint;
        private DateTime TimeOfLastReceivedMessage;
        private bool Connecting;
        private bool Connected;

        public Server()
        {
            TimeOfLastReceivedMessage = DateTime.UtcNow;
            Connecting = false;
            Connected = false;
        }

        public void SetServerEndPoint(EndPoint ServerEndPoint)
        {
            lock (_lock)
            { 
                this.ServerEndPoint = ServerEndPoint;
            }
        }
        public void SetTimeOfLastReceivedMessage(DateTime time)
        {
            lock (_lock)
            {
                TimeOfLastReceivedMessage = time;
            }
        }
        public void SetConnecting(bool b)
        {
            lock (_lock)
            {
                Connecting = b;
            }
        }
        public void SetConnected(bool b)
        {
            lock (_lock)
            {
                Connected = b;
            }
        }

        public EndPoint GetServerEndPoint()
        {
            lock (_lock)
            {
                return ServerEndPoint;
            }
        }
        public DateTime GetTimeOfLastReceivedMessage()
        {
            lock (_lock)
            {
                return TimeOfLastReceivedMessage;
            }
        }
        public bool GetConnecting()
        {
            lock (_lock)
            {
                return Connecting;
            }
        }
        public bool GetConnected()
        {
            lock (_lock)
            {
                return Connected;
            }
        }
    }
}
