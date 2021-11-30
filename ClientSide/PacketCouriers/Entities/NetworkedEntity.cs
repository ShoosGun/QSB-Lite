using SNet_Client.EntityScripts;
using SNet_Client.Sockets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace SNet_Client.PacketCouriers.Entities
{
    public class NetworkedEntity : MonoBehaviour
    {
        public Dictionary<int, EntityScriptBehaviour> ComponentsToIO = new Dictionary<int, EntityScriptBehaviour>();

        public int id;
        public int ownerId;

        private void Awake()
        {
            foreach (var script in GetComponents<EntityScriptBehaviour>())
                SetEntityScript(script);
        }

        public bool IsOurs() => ServerInteraction.GetOwnerID() == ownerId;
        
        public T AddEntityScript<T>() where T : EntityScriptBehaviour
        {
            T script = gameObject.AddComponent<T>();
            if (ComponentsToIO.ContainsKey(script.GetScriptID()))
                throw new OperationCanceledException(string.Format("The Script id from {0} ({1}) is already being used by another script", script.GetType(), script.GetScriptID()));

            ComponentsToIO.Add(script.GetScriptID(), script);
            return script;
        }
        public void SetEntityScript<T>(T script) where T : EntityScriptBehaviour
        {
            if (ComponentsToIO.ContainsKey(script.GetScriptID()))
                throw new OperationCanceledException(string.Format("The Script id from {0} ({1}) is already being used by another script", script.GetType(), script.GetScriptID()));

            if (gameObject.GetComponent<T>() == script)
                ComponentsToIO.Add(script.GetScriptID(), script);
        }
        public void RemoveEntityScript(EntityScriptBehaviour script)
        {
            if (!ComponentsToIO.Remove(script.GetScriptID()))
                Debug.LogWarning(string.Format("The script {0}({1}) isn't being synced in {1}", script.GetType(), script.GetScriptID(), transform.name), this);
        }
        
        public void SendMessage(EntityScriptBehaviour receiverEquivalent, byte[] message)
        {
            EntityInitializer.client_EntityInitializer.SendEntityMessage(this, receiverEquivalent.GetScriptID(), message);
        }
        public void OnReceiveMessage(byte[] messageData, int scriptID, ReceivedPacketData receivedPacketData)
        {
            if (ComponentsToIO.TryGetValue(scriptID, out var script))
            {
                script.OnReceiveMessage(messageData, receivedPacketData);
            }
            else
            {
                Debug.LogError(string.Format("The script with id {0} couldn't be found for this message", scriptID));
            }
        }

        public void OnSerializeEntity(ref PacketWriter writer)
        {
            if (ComponentsToIO.Count <= 0)
                return;

            int serializedScripts = 0;
            PacketWriter scriptSerializeBuffer = new PacketWriter();
            foreach (var networkedScript in ComponentsToIO)
            {
                if (networkedScript.Value.IsToSerialize())
                {
                    scriptSerializeBuffer.Write(networkedScript.Key);
                    networkedScript.Value.OnSerialize(ref scriptSerializeBuffer);
                    serializedScripts++;
                }
            }

            writer.Write(serializedScripts);
            writer.Write(scriptSerializeBuffer.GetBytes());
        }
        public void OnDeserializeEntity(byte[] data, ReceivedPacketData receivedPacketData)
        {
            PacketReader reader = new PacketReader(data);
            int count = reader.ReadInt32();
            for (int i = 0; i < count; i++)
            {
                int scriptId = reader.ReadInt32();
                if (ComponentsToIO.TryGetValue(scriptId, out var script))
                {
                    script.OnDeserialize(ref reader, receivedPacketData);
                }
                else
                {
                    Debug.LogError(string.Format("The script with id {0} couldn't be found", scriptId));
                    break;
                }
            }
        }

        protected void OnDestroy()
        {
            EntityInitializer.client_EntityInitializer.DestroyEntity(this);
        }
    }
}