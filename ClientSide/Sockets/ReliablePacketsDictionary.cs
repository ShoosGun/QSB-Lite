using System.Collections.ObjectModel;

namespace SNet_Client.Sockets
{ 
    public class ReliablePacketHandler : KeyedCollection<int, ReliablePacket>
    {
        int NextGeneratedID = int.MinValue;

        public void Add(byte[] packetData, out int packetID)
        {
            packetID = NextGeneratedID;

            if (NextGeneratedID == int.MaxValue)
                NextGeneratedID = int.MinValue;
            else
                NextGeneratedID++;

            var rP = new ReliablePacket(packetID, packetData);

            Add(rP);
        }

        protected override int GetKeyForItem(ReliablePacket reliablePacket) => reliablePacket.PacketID;

        public bool ReceptorReceivedData(int PacketID) 
        {
            if (Dictionary.TryGetValue(PacketID, out ReliablePacket reliablePacket))
            {
                Remove(reliablePacket);
                return true;
            }

            return false;
        }
    }
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
}
