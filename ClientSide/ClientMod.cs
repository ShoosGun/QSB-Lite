using UnityEngine;
using BepInEx;

using SNet_Client.Sockets;
using SNet_Client.Utils;

using SNet_Client.PacketCouriers.Entities;
using SNet_Client.PacketCouriers;

using SNet_Client.EntityCreators;

namespace SNet_Client
{
    [BepInPlugin("locochoco.SNet","SNet","0.0.1")]
    public class ClientMod : BaseUnityPlugin
    {
        public Client _clientSide;
        
        private void Start()
        {
            if (!Application.runInBackground)
                Application.runInBackground = true;

            _clientSide = new Client();
            
            //Network Specific Scripts
            gameObject.AddComponent<ServerInteraction>();
            gameObject.AddComponent<EntityInitializer>();

            //Game Specific Scripts
            gameObject.AddComponent<PlayerEntities>();
            gameObject.AddComponent<ProbeEntities>();

        }
        
        private void Update()
        {
            //Atualizar quando achar que estiver em uma nova cena
            if (Time.timeSinceLevelLoad < Time.deltaTime * 2f)
            {
                ReferenceFrameLocator.CacheAllReferenceFrame();
            }
        }

                    string IP = "127.0.0.1";
        public void OnGUI()
        {
            if (!_clientSide.Connected)
            {
                IP = GUI.PasswordField(new Rect(10, 10, 150, 25), IP, "*"[0]);
                if (GUI.Button(new Rect(10, 35, 150, 25), "Conectar para esse IP"))
                    _clientSide.Connect(IP, 2121);
            }
            else
            {
                if (GUI.Button(new Rect(10, 10, 150, 25), "Desconectar"))
                    _clientSide.Disconnect();
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
    }
}
