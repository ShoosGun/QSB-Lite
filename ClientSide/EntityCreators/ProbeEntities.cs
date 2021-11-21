using UnityEngine;

using SNet_Client.PacketCouriers;
using SNet_Client.PacketCouriers.Entities;
using SNet_Client.Sockets;
using SNet_Client.EntityScripts.TransfromSync;
using SNet_Client.Utils;

namespace SNet_Client.EntityCreators
{
    public class ProbeEntities : MonoBehaviour
    {
        bool weHavePlayerID = false;
        public void Start()
        {
            EntityInitializer.client_EntityInitializer.AddGameObjectPrefab("ProbeEntity", CreateProbeEntity);
            ServerInteraction.OnReceiveOwnerID += ServerInteraction_OnReceiveOwnerID;
            Client.GetClient().Disconnection += PlayerEntities_Disconnection;

            GlobalMessenger<OWRigidbody>.AddListener("LaunchProbe", OnLaunchProbe);
        }

        public void OnDestroy()
        {
            GlobalMessenger<OWRigidbody>.RemoveListener("LaunchProbe", OnLaunchProbe);
        }
        private void OnLaunchProbe(OWRigidbody probeBody)
        {
            if (weHavePlayerID)
                SpawnProbe(probeBody);
        }

        private void ServerInteraction_OnReceiveOwnerID()
        {
            weHavePlayerID = true;
        }
        private void PlayerEntities_Disconnection()
        {
            weHavePlayerID = false;
        }

        OWRigidbody currentProbeBody;
        private void SpawnProbe(OWRigidbody probeBody)
        {
            currentProbeBody = probeBody;
            EntityInitializer.client_EntityInitializer.InstantiateEntity("ProbeEntity", probeBody.transform.position, probeBody.transform.rotation, EntityInitializer.InstantiateType.Buffered);
        }

        public NetworkedEntity CreateProbeEntity(Vector3 position, Quaternion rotation, int ownerID, object[] InitializationData)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);

            go.layer = LayerMask.NameToLayer("Primitive");

            Collider c = go.GetComponent<Collider>();
            c.enabled = false;

            Rigidbody rigidbody = go.AddComponent<Rigidbody>();

            go.AddComponent<OWRigidbody>();

            if (ownerID == ServerInteraction.GetOwnerID())
            {
                go.transform.parent = currentProbeBody.transform;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                rigidbody.isKinematic = true;
            }
            else
            {
                go.transform.position = position;
                go.transform.rotation = rotation;
            }

            NetworkedEntity networkedEntity = go.AddComponent<NetworkedEntity>();

            TransformEntitySync transformEntitySync = networkedEntity.AddEntityScript<TransformEntitySync>();
            transformEntitySync.syncTransformType = SyncTransform.All;
            transformEntitySync.referenceFrame = ReferenceFrames.Timber_Hearth;

            RigidbodyEntitySync rigibodyEntitySync = networkedEntity.AddEntityScript<RigidbodyEntitySync>();
            rigibodyEntitySync.syncRigidbodyType = SyncRigidbody.Both;
            rigibodyEntitySync.referenceFrame = ReferenceFrames.Timber_Hearth;

            return networkedEntity;
        }
    }
}
