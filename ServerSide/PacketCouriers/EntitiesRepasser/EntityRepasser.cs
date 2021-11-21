using System;
using System.Collections.Generic;
using SNet_Server.Sockets;
using SNet_Server.Utils;


namespace SNet_Server.PacketCouriers
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

            HeaderValue = server.PacketReceiver.AddPacketReader(EI_LOCALIZATION_STRING, ReadPacket);

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
        private void TransmitAddEntity(ReceivedPacketData receivedPacketData, ref PacketReader reader)
        {
            if (entityBuffer.TryGetValue(receivedPacketData.ClientID, out EntityOwnerBuffer ownerBuffer))
            {
                InstantiateType instantiateType = (InstantiateType)reader.ReadByte();
                int id = reader.ReadInt32();
                string prefab = reader.ReadString();
                byte[] initializationData = reader.ReadByteArray();

                if (instantiateType == InstantiateType.Buffered && !ownerBuffer.entityDatas.Contains(id))
                    ownerBuffer.entityDatas.Add(new BufferedEntityData(id, prefab, initializationData));

                TransmitEntityInitializationData(ownerBuffer.ClientData.OwnerID, id, prefab, initializationData);
            }
        }
        public void TransmitEntityInitializationData(int ownerID, int id, string prefab, byte[] initializationData, string clientId = "")
        {
            PacketWriter buffer = new PacketWriter();
            buffer.Write((byte)EntityInitializerHeaders.Instantiate);
            buffer.Write(ownerID);
            buffer.Write(id);
            buffer.Write(prefab);
            buffer.Write(initializationData);
            if (!string.IsNullOrEmpty(clientId))
                server.SendReliable(buffer.GetBytes(), HeaderValue, clientId);
            else
                server.SendAllReliable(buffer.GetBytes(), HeaderValue);

            Console.WriteLine("Enviando dado da entidade de {0} (ID {1} Prefab {2}) (tamanho {3}) para os clientes", ownerID, id, prefab, initializationData.Length);
        }

        private void SendBufferedEntitiesToNewClient(string clientID)
        {
            foreach(var entities in entityBuffer)
            {
                EntityOwnerBuffer buffer = entities.Value;
                foreach (var entity in buffer.entityDatas)
                {
                    TransmitEntityInitializationData(buffer.ClientData.OwnerID, entity.ID, entity.Prefab, entity.InitializationData, clientID);
                }
            }
        }

        public void ReadPacket(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            switch ((EntityInitializerHeaders)reader.ReadByte())
            {
                case EntityInitializerHeaders.Instantiate:
                    TransmitAddEntity(receivedPacketData, ref reader);
                    break;
                case EntityInitializerHeaders.Remove:
                    TransmitRemoveEntity(ref reader, receivedPacketData);
                    break;
                case EntityInitializerHeaders.EntitySerialization:
                    TransmitEntityScriptsOnDeserialization(ref reader, receivedPacketData);
                    break;
                case EntityInitializerHeaders.RefreshInstantiatedEntities:
                    Console.WriteLine("{0} requesitou refresh de entidades", receivedPacketData.ClientID);
                    SendBufferedEntitiesToNewClient(receivedPacketData.ClientID);
                    break;
            }
        }
        
        private void TransmitEntityScriptsOnDeserialization(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            if (serverInteraction.TryGetOwnerID(receivedPacketData.ClientID, out int ownerID))
            {
                PacketWriter buffer = new PacketWriter();
                buffer.Write((byte)EntityInitializerHeaders.EntitySerialization);
                buffer.Write(ownerID);
                buffer.Write(reader.ReadByteArray());
                server.SendAll(buffer.GetBytes(), HeaderValue, receivedPacketData.ClientID);
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
                server.SendAllReliable(buffer.GetBytes(), HeaderValue);
                Console.WriteLine("Entidade de {0}, ID {1}, foi removida", receivedPacketData.ClientID, entityID);
            }
        }

        enum EntityInitializerHeaders : byte
        {
            Instantiate,
            Remove,
            EntitySerialization,
            RefreshInstantiatedEntities
        }
        public enum InstantiateType : byte
        {
            NotBuffered,
            Buffered
        }
    }
}

