using System;
using System.Collections.Generic;

using SNet_Client.Sockets;
using UnityEngine;


namespace SNet_Client.PacketCouriers.Entities
{
    static class InstantiadableGameObjectsPrefabHub
    {
        public delegate NetworkedEntity EntityPrefab(Vector3 position, Quaternion rotation, int ownerID, params object[] InitializationData);

        public static readonly Dictionary<string, EntityPrefab> instantiadableGOPrefabMethods = new Dictionary<string, EntityPrefab>();
        public static readonly NetworkedEntityOwnerDictionary ownersDictionary = new NetworkedEntityOwnerDictionary();

        public static EntityIdsGenerator idsGenerator = new EntityIdsGenerator(100);

        public static void AddPrefab(EntityPrefab entityPrefab, string prefabName)
        {
            if (instantiadableGOPrefabMethods.ContainsKey(prefabName))
                throw new OperationCanceledException(string.Format("There is already a GameObject in {0}", prefabName));

            instantiadableGOPrefabMethods.Add(prefabName, entityPrefab);
        }

        public static void ResetInstantiadableGameObjectsPrefabHub()
        {
            instantiadableGOPrefabMethods.Clear();
            DisconnectionReset();
        }
        public static void DisconnectionReset()
        {
            ownersDictionary.ClearAndDestroyAllEntites();
            idsGenerator.Reset();
        }
    }

    class EntityInitializer : MonoBehaviour
    {
        public static EntityInitializer client_EntityInitializer;
        
        const string EI_LOCALIZATION_STRING = "EntityInitializer";
        public int HeaderValue { get; private set; }

        private void Awake()
        {
            if (client_EntityInitializer != null)
            {
                Destroy(this);
                return;
            }
            client_EntityInitializer = this;
            
            HeaderValue = Client.GetClient().packetReceiver.AddPacketReader(EI_LOCALIZATION_STRING, ReadPacket);            
            ServerInteraction.OnServerRemoveOwnerID += ServerInteraction_OnServerRemoveOwnerID;
            Client.GetClient().Disconnection += EntityInitializer_Disconnection;
        }


        private void EntityInitializer_Disconnection()
        {
            InstantiadableGameObjectsPrefabHub.DisconnectionReset();
        }
        private void ServerInteraction_OnServerRemoveOwnerID(int removedOwnerID)
        {
            InstantiadableGameObjectsPrefabHub.ownersDictionary.RemoveAndDestroyOwnerIDEntites(removedOwnerID);
        }

        private void OnDestroy()
        {
            if (client_EntityInitializer != this)
                return;

            InstantiadableGameObjectsPrefabHub.ResetInstantiadableGameObjectsPrefabHub();
            ServerInteraction.OnServerRemoveOwnerID -= ServerInteraction_OnServerRemoveOwnerID;
        }

        public void AddGameObjectPrefab(string gameObjectName, InstantiadableGameObjectsPrefabHub.EntityPrefab entityPrefab)
        {
            InstantiadableGameObjectsPrefabHub.AddPrefab(entityPrefab, gameObjectName);
        }
        public void InstantiateEntity(string prefabName, Vector3 position, Quaternion rotation, InstantiateType instantiateType, params object[] data)
        {
            if (InstantiadableGameObjectsPrefabHub.idsGenerator.TryGetID(out int id))
            {
                PacketWriter buffer = new PacketWriter();
                buffer.Write((byte)EntityInitializerHeaders.Instantiate);
                buffer.Write((byte)instantiateType);

                buffer.Write(id);
                buffer.Write(prefabName);

                PacketWriter initializationData = new PacketWriter();
                initializationData.WriteAsObjectArray(data);
                initializationData.Write(position);
                initializationData.Write(rotation);

                buffer.WriteAsArray(initializationData.GetBytes());

                Client.GetClient().SendReliable(buffer.GetBytes(), HeaderValue);
            }
        }

        private void InstantiateEntityFromServer(string prefabName, int ID, int ownerID, Vector3 position, Quaternion rotation, params object[] data)
        {
            if (!InstantiadableGameObjectsPrefabHub.instantiadableGOPrefabMethods.TryGetValue(prefabName, out InstantiadableGameObjectsPrefabHub.EntityPrefab prefab))
                throw new OperationCanceledException(string.Format("There is no GameObject in {0}", prefabName));

            NetworkedEntity networkedEntity = prefab(position, rotation, ownerID, data);
            networkedEntity.id = ID;
            networkedEntity.ownerId = ownerID;
            Debug.Log(string.Format("{0} {1} {2}", ownerID, ID, prefabName));
            
            InstantiadableGameObjectsPrefabHub.ownersDictionary.AddEntity(networkedEntity);
        }

        public void DestroyEntity(NetworkedEntity networkedEntity)
        {
            if (networkedEntity.IsOurs())
            {
                PacketWriter buffer = new PacketWriter();
                buffer.Write((byte)EntityInitializerHeaders.Remove);
                buffer.Write(networkedEntity.id);
                Client.GetClient().SendReliable(buffer.GetBytes(), HeaderValue);
            }
        }
        
        private void ReceiveRemoveEntity(ref PacketReader reader)
        {
            int ownerID = reader.ReadInt32();
            int id = reader.ReadInt32();
            
            if (!InstantiadableGameObjectsPrefabHub.ownersDictionary.TryGetNetworkedEntity(ownerID, id, out NetworkedEntity networkedEntity))
                return;

            InstantiadableGameObjectsPrefabHub.ownersDictionary.RemoveEntity(networkedEntity);

            if(networkedEntity != null)
                Destroy(networkedEntity.gameObject);

            if (networkedEntity.IsOurs())
                InstantiadableGameObjectsPrefabHub.idsGenerator.ReturnID(networkedEntity.id);
        }

        private void ReadEntityInstantiateData(ref PacketReader reader)
        {
            //Parte adicionada pelo servidor
            int ownerID = reader.ReadInt32();

            //Parte advinda de um cliente 
            int id = reader.ReadInt32();
            string prefabName = reader.ReadString();
            object[] intantiateData = reader.ReadObjectArray();
            Vector3 position = reader.ReadVector3();
            Quaternion rotation = reader.ReadQuaternion();

            InstantiateEntityFromServer(prefabName, id, ownerID, position, rotation, intantiateData);
        }
        public void SendEntityScriptsOnSerialization()
        {
            if (!InstantiadableGameObjectsPrefabHub.ownersDictionary.ContainsKey(ServerInteraction.GetOwnerID()))
                return;

            NetworkedEntityDictionary entities = InstantiadableGameObjectsPrefabHub.ownersDictionary[ServerInteraction.GetOwnerID()];
            
            PacketWriter postFixWriter = new PacketWriter();
            int amountOfEntitiesThatWrote = 0;

            foreach (var entity in entities)
            {
                PacketWriter entityWriter = new PacketWriter();
                entity.OnSerializeEntity(ref entityWriter);
                byte[] data = entityWriter.GetBytes();
                if (data.Length > 0)
                {
                    postFixWriter.Write(entity.id);
                    postFixWriter.WriteAsArray(data);
                    amountOfEntitiesThatWrote++;
                }
            }
            if (amountOfEntitiesThatWrote > 0)
            {
                PacketWriter writer = new PacketWriter();
                writer.Write((byte)EntityInitializerHeaders.EntitySerialization);
                PacketWriter desirializationData = new PacketWriter();
                desirializationData.Write(amountOfEntitiesThatWrote);
                desirializationData.Write(postFixWriter.GetBytes());
                writer.WriteAsArray(desirializationData.GetBytes());

                Client.GetClient().Send(writer.GetBytes(), HeaderValue);
            }
        }
        public void ReadEntityScriptsOnDeserialization(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            int ownerID = reader.ReadInt32();
            int count = reader.ReadInt32();
            for (int j = 0; j < count; j++)
            {
                int entityId = reader.ReadInt32();
                byte[] data = reader.ReadByteArray();

                if (InstantiadableGameObjectsPrefabHub.ownersDictionary.TryGetNetworkedEntity(ownerID, entityId, out NetworkedEntity entity))
                    entity.OnDeserializeEntity(data, receivedPacketData);
            }
        }
        public void ReadEntityMessage(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            int ownerID = reader.ReadInt32();
            int entityId = reader.ReadInt32();
            int scriptID = reader.ReadInt32();
            byte[] messageData = reader.ReadByteArray();

            if (InstantiadableGameObjectsPrefabHub.ownersDictionary.TryGetNetworkedEntity(ownerID, entityId, out NetworkedEntity entity))
                entity.OnReceiveMessage(messageData, scriptID, receivedPacketData);
        }
        public void SendEntityMessage(NetworkedEntity entity, int scriptID, byte[] messageData)
        {
            if (entity == null)
                throw new OperationCanceledException("The entity used is null");

            if (!entity.ComponentsToIO.ContainsKey(scriptID))
                throw new OperationCanceledException(string.Format("The script with ID {0} doesn't exist in this entity", scriptID));

            PacketWriter writer = new PacketWriter();
            writer.Write((byte)EntityInitializerHeaders.EntityMessage);
            writer.Write(entity.ownerId);
            writer.Write(entity.id);
            writer.Write(scriptID);
            writer.WriteAsArray(messageData);

            Client.GetClient().Send(writer.GetBytes(), HeaderValue);
        }
        public void RequestRefreshOfEntities()
        {
            InstantiadableGameObjectsPrefabHub.DisconnectionReset();
            Client.GetClient().SendReliable(BitConverter.GetBytes((byte)EntityInitializerHeaders.RefreshInstantiatedEntities), HeaderValue);
        }

        public void ReadPacket(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            switch ((EntityInitializerHeaders)reader.ReadByte())
            {
                case EntityInitializerHeaders.Instantiate:
                    //Debug.Log("Nova Entidade");
                    ReadEntityInstantiateData(ref reader);
                    break;
                case EntityInitializerHeaders.Remove:
                    ReceiveRemoveEntity(ref reader);
                    //Debug.Log("Remover Entidade");
                    break;
                case EntityInitializerHeaders.EntitySerialization:
                    ReadEntityScriptsOnDeserialization(ref reader, receivedPacketData);
                    //Debug.Log("Update de Entidades");
                    break;
                case EntityInitializerHeaders.EntityMessage:
                    ReadEntityMessage(ref reader, receivedPacketData);
                    break;
            }
        }

        private void FixedUpdate()
        {
            SendEntityScriptsOnSerialization();
        }

        enum EntityInitializerHeaders : byte
        {
            Instantiate,
            Remove,
            EntitySerialization,
            EntityMessage,
            RefreshInstantiatedEntities
        }

        public enum InstantiateType : byte
        {
            NotBuffered,
            Buffered
        }
    }
}

