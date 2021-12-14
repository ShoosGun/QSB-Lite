using UnityEngine;

using SNet_Client.Utils;
using SNet_Client.EntityScripts.StateSync;
using System;

namespace SNet_Client.EntityCreators.Player
{
    public class PlayerItemStates : MonoBehaviour
    {
        private EntityStatesSync statesSync;

        private PlayerTelescope telescope;

        public GameObject MellowStick;

        private PlayerLight playerLight;

        //Suit Stuff
        private PlayerSuit playerSuit;

        public void Start()
        {
            statesSync = GetComponent<EntityStatesSync>();
            playerSuit = GetComponent<PlayerSuit>();
            playerLight = GetComponent<PlayerLight>();
            telescope = GetComponent<PlayerTelescope>();

            bool isPlayerAlreadyWithSuit = false;
            if (gameObject.GetAttachedNetworkedEntity().IsOurs())
            {
                GlobalMessenger.AddListener("SuitUp", OnSuitUp);
                GlobalMessenger.AddListener("RemoveSuit", OnRemoveSuit);

                GlobalMessenger.AddListener("TurnOnFlashlight", OnFlashlightOn);
                GlobalMessenger.AddListener("TurnOffFlashlight", OnFlashlightOff);

                GlobalMessenger<Telescope>.AddListener("EnterTelescopeView", OnEnterTelescopeView);
                GlobalMessenger.AddListener("ExitTelescopeView", OnExitTelescopeView);
            }

            statesSync.AddStateListener((byte)PlayerStates.SUIT_EQUIP, isPlayerAlreadyWithSuit, OnSuitStateChange);
            statesSync.AddStateListener((byte)PlayerStates.FLASHLIGHT, false, OnFlashlightStateChange);
            statesSync.AddStateListener((byte)PlayerStates.ROASTING_MELLOWS, false, OnMellowsStateChange);
            statesSync.AddStateListener((byte)PlayerStates.USING_TELESCOPE, false, OnTelescopeStateChange);
        }

        public void OnDestroy()
        {
            GlobalMessenger.RemoveListener("SuitUp", OnSuitUp);
            GlobalMessenger.RemoveListener("RemoveSuit", OnRemoveSuit);
            GlobalMessenger.RemoveListener("TurnOnFlashlight", OnFlashlightOn);
            GlobalMessenger.RemoveListener("TurnOffFlashlight", OnFlashlightOff);
            GlobalMessenger<Telescope>.RemoveListener("EnterTelescopeView", OnEnterTelescopeView);
            GlobalMessenger.RemoveListener("ExitTelescopeView", OnExitTelescopeView);
        }

        private void OnExitTelescopeView() => statesSync.ChangeValue((byte)PlayerStates.USING_TELESCOPE, false);
        private void OnEnterTelescopeView(Telescope tele) => statesSync.ChangeValue((byte)PlayerStates.USING_TELESCOPE, true);

        private void OnTelescopeStateChange(bool isUsingTelescope)
        {
            Debug.Log(string.Format("{0} - {1} : Equiped Telescope", gameObject.GetAttachedNetworkedEntity().id, gameObject.GetAttachedNetworkedEntity().ownerId));
            if (telescope != null)
                telescope.EquipTele(isUsingTelescope);
        }

        private void OnMellowsStateChange(bool isRoastingMellows)
        {
            //Fazer a animação do graveto aparecer para o outro player
        }

        private void OnFlashlightOff() => statesSync.ChangeValue((byte)PlayerStates.FLASHLIGHT, false);
        private void OnFlashlightOn() => statesSync.ChangeValue((byte)PlayerStates.FLASHLIGHT, true);

        private void OnFlashlightStateChange(bool isLightEnabled)
        {
            if (playerLight != null)
                playerLight.TurnLight(isLightEnabled);
        }

        private void OnRemoveSuit() => statesSync.ChangeValue((byte)PlayerStates.SUIT_EQUIP, false);
        private void OnSuitUp() => statesSync.ChangeValue((byte)PlayerStates.SUIT_EQUIP, true);

        private void OnSuitStateChange(bool isWithSuit)
        {
            if(playerSuit != null)
                playerSuit.EquipSuit(isWithSuit);
        }
    }
}
