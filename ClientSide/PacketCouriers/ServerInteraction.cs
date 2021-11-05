using System;

using ClientSide.Sockets;
using UnityEngine;

namespace ClientSide.PacketCouriers
{
    public class ServerInteraction : MonoBehaviour
    {
        private static ServerInteraction serverInteractionInstance;
        
        const string SI_LOCALIZATION_STRING = "ServerInteraction";
        public int HeaderValue { get; private set; }

        private int ourOwnerId;

        public static int GetOwnerID() => serverInteractionInstance.ourOwnerId;

        public static event Action OnReceiveOwnerID;
        public static event Action<int> OnServerRemoveOwnerID;

        
        private void Awake()
        {
            if (serverInteractionInstance != null)
            {
                Destroy(this);
                return;
            }
            serverInteractionInstance = this;
            
            HeaderValue = Client.GetClient().packetReceiver.AddPacketReader(SI_LOCALIZATION_STRING, ReadPacket);
        }
        private void ReadPacket(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            switch ((SIHeaders)reader.ReadByte())
            {
                case SIHeaders.CONNECTED:
                    ReadConnectedData(ref reader);
                    break;
                case SIHeaders.OWNER_ID_REMOVED:
                    ReadOwnerIdRemovedData(ref reader);
                    break;
            }
        }
        private void ReadConnectedData(ref PacketReader reader)
        {
            ourOwnerId = reader.ReadInt32();
            OnReceiveOwnerID?.Invoke();
        }
        private void ReadOwnerIdRemovedData(ref PacketReader reader)
        {
            int removedOwnerId = reader.ReadInt32();
            OnServerRemoveOwnerID?.Invoke(removedOwnerId);
        }

        enum SIHeaders : byte
        {
            CONNECTED = 0,
            OWNER_ID_REMOVED = 1,
        }
    }
}
