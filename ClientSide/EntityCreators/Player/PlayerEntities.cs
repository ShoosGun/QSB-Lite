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
            
            go.AddComponent<PlayerItemStates>();
            
            if (ownerID != ServerInteraction.GetOwnerID())
            {
                go.AddComponent<PlayerLight>();
            }
            go.AddComponent<PlayerSuit>();
            go.AddComponent<PlayerTelescope>();

            if (createMesh)
            {
				//CTRL - JNT
                //Copiar o objeto "Villager_Base" para podermos ter IK e outras coisas para animar 
                //Mesh do player
                if(CreatePlayerMesh(go.transform, out GameObject playerMesh))
                {
                    Debug.Log("Animacione");
                    AnimationClip clip = new AnimationClip();
                    Quaternion rotationA = Quaternion.Euler(0f, 0f, -245.683f);
                    clip.SetCurve("villager_rig:Root_JNT/villager_rig:Spine1_JNT/villager_rig:Spine2_JNT/villager_rig:Spine3_JNT/villager_rig:L_clavicle_JNT", typeof(Transform), "localRotation.w", AnimationCurve.Linear(0f, rotationA.w, 1f, rotationA.w));
                    clip.SetCurve("villager_rig:Root_JNT/villager_rig:Spine1_JNT/villager_rig:Spine2_JNT/villager_rig:Spine3_JNT/villager_rig:L_clavicle_JNT", typeof(Transform), "localRotation.x", AnimationCurve.Linear(0f, rotationA.x, 1f, rotationA.x));
                    clip.SetCurve("villager_rig:Root_JNT/villager_rig:Spine1_JNT/villager_rig:Spine2_JNT/villager_rig:Spine3_JNT/villager_rig:L_clavicle_JNT", typeof(Transform), "localRotation.y", AnimationCurve.Linear(0f, rotationA.y, 1f, rotationA.y));
                    clip.SetCurve("villager_rig:Root_JNT/villager_rig:Spine1_JNT/villager_rig:Spine2_JNT/villager_rig:Spine3_JNT/villager_rig:L_clavicle_JNT", typeof(Transform), "localRotation.z", AnimationCurve.Linear(0f, rotationA.z, 1f, rotationA.z));

                    Quaternion rotationB = Quaternion.Euler(-6.571f, 68.04601f, 2.641f);
                    clip.SetCurve("villager_rig:Root_JNT/villager_rig:Spine1_JNT/villager_rig:Spine2_JNT/villager_rig:Spine3_JNT/villager_rig:L_clavicle_JNT/villager_rig:L_shoulder_JNT", typeof(Transform), "localRotation.w", AnimationCurve.Linear(0f, rotationB.w, 1f, rotationB.w));
                    clip.SetCurve("villager_rig:Root_JNT/villager_rig:Spine1_JNT/villager_rig:Spine2_JNT/villager_rig:Spine3_JNT/villager_rig:L_clavicle_JNT/villager_rig:L_shoulder_JNT", typeof(Transform), "localRotation.x", AnimationCurve.Linear(0f, rotationB.x, 1f, rotationB.x));
                    clip.SetCurve("villager_rig:Root_JNT/villager_rig:Spine1_JNT/villager_rig:Spine2_JNT/villager_rig:Spine3_JNT/villager_rig:L_clavicle_JNT/villager_rig:L_shoulder_JNT", typeof(Transform), "localRotation.y", AnimationCurve.Linear(0f, rotationB.y, 1f, rotationB.y));
                    clip.SetCurve("villager_rig:Root_JNT/villager_rig:Spine1_JNT/villager_rig:Spine2_JNT/villager_rig:Spine3_JNT/villager_rig:L_clavicle_JNT/villager_rig:L_shoulder_JNT", typeof(Transform), "localRotation.z", AnimationCurve.Linear(0f, rotationB.z, 1f, rotationB.z));

                    Quaternion rotationC = Quaternion.Euler(-46.611f, -7.092f, 9.715f);
                    clip.SetCurve("villager_rig:Root_JNT/villager_rig:Spine1_JNT/villager_rig:Spine2_JNT/villager_rig:Spine3_JNT/villager_rig:L_clavicle_JNT/villager_rig:L_shoulder_JNT/villager_rig:L_elbow_JNT", typeof(Transform), "localRotation.w", AnimationCurve.Linear(0f, rotationC.w, 1f, rotationC.w));
                    clip.SetCurve("villager_rig:Root_JNT/villager_rig:Spine1_JNT/villager_rig:Spine2_JNT/villager_rig:Spine3_JNT/villager_rig:L_clavicle_JNT/villager_rig:L_shoulder_JNT/villager_rig:L_elbow_JNT", typeof(Transform), "localRotation.x", AnimationCurve.Linear(0f, rotationC.x, 1f, rotationC.x));
                    clip.SetCurve("villager_rig:Root_JNT/villager_rig:Spine1_JNT/villager_rig:Spine2_JNT/villager_rig:Spine3_JNT/villager_rig:L_clavicle_JNT/villager_rig:L_shoulder_JNT/villager_rig:L_elbow_JNT", typeof(Transform), "localRotation.y", AnimationCurve.Linear(0f, rotationC.y, 1f, rotationC.y));
                    clip.SetCurve("villager_rig:Root_JNT/villager_rig:Spine1_JNT/villager_rig:Spine2_JNT/villager_rig:Spine3_JNT/villager_rig:L_clavicle_JNT/villager_rig:L_shoulder_JNT/villager_rig:L_elbow_JNT", typeof(Transform), "localRotation.z", AnimationCurve.Linear(0f, rotationC.z, 1f, rotationC.z));
                    clip.wrapMode = WrapMode.Loop;
                    playerMesh.GetComponent<Animator>().enabled = false;
                    playerMesh.AddComponent<Animation>().AddClip(clip, "AAAAA");
                    playerMesh.animation.Play("AAAAA", PlayMode.StopAll);
                }
            }

            return networkedEntity;
        }

        private bool CreatePlayerMesh(Transform playerT, out GameObject playerMesh)
        {
            playerMesh = null;
            if (!ResourceLoader.GetVillagerRigGameObject(out GameObject villagerRigGO))
                return false;

            playerMesh = (GameObject)Instantiate(villagerRigGO);

            playerMesh.transform.name = "player_mesh";

            playerMesh.transform.parent = playerT;
            playerMesh.transform.localPosition = new Vector3(0f, -1f, 0f);
            playerMesh.transform.localRotation = Quaternion.identity;
            playerMesh.transform.localScale = Vector3.one;

            return true;
        }
    }
}
