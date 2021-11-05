using System;
using System.Collections.Generic;
using ServerSide.Sockets.Servers;
using ServerSide.Utils;


namespace ServerSide.PacketCouriers
{
    public class EntityRepasser
    {
        const string EI_LOCALIZATION_STRING = "EntityInitializer";
        public int HeaderValue { get; private set; }
        private Server server;

        private Dictionary<string, EntityOwnerBuffer> entityBuffer;

        private ServerInteraction serverInteraction;

        public EntityRepasser(Server server, ServerInteraction serverInteraction)
        {
            this.server = server;

            HeaderValue = server.packetReceiver.AddPacketReader(EI_LOCALIZATION_STRING, ReadPacket);

            this.serverInteraction = serverInteraction;
            this.serverInteraction.OnAfterSendOwnerID += ServerInteraction_OnAfterSendOwnerID;
            server.OnClientDisconnection += Server_DisconnectionID;

            entityBuffer = new Dictionary<string, EntityOwnerBuffer>();
        }

        private void ServerInteraction_OnAfterSendOwnerID(ConnectedClientData clientData)
        {
            entityBuffer.Add(clientData.SocketID, new EntityOwnerBuffer(clientData));
            SendBufferedEntitiesToNewClient(clientData.SocketID);
        }
        private void Server_DisconnectionID(string clientID)
        {
            entityBuffer.Remove(clientID);
        }
        private void TransmitAddEntity(InstantiateType instantiateType, ReceivedPacketData receivedPacketData, byte[] data)
        {
            //No byte[] data:
            //0 -> Header
            //1 -> InstantiateType
            //[2,6] -> ID
            //(6,n] -> Resto da data
            if (entityBuffer.TryGetValue(receivedPacketData.ClientID, out EntityOwnerBuffer ownerBuffer))
            {
                int id = BitConverter.ToInt32(data, 2);
                byte[] initializationDataWithID = new byte[data.Length - 2];
                Array.Copy(data, 2, initializationDataWithID, 0, initializationDataWithID.Length);

                if (instantiateType == InstantiateType.Buffered && !ownerBuffer.entityDatas.Contains(id))
                    ownerBuffer.entityDatas.Add(new BufferedEntityData(id, initializationDataWithID));

                TransmitEntityInitializationData(ownerBuffer.ClientData.OwnerID, initializationDataWithID);
            }
        }
        public void TransmitEntityInitializationData(int ownerID, byte[] initializationDataWithID, string clientId = "")
        {
            PacketWriter buffer = new PacketWriter();
            buffer.Write((byte)EntityInitializerHeaders.Instantiate);
            buffer.Write(ownerID);
            buffer.Write(initializationDataWithID);
            if (clientId.Length > 1)
                server.Send(buffer.GetBytes(), HeaderValue, clientId);
            else
                server.SendAll(buffer.GetBytes(), HeaderValue);
            Console.WriteLine("Enviando dado da entidade de {0} (tamanho {1})para os clientes", ownerID, initializationDataWithID.Length);
        }
        private void SendBufferedEntitiesToNewClient(string clientID)
        {
            foreach(var entities in entityBuffer)
            {
                EntityOwnerBuffer buffer = entities.Value;
                foreach (var entity in buffer.entityDatas)
                {
                    TransmitEntityInitializationData(buffer.ClientData.OwnerID, entity.initializationData, clientID);
                }
            }
        }

        public void ReadPacket(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            switch ((EntityInitializerHeaders)reader.ReadByte())
            {
                case EntityInitializerHeaders.Instantiate:
                    //TransmitAddEntity((InstantiateType)reader.ReadByte(), receivedPacketData, data);
                    break;
                case EntityInitializerHeaders.Remove:
                    TransmitRemoveEntity(ref reader, receivedPacketData);
                    break;
                case EntityInitializerHeaders.EntitySerialization:
                    //TransmitEntityScriptsOnDeserialization(ref reader, receivedPacketData, data);
                    break;
            }
        }
        
        private void TransmitEntityScriptsOnDeserialization(ref PacketReader reader, ReceivedPacketData receivedPacketData, byte[] data)
        {
            if (serverInteraction.TryGetOwnerID(receivedPacketData.ClientID, out int ownerID))
            {
                PacketWriter buffer = new PacketWriter();
                buffer.Write((byte)EntityInitializerHeaders.EntitySerialization);
                buffer.Write(ownerID);
                //Skip the first byte used by the header
                buffer.Write(data, 1, data.Length - 1);
                server.SendAll(buffer.GetBytes(), HeaderValue);
            }
        }
        private void TransmitRemoveEntity(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            if (entityBuffer.TryGetValue(receivedPacketData.ClientID, out EntityOwnerBuffer ownerBuffer))
            {
                int entityID = reader.ReadInt32();
                ownerBuffer.entityDatas.Remove(entityID);

                PacketWriter buffer = new PacketWriter();
                buffer.Write((byte)EntityInitializerHeaders.Remove);
                buffer.Write(ownerBuffer.ClientData.OwnerID);
                buffer.Write(entityID);
                server.SendAll(buffer.GetBytes(), HeaderValue);
                Console.WriteLine("Entidade de {0}, ID {1}, foi removida", receivedPacketData.ClientID, entityID);
            }
        }

        enum EntityInitializerHeaders : byte
        {
            Instantiate,
            Remove,
            EntitySerialization
        }
        public enum InstantiateType : byte
        {
            NotBuffered,
            Buffered
        }
    }
}

