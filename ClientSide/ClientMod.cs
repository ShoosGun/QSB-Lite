using UnityEngine;
using BepInEx;

using ClientSide.Sockets;
using ClientSide.SettingsMenu;
using ClientSide.Utils;

using ClientSide.PacketCouriers.GameRelated.Entities;
using ClientSide.PacketCouriers;
using System.Collections;
using System;
using ClientSide.EntityScripts.TransfromSync;

namespace ClientSide
{
    [BepInPlugin("locochoco.QSB-Lite","Qsb Lite","0.0.1")]
    public class ClientMod : BaseUnityPlugin
    {
        public Client _clientSide;
        
        private void Start()
        {
            if (!Application.runInBackground)
                Application.runInBackground = true;

            _clientSide = new Client();

            gameObject.AddComponent<ServerInteraction>();

            //gameObject.AddComponent<MajorSectorLocator>();

            gameObject.AddComponent<EntityInitializer>();

            //TODO Refazer o menu para se conectar a um servidor
            //gameObject.AddComponent<ClientModSettingsMenu>();        
            
            //Entity Test
            EntityInitializer.client_EntityInitializer.AddGameObjectPrefab("CuB0", CreateNetworkedCube);
            ServerInteraction.OnReceiveOwnerID += ServerInteraction_OnReceiveOwnerID;
        }

        string IP = "127.0.0.1";
        public void OnGUI()
        {
            if (!_clientSide.Connected)
            {
                IP = GUI.PasswordField(new Rect(10, 10, 150, 25), IP, "*"[0]);
                if (GUI.Button(new Rect(10, 35, 150, 25), "Conectar para esse IP"))
                    _clientSide.Connect(IP, 1000);
            }
        }
        private void FixedUpdate()
        {
            _clientSide.Update();
        }
        private void OnDestroy()
        {
            _clientSide.Disconnect();
        }

        private void ServerInteraction_OnReceiveOwnerID()
        {
            StartCoroutine("CreateAndDestroyCubePeriodically");
        }
        NetworkedEntity entity;
        IEnumerator CreateAndDestroyCubePeriodically()
        {
            while (true)
            {
                yield return new WaitForSeconds(7.5f);

                if (entity == null)
                {
                    EntityInitializer.client_EntityInitializer.InstantiateEntity("CuB0", GameObject.Find("Camera").transform.position + GameObject.Find("Camera").transform.forward * 2f, Quaternion.identity, EntityInitializer.InstantiateType.Buffered, (byte)SyncTransform.All, (byte)ReferenceFrames.GlobalRoot);
                }
                else
                {
                    EntityInitializer.client_EntityInitializer.DestroyEntity(entity);
                }
            }
        }
        public NetworkedEntity CreateNetworkedCube(Vector3 position, Quaternion rotation, object[] InitializationData)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = position;
            go.transform.rotation = rotation;
            go.transform.localScale = Vector3.one;

            go.transform.parent = GameObject.Find("Camera").transform;

            NetworkedEntity networkedEntity = go.AddComponent<NetworkedEntity>();
            TransformEntitySync transformEntitySync = networkedEntity.AddEntityScript<TransformEntitySync>();
            transformEntitySync.syncTransformType = (SyncTransform)InitializationData[0];
            transformEntitySync.referenceFrame = (ReferenceFrames)InitializationData[1];
            entity = networkedEntity;
            return networkedEntity;
        }
    }
}
