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

        Vector3 latestPosition;
        Vector3 lastestUp;
        Vector3 lastestFoward;

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

            latestPosition = transform.position;
            lastestUp = transform.up;
            lastestFoward = transform.forward;
            //Debug.Log(string.Format("Reference Type {0} e Sync Type {1}", syncTransformType, referenceFrame));
        }
        protected virtual void FixedUpdate()
        {
            if (!isOurs)
            {
                transform.position = WithReferenceFrame(latestPosition);
                transform.rotation = RotationTransformDirection(lastestUp, lastestFoward);
            }
        }
        public override void OnDeserialize(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            if (syncTransformType == SyncTransform.PositionOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
                latestPosition = reader.ReadVector3();
            if (syncTransformType == SyncTransform.RotationOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
            {
                lastestUp = reader.ReadVector3();
                lastestFoward = reader.ReadVector3();
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

        private Quaternion RotationTransformDirection(Vector3 up, Vector3 foward)
        {
            if (referenceFrameTransform != null)
            {
                up = referenceFrameTransform.TransformDirection(up);
                foward = referenceFrameTransform.TransformDirection(foward);
            }

            return Quaternion.LookRotation(foward, up);
        }
        private void RotationInverseTransformDirection(Transform transform, out Vector3 up, out Vector3 foward)
        {
            up = transform.up;
            foward = transform.forward;
            if (referenceFrameTransform == null)
                return;

            up = referenceFrameTransform.InverseTransformDirection(up);
            foward = referenceFrameTransform.InverseTransformDirection(foward);
        }

        public override void OnSerialize(ref PacketWriter writer)
        {
            if (syncTransformType == SyncTransform.PositionOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
                writer.Write(InverseWithReferenceFrame(transform.position));
            if (syncTransformType == SyncTransform.RotationOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
            {
                RotationInverseTransformDirection(transform, out Vector3 up, out Vector3 foward);
                writer.Write(up);
                writer.Write(foward);
            }
            if (syncTransformType == SyncTransform.ScaleOnly || syncTransformType == SyncTransform.All)
                writer.Write(transform.localScale);
        }
    }
}
