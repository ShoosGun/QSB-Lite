using UnityEngine;

using SNet_Client.Utils;
using SNet_Client.Sockets;

namespace SNet_Client.EntityScripts.TransfromSync
{
    public class DynamicReferenceRigidbodyEntitySync : RigidbodyEntitySync
    {
        protected override void Awake()
        {
            base.Awake();
            UniqueScriptIdentifingString = "DynamicReferenceRigidbodyEntitySync";
        }

        public void ChangeReferenceFrame(ReferenceFrames referenceFrame, Transform referenceFrameTransform)
        {
            this.referenceFrame = referenceFrame;
            referenceFrameRigidbody = referenceFrameTransform.GetComponent<Rigidbody>();
        }

        public override void OnDeserialize(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            ReferenceFrames newReferenceFrame = (ReferenceFrames)reader.ReadByte();
            if(newReferenceFrame != referenceFrame)
            {
                referenceFrame = newReferenceFrame;
                Transform t = ReferenceFrameLocator.GetReferenceFrame(referenceFrame);
                if (t != null)
                    referenceFrameRigidbody = t.GetComponent<Rigidbody>();
            }

            base.OnDeserialize(ref reader, receivedPacketData);
        }
        public override void OnSerialize(ref PacketWriter writer)
        {
            writer.Write((byte)referenceFrame);

            base.OnSerialize(ref writer);
        }
    }
}
