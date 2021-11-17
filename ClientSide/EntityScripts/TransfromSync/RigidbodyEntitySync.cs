using SNet_Client.Sockets;
using SNet_Client.Utils;
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
        public ReferenceFrames referenceFrame = ReferenceFrames.Sun;
        protected Rigidbody referenceFrameRigidbody;

        private void Awake()
        {
            UniqueScriptIdentifingString = "RigidbodyEntitySync";
            Serialize = true;
        }
        protected override void Start()
        {
            base.Start();
            Transform t = ReferenceFrameLocator.GetReferenceFrame(referenceFrame);
            if (t != null)
                referenceFrameRigidbody = t.GetComponent<Rigidbody>();
        }

        public override void OnDeserialize(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            if (syncRigidbodyType == SyncRigidbody.VelocityOnly || syncRigidbodyType == SyncRigidbody.Both)
                rigidbody.velocity = RealVelocitySeenByReferenceFrame(reader.ReadVector3());
            if (syncRigidbodyType == SyncRigidbody.AngularMomentumOnly || syncRigidbodyType == SyncRigidbody.Both)
                rigidbody.angularVelocity = reader.ReadVector3();
        }

        private Vector3 VelocitySeenByReferenceFrame(Vector3 velocity)
        {
            if (referenceFrameRigidbody == null)
                return velocity;

            return velocity - referenceFrameRigidbody.velocity;
        }
        private Vector3 RealVelocitySeenByReferenceFrame(Vector3 velocity)
        {
            if (referenceFrameRigidbody == null)
                return velocity;

            return velocity + referenceFrameRigidbody.velocity;
        }

        public override void OnSerialize(ref PacketWriter writer)
        {
            if (syncRigidbodyType == SyncRigidbody.VelocityOnly || syncRigidbodyType == SyncRigidbody.Both)
                writer.Write(VelocitySeenByReferenceFrame(rigidbody.velocity));
            if (syncRigidbodyType == SyncRigidbody.AngularMomentumOnly || syncRigidbodyType == SyncRigidbody.Both)
                writer.Write(rigidbody.angularVelocity);
        }

        
    }
}
