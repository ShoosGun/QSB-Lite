using System.Collections.ObjectModel;

namespace ServerSide.PacketCouriers
{ 

    public struct EntityOwnerBuffer
    {
        public ConnectedClientData ClientData;
        public BufferedEntityDataDictionary entityDatas;
        public EntityOwnerBuffer(ConnectedClientData clientData)
        {
            ClientData = clientData;
            entityDatas = new BufferedEntityDataDictionary();
        }
    }
    public class BufferedEntityDataDictionary : KeyedCollection<int, BufferedEntityData>
    {
        protected override int GetKeyForItem(BufferedEntityData entity) => entity.ID;
    }
    public struct BufferedEntityData
    {
        public int ID;
        public byte[] initializationData;
        public BufferedEntityData(int ID, byte[] initializationData)
        {
            this.ID = ID;
            this.initializationData = initializationData;
        }
    }
}
