using System;
using System.Collections.Generic;
using System.IO;

using ServerSide.Utils;
namespace ServerSide.Sockets.Servers
{    
    public class DynamicPacketIO
    {
        public delegate void ReadPacket(byte[] data, ReceivedPacketData receivedPacketData);
        private Dictionary<int, ReadPacket> ReadPacketHolders;       

        private PacketWriter globalPacketWriter;
        private Dictionary<string, PacketWriter> clientSpecificPacketWriters;

        public DynamicPacketIO()
        {
            clientSpecificPacketWriters = new Dictionary<string, PacketWriter>();
            ReadPacketHolders = new Dictionary<int, ReadPacket>();
        }

        private byte[] GetAllData(ref PacketWriter packetWriter)
        {
            if (packetWriter != null)
            {
                byte[] data = packetWriter.GetBytes();
                packetWriter = null;
                return data;
            }
            return new byte[]{ };
        }
        public byte[] GetGlobalPacketWriterData()
        {
            return GetAllData(ref globalPacketWriter);
        }
        public byte[] GetClientSpecificPacketWriterData(string ClientID)
        {
            if (clientSpecificPacketWriters.TryGetValue(ClientID, out PacketWriter packet))
            {
                clientSpecificPacketWriters.Remove(ClientID);
                return GetAllData(ref packet);
            }
            return new byte[] { };
        }

        public void ResetClientSpecificDataHolder()
        {
            clientSpecificPacketWriters.Clear();
        }

        private void WritePackedData(int HeaderValue, byte[] data, ref PacketWriter writer)
        {
            if (writer == null)
            {
                writer = new PacketWriter();
                writer.Write(DateTime.UtcNow);
            }

            writer.Write(HeaderValue);
            writer.Write(data.Length);
            writer.Write(data);
        }
        public void SendPackedData(int HeaderValue, byte[] data, params string[] ClientIDs)
        {
            if (ClientIDs.Length == 0)
            {
                WritePackedData(HeaderValue, data, ref globalPacketWriter);
                return;
            }
            for (int i = 0; i < ClientIDs.Length; i++)
            {
                if (!clientSpecificPacketWriters.ContainsKey(ClientIDs[i]))
                    clientSpecificPacketWriters.Add(ClientIDs[i], null);
                
                if (clientSpecificPacketWriters[ClientIDs[i]] == null)
                {
                    clientSpecificPacketWriters[ClientIDs[i]] = new PacketWriter();
                    clientSpecificPacketWriters[ClientIDs[i]].Write(DateTime.UtcNow);
                }

                clientSpecificPacketWriters[ClientIDs[i]].Write(HeaderValue);
                clientSpecificPacketWriters[ClientIDs[i]].Write(data.Length);
                clientSpecificPacketWriters[ClientIDs[i]].Write(data);
            }
        }

        public void ReadReceivedPacket(ref PacketReader packetReader, string ClientID)
        {
            bool continueLoop = true;
            List<int> ReceivedDataFromNonExistingHeaders = new List<int>();
            DateTime sentTime = packetReader.ReadDateTime();
            int latency = (DateTime.UtcNow - sentTime).Milliseconds;

            ReceivedPacketData receivedPacketData = new ReceivedPacketData(ClientID, sentTime, latency);
            while (continueLoop)
            {
                try
                {
                    int HeaderValue = packetReader.ReadInt32();
                    int PackedDataSize = packetReader.ReadInt32();
                    byte[] PackedData = packetReader.ReadBytes(PackedDataSize);
                    if (ReadPacketHolders.TryGetValue(HeaderValue, out ReadPacket readPacket))
                    {
                        try
                        {
                            readPacket(PackedData, receivedPacketData);
                        }
                        catch (Exception ex) { Console.WriteLine("Erro em DyncamicPacketIO: {0} - {1} {2}", ex.Message, ex.Source, ex.StackTrace); }
                    }
                    else
                    {
                        ReceivedDataFromNonExistingHeaders.Add(HeaderValue);
                    }
                }
                catch (Exception ex)
                {
                    packetReader.Close();
                    continueLoop = false;
                    if (ex.GetType() != typeof(EndOfStreamException)) //Quando chegar no final da Stream, ele joga um EndOfStreamException, então sabemos que ela acabou sem outros erros
                        throw ex;
                }
            }
            if (ReceivedDataFromNonExistingHeaders.Count > 0)
            {
                string s = "Received data from these non existing headers: ";
                foreach (int i in ReceivedDataFromNonExistingHeaders)
                {
                    s += $"{i} ";
                }
                throw new Exception(s);
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
