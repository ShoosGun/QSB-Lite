using UnityEngine;

using SNet_Client.Utils;
using SNet_Client.EntityScripts.StateSync;
using System;

namespace SNet_Client.EntityCreators.Player
{
    public class PlayerItemStates : MonoBehaviour
    {
        EntityStatesSync statesSync;

        GameObject Telescope;

        GameObject MellowStick;

        GameObject Flashlight;

        //Suit Stuff
        GameObject JetPack;
        GameObject Helmet;

        public void Start()
        {
            statesSync = GetComponent<EntityStatesSync>();

            bool isPlayerAlreadyWithSuit = false;
            if (!gameObject.GetAttachedNetworkedEntity().IsOurs())
            {

            }
            else
            {
                //TODO Pegar se o player ja esta com a roupa e jogar aqui
                isPlayerAlreadyWithSuit = Locator.GetPlayerTransform().GetComponent<Player>();
            }

            statesSync.AddStateListener((byte)PlayerStates.SUIT_EQUIP, isPlayerAlreadyWithSuit, OnSuitStateChange);
            statesSync.AddStateListener((byte)PlayerStates.FLASHLIGHT, false, OnFlashlightStateChange);
            statesSync.AddStateListener((byte)PlayerStates.ROASTING_MELLOWS, false, OnMellowsStateChange);
            statesSync.AddStateListener((byte)PlayerStates.USING_TELESCOPE, false, OnTelescopeStateChange);
        }

        private void OnTelescopeStateChange(bool isUsingTelescope)
        {
            //Fazer o telescopio aparecer
        }

        private void OnMellowsStateChange(bool isRoastingMellows)
        {
            //Fazer a animação do graveto aparecer para o outro player
        }

        private void OnFlashlightStateChange(bool isLightEnabled)
        {
            //Ligar a lanterna do outro player
        }

        private void OnSuitStateChange(bool isWithSuit)
        {
            //Ativar a animação de colocar a roupa
        }
    }
}
