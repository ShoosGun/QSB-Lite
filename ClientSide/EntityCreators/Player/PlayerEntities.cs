using UnityEngine;

using SNet_Client.PacketCouriers.Entities;
using SNet_Client.PacketCouriers;
using SNet_Client.EntityScripts.TransfromSync;
using SNet_Client.Utils;
using SNet_Client.Sockets;
using SNet_Client.EntityScripts.StateSync;

namespace SNet_Client.EntityCreators.Player
{
    public class PlayerEntities : MonoBehaviour
    {
        bool hasSpawnedPlayer = false;
        bool weHavePlayerID = false;
        public void Start()
        {
            EntityInitializer.client_EntityInitializer.AddGameObjectPrefab("PlayerEntity", CreatePlayerEntity);
            ServerInteraction.OnReceiveOwnerID += ServerInteraction_OnReceiveOwnerID;
            Client.GetClient().Disconnection += PlayerEntities_Disconnection;

            GlobalMessenger<DeathType>.AddListener("PlayerDeath", OnPlayerDeath);
            GlobalMessenger<int>.AddListener("StartOfTimeLoop", OnStartOfTimeLoop);
            GlobalMessenger.AddListener("ResumeSimulation", OnResumeSimulation);
        }

        public void OnDestroy()
        {
            GlobalMessenger<DeathType>.RemoveListener("PlayerDeath", OnPlayerDeath);
            GlobalMessenger<int>.RemoveListener("StartOfTimeLoop", OnStartOfTimeLoop);
            GlobalMessenger.RemoveListener("ResumeSimulation", OnResumeSimulation);
        }
        private void OnStartOfTimeLoop(int loop)
        {
            EntityInitializer.client_EntityInitializer.RequestRefreshOfEntities();

            if (!hasSpawnedPlayer && weHavePlayerID)
                SpawnPlayer();
        }
        private void OnResumeSimulation()
        {
            if (!hasSpawnedPlayer && weHavePlayerID)
                SpawnPlayer();
        }
        private void OnPlayerDeath(DeathType deathType)
        {
            hasSpawnedPlayer = false;
        }
        private void ServerInteraction_OnReceiveOwnerID()
        {
            weHavePlayerID = true;
            if (!hasSpawnedPlayer)
                SpawnPlayer();
        }
        private void PlayerEntities_Disconnection()
        {
            weHavePlayerID = false;
            hasSpawnedPlayer = false;
        }

        private void SpawnPlayer()
        {
            Transform playerTransf = Locator.GetPlayerTransform();
            if (playerTransf != null)
                EntityInitializer.client_EntityInitializer.InstantiateEntity("PlayerEntity", playerTransf.position, playerTransf.rotation, EntityInitializer.InstantiateType.Buffered);

            hasSpawnedPlayer = true;
        }

        public NetworkedEntity CreatePlayerEntity(Vector3 position, Quaternion rotation, int ownerID, object[] InitializationData)
        {
            GameObject go = new GameObject("player_networked_entity")
            {
                layer = LayerMask.NameToLayer("Primitive")
            };

            CapsuleCollider c = go.AddComponent<CapsuleCollider>();
            c.radius = 0.5f;
            c.height = 2f;
            c.enabled = false;

            Rigidbody rigidbody = go.AddComponent<Rigidbody>();
            rigidbody.mass = 0.001f;
            rigidbody.drag = 0f;
            rigidbody.angularDrag = 0f;

            OWRigidbody owrigid = go.AddComponent<OWRigidbody>();

            bool createMesh = true;

            if (ownerID == ServerInteraction.GetOwnerID())
            {
                go.transform.parent = Locator.GetPlayerTransform();
                go.transform.localPosition = Vector3.zero;
                go.transform.localRotation = Quaternion.identity;
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
            transformEntitySync.syncTransformType = SyncTransform.PositionAndRotationOnly;
            transformEntitySync.referenceFrame = ReferenceFrames.Timber_Hearth;

            DynamicReferenceRigidbodyEntitySync rigibodyEntitySync = networkedEntity.AddEntityScript<DynamicReferenceRigidbodyEntitySync>();
            rigibodyEntitySync.syncRigidbodyType = SyncRigidbody.Both;
            rigibodyEntitySync.referenceFrame = ReferenceFrames.Timber_Hearth;

            EntityStatesSync statesSync = networkedEntity.AddEntityScript<EntityStatesSync>();

            //TODO adicionar todos os estados em PlayerSates em classes qe cuidem da animação e etc
            go.AddComponent<PlayerItemStates>();

            //TODO Deixar melhor separado cada elemento (mellow stick, suit, flashlight, ...)
            if (ownerID != ServerInteraction.GetOwnerID())
            {
                go.AddComponent<PlayerLight>();
            }
            go.AddComponent<PlayerSuit>();

            if (createMesh)
            {
                //Copiar o objeto "Villager_Base" para podermos ter IK e outras coisas para animar 
                Debug.Log("player");
                //Mesh do player
                CreatePlayerMesh(go.transform);
            }

            return networkedEntity;
        }

        private GameObject CreatePlayerMesh(Transform playerT)
        {
            GameObject mesh = new GameObject("player_mesh");
            mesh.transform.parent = playerT;
            mesh.transform.localPosition = new Vector3(0f, -1f, 0f);
            mesh.transform.localRotation = Quaternion.identity;
            mesh.transform.localScale = Vector3.one;
            if (ResourceLoader.GetVillagerMeshAndMaterial(out MeshMaterialCombo playerMeshAndMaterial))
            {
                mesh.AddComponent<MeshFilter>().mesh = playerMeshAndMaterial.mesh;
                mesh.AddComponent<MeshRenderer>().material = playerMeshAndMaterial.material;
            }
            return mesh;
        }
    }
}
