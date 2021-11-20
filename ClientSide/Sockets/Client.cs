using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace SNet_Client.Sockets
{
    public class Client
    {
        //TODO fazer esse valor ser lido de um arquivo que acompanhe o dll do mod
        Listener l;
        private const int serverPort = 2121;

        public bool Connected { private set; get; }

        private readonly object receivedData_lock = new object();
        private Queue<byte[]> receivedData = new Queue<byte[]>();

        public PacketReceiver packetReceiver { get; private set; }

        private static Client CurrentClient = null;
        public static Client GetClient()
        {
            return CurrentClient;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="debugger"></param>
        public Client()
        {
            if (CurrentClient != null)
                return;

            packetReceiver = new PacketReceiver();
            CurrentClient = this;

            Connected = false;
            Connecting = false;

            l = new Listener();
            l.OnConnection += L_OnConnection;
            l.OnFailConnection += L_OnFailConnection;
            l.OnDisconnection += L_OnDisconnection;
            l.OnReceiveData += L_OnReceiveData;
        }

        private void L_OnReceiveData(byte[] dgram)
        {
            lock (receivedData_lock)
            {
                receivedData.Enqueue(dgram);
            }
        }

        private void L_OnDisconnection()
        {
            Connected = false;
        }

        private void L_OnFailConnection()
        {
            Connecting = false;
            Connected = false;
        }

        private void L_OnConnection()
        {
            Connecting = false;
            Connected = true;
        }
        private bool Connecting = false;
        public void Connect(string IP, int port = serverPort)
        {
            if (!Connecting && !Connected)
            {
                Connecting = true;
                l.TryConnect(IP, port);
            }
        }

        private bool wasConnected = false;
        public void Update()
        {
            if (Connected)
            {
                if (!wasConnected)
                {
                    UnityEngine.Debug.Log("Conectados!");
                    wasConnected = true;
                    Connection?.Invoke();
                }

                //Ler dados
                bool WaspacketBuffers_LockAquired = false;
                try
                {
                    WaspacketBuffers_LockAquired = Monitor.TryEnter(receivedData_lock, 10);
                    if (WaspacketBuffers_LockAquired)
                    {
                        int amountOfDGrams = receivedData.Count;
                        for (int i = 0; i < amountOfDGrams; i++)
                        {
                            ReceiveData(receivedData.Dequeue());
                        }
                    }
                }
                finally
                {
                    if(WaspacketBuffers_LockAquired)
                        Monitor.Exit(receivedData_lock);
                }
            }
            else if (wasConnected)
            {
                UnityEngine.Debug.Log("Desconectados!");
                wasConnected = false;
                Disconnection?.Invoke();
            }
        }
        private byte[] MakeDataWithHeader(byte[] data, int header)
        {
            byte[] dataWithHeader = new byte[4 + 8 + data.Length];
            Array.Copy(BitConverter.GetBytes(header), 0, dataWithHeader, 0, 4); //Header
            Array.Copy(BitConverter.GetBytes(DateTime.UtcNow.ToBinary()), 0, dataWithHeader, 4, 8); //Send Time
            Array.Copy(data, 0, dataWithHeader, 12, data.Length);
            return dataWithHeader;
        }
        public bool Send(byte[] data, int header) => l.Send(MakeDataWithHeader(data, header));

        public bool SendReliable(byte[] data, int header) => l.SendReliable(MakeDataWithHeader(data, header));

        private void ReceiveData(byte[] dgram)
        {
            PacketReader packet = new PacketReader(dgram);
            try
            {
                int Header = packet.ReadInt32();
                DateTime sendTime = packet.ReadDateTime();
                ReceivedPacketData receivedPacketData = new ReceivedPacketData(sendTime, (DateTime.UtcNow - sendTime).Milliseconds);

                packetReceiver.ReadReceivedPacket(ref packet, Header, receivedPacketData);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log($"Erro ao ler dados do servidor: {ex.Source} | {ex.Message}");                
            }
        }
        public void Disconnect()
        {
            l.Disconnect();
        }

        public event ConnectionHandler Connection;
        public delegate void ConnectionHandler();

        public event DisconnectionHandler Disconnection;
        public delegate void DisconnectionHandler();
    }
}
