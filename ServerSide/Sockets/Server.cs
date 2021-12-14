using System;
using System.Collections.Concurrent;

using SNet_Server.Utils;

namespace SNet_Server.Sockets
{
    public class Server
    {
        private Listener l;
        
        private ConcurrentQueue<ClientEssentials> ReceivedDataCache;
        
        public PacketReceiver PacketReceiver { get; private set; }
        
        public Server(int port)
        {
            PacketReceiver = new PacketReceiver();
            ReceivedDataCache = new ConcurrentQueue<ClientEssentials>();

            l = new Listener(port);
            l.OnClientConnection += L_OnClientConnection;
            l.OnClientDisconnection += L_OnClientDisconnection;
            l.OnClientReceivedData += L_OnClientReceivedData;
            l.Start();
        }

        private void L_OnClientReceivedData(byte[] dgram, string id)
        {
            ReceivedDataCache.Enqueue(new ClientEssentials(id, dgram));
        }

        private void L_OnClientConnection(string id)
        {
            Console.WriteLine("Novo cliente ID = {0}", id);
            OnNewClient(id);
        }

        private void L_OnClientDisconnection(string id, DisconnectionType disconnectionType)
        {
            Console.WriteLine("Um cliente se desconectou ID = {0}, motivo {1}", id, disconnectionType);
            OnClientDisconnection(id);
        }

        private byte[] MakeDataWithHeader(byte[] data, int header)
        {
            byte[] dataWithHeader = new byte[4 + 8 + data.Length];
            Array.Copy(BitConverter.GetBytes(header), 0, dataWithHeader, 0, 4); //Header
            Array.Copy(BitConverter.GetBytes(DateTime.UtcNow.ToBinary()), 0, dataWithHeader, 4, 8); //Send Time
            Array.Copy(data, 0, dataWithHeader, 12, data.Length);
            return dataWithHeader;
        }
        public bool Send(byte[] data, int header, string client) => l.Send(MakeDataWithHeader(data, header), client);

        public void SendAll(byte[] data, int header, params string[] dontSendTo) => l.SendAll(MakeDataWithHeader(data, header), dontSendTo);

        public bool SendReliable(byte[] data, int header, string client) => l.SendReliable(MakeDataWithHeader(data, header), client);

        public void SendAllReliable(byte[] data, int header, params string[] dontSendTo) => l.SendAllReliable(MakeDataWithHeader(data, header), dontSendTo);

        private void ReceivedData(ClientEssentials receivedDGram)
        {
            PacketReader packet = new PacketReader(receivedDGram.DGram);
            try
            {
                int Header = packet.ReadInt32();
                DateTime sendTime = packet.ReadDateTime();
                ReceivedPacketData receivedPacketData = new ReceivedPacketData(receivedDGram.ClientID, sendTime, (int)(DateTime.UtcNow - sendTime).TotalMilliseconds);

                PacketReceiver.ReadReceivedPacket(ref packet, Header, receivedPacketData);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao ler dados de {0}: {1} {2}", receivedDGram.ClientID, ex.Source, ex.Message);
            }
        }

        
        
        public void CheckReceivedData()
        {
            while (ReceivedDataCache.TryDequeue(out ClientEssentials clientEssentials))
                ReceivedData(clientEssentials);
        }

        public void Stop()
        {
            l.Stop();
        }

        public event NewClient OnNewClient;
        public delegate void NewClient(string clientID);
        

        public event ClientDisconnection OnClientDisconnection;
        public delegate void ClientDisconnection (string clientID);        
    }

    public struct ClientEssentials
    {
        public string ClientID;
        public byte[] DGram;

        public ClientEssentials(string clientID, byte[] dgram)
        {
            ClientID = clientID;
            DGram = dgram;
        }
    }
}
