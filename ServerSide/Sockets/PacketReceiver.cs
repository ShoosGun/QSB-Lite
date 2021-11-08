using System;
using System.Collections.Generic;

using ServerSide.Utils;
namespace ServerSide.Sockets
{
    public class PacketReceiver
    {
        public delegate void ReadPacket(ref PacketReader reader, ReceivedPacketData receivedPacketData);

        private Dictionary<int, ReadPacket> ReadPacketHolders;

        public PacketReceiver()
        {
            ReadPacketHolders = new Dictionary<int, ReadPacket>();
        }

        public void ReadReceivedPacket(ref PacketReader reader, int Header, ReceivedPacketData receivedPacketData)
        {
            if (ReadPacketHolders.TryGetValue(Header, out ReadPacket readPacket))
            {
                try
                {
                    readPacket(ref reader, receivedPacketData);
                }
                catch (Exception ex) { Console.WriteLine("Erro em DyncamicPacketIO: {0} - {1} {2}", ex.Message, ex.Source, ex.StackTrace); }
            }
            else
            {
                Console.WriteLine("Received data from non existing header {0}", Header);
            }
        }

        public int AddPacketReader(string LocalizationString, ReadPacket readPacket)
        {
            int hash = Util.GerarHashInt(LocalizationString);

            if (ReadPacketHolders.ContainsKey(hash))
                throw new OperationCanceledException(string.Format("This string has a hash thay is already being used {0}", hash));

            ReadPacketHolders.Add(hash, readPacket);
            return hash;
        }
    }
    public struct ReceivedPacketData
    {
        public readonly string ClientID;
        public readonly DateTime SentTime;
        public readonly int Latency;

        public ReceivedPacketData(string ClientID, DateTime SentTime, int Latency)
        {
            this.ClientID = ClientID;
            this.SentTime = SentTime;
            this.Latency = Latency;
        }
    }
}
