using System.Collections.ObjectModel;

namespace ServerSide.PacketCouriers
{ 
    public class ConnectedClientDataDictionary : KeyedCollection<string, ConnectedClientData>
    {
        protected override string GetKeyForItem(ConnectedClientData clientData) => clientData.SocketID;
    }
    public struct ConnectedClientData
    {
        public string SocketID;
        public int OwnerID;
        public ConnectedClientData(string SocketID, int OwnerID)
        {
            this.SocketID = SocketID;
            this.OwnerID = OwnerID;
        }
    }
}
