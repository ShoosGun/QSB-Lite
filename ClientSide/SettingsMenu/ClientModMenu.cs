using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SNet_Client.SettingsMenu
{
    public class ClientModMenu : MonoBehaviour
    {
        static public bool UseClient = false;
        private bool aberto = false;

        public void Update()
        {
            if (Input.GetKeyUp(KeyCode.Backspace))
                aberto = !aberto;
        }

        public void OnGUI()
        {
            if (aberto)
                if (GUI.Button(new Rect(10, 10, 150, 25), UseClient? "Mudar para Servidor" : "Mudar para Cliente"))
                    UseClient = !UseClient;
        }
    }
}
