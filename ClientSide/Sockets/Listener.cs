using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;


namespace ClientSide.Sockets
{
    public class Listener
    {
        private EndPoint ServerEndPoint;

        private const int MAX_WAITING_TIME_FOR_VERIFICATION = 2000;

        private Socket s;
        private const int DATAGRAM_MAX_SIZE = 1284;

        private bool Connected;
        private object Connected_LOCK = new object();

        public Listener()
        {
            s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Bind(new IPEndPoint(IPAddress.Any, 0));
        }

        /// <summary>
        /// Disconected any prior connection before attempting to connect, the attempts happen in another thread
        /// </summary>
        /// <param name="IP"></param>
        public void TryConnect(string IP, int port)
        {
            ServerEndPoint = new IPEndPoint(IPAddress.Parse(IP), port);
            s.Connect(ServerEndPoint);

            byte[] connectionRequestBuffer = BitConverter.GetBytes((byte)PacketTypes.CONNECTION);
            s.SendTo(connectionRequestBuffer, ServerEndPoint);

            UnityEngine.Debug.Log(string.Format("Tentando Conectar em: {0}:{1}", IP, port));

            Connected = false;

            new Thread(() =>
            {
                Thread.Sleep(MAX_WAITING_TIME_FOR_VERIFICATION);
                lock (Connected_LOCK)
                {
                    if (!Connected)
                    {
                        s.Close();
                        OnFailConnection?.Invoke();
                    }
                }
            }).Start();

            byte[] nextDatagramBuffer = new byte[DATAGRAM_MAX_SIZE];
            s.BeginReceiveFrom(nextDatagramBuffer, 0, nextDatagramBuffer.Length, SocketFlags.None, ref ServerEndPoint, ReceiveCallback, nextDatagramBuffer);
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            try
            {
                byte[] datagramBuffer = (byte[])ar.AsyncState;
                int datagramSize = s.EndReceiveFrom(ar, ref sender);

                if (datagramSize > 0)
                {
                    switch ((PacketTypes)datagramBuffer[0])
                    {
                        case PacketTypes.CONNECTION:
                            Connection(datagramBuffer);
                            break;
                        case PacketTypes.PACKET:
                            Receive(datagramBuffer);
                            break;
                        case PacketTypes.DISCONNECTION:
                            Disconnection(datagramBuffer);
                            break;
                    }
                }

                byte[] nextDatagramBuffer = new byte[DATAGRAM_MAX_SIZE];
                s.BeginReceiveFrom(nextDatagramBuffer, 0, nextDatagramBuffer.Length, SocketFlags.None, ref ServerEndPoint, ReceiveCallback, nextDatagramBuffer);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(SocketException))
                {
                    Disconnection(null);
                    return;
                }

                byte[] nextDatagramBuffer = new byte[DATAGRAM_MAX_SIZE];
                s.BeginReceiveFrom(nextDatagramBuffer, 0, nextDatagramBuffer.Length, SocketFlags.None, ref ServerEndPoint, ReceiveCallback, nextDatagramBuffer);
            }
        }

        //Cliente -> Servidor -> Cliente -> Servidor
        private void Connection(byte[] dgram)
        {
            lock (Connected_LOCK)
            {
                //Recebemos a confirmação que estamos conectados, portanto a verificação foi um sucesso
                if (!Connected)
                {
                    Connected = true;
                    OnConnection?.Invoke();
                }
            }
            //Enviar a confirmação que recebemos a conecção
            byte[] awnserBuffer = BitConverter.GetBytes((byte)PacketTypes.CONNECTION);
            s.SendTo(awnserBuffer, ServerEndPoint);
            //Usar aqui possiveis dados que vieram com o dgram
        }

        //Cliente -> Servidor -> Cliente
        private void Disconnection(byte[] dgram)
        {
            //Usar aqui possiveis dados que vieram com o dgram
            s.Close();
            OnDisconnection?.Invoke();

            lock (Connected_LOCK)
            {
                Connected = false;
            }
        }

        private void Receive(byte[] dgram)
        {
            byte[] treatedDGram = new byte[dgram.Length - 1];
            Array.Copy(dgram, 1, treatedDGram, 0, treatedDGram.Length);
            OnReceiveData?.Invoke(treatedDGram);
        }

        public bool Send(byte[] dgram)
        {
            lock (Connected_LOCK)
            {
                if (Connected && dgram.Length < DATAGRAM_MAX_SIZE)
                {
                    //Adicionar o header PACKET na frente da mensagem
                    byte[] dataGramToSend = new byte[dgram.Length + 1];
                    dataGramToSend[0] = (byte)PacketTypes.PACKET;
                    Array.Copy(dgram, 0, dataGramToSend, 1, dgram.Length);
                    s.SendTo(dataGramToSend, ServerEndPoint);
                    return true;
                }
                return false;
            }
        }
        public void Disconnect()
        {
            lock (Connected_LOCK)
            {
                if (!Connected)
                    return;
            }

            byte[] disconnectionBuffer = BitConverter.GetBytes((byte)PacketTypes.DISCONNECTION);
            s.SendTo(disconnectionBuffer, ServerEndPoint);

            s.Close();
        }
        
        public delegate void ReceivedData(byte[] dgram);

        public event Action OnConnection;
        public event Action OnFailConnection;
        public event Action OnDisconnection;
        public event ReceivedData OnReceiveData;
    }
    enum PacketTypes : byte
    {
        CONNECTION,
        PACKET,
        DISCONNECTION
    }
}
