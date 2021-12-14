using UnityEngine;

using SNet_Client.PacketCouriers;
using SNet_Client.PacketCouriers.Entities;
using SNet_Client.Sockets;

using SNet_Client.Utils;

using SNet_Client.EntityScripts.StateSync;
using SNet_Client.EntityScripts.TransfromSync;

namespace SNet_Client.EntityCreators.Probe
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
            GameObject go = new GameObject("probe_networked_entity")
            {
                layer = LayerMask.NameToLayer("Primitive")
            };

            Rigidbody rigidbody = go.AddComponent<Rigidbody>();

            go.AddComponent<OWRigidbody>();

            bool createMesh = true;
            bool createNormalMesh = true;

            if (ownerID == ServerInteraction.GetOwnerID())
            {
                go.transform.parent = currentProbeBody.transform;
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
                go.transform.localScale = Vector3.one;
                rigidbody.isKinematic = true;

                go.AddComponent<ClosestReferenceFrameLocator>();
            }
            else
            {
                go.transform.position = position;
                go.transform.rotation = rotation;
            }

            NetworkedEntity networkedEntity = go.AddComponent<NetworkedEntity>();

            DynamicReferenceTransformEntitySync transformEntitySync = networkedEntity.AddEntityScript<DynamicReferenceTransformEntitySync>();
            transformEntitySync.syncTransformType = SyncTransform.All;
            transformEntitySync.referenceFrame = ReferenceFrames.Timber_Hearth;

            DynamicReferenceRigidbodyEntitySync rigibodyEntitySync = networkedEntity.AddEntityScript<DynamicReferenceRigidbodyEntitySync>();
            rigibodyEntitySync.syncRigidbodyType = SyncRigidbody.Both;
            rigibodyEntitySync.referenceFrame = ReferenceFrames.Timber_Hearth;

            networkedEntity.AddEntityScript<EntityStatesSync>();

            go.AddComponent<ProbeStatesSync>();

            if (ownerID != ServerInteraction.GetOwnerID())
            {
                go.AddComponent<ProbeLightsAndSound>();
            }

            if (createMesh)
            {
                GameObject mesh = new GameObject("mesh");
                mesh.transform.parent = go.transform;
                mesh.transform.localPosition = new Vector3(0f, 0f, -0.2f);
                mesh.transform.localRotation = Quaternion.Euler(-90f, 0, 0);
                if (createNormalMesh)
                {
                    //Mesh do probe
                    mesh.transform.localScale = Vector3.one * 1.5f;
                    if (ResourceLoader.GetProbeMeshAndMaterial(out MeshMaterialCombo probeMeshAndMaterial))
                    {
                        mesh.AddComponent<MeshFilter>().mesh = probeMeshAndMaterial.mesh;
                        mesh.AddComponent<MeshRenderer>().material = probeMeshAndMaterial.material;
                    }
                }
                else
                { 
                    //Mesh do angler
                    mesh.transform.localScale = Vector3.one * 0.005f;

                    if (ResourceLoader.GetAnglerfishMeshAndMaterial(out MeshMaterialCombo anglerMeshAndMaterial))
                    {
                        mesh.AddComponent<MeshFilter>().mesh = anglerMeshAndMaterial.mesh;
                        mesh.AddComponent<MeshRenderer>().material = anglerMeshAndMaterial.material;
                    }
                }
            }

            return networkedEntity;
        }
    }
}
