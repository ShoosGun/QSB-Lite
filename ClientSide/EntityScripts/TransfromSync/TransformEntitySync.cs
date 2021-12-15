using SNet_Client.Sockets;
using SNet_Client.Utils;
using UnityEngine;

namespace SNet_Client.EntityScripts.TransfromSync
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

        protected virtual void Awake()
        {
            UniqueScriptIdentifingString = "TransformEntitySync";
            Serialize = true;
        }
        bool isOurs = false;
        protected override void Start()
        {
            base.Start();
            referenceFrameTransform = ReferenceFrameLocator.GetReferenceFrame(referenceFrame);

            isOurs = gameObject.GetAttachedNetworkedEntity().IsOurs();
            //Debug.Log(string.Format("Reference Type {0} e Sync Type {1}", syncTransformType, referenceFrame));
        }

        protected virtual void FixedUpdate()
        {
            if (!isOurs)
            {
                transform.position = WithReferenceFrame(latestPosition);
                transform.rotation = RotationTransformDirection(lastestRotationAngle, lastestRotationAxis);
            }
        }

        Vector3 latestPosition;
        Vector3 lastestRotationAxis;
        float lastestRotationAngle;
        public override void OnDeserialize(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            if (syncTransformType == SyncTransform.PositionOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
                latestPosition = reader.ReadVector3();
            if (syncTransformType == SyncTransform.RotationOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
            {
                lastestRotationAngle = reader.ReadSingle();
                lastestRotationAxis = reader.ReadVector3();
            }
            if (syncTransformType == SyncTransform.ScaleOnly || syncTransformType == SyncTransform.All)
                transform.localScale = reader.ReadVector3();
        }

        private Vector3 InverseWithReferenceFrame(Vector3 position)
        {
            if (referenceFrameTransform == null)
                return position;

            return referenceFrameTransform.InverseTransformPoint(position);
        }
        private Vector3 WithReferenceFrame(Vector3 position)
        {
            if (referenceFrameTransform == null)
                return position;

            return referenceFrameTransform.TransformPoint(position);
        }

        private Quaternion RotationTransformDirection(float angle, Vector3 axis)
        {
            if (referenceFrameTransform != null)
                axis = referenceFrameTransform.TransformDirection(axis);

            return Quaternion.AngleAxis(angle, axis);
        }
        private void RotationInverseTransformDirection(Quaternion rotation, out float angle, out Vector3 axis)
        {
            rotation.ToAngleAxis(out angle, out axis);
            if (referenceFrameTransform == null)
                return;

            axis = referenceFrameTransform.InverseTransformDirection(axis);
        }

        public override void OnSerialize(ref PacketWriter writer)
        {
            if (syncTransformType == SyncTransform.PositionOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
                writer.Write(InverseWithReferenceFrame(transform.position));
            if (syncTransformType == SyncTransform.RotationOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
            {
                RotationInverseTransformDirection(transform.rotation, out float angle, out Vector3 axis);
                writer.Write(angle);
                writer.Write(axis);
            }
            if (syncTransformType == SyncTransform.ScaleOnly || syncTransformType == SyncTransform.All)
                writer.Write(transform.localScale);
        }
    }
}
