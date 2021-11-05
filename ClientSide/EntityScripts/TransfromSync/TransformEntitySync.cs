using ClientSide.Sockets;
using ClientSide.Utils;
using UnityEngine;

namespace ClientSide.EntityScripts.TransfromSync
{
    public enum SyncTransform : byte
    {
        PositionOnly,
        RotationOnly,
        ScaleOnly,
        PositionAndRotationOnly,
        All
    }
    public class TransformEntitySync : EntityScriptBehaviour
    {
        public SyncTransform syncTransformType = SyncTransform.PositionAndRotationOnly;
        public ReferenceFrames referenceFrame = ReferenceFrames.Sun;
        protected Transform referenceFrameTransform;

        private void Awake()
        {
            UniqueScriptIdentifingString = "TransformEntitySync";
            Serialize = true;
        }
        protected override void Start()
        {
            base.Start();
            referenceFrameTransform = ReferenceFrameLocator.GetReferenceFrame(referenceFrame);
            Debug.Log(string.Format("Reference Type {0} e Sync Type {1}", syncTransformType, referenceFrame));
        }

        public override void OnDeserialize(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            Debug.Log(reader.ReadString());
            //Vector3 referencePosition = Vector3.zero;
            //if (referenceFrameTransform != null)
            //    referencePosition = referenceFrameTransform.position;

            //if (syncTransformType == SyncTransform.PositionOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
            //    transform.position = reader.ReadVector3() + referencePosition;
            //if (syncTransformType == SyncTransform.RotationOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
            //    transform.rotation = reader.ReadQuaternion();
            //if (syncTransformType == SyncTransform.ScaleOnly || syncTransformType == SyncTransform.All)
            //    transform.localScale = reader.ReadVector3();
        }
        int times = 0;
        public override void OnSerialize(ref PacketWriter writer)
        {
            writer.Write(string.Format("Baba Boie {0}", times));
            times++;
            //Vector3 referencePosition = Vector3.zero;
            //if (referenceFrameTransform != null)
            //    referencePosition = referenceFrameTransform.position;

            //if (syncTransformType == SyncTransform.PositionOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
            //    writer.Write(transform.position - referencePosition);
            //if (syncTransformType == SyncTransform.RotationOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
            //    writer.Write(transform.rotation);
            //if (syncTransformType == SyncTransform.ScaleOnly || syncTransformType == SyncTransform.All)
            //    writer.Write(transform.localScale);
        }
    }
}
