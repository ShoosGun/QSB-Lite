using ClientSide.Sockets;
using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace ClientSide.SettingsMenu
{
    public class ClientModSettingsMenu : MonoBehaviour
    {
        private bool conectado = false;
        private Client cliente;
        private string IP = "127.0.0.1";
        private bool conectar = false;

        public void Awake()
        {
            cliente = gameObject.GetComponent<ClientMod>()._clientSide;
        }
        
        public void Close()
        {   conectar = false;
        }

        public void Update()
        {
            if (Input.GetKeyUp(KeyCode.Backspace))
            {
                conectar = !conectar;
                if (Time.timeScale != 0)
                {
                    if (conectar)
                    {
                        Screen.showCursor = true;
                        Screen.lockCursor = false;
                    }
                    else
                    {
                        Screen.showCursor = false;
                        Screen.lockCursor = true;
                    }
                }
            }

            if(cliente.Connected != conectado)
                conectado = cliente.Connected;
        }

        public void OnGUI()
        {
            if (conectar && !cliente.Connected)
            {
                IP = GUI.PasswordField(new Rect(10, 10, 150, 25), IP, "*"[0]);
                if (GUI.Button(new Rect(10, 35, 150, 25), "Conectar para esse IP"))
                {
                    cliente.TryConnect(IP, 1000);
                    conectar = false;
                    if (Time.timeScale != 0)
                    {
                        Screen.showCursor = false;
                        Screen.lockCursor = true;
                    }
                }
            }
        }
        
    }
}
