using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using SNet_Server.Utils;

namespace SNet_Server.Sockets
{
    public class Listener
    {
        private Dictionary<string, IPEndPoint> Clients;

        private List<IPEndPoint> PendingConnectionVerifications;
        private object PendingConnectionVerifications_LOCK = new object();
        private const int MAX_WAITING_TIME_FOR_VERIFICATION = 2000;

        private ReliablePacketHandler ReliablePackets;
        private object ReliablePackets_LOCK = new object();
        private const int MAX_WAITING_TIME_FOR_RELIABLE_PACKETS = 1000;

        private Dictionary<IPEndPoint,DateTime> TimeOfLastReceivedData;
        private object TimeOfLastReceivedData_LOCK = new object();
        private const int MAX_WAITING_TIME_FOR_TIMEDOUT = 4000;


        public bool Listening
        {
            get;
            private set;
        }

        public int Port
        {
            get;
            private set;
        }

        private EndPoint AllowedClients;

        private Socket s;
        private const int DATAGRAM_MAX_SIZE = 1284;

        public Listener(int port)
        {
            Port = port;
            s = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            Clients = new Dictionary<string, IPEndPoint>();

            TimeOfLastReceivedData = new Dictionary<IPEndPoint, DateTime>();
            PendingConnectionVerifications = new List<IPEndPoint>();
            ReliablePackets = new ReliablePacketHandler();
        }
        public void Start(bool anyConnections = true)
        {
            if (Listening)
                return;

            s.Bind(new IPEndPoint(0, Port));

            if (anyConnections)
            {
                AllowedClients = new IPEndPoint(IPAddress.Any, 0);

                string localIPv4 = GetLocalIPAddress();
                if (localIPv4 != null)
                    Console.WriteLine("Server IP = {0}:{1} <<", localIPv4, Port);
                else
                    Console.WriteLine("Não conseguimos o IPv4");
            }
            else
                AllowedClients = new IPEndPoint(IPAddress.Parse("127.1.0.0"), 0);

            Listening = true;

            //Checa se os clientes não desconectaram sem avisar
            Util.DelayedAction(MAX_WAITING_TIME_FOR_TIMEDOUT / 2, () => { CheckIfClientsAreConnected(); });

            byte[] nextDatagramBuffer = new byte[DATAGRAM_MAX_SIZE];
            s.BeginReceiveFrom(nextDatagramBuffer, 0, nextDatagramBuffer.Length, SocketFlags.None, ref AllowedClients, ReceiveCallback, nextDatagramBuffer);
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
                            Connection(datagramBuffer, (IPEndPoint)sender);
                            break;
                        case PacketTypes.PACKET:
                            Receive(datagramBuffer, (IPEndPoint)sender);
                            break;
                        case PacketTypes.RELIABLE_RECEIVED:
                            ReceiveReliable_Receive(datagramBuffer, (IPEndPoint)sender);
                            break;
                        case PacketTypes.RELIABLE_SEND:
                            ReceiveReliable_Send(datagramBuffer, (IPEndPoint)sender);
                            break;
                        case PacketTypes.DISCONNECTION:
                            Disconnections((IPEndPoint)sender);
                            break;
                    }
                }
                lock (TimeOfLastReceivedData_LOCK)
                {
                    TimeOfLastReceivedData[(IPEndPoint)sender] = DateTime.UtcNow;
                }

                byte[] nextDatagramBuffer = new byte[DATAGRAM_MAX_SIZE];
                s.BeginReceiveFrom(nextDatagramBuffer, 0, nextDatagramBuffer.Length, SocketFlags.None, ref AllowedClients, ReceiveCallback, nextDatagramBuffer);
            }
            catch (Exception ex)
            {
                if (ex.GetType() != typeof(SocketException))
                    Console.WriteLine("Erro no callback ao receber dados do Client {0}: {1}\n\t{2}", ((IPEndPoint)sender).Address, ex.Source, ex.Message);
                else
                {
                    Console.WriteLine("Erro com socket tipo {0}: {1}\n\t{2}", ((SocketException)ex).ErrorCode, ex.Source, ex.Message);
                    Disconnections((IPEndPoint)sender, DisconnectionType.ClosedByUser, false);
                }

                byte[] nextDatagramBuffer = new byte[DATAGRAM_MAX_SIZE];
                s.BeginReceiveFrom(nextDatagramBuffer, 0, nextDatagramBuffer.Length, SocketFlags.None, ref AllowedClients, ReceiveCallback, nextDatagramBuffer);
            }
        }

        //Cliente -> Servidor -> Cliente -> Servidor
        private void Connection(byte[] dgram, IPEndPoint sender)
        {
            lock (PendingConnectionVerifications_LOCK)
            {
                if (PendingConnectionVerifications.Contains(sender))
                {
                    //Quer dizer que ele ainda ta conectado, então resetar a verificação de conecção
                    if (!Clients.ContainsValue(sender))
                    {
                        string id = Guid.NewGuid().ToString();
                        Clients.Add(id, sender);

                        lock (TimeOfLastReceivedData_LOCK)
                        {
                            TimeOfLastReceivedData.Add(sender, DateTime.UtcNow);
                        }

                        OnClientConnection?.Invoke(id);
                    }
                    PendingConnectionVerifications.Remove(sender);
                    return;
                }
                //Quer dizer que ele enviou sem necessidade
                if (Clients.ContainsValue(sender))
                    return;

                //Quer dizer que é um novo cliente, fazer o processo de enviar e receber o pedido
                PendingConnectionVerifications.Add(sender);
            }
            //Enviar que precisamos da confirmação
            byte[] awnserBuffer = new byte[5];

            awnserBuffer[0] = (byte)PacketTypes.CONNECTION;
            Array.Copy(BitConverter.GetBytes(MAX_WAITING_TIME_FOR_TIMEDOUT), 0, awnserBuffer, 1, 4);

            s.SendTo(awnserBuffer, sender);

            //Usar aqui possiveis dados que vieram com o dgram

            //O limite de tempo para a confirmação da verificação
            Util.DelayedAction(MAX_WAITING_TIME_FOR_VERIFICATION, ()=> { VerifyIfClientIsConnected(sender); });
        }
        private void VerifyIfClientIsConnected(IPEndPoint senderToVerify)
        {
            //Retira da lista, dizendo que o pedido é ignorado para clientes novos
            //e se já for um cliente quer dizer que deu timedout
            lock (PendingConnectionVerifications_LOCK)
            {
                PendingConnectionVerifications.Remove(senderToVerify);

                if (Clients.ContainsValue(senderToVerify))
                    Disconnections(senderToVerify, DisconnectionType.TimedOut);
            }
        }

        //Cliente -> Servidor -> Cliente
        private void Disconnections(IPEndPoint sender, DisconnectionType disconnectionType = DisconnectionType.ClosedByUser, bool sendDisconectionMessage = true)
        {
            //Se o cliente está mesmo conectado então enviar que desconectou mesmo e uma verificação de tal fato
            if (Clients.ContainsValue(sender))
            {
                var keyValuePair = Clients.First((pair) => pair.Value.Equals(sender));
                Clients.Remove(keyValuePair.Key);

                lock (TimeOfLastReceivedData_LOCK)
                {
                    TimeOfLastReceivedData.Remove(sender);
                }

                OnClientDisconnection?.Invoke(keyValuePair.Key, disconnectionType);

                if (sendDisconectionMessage)
                {
                    byte[] awnserBuffer = BitConverter.GetBytes((byte)PacketTypes.DISCONNECTION);
                    s.SendTo(awnserBuffer, sender);
                }
            }            
        }

        private bool CheckIfClientsAreConnected()
        {
            DateTime currentTime = DateTime.UtcNow;
            lock (TimeOfLastReceivedData_LOCK)
            {
                foreach(var clientTimes in TimeOfLastReceivedData)
                {
                    int lastReceiveDeltaTime = (currentTime - clientTimes.Value).Milliseconds;

                    //Se o cliente mesmo depois MAX_WAITING_TIME_FOR_TIMEDOUT não ter enviado nada fazer o processo de confirmar se ele está conectado
                    if (lastReceiveDeltaTime > MAX_WAITING_TIME_FOR_TIMEDOUT)
                    {
                        byte[] awnserBuffer = BitConverter.GetBytes((byte)PacketTypes.CONNECTION);
                        s.SendTo(awnserBuffer, clientTimes.Key);

                        Util.DelayedAction(MAX_WAITING_TIME_FOR_TIMEDOUT, () => { VerifyIfClientIsConnected(clientTimes.Key); });
                    }
                }
            }
            return !Listening;
        }

        private void Receive(byte[] dgram, IPEndPoint sender)
        {
            //Se o cliente está mesmo conectado então podemos receber dados dele
            if (Clients.ContainsValue(sender))
            {
                var keyValuePair = Clients.First((pair) => pair.Value.Equals(sender));
                byte[] treatedDGram = new byte[dgram.Length - 1];
                Array.Copy(dgram, 1, treatedDGram, 0, treatedDGram.Length);
                OnClientReceivedData?.Invoke(treatedDGram, keyValuePair.Key);
            }
        }

        private void ReceiveReliable_Send(byte[] dgram, IPEndPoint sender)
        {
            if (Clients.ContainsValue(sender))
            {
                //Resposta de que recebemos o pacote reliable
                byte[] awnserBuffer = new byte[5];
                awnserBuffer[0] = (byte)PacketTypes.RELIABLE_RECEIVED; //Header de ter recebido
                Array.Copy(dgram, 1, awnserBuffer, 1, 4); //ID da mensagem recebida
                s.SendTo(awnserBuffer, sender);
                // --

                var keyValuePair = Clients.First((pair) => pair.Value.Equals(sender));
                byte[] treatedDGram = new byte[dgram.Length - 5];
                Array.Copy(dgram, 5, treatedDGram, 0, treatedDGram.Length);
                OnClientReceivedData?.Invoke(treatedDGram, keyValuePair.Key);
            }
        }

        private void ReceiveReliable_Receive(byte[] dgram, IPEndPoint sender)
        {
            if (Clients.ContainsValue(sender))
            {
                int packeID = BitConverter.ToInt32(dgram, 1);
                lock (ReliablePackets_LOCK)
                {
                    var keyValuePair = Clients.First((pair) => pair.Value.Equals(sender));
                    ReliablePackets.ClientReceivedData(packeID, keyValuePair.Key);
                }
            }
        }

        public void SendAll(byte[] dgram, params string[] dontSendTo)
        {
            byte[] dataGramToSend = new byte[dgram.Length + 1];
            dataGramToSend[0] = (byte)PacketTypes.PACKET;
            Array.Copy(dgram, 0, dataGramToSend, 1, dgram.Length);

            foreach (var c in Clients)
            {
                if(!dontSendTo.Contains(c.Key))
                    s.SendTo(dataGramToSend, c.Value);
            }
        }

        public bool Send(byte[] dgram, string client)
        {
            if (Clients.TryGetValue(client, out IPEndPoint clientEndPoint) && dgram.Length < DATAGRAM_MAX_SIZE)
            {
                //Adicionar o header PACKET na frente da mensagem
                byte[] dataGramToSend = new byte[dgram.Length + 1];
                dataGramToSend[0] = (byte)PacketTypes.PACKET;
                Array.Copy(dgram, 0, dataGramToSend, 1, dgram.Length);

                s.SendTo(dataGramToSend, clientEndPoint);
                return true;
            }
            return false;
        }

        public bool SendAllReliable(byte[] dgram, params string[] dontSendTo)
        {
            if (dgram.Length >= DATAGRAM_MAX_SIZE)
                return false;

            lock (ReliablePackets_LOCK)
            {
                string[] clientsToSendTo = Clients.Keys.Where((client) => !dontSendTo.Contains(client)).ToArray();

                if (clientsToSendTo.Length > 0)
                {
                    ReliablePackets.Add(dgram, out int packetID, clientsToSendTo);

                    Util.RepeatDelayedAction(MAX_WAITING_TIME_FOR_RELIABLE_PACKETS, MAX_WAITING_TIME_FOR_RELIABLE_PACKETS
                        , () => CheckReliableSentData(packetID));

                    for (int i =0; i< clientsToSendTo.Length;i++)
                        SendReliable(dgram, packetID, clientsToSendTo[i]);
                }
            }
            return true;

        }
        public bool SendReliable(byte[] dgram, string client)
        {
            if (Clients.TryGetValue(client, out IPEndPoint clientEndPoint) && dgram.Length < DATAGRAM_MAX_SIZE)
            {
                lock (ReliablePackets_LOCK)
                    {
                        ReliablePackets.Add(dgram, out int packetID, client);

                        Util.RepeatDelayedAction(MAX_WAITING_TIME_FOR_RELIABLE_PACKETS, MAX_WAITING_TIME_FOR_RELIABLE_PACKETS
                            , () => CheckReliableSentData(packetID));

                        SendReliable(dgram, packetID, client);
                    }
                    return true;
            }
            return false;
        }
        private bool CheckReliableSentData(int packetID)
        {
            lock (ReliablePackets_LOCK)
            {
                if (!ReliablePackets.TryGetValue(packetID, out ReliablePacket reliablePacket))
                    return true;

                //Checar se algum cliente que era para responder se desconectou
                foreach (string clientsNoLongerConnected in reliablePacket.ClientsLeftToReceive.Except(Clients.Keys))
                    ReliablePackets.ClientReceivedData(reliablePacket.PacketID, clientsNoLongerConnected);
                
                if (ReliablePackets.Contains(reliablePacket))
                {
                    for (int i = 0; i < reliablePacket.ClientsLeftToReceive.Count; i++)
                        SendReliable(reliablePacket.Data, packetID, reliablePacket.ClientsLeftToReceive[i]);

                    return false;
                }
                return true;
            }
        }
        //Client -> Server -> Client
        private void SendReliable(byte[] dgram, int packetID, string client)
        {
            byte[] dataGramToSend = new byte[dgram.Length + 1 + 4];
            dataGramToSend[0] = (byte)PacketTypes.RELIABLE_SEND; // Header
            Array.Copy(BitConverter.GetBytes(packetID), 0, dataGramToSend, 1, 4); // Packet ID para reliable packet

            Array.Copy(dgram, 0, dataGramToSend, 5, dgram.Length);
            s.SendTo(dataGramToSend, Clients[client]);
        }

        public void Stop()
        {
            if (!Listening)
                return;
            Listening = false;

            lock (PendingConnectionVerifications_LOCK)
            {
                PendingConnectionVerifications.Clear();
            }

            foreach (var c in Clients)
            {
                byte[] disconnectionBuffer = BitConverter.GetBytes((byte)PacketTypes.DISCONNECTION);

                s.SendTo(disconnectionBuffer, c.Value);
            }

            s.Close();
            Clients.Clear();
        }

        public delegate void ClientConnection(string id);
        public delegate void ClientDisconnection(string id, DisconnectionType reason);
        public delegate void ClientReceivedData(byte[] dgram, string id);

        public event ClientConnection OnClientConnection;
        public event ClientDisconnection OnClientDisconnection;
        public event ClientReceivedData OnClientReceivedData;

        /// <summary>
        /// Returns the local IP
        /// </summary>
        /// <param name="addressFamily">Defaults to IPv4</param>
        /// <returns>If no IP from that AddressFamily is found, returns an empty string</returns>
        public static string GetLocalIPAddress(AddressFamily addressFamily = AddressFamily.InterNetwork)
        {
            IPAddress[] IPArray = Dns.GetHostEntry(Dns.GetHostName()).AddressList;
            foreach (var ip in IPArray)
            {
                if (ip.AddressFamily == addressFamily)
                {
                    return ip.ToString();
                }
            }
            return "";
        }
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