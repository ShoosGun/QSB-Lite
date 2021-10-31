using ClientSide.Sockets;
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
    public class TransformEntitySync : EntityScriptBehaviour //Usar o primeiro byte do primeiro byte[] para suas informações de inicialização
    {
        public SyncTransform syncTransformType;
        
        private void Awake()
        {
            UniqueScriptIdentifingString = "TransformEntitySync";
            Serialize = true;
        }

        public override void OnDeserialize(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            Sector.SectorName referenceFrameName = (Sector.SectorName)reader.ReadByte();
            Transform referenceFrame = Utils.MajorSectorLocator.GetMajorSector(referenceFrameName).GetAttachedOWRigidbody().transform;

            if (syncTransformType == SyncTransform.PositionOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
                transform.position = reader.ReadVector3() + referenceFrame.position;
            if (syncTransformType == SyncTransform.RotationOnly || syncTransformType == SyncTransform.PositionAndRotationOnly || syncTransformType == SyncTransform.All)
                transform.rotation = reader.ReadQuaternion();
            if (syncTransformType == SyncTransform.ScaleOnly || syncTransformType == SyncTransform.All)
                transform.localScale = reader.ReadVector3();
        }
    }
}
