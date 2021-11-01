using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace ClientSide.Sockets
{
    public class Client
    {
        //TODO fazer esse valor ser lido de um arquivo que acompanhe o dll do mod
        private const int serverPort = 2121;
        private Socket reliableServerConnectionSocket;
        private UdpClient unreliableServerConnectionSocket;

        private string ConnectedServerIP = ""; //se a conecção der certo, gravar para tentar reconectar no futuro caso haja uma desconecção
        public bool Connected { private set; get; }
        
        private readonly object packetBuffers_lock = new object();
        private Queue<byte[]> packetBuffers = new Queue<byte[]>();
        
        private bool wasConnected = false;
        
        public Client_DynamicPacketIO DynamicPacketIO { get; private set; }

        private static Client CurrentClient = null;
        public static Client GetClient()
        {
            return CurrentClient;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="debugger"></param>
        public Client(int receivingLimit = 30)
        {
            if (CurrentClient != null)
                return;

            Connected = false;

            DynamicPacketIO = new Client_DynamicPacketIO();

            CurrentClient = this;
        }

        /// <summary>
        /// Disconected any prior connection before attempting to connect, the attempts happen in another thread
        /// </summary>
        /// <param name="IP"></param>
        /// <param name="timeOut"> In milliseconds. It will wait indefinitely if set to a negative number</param>
        public void TryConnect(string IP, int timeOut = -1)
        {
            reliableServerConnectionSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            unreliableServerConnectionSocket = new UdpClient(AddressFamily.InterNetwork);// new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            ConnectedServerIP = IP;
            //Tentar conectar, e se conectar gravar o IP na string
            //Se for negativo ira esperar para sempre por uma conecção

            UnityEngine.Debug.Log("Tentando Conectar em: " + IP);

            if (timeOut >= 0)
                new Thread(() =>
                {
                    reliableServerConnectionSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(IP), serverPort), ConnectCallback, null).AsyncWaitHandle.WaitOne(timeOut, true);
                    if (!reliableServerConnectionSocket.Connected)
                    {
                        reliableServerConnectionSocket.Close();
                        ConnectedServerIP = "";
                    }
                }).Start();
            else
                reliableServerConnectionSocket.BeginConnect(new IPEndPoint(IPAddress.Parse(IP), serverPort), ConnectCallback, null);
        }
        private void ConnectCallback(IAsyncResult ar)
        {
            reliableServerConnectionSocket.EndConnect(ar);
            Connected = true;
            
            reliableServerConnectionSocket.BeginReceive(new byte[] { 0 }, 0, 0, SocketFlags.None, ReceiveCallback, null);

            IPEndPoint tempIPEndPoint = new IPEndPoint(IPAddress.Parse(ConnectedServerIP), serverPort);
            unreliableServerConnectionSocket.Connect(tempIPEndPoint);
            unreliableServerConnectionSocket.BeginReceive(UDPReceiveCallback, tempIPEndPoint);
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                reliableServerConnectionSocket.EndReceive(ar);
                //Tratamento de dados
                byte[] buffer = new byte[4];
                reliableServerConnectionSocket.Receive(buffer, 0, 4, 0);
                int dataSize = BitConverter.ToInt32(buffer, 0);
                if (dataSize <= 0)
                    throw new SocketException();

                buffer = new byte[dataSize];
                int received = reliableServerConnectionSocket.Receive(buffer, 0, buffer.Length, 0);
                while (received < dataSize)
                {
                    received += reliableServerConnectionSocket.Receive(buffer, received, dataSize - received, 0);
                }

                lock (packetBuffers_lock)
                    packetBuffers.Enqueue(buffer);

                reliableServerConnectionSocket.BeginReceive(new byte[] { 0 }, 0, 0, 0, ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                Connected = false;
            }
        }
        private void UDPReceiveCallback(IAsyncResult ar)
        {
            try
            {
                IPEndPoint endPoint = (IPEndPoint)ar.AsyncState;
                
                byte[] buffer = unreliableServerConnectionSocket.EndReceive(ar, ref endPoint);
                
                if (buffer.Length <= 0)
                    throw new SocketException();
                
                lock (packetBuffers_lock)
                    packetBuffers.Enqueue(buffer);

                unreliableServerConnectionSocket.BeginReceive(UDPReceiveCallback, endPoint);
            }
            catch
            {
            }
        }

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
                //Using TCP
                byte[] buffer = DynamicPacketIO.GetAllData();
                if (buffer.Length > 0)
                    Send(buffer);

                //Using UDP
                buffer = DynamicPacketIO.GetAllUnreliableData();
                if (buffer.Length > 0)
                    unreliableServerConnectionSocket.Send(buffer, buffer.Length);

                //Ler dados
                bool packetBuffers_NotLocked = Monitor.TryEnter(packetBuffers_lock, 10);
                try
                {
                    if (packetBuffers_NotLocked && packetBuffers.Count > 0)
                    {
                        ReceiveData(packetBuffers); //We could use Dequeue inside ReceiveData
                        packetBuffers.Clear();
                    }
                }
                finally
                {
                    Monitor.Exit(packetBuffers_lock);
                }
            }
            else if (wasConnected)
            {
                UnityEngine.Debug.Log("Desconectados!");
                wasConnected = false;
                Disconnection?.Invoke();
            }
        }
        private void ReceiveData(Queue<byte[]> packets)
        {
            foreach(byte[] data in packets)
            {
                if (data.Length > 0)
                {
                    PacketReader packet = new PacketReader(data);
                    try
                    {
                        DynamicPacketIO.ReadReceivedPacket(ref packet);
                    }
                    catch (Exception ex)
                    {
                        UnityEngine.Debug.Log($"Erro ao ler dados do servidor: {ex.Source} | {ex.Message}");
                    }
                }
            }
        }
        private void Send(byte[] data)
        {
            lock (this) //Será que isso ajuda? Sla, mas não quero que crashe de novo, se quiser tire esse lock e teste ai
            {
                try
                {   //Bruh momiento (pf não inverter na próxima vez, más lembranças)
                    byte[] sizeBuffer = BitConverter.GetBytes(data.Length);
                    reliableServerConnectionSocket.Send(sizeBuffer, 0, sizeBuffer.Length, 0);
                    reliableServerConnectionSocket.Send(data, 0, data.Length, 0);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.Log("Erro ao enviar dados >> " + ex.Message);
                }
            }
        }
        public void Close()
        {
            if(reliableServerConnectionSocket != null)
                reliableServerConnectionSocket.Close();

            if (unreliableServerConnectionSocket != null)
                unreliableServerConnectionSocket.Close();

            CurrentClient = null;
        }

        public event ConnectionHandler Connection;
        public delegate void ConnectionHandler();

        public event DisconnectionHandler Disconnection;
        public delegate void DisconnectionHandler();
    }
}
