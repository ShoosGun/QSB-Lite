using System;
using System.Collections.Generic;

using SNet_Client.Sockets;
using UnityEngine;


namespace SNet_Client.PacketCouriers.Entities
{
    static class InstantiadableGameObjectsPrefabHub
    {
        public delegate NetworkedEntity EntityPrefab(Vector3 position, Quaternion rotation, long ownerID, params object[] InitializationData);

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
            Client.GetClient().MemberConnection += OnMemberConnection;
            Client.GetClient().MemberDisconnection += OnMemberDisconnection;
            Client.GetClient().Disconnection += OnDisconnection;
        }
        private void OnMemberConnection(long memberId)//TODO a entidade do player não esta sendo enviada!
        {
            if (!InstantiadableGameObjectsPrefabHub.ownersDictionary.TryGetNetworkedEntities(Client.GetClient().GetUserId(),
                out NetworkedEntity[] ourEntities))
                return;
            
            for (int i = 0; i < ourEntities.Length; i++)
            {
                NetworkedEntity entity = ourEntities[i];
                byte[] buffer = GetInstantiatedEntityData(entity.id, entity.prefabName,
                    entity.transform.position, entity.transform.rotation, entity.initializationData);
                Client.GetClient().Send(memberId, buffer, HeaderValue, true);
            }
        }
        private void OnMemberDisconnection(long memberId)
        {
            InstantiadableGameObjectsPrefabHub.ownersDictionary.RemoveAndDestroyOwnerIDEntites(memberId);
        }
        private void OnDisconnection()
        {
            InstantiadableGameObjectsPrefabHub.DisconnectionReset();
        }

        private void OnDestroy()
        {
            if (client_EntityInitializer != this)
                return;

            InstantiadableGameObjectsPrefabHub.ResetInstantiadableGameObjectsPrefabHub();
            Client.GetClient().MemberConnection -= OnMemberConnection;
            Client.GetClient().MemberDisconnection -= OnMemberDisconnection;
            Client.GetClient().Disconnection -= OnDisconnection;
        }

        public void AddGameObjectPrefab(string gameObjectName, InstantiadableGameObjectsPrefabHub.EntityPrefab entityPrefab)
        {
            InstantiadableGameObjectsPrefabHub.AddPrefab(entityPrefab, gameObjectName);
        }
        public void InstantiateEntity(string prefabName, Vector3 position, Quaternion rotation, params object[] data)
        {
            if (InstantiadableGameObjectsPrefabHub.idsGenerator.TryGetID(out int id))
            {
                byte[] buffer = GetInstantiatedEntityData(id, prefabName, position, rotation, data);
                Client.GetClient().SendToAll(buffer, HeaderValue, true);
                InstantiateEntityFromServer(prefabName, id, Client.GetClient().GetUserId(), position, rotation, data);
            }
        }
        private byte[] GetInstantiatedEntityData(int id, string prefabName, Vector3 position, Quaternion rotation, object[] data) 
        {
            PacketWriter buffer = new PacketWriter();
            buffer.Write((byte)EntityInitializerHeaders.Instantiate);

            buffer.Write(id);
            buffer.Write(prefabName);

            PacketWriter initializationData = new PacketWriter();
            initializationData.WriteAsObjectArray(data);
            initializationData.Write(position);
            initializationData.Write(rotation);

            buffer.Write(initializationData.GetBytes());
            return buffer.GetBytes();
        }
        private void ReadEntityInstantiateData(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            int id = reader.ReadInt32();
            string prefabName = reader.ReadString();
            object[] intantiateData = reader.ReadObjectArray();
            Vector3 position = reader.ReadVector3();
            Quaternion rotation = reader.ReadQuaternion();

            InstantiateEntityFromServer(prefabName, id, receivedPacketData.SendingId, position, rotation, intantiateData);
        }
        private void InstantiateEntityFromServer(string prefabName, int ID, long ownerID, Vector3 position, Quaternion rotation, params object[] data)
        {
            if (!InstantiadableGameObjectsPrefabHub.instantiadableGOPrefabMethods.TryGetValue(prefabName, out InstantiadableGameObjectsPrefabHub.EntityPrefab prefab))
                throw new OperationCanceledException(string.Format("There is no GameObject in {0}", prefabName));

            NetworkedEntity networkedEntity = prefab(position, rotation, ownerID, data);
            if (networkedEntity == null)
                return;

            networkedEntity.id = ID;
            networkedEntity.ownerId = ownerID;
            networkedEntity.prefabName = prefabName;
            networkedEntity.initializationData = data;
            Debug.Log(string.Format("{0} {1} {2}", ownerID, ID, prefabName));
            if (!InstantiadableGameObjectsPrefabHub.ownersDictionary.AddEntity(networkedEntity))
            {
                Debug.Log(string.Format("The entity {0} {1} {2} already exists, destroying duplicate", ownerID, ID, prefabName));
                networkedEntity.ownerId = -1; //Setting it as having no owner so DestroyEntity doesn't get triggered
                Destroy(networkedEntity);
            }
        }
        
        public void DestroyEntity(NetworkedEntity networkedEntity)
        {
            if (networkedEntity.IsOurs())
            {
                PacketWriter buffer = new PacketWriter();
                buffer.Write((byte)EntityInitializerHeaders.Remove);
                buffer.Write(networkedEntity.id);
                Client.GetClient().SendToAll(buffer.GetBytes(), HeaderValue, true);
            }
        }        
        private void ReceiveRemoveEntity(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            int id = reader.ReadInt32();
            
            if (!InstantiadableGameObjectsPrefabHub.ownersDictionary.TryGetNetworkedEntity(receivedPacketData.SendingId, id, out NetworkedEntity networkedEntity))
                return;

            InstantiadableGameObjectsPrefabHub.ownersDictionary.RemoveEntity(networkedEntity);

            if(networkedEntity != null)
                Destroy(networkedEntity.gameObject);

            if (networkedEntity.IsOurs())
                InstantiadableGameObjectsPrefabHub.idsGenerator.ReturnID(networkedEntity.id);
        }

       
        public void SendEntityScriptsOnSerialization()
        {
            if (!InstantiadableGameObjectsPrefabHub.ownersDictionary.ContainsKey(Client.GetClient().GetUserId()))
                return;

            NetworkedEntityDictionary entities = InstantiadableGameObjectsPrefabHub.ownersDictionary[Client.GetClient().GetUserId()];
            
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
                PacketWriter desirializationData = new PacketWriter();
                desirializationData.Write(amountOfEntitiesThatWrote);
                desirializationData.Write(postFixWriter.GetBytes());

                PacketWriter writer = new PacketWriter();
                writer.Write((byte)EntityInitializerHeaders.EntitySerialization);
                writer.Write(desirializationData.GetBytes());

                Client.GetClient().SendToAll(writer.GetBytes(), HeaderValue, false);
            }
        }
        public void ReadEntityScriptsOnDeserialization(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            int count = reader.ReadInt32();
            for (int j = 0; j < count; j++)
            {
                int entityId = reader.ReadInt32();
                byte[] data = reader.ReadByteArray();

                if (InstantiadableGameObjectsPrefabHub.ownersDictionary.TryGetNetworkedEntity(receivedPacketData.SendingId, entityId, out NetworkedEntity entity))
                    entity.OnDeserializeEntity(data, receivedPacketData);
            }
        }
        public void ReadEntityMessage(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            int entityId = reader.ReadInt32();
            int scriptID = reader.ReadInt32();
            byte[] messageData = reader.ReadByteArray();

            if (InstantiadableGameObjectsPrefabHub.ownersDictionary.TryGetNetworkedEntity(receivedPacketData.SendingId, entityId, out NetworkedEntity entity))
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
            writer.Write(entity.id);
            writer.Write(scriptID);
            writer.WriteAsArray(messageData);

            Client.GetClient().SendToAll(writer.GetBytes(), HeaderValue, true);
        }

        public void ReadPacket(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            switch ((EntityInitializerHeaders)reader.ReadByte())
            {
                case EntityInitializerHeaders.Instantiate:
                    ReadEntityInstantiateData(ref reader, receivedPacketData);
                    break;
                case EntityInitializerHeaders.Remove:
                    ReceiveRemoveEntity(ref reader, receivedPacketData);
                    break;
                case EntityInitializerHeaders.EntitySerialization:
                    ReadEntityScriptsOnDeserialization(ref reader, receivedPacketData);
                    break;
                case EntityInitializerHeaders.EntityMessage:
                    ReadEntityMessage(ref reader, receivedPacketData);
                    break;
            }
        }

        private void Update()
        {
            SendEntityScriptsOnSerialization();
        }

        enum EntityInitializerHeaders : byte
        {
            Instantiate,
            Remove,
            EntitySerialization,
            EntityMessage
        }
    }
}

