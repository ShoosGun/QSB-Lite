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
    
    public class RigidbodyEntitySync : EntityScriptBehaviour 
    {
        public SyncRigidbody syncRigidbodyType;
        public ReferenceFrames referenceFrame = ReferenceFrames.Sun;
        protected Rigidbody referenceFrameRigidbody;

        protected virtual void Awake()
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

        protected virtual void FixedUpdate()
        {
            rigidbody.velocity = RealVelocitySeenByReferenceFrame(latestVelocity);
            rigidbody.angularVelocity = AngularVelocityToReferenceFrame(lastestAngularVelocity);
        }

        Vector3 latestVelocity;
        Vector3 lastestAngularVelocity;
        public override void OnDeserialize(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            if (syncRigidbodyType == SyncRigidbody.VelocityOnly || syncRigidbodyType == SyncRigidbody.Both)
                latestVelocity = reader.ReadVector3();
            if (syncRigidbodyType == SyncRigidbody.AngularMomentumOnly || syncRigidbodyType == SyncRigidbody.Both)
                lastestAngularVelocity = reader.ReadVector3();
        }

        private Vector3 VelocitySeenByReferenceFrame(Vector3 velocity)
        {
            if (referenceFrameRigidbody == null)
                return velocity;

            return velocity - referenceFrameRigidbody.velocity;
        }
        private Vector3 RealVelocitySeenByReferenceFrame(Vector3 localVelocity)
        {
            if (referenceFrameRigidbody == null)
                return localVelocity;

            return localVelocity + referenceFrameRigidbody.velocity;
        }
        private Vector3 InverseAngularVelocityToReferenceFrame(Vector3 angularVelocity)
        {
            if (referenceFrameRigidbody == null)
                return angularVelocity;

            return referenceFrameRigidbody.transform.InverseTransformDirection(angularVelocity);
        }
        private Vector3 AngularVelocityToReferenceFrame(Vector3 localAngularVelocity)
        {
            if (referenceFrameRigidbody == null)
                return localAngularVelocity;

            return referenceFrameRigidbody.transform.TransformDirection(localAngularVelocity);
        }

        public override void OnSerialize(ref PacketWriter writer)
        {
            if (syncRigidbodyType == SyncRigidbody.VelocityOnly || syncRigidbodyType == SyncRigidbody.Both)
                writer.Write(VelocitySeenByReferenceFrame(rigidbody.velocity));
            if (syncRigidbodyType == SyncRigidbody.AngularMomentumOnly || syncRigidbodyType == SyncRigidbody.Both)
                writer.Write(InverseAngularVelocityToReferenceFrame(rigidbody.angularVelocity));
        }

        
    }
}
