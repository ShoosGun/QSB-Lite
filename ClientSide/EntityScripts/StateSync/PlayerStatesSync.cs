using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using SNet_Client.Sockets;
using UnityEngine;

namespace SNet_Client.EntityScripts.AnimationSync
{
    //TODO transformar isso em uma forma mais genérica
    class PlayerStatesSync : EntityScriptBehaviour
    {
        bool isWithSuit = false;
        bool isFlashlightOn = false;
        bool isUsingTelescope = false;
        bool isRoastingMarshmellows = false;
        protected virtual void Awake()
        {
            UniqueScriptIdentifingString = "PlayerStatesSync";
            Serialize = true;

            //Se inscrever aos eventos, e neles mudar os status das coisas
            if (networkedEntity.IsOurs())
            {

            }
        }
        protected override void Start()
        {
            base.Start();
        }
        private void ChangeSuitStatus()
        {
        }
        private void ChangesFlashlighStatus()
        {
        }
        private void ChangesTelescopeStatus()
        {
        }
        private void ChangesRoastingMarshmellowsStatus()
        {
        }
        public override void OnDeserialize(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
            bool wasWithSuit = isWithSuit;
            bool wasFlashlightOn = isFlashlightOn;
            bool wasUsingTelescope = isUsingTelescope;
            bool wasRoastingMarshmellows = isRoastingMarshmellows;

            isWithSuit = reader.ReadBoolean();
            isFlashlightOn = reader.ReadBoolean();
            isUsingTelescope = reader.ReadBoolean();
            isRoastingMarshmellows = reader.ReadBoolean();

            if (wasWithSuit != isWithSuit)
                ChangeSuitStatus();

            if (wasFlashlightOn != isFlashlightOn)
                ChangesTelescopeStatus();

            if (wasUsingTelescope != isUsingTelescope)
                ChangesTelescopeStatus();

            if (wasRoastingMarshmellows != isRoastingMarshmellows)
                ChangesRoastingMarshmellowsStatus();
        }
        public override void OnSerialize(ref PacketWriter writer)
        {
            writer.Write(isWithSuit);
            writer.Write(isFlashlightOn);
            writer.Write(isUsingTelescope);
            writer.Write(isRoastingMarshmellows);
        }
    }
}
