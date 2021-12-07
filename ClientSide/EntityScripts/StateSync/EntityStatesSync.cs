using System;
using System.Collections.Generic;
using SNet_Client.Sockets;

namespace SNet_Client.EntityScripts.StateSync
{
    
    public class EntityStatesSync : EntityScriptBehaviour
    {
        Dictionary<byte, EntityState> EntityStates;
        Dictionary<byte, ByteEntityState> ByteEntityStates;

        protected virtual void Awake()
        {
            EntityStates = new Dictionary<byte, EntityState>();
            ByteEntityStates = new Dictionary<byte, ByteEntityState>();

            UniqueScriptIdentifingString = "PlayerStatesSync";
            Serialize = true;
        }
        protected override void Start()
        {
            base.Start();
        }
        public void AddStateListener(byte stateName, bool value, Callback<bool> OnStateChange)
        {
            if (!EntityStates.ContainsKey(stateName))
                EntityStates.Add(stateName, new EntityState { State = value, OnStateChange = null });

            EntityStates[stateName].OnStateChange = Delegate.Combine(EntityStates[stateName].OnStateChange, OnStateChange);
        }
        public void ChangeValue(byte stateName, bool value)
        {
            if (EntityStates.ContainsKey(stateName))
            {
                if(value != EntityStates[stateName].State)
                    ((Callback<bool>)EntityStates[stateName].OnStateChange)?.Invoke(value);

                EntityStates[stateName].State = value;
            }
        }
        public void RemoveStateListener(byte stateName, Callback<bool> OnStateChange)
        {
            if (EntityStates.ContainsKey(stateName))
            {
                EntityStates[stateName].OnStateChange = Delegate.Remove(EntityStates[stateName].OnStateChange, OnStateChange);
                if (EntityStates[stateName].OnStateChange == null)
                    EntityStates.Remove(stateName);
            }
        }
        public void AddByteStateListener(byte stateName, byte value, Callback<byte> OnStateChange)
        {
            if (!ByteEntityStates.ContainsKey(stateName))
                ByteEntityStates.Add(stateName, new ByteEntityState { State = value, OnStateChange = null });

            ByteEntityStates[stateName].OnStateChange = Delegate.Combine(ByteEntityStates[stateName].OnStateChange, OnStateChange);
        }
        public void ChangeValue(byte stateName, byte value)
        {
            if (ByteEntityStates.ContainsKey(stateName))
            {
                if (value != ByteEntityStates[stateName].State)
                    ((Callback<byte>)ByteEntityStates[stateName].OnStateChange)?.Invoke(value);

                ByteEntityStates[stateName].State = value;
            }
        }
        public void RemoveByteStateListener(byte stateName, Callback<byte> OnStateChange)
        {
            if (ByteEntityStates.ContainsKey(stateName))
            {
                ByteEntityStates[stateName].OnStateChange = Delegate.Remove(ByteEntityStates[stateName].OnStateChange, OnStateChange);
                if (ByteEntityStates[stateName].OnStateChange == null)
                    ByteEntityStates.Remove(stateName);
            }
        }
        public override void OnDeserialize(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            //Bool States
            int statesAmount = reader.ReadInt32();
            for(int i =0;i< statesAmount; i++)
            {
                byte stateName = reader.ReadByte();
                bool state = reader.ReadBoolean();

                ChangeValue(stateName, state);
            }
            //Byte States
            statesAmount = reader.ReadInt32();
            for (int i = 0; i < statesAmount; i++)
            {
                byte stateName = reader.ReadByte();
                byte state = reader.ReadByte();

                ChangeValue(stateName, state);
            }
        }
        public override void OnSerialize(ref PacketWriter writer)
        {
            //Bool States
            writer.Write(EntityStates.Count);
            foreach (var pair in EntityStates)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value.State);
            }

            //Byte States
            writer.Write(ByteEntityStates.Count);
            foreach (var pair in ByteEntityStates)
            {
                writer.Write(pair.Key);
                writer.Write(pair.Value.State);
            }
        }
    }


    public class EntityState
    {
        public bool State;
        public Delegate OnStateChange;
    }
    public class ByteEntityState
    {
        public byte State;
        public Delegate OnStateChange;
    }
}

