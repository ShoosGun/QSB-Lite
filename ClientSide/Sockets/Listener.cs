using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;

using SNet_Client.Utils;


namespace SNet_Client.Sockets
{
    //TODO Add timeout disconects
    //a ) Store a DateTime with the time of the last server sent package
    //b ) If it took more then the MAX_WAITING_TIME_FOR_TIMEDOUT consider that the server is gone
    //b.1 ) We can trust in this fact because the server will always ask if the client is connected every MAX_WAITING_TIME_FOR_TIMEDOUT miliseconds
    //so we can just pack that data inside de Connection message and set our MAX_WAITING_TIME_FOR_TIMEDOUT to be equal to 4 times it
    public class Listener
    {
        private EndPoint ServerEndPoint;

        private const int MAX_WAITING_TIME_FOR_VERIFICATION = 2000;

        private int maxWaitingTimeForTimeoutOfTheServer = 4000;
        private DateTime timeOfLastReceivedServerMessage;

        private ReliablePacketHandler ReliablePackets;
        private object ReliablePackets_LOCK = new object();
        private const int MAX_WAITING_TIME_FOR_RELIABLE_PACKETS = 1000;


        private Socket s;
        private const int DATAGRAM_MAX_SIZE = 1284;

        private bool Connecting;
        private bool Connected;
        private object Connected_LOCK = new object();

        public Listener()
        {
            ReliablePackets = new ReliablePacketHandler();
            Connecting = false;
            Connected = false;
        }

        /// <summary>
        /// Disconected any prior connection before attempting to connect, the attempts happen in another thread
        /// </summary>
        /// <param name="IP"></param>
        public void TryConnect(string IP, int port)
        {
            lock (Connected_LOCK)
            {
                if (Connecting || Connected)
                    return;

                Connecting = true;
            }

            s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Bind(new IPEndPoint(IPAddress.Any, 0));

            ServerEndPoint = new IPEndPoint(IPAddress.Parse(IP), port);
            s.Connect(ServerEndPoint);
            byte[] connectionRequestBuffer = BitConverter.GetBytes((byte)PacketTypes.CONNECTION);
            s.SendTo(connectionRequestBuffer, ServerEndPoint);

            UnityEngine.Debug.Log(string.Format("Tentando Conectar em: {0}:{1}", IP, port));

            Util.DelayedAction(MAX_WAITING_TIME_FOR_VERIFICATION, () =>
            {
                 lock (Connected_LOCK)
                 {
                     Connecting = false;
                     if (!Connected)
                     {
                         s.Close();
                         OnFailConnection?.Invoke();
                     }
                 }
            });
            
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

                PacketTypes packetType = (PacketTypes)datagramBuffer[0];
                
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
                        case PacketTypes.RELIABLE_RECEIVED:
                            ReceiveReliable_Receive(datagramBuffer);
                            break;
                        case PacketTypes.RELIABLE_SEND:
                            ReceiveReliable_Send(datagramBuffer);
                            break;
                        case PacketTypes.DISCONNECTION:
                            Disconnection(datagramBuffer);
                            break;
                    }
                }

                timeOfLastReceivedServerMessage = DateTime.UtcNow;

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
                    //O timeout que o server nos da caso não mandemos mensagens por um periodo
                    maxWaitingTimeForTimeoutOfTheServer = BitConverter.ToInt32(dgram, 1);
                    Connected = true;
                    OnConnection?.Invoke();


                    Util.DelayedAction(maxWaitingTimeForTimeoutOfTheServer, () => CheckIfServerIsStillUp());
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

        private void CheckIfServerIsStillUp()
        {
            if((DateTime.UtcNow - timeOfLastReceivedServerMessage).Milliseconds > maxWaitingTimeForTimeoutOfTheServer * 6)
            {
                //Disconnection por timedout
                Disconnect();
            }
            else
            {
                Util.DelayedAction(maxWaitingTimeForTimeoutOfTheServer, () => CheckIfServerIsStillUp());
            }
        }

        private void Receive(byte[] dgram)
        {
            byte[] treatedDGram = new byte[dgram.Length - 1];
            Array.Copy(dgram, 1, treatedDGram, 0, treatedDGram.Length);
            OnReceiveData?.Invoke(treatedDGram);
        }

        private void ReceiveReliable_Send(byte[] dgram)
        {
            //Resposta de que recebemos o pacote reliable
            byte[] awnserBuffer = new byte[5];
            awnserBuffer[0] = (byte)PacketTypes.RELIABLE_RECEIVED; //Header de ter recebido
            Array.Copy(dgram, 1, awnserBuffer, 1, 4); //ID da mensagem recebida
            s.SendTo(awnserBuffer, ServerEndPoint);
            // --

            byte[] treatedDGram = new byte[dgram.Length - 5];
            Array.Copy(dgram, 5, treatedDGram, 0, treatedDGram.Length);
            OnReceiveData?.Invoke(treatedDGram);
        }

        private void ReceiveReliable_Receive(byte[] dgram)
        {
            int packeID = BitConverter.ToInt32(dgram, 1);
            lock (ReliablePackets_LOCK)
            {
                ReliablePackets.ReceptorReceivedData(packeID);
            }
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

        public bool SendReliable(byte[] dgram)
        {
            lock (Connected_LOCK)
            {
                if (Connected && dgram.Length < DATAGRAM_MAX_SIZE)
                {
                    lock (ReliablePackets_LOCK)
                    {
                        ReliablePackets.Add(dgram, out int packetID);
                        //Inicia o processo que a cada MAX_WAITING_TIME_FOR_RELIABLE_PACKETS vai vereficar se foi enviado e retransmitir
                        Util.RepeatDelayedAction(MAX_WAITING_TIME_FOR_RELIABLE_PACKETS, MAX_WAITING_TIME_FOR_RELIABLE_PACKETS
                            , () => CheckReliableSentData(packetID));

                        SendReliable(dgram, packetID);
                    }
                    return true;
                }
                return false;
            }
        }
        private bool CheckReliableSentData(int packetID)
        {
            lock (ReliablePackets_LOCK)
            {
                if (ReliablePackets.Contains(packetID))
                {
                    SendReliable(ReliablePackets[packetID].Data, packetID);
                    return false; //Se tiver que receber não precisa pedir para parar
                }
                return true;//Se não existir mais pode parar
            }
        }
        //Client -> Server -> Client
        private void SendReliable(byte[] dgram, int packetID)
        {
            byte[] dataGramToSend = new byte[dgram.Length + 1 + 4];
            dataGramToSend[0] = (byte)PacketTypes.RELIABLE_SEND; // Header
            Array.Copy(BitConverter.GetBytes(packetID), 0, dataGramToSend, 1, 4); // Packet ID para reliable packet

            Array.Copy(dgram, 0, dataGramToSend, 5, dgram.Length);
            s.SendTo(dataGramToSend, ServerEndPoint);
        }

        public void Disconnect()
        {
            lock (Connected_LOCK)
            {
                if (!Connected)
                    return;
                Connected = false;
            }

            byte[] disconnectionBuffer = BitConverter.GetBytes((byte)PacketTypes.DISCONNECTION);
            s.SendTo(disconnectionBuffer, ServerEndPoint);

            OnDisconnection?.Invoke();
            s.Close();

            lock (ReliablePackets_LOCK)
            {
                ReliablePackets.Clear();
            }
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
        DISCONNECTION,
        RELIABLE_SEND,
        RELIABLE_RECEIVED
    }
}
