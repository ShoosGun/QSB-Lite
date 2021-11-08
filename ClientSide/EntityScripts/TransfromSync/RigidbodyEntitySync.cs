using SNet_Client.Sockets;
using UnityEngine;

namespace SNet_Client.EntityScripts.TransfromSync
{
    public enum SyncRigidbody : byte
    {
        VelocityOnly,
        AngularMomentumOnly,
        Both
    }
    public class RigidbodyEntitySync : EntityScriptBehaviour //Usar o segundo byte do primeiro das informacoes para suas informações de inicialização
    {
        public SyncRigidbody syncRigidbodyType;

        private void Awake()
        {
            UniqueScriptIdentifingString = "RigidbodyEntitySync";
            Serialize = true;
        }

        public override void OnDeserialize(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            if (syncRigidbodyType == SyncRigidbody.VelocityOnly || syncRigidbodyType == SyncRigidbody.Both)
                rigidbody.velocity = reader.ReadVector3();
            if (syncRigidbodyType == SyncRigidbody.AngularMomentumOnly || syncRigidbodyType == SyncRigidbody.Both)
                rigidbody.angularVelocity = reader.ReadVector3();
        }
        public override void OnSerialize(ref PacketWriter writer)
        {
            if (syncRigidbodyType == SyncRigidbody.VelocityOnly || syncRigidbodyType == SyncRigidbody.Both)
                writer.Write(rigidbody.velocity);
            if (syncRigidbodyType == SyncRigidbody.AngularMomentumOnly || syncRigidbodyType == SyncRigidbody.Both)
                writer.Write(rigidbody.angularVelocity);
        }
    }
}
