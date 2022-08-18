using UnityEngine;
using BepInEx;
using BepInEx.Logging;

using SNet_Client.Sockets;
using SNet_Client.Utils;

using SNet_Client.PacketCouriers.Entities;

using SNet_Client.EntityCreators.Player;
using SNet_Client.EntityCreators.Probe;

namespace SNet_Client
{
    [BepInPlugin("locochoco.SNet","SNet","0.0.1")]
    public class ClientMod : BaseUnityPlugin
    {
        public Client _clientSide;
        public static ManualLogSource LogSource;

        private void Start()
        {
            LogSource = Logger;

            if (!Application.runInBackground)
                Application.runInBackground = true;

            _clientSide = new Client();

            //Network Specific Scripts
            gameObject.AddComponent<EntityInitializer>();

            //Game Specific Scripts
            gameObject.AddComponent<PlayerEntities>();
            gameObject.AddComponent<ProbeEntities>();

            //TODO Adicionar Script para spawnar a nave usando os global events "EnterShipProximity" e "ExitShipProximity"
            //TODO Adicionar Script para spawnar a nave remota usando os global events <OWRigidbody>.AddListener("EnterRemoteFlightConsole" e "ExitRemoteFlightConsole"
            //TODO Adicionar Script para spawnar o marshmellow e seu graveto usando os global events "BeginRoasting", "StopRoasting" e "EatMarshmallow"

        }
        
        private void Update()
        {
            //Atualizar quando achar que estiver em uma nova cena
            if (Time.timeSinceLevelLoad < Time.deltaTime * 2f)
            {
                ReferenceFrameLocator.CacheAllReferenceFrames();
            }
            _clientSide.Update();
        }
        private void LateUpdate() 
        {
            _clientSide.LateUpdate();
        }
        
        string ActivitySecrete = "";
        public void OnGUI()
        {
            //TODO adicionar UI para abrir servidor
            if (!_clientSide.Connected)
            {
                ActivitySecrete = GUI.TextField(new Rect(10, 10, 150, 25), ActivitySecrete);
                if (GUI.Button(new Rect(10, 35, 150, 25), "Connect to Lobby") && !_clientSide.Connecting)
                    _clientSide.ConnectToLobby(ActivitySecrete);
                if (GUI.Button(new Rect(10, 55, 150, 25), "Open Lobby") && !_clientSide.Connecting)
                    _clientSide.OpenLobby();
            }
            else
            {
                if (GUI.Button(new Rect(10, 10, 150, 25), "Desconectar"))
                    _clientSide.Disconnect();
            }
        }
        private void OnDestroy()
        {
            _clientSide.Disconnect();
        }
    }
}
