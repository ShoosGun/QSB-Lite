using UnityEngine;

using SNet_Client.Utils;
using System.Collections.Generic;
using SNet_Client.Sockets;

namespace SNet_Client.EntityScripts.TransfromSync
{
    public class DynamicReferenceTransformEntitySync : TransformEntitySync
    {
        protected override void Awake()
        {
            base.Awake();
            UniqueScriptIdentifingString = "DynamicReferenceTransformEntitySync";
        }

        public void ChangeReferenceFrame(ReferenceFrames referenceFrame, Transform referenceFrameTransform)
        {
            this.referenceFrame = referenceFrame;
            this.referenceFrameTransform = referenceFrameTransform;
        }

        public override void OnDeserialize(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            ReferenceFrames newReferenceFrame = (ReferenceFrames)reader.ReadByte();
            if (newReferenceFrame != referenceFrame)
            {
                Debug.Log(newReferenceFrame);
                referenceFrame = newReferenceFrame;
                referenceFrameTransform = ReferenceFrameLocator.GetReferenceFrame(referenceFrame);
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
