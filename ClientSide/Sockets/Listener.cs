using System;
using System.Net;
using System.Net.Sockets;

using SNet_Client.Utils;


namespace SNet_Client.Sockets
{
    //TODO fazer a mesma mudança feita no servidor em relação a verificação de coisas
    public class Listener
    {
        private Server server;
        private int maxWaitingTimeForTimeoutOfTheServer = 4000;

        private const int MAX_WAITING_TIME_FOR_VERIFICATION = 2000;
        private const int MAX_WAITING_TIME_FOR_RELIABLE_PACKETS = 1000;

        private SNETConcurrentDictionary<int, ReliablePacket> ReliablePackets;

        private Socket s;
        private const int DATAGRAM_MAX_SIZE = 1284;

        public Listener()
        {
            ReliablePackets = new SNETConcurrentDictionary<int, ReliablePacket>();
            server = new Server();
        }

        /// <summary>
        /// Disconected any prior connection before attempting to connect, the attempts happen in another thread
        /// </summary>
        /// <param name="IP"></param>
        public void TryConnect(string IP, int port)
        {
                if (server.GetConnecting() || server.GetConnected())
                    return;

            server.SetConnecting(true);


        s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            s.Bind(new IPEndPoint(IPAddress.Any, 0));

            EndPoint serverEndpoint = new IPEndPoint(IPAddress.Parse(IP), port);
            server.SetServerEndPoint(serverEndpoint);

            s.Connect(serverEndpoint);
            byte[] connectionRequestBuffer = BitConverter.GetBytes((byte)PacketTypes.CONNECTION);
            s.SendTo(connectionRequestBuffer, serverEndpoint);

            UnityEngine.Debug.Log(string.Format("Tentando Conectar em: {0}:{1}", IP, port));

            Util.DelayedAction(MAX_WAITING_TIME_FOR_VERIFICATION, () =>
            {
                if (!server.GetConnected())
                {
                    server.SetConnecting(false);

                    s.Close();
                    OnFailConnection?.Invoke();
                }
            });
            
            byte[] nextDatagramBuffer = new byte[DATAGRAM_MAX_SIZE];
            s.BeginReceiveFrom(nextDatagramBuffer, 0, nextDatagramBuffer.Length, SocketFlags.None, ref serverEndpoint, ReceiveCallback, nextDatagramBuffer);
        }
        private void ReceiveCallback(IAsyncResult ar)
        {
            EndPoint sender = new IPEndPoint(IPAddress.Any, 0);
            EndPoint serverEndpoint = server.GetServerEndPoint();
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

                server.SetTimeOfLastReceivedMessage(DateTime.UtcNow);

                byte[] nextDatagramBuffer = new byte[DATAGRAM_MAX_SIZE];
                s.BeginReceiveFrom(nextDatagramBuffer, 0, nextDatagramBuffer.Length, SocketFlags.None, ref serverEndpoint, ReceiveCallback, nextDatagramBuffer);
            }
            catch (Exception ex)
            {
                if (ex.GetType() == typeof(SocketException))
                {
                    Disconnection(null);
                    return;
                }

                byte[] nextDatagramBuffer = new byte[DATAGRAM_MAX_SIZE];
                s.BeginReceiveFrom(nextDatagramBuffer, 0, nextDatagramBuffer.Length, SocketFlags.None, ref serverEndpoint, ReceiveCallback, nextDatagramBuffer);
            }
        }

        //Cliente -> Servidor -> Cliente -> Servidor
        private void Connection(byte[] dgram)
        {
            //Recebemos a confirmação que estamos conectados, portanto a verificação foi um sucesso
            if (!server.GetConnected())
            {
                //O timeout que o server nos da caso não mandemos mensagens por um periodo
                maxWaitingTimeForTimeoutOfTheServer = BitConverter.ToInt32(dgram, 1);
                server.SetConnected(true);
                OnConnection?.Invoke();

                Util.DelayedAction(maxWaitingTimeForTimeoutOfTheServer, () => CheckIfServerIsStillUp());
            }
            //Enviar a confirmação que recebemos a conecção
            byte[] awnserBuffer = BitConverter.GetBytes((byte)PacketTypes.CONNECTION);
            s.SendTo(awnserBuffer, server.GetServerEndPoint());
            //Usar aqui possiveis dados que vieram com o dgram
        }

        //Cliente -> Servidor -> Cliente
        private void Disconnection(byte[] dgram)
        {
            //Usar aqui possiveis dados que vieram com o dgram
            s.Close();
            OnDisconnection?.Invoke();

            server.SetConnected(false);
        }

        private void CheckIfServerIsStillUp()
        {
            if((DateTime.UtcNow - server.GetTimeOfLastReceivedMessage()).Milliseconds > maxWaitingTimeForTimeoutOfTheServer * 6)
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
            s.SendTo(awnserBuffer, server.GetServerEndPoint());
            // --

            byte[] treatedDGram = new byte[dgram.Length - 5];
            Array.Copy(dgram, 5, treatedDGram, 0, treatedDGram.Length);
            OnReceiveData?.Invoke(treatedDGram);
        }

        private void ReceiveReliable_Receive(byte[] dgram)
        {
            int packeID = BitConverter.ToInt32(dgram, 1);

            ReliablePackets.Remove(packeID);
        }

        public bool Send(byte[] dgram)
        {
            if (server.GetConnected() && dgram.Length < DATAGRAM_MAX_SIZE)
            {
                //Adicionar o header PACKET na frente da mensagem
                byte[] dataGramToSend = new byte[dgram.Length + 1];
                dataGramToSend[0] = (byte)PacketTypes.PACKET;
                Array.Copy(dgram, 0, dataGramToSend, 1, dgram.Length);
                s.SendTo(dataGramToSend, server.GetServerEndPoint());
                return true;
            }
            return false;
        }

        private int NextGeneratedID = int.MinValue;
        private ReliablePacket CreateReliablePacket(byte[] packet)
        {
            int packetID = NextGeneratedID;

            if (NextGeneratedID == int.MaxValue)
                NextGeneratedID = int.MinValue;
            else
                NextGeneratedID += 1;

            return new ReliablePacket(packetID, packet);
        }

        public bool SendReliable(byte[] dgram)
        {
            if (server.GetConnected() && dgram.Length < DATAGRAM_MAX_SIZE)
            {
                ReliablePacket reliablePacket = CreateReliablePacket(dgram);

                ReliablePackets.Add(reliablePacket.PacketID, reliablePacket);
                //Inicia o processo que a cada MAX_WAITING_TIME_FOR_RELIABLE_PACKETS vai vereficar se foi enviado e retransmitir
                Util.RepeatDelayedAction(MAX_WAITING_TIME_FOR_RELIABLE_PACKETS, MAX_WAITING_TIME_FOR_RELIABLE_PACKETS
                    , () => CheckReliableSentData(reliablePacket.PacketID));

                SendReliable(reliablePacket);
                return true;
            }
            return false;
        }
        private bool CheckReliableSentData(int packetID)
        {
            if (ReliablePackets.TryGetValue(packetID, out ReliablePacket packet))
            {
                SendReliable(packet);
                return false; //Se tiver que receber não precisa pedir para parar
            }
            return true;//Se não existir mais pode parar
        }

        //Client -> Server -> Client
        private void SendReliable(ReliablePacket packet)
        {
            byte[] dataGramToSend = new byte[packet.Data.Length + 1 + 4];
            dataGramToSend[0] = (byte)PacketTypes.RELIABLE_SEND; // Header
            Array.Copy(BitConverter.GetBytes(packet.PacketID), 0, dataGramToSend, 1, 4); // Packet ID para reliable packet

            Array.Copy(packet.Data, 0, dataGramToSend, 5, packet.Data.Length);
            s.SendTo(dataGramToSend, server.GetServerEndPoint());
        }

        public void Disconnect()
        {
            if (!server.GetConnected())
                return;
            server.SetConnected(false);

            byte[] disconnectionBuffer = BitConverter.GetBytes((byte)PacketTypes.DISCONNECTION);
            s.SendTo(disconnectionBuffer, server.GetServerEndPoint());

            OnDisconnection?.Invoke();
            s.Close();

            ReliablePackets.Clear();
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
