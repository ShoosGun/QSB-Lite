﻿using SNet_Client.Sockets;
using SNet_Client.Utils;
using System.Collections;
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
        protected override void Start()
        {
            base.Start();
            referenceFrameTransform = ReferenceFrameLocator.GetReferenceFrame(referenceFrame);
            //Debug.Log(string.Format("Reference Type {0} e Sync Type {1}", syncTransformType, referenceFrame));
        }

        public override void OnDeserialize(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            if (syncTransformType == SyncTransform.PositionOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
                transform.position = WithReferenceFrame(reader.ReadVector3());
            if (syncTransformType == SyncTransform.RotationOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
                transform.rotation = RotationToReferenceFrame(reader.ReadQuaternion());
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

        private Quaternion RotationToReferenceFrame(Quaternion rotation)
        {
            if (referenceFrameTransform == null)
                return rotation;

            return Quaternion.Inverse(referenceFrameTransform.rotation) * rotation;
        }
        private Quaternion InverseRotationToReferenceFrame(Quaternion rotation)
        {
            if (referenceFrameTransform == null)
                return rotation;

            return referenceFrameTransform.rotation * rotation;
        }

        public override void OnSerialize(ref PacketWriter writer)
        {
            if (syncTransformType == SyncTransform.PositionOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
                writer.Write(InverseWithReferenceFrame(transform.position));
            if (syncTransformType == SyncTransform.RotationOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
                writer.Write(InverseRotationToReferenceFrame(transform.rotation));
            if (syncTransformType == SyncTransform.ScaleOnly || syncTransformType == SyncTransform.All)
                writer.Write(transform.localScale);
        }
    }
}
