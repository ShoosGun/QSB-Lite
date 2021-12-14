using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;

using SNet_Server.Utils;

namespace SNet_Server.Sockets
{
    public class Listener
    {
        private ConcurrentDictionary<string, Client> Clients;
        private ConcurrentDictionary<IPEndPoint, string> IPEndpointToClientIDMap;
        private ConcurrentDictionary<int, ReliablePacket> ReliablePackets;

        private const int DELTA_TIME_OF_VERIFICATION_LOOP = 1000;
        private const int MAX_WAITING_TIME_FOR_CONNECTION_VERIFICATION = 2000;
        private const int MAX_WAITING_TIME_FOR_TIMEOUT = 4000;

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

            Clients = new ConcurrentDictionary<string, Client>();
            IPEndpointToClientIDMap = new ConcurrentDictionary<IPEndPoint, string>();
            ReliablePackets = new ConcurrentDictionary<int, ReliablePacket>();
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

            //Loop de verificação de informações que dependem de tempo
            Util.RepeatDelayedAction(DELTA_TIME_OF_VERIFICATION_LOOP, DELTA_TIME_OF_VERIFICATION_LOOP, TimedVerificationsLoop);

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
                //Diz que o cliente ainda está conectado
                if(IPEndpointToClientIDMap.TryGetValue((IPEndPoint)sender,out string key))
                {
                    Clients.TryGetValue(key, out Client client);
                    client.TimeOfLastPacket = DateTime.UtcNow;
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

        private bool TimedVerificationsLoop()
        {
            DateTime currentVerificationTime = DateTime.UtcNow;

            List<IPEndPoint> clientsToDisconnect = new List<IPEndPoint>();

            //1 - Checar se os clientes não estão passiveis de receber timeout (novas conecções ou não)
            //Fazer checagem se ele pode receber timeout
            foreach (var client in Clients)
            {
                int timeDifference = (int)(currentVerificationTime - client.Value.TimeOfLastPacket).TotalMilliseconds;
                if (client.Value.IsConnected)
                {
                    if (timeDifference > MAX_WAITING_TIME_FOR_TIMEOUT) //Dar timeout
                    {
                        clientsToDisconnect.Add(client.Value.IpEndpoint);
                    }
                    else if (timeDifference > MAX_WAITING_TIME_FOR_TIMEOUT / 2) //Dar aviso que ele poderá receber timeout
                    {
                        //Pela maneira em que está programado a resposta do cliente para mensagens com CONNECTION podemos reutiliza-las como maneira de verificar se estão conectados
                        s.SendTo(BitConverter.GetBytes((byte)PacketTypes.CONNECTION), client.Value.IpEndpoint);
                    }
                }
                else if (timeDifference > MAX_WAITING_TIME_FOR_CONNECTION_VERIFICATION) //Dar timeout por não ter respondido a tempo
                {
                    clientsToDisconnect.Add(client.Value.IpEndpoint);
                }
            }

            //Dar timeout para todos os clientes que precisam
            for (int i = 0; i < clientsToDisconnect.Count; i++)
                Disconnections(clientsToDisconnect[i], DisconnectionType.TimedOut);

            clientsToDisconnect.Clear();

            //2 - Checar se precisa enviar novas mensagens que sejam reliable
            List<ReliablePacket> packetsToRemove = new List<ReliablePacket>();
            foreach (var packet in ReliablePackets)
            {
                if (packet.Value.ClientsLeftToReceive.Count <= 0)
                {
                    packetsToRemove.Add(packet.Value);
                }
                else
                {
                    for (int i = 0; i < packet.Value.ClientsLeftToReceive.Count; i++)
                        SendReliable(packet.Value.Data, packet.Value.PacketID, packet.Value.ClientsLeftToReceive[i]);
                }
            }
            for (int i = 0; i < packetsToRemove.Count; i++)
                ReliablePackets.Remove(packetsToRemove[i].PacketID, out var p);

            return Listening; //Vai parar esse loop se listener estiver desativado
        }

        //Cliente -> Servidor -> Cliente -> Servidor
        private void Connection(byte[] dgram, IPEndPoint sender)
        {
            Client client;

            if (IPEndpointToClientIDMap.TryGetValue(sender, out string key))
            {
                Clients.TryGetValue(key, out client);

                if (!client.IsConnected)
                    OnClientConnection?.Invoke(client.ID);

                //Se esse cliente existe então podemos definir que ele está sim conectado
                client.IsConnected = true;
                return;
            }

            client = new Client() { IpEndpoint = sender, ID = Guid.NewGuid().ToString() };

            //Se não existia antes quer dizer que é um novo cliente, fazer o processo de enviar e receber o pedido e gravalo como um cliente não conectado
            Clients.TryAdd(client.ID, client);
            IPEndpointToClientIDMap.TryAdd(sender, client.ID);


            byte[] awnserBuffer = new byte[5];
            awnserBuffer[0] = (byte)PacketTypes.CONNECTION;
            Array.Copy(BitConverter.GetBytes(MAX_WAITING_TIME_FOR_TIMEOUT), 0, awnserBuffer, 1, 4);
            s.SendTo(awnserBuffer, sender);
            //Usar aqui possiveis dados que vieram com o dgram            
        }

        //Cliente -> Servidor -> Cliente
        private void Disconnections(IPEndPoint sender, DisconnectionType disconnectionType = DisconnectionType.ClosedByUser, bool sendDisconectionMessage = true)
        {
            //Se o cliente está mesmo conectado então enviar que desconectou mesmo e uma verificação de tal fato
            if (IPEndpointToClientIDMap.TryRemove(sender,out string key))
            {
                Clients.TryRemove(key, out Client client);

                OnClientDisconnection?.Invoke(client.ID, disconnectionType);

                if (sendDisconectionMessage)
                {
                    byte[] awnserBuffer = BitConverter.GetBytes((byte)PacketTypes.DISCONNECTION);
                    s.SendTo(awnserBuffer, sender);
                }
            }
        }

        private void Receive(byte[] dgram, IPEndPoint sender)
        {
            if (!IPEndpointToClientIDMap.TryGetValue(sender, out string key))
                return;

            Clients.TryGetValue(key, out Client client);
            //Se o cliente está mesmo conectado então podemos receber dados dele
            if (!client.IsConnected)
                return;

            byte[] treatedDGram = new byte[dgram.Length - 1];
            Array.Copy(dgram, 1, treatedDGram, 0, treatedDGram.Length);
            OnClientReceivedData?.Invoke(treatedDGram, client.ID);

        }

        private void ReceiveReliable_Send(byte[] dgram, IPEndPoint sender)
        {
            if (!IPEndpointToClientIDMap.TryGetValue(sender, out string key))
                return;

            Clients.TryGetValue(key, out Client client);
            if (!client.IsConnected)
                return;

            //Resposta de que recebemos o pacote reliable
            byte[] awnserBuffer = new byte[5];
            awnserBuffer[0] = (byte)PacketTypes.RELIABLE_RECEIVED; //Header de ter recebido
            Array.Copy(dgram, 1, awnserBuffer, 1, 4); //ID da mensagem recebida
            s.SendTo(awnserBuffer, sender);
            // --

            byte[] treatedDGram = new byte[dgram.Length - 5];
            Array.Copy(dgram, 5, treatedDGram, 0, treatedDGram.Length);
            OnClientReceivedData?.Invoke(treatedDGram, client.ID);
        }

        private void ReceiveReliable_Receive(byte[] dgram, IPEndPoint sender)
        {
            if (!IPEndpointToClientIDMap.TryGetValue(sender, out string key))
                return;

            Clients.TryGetValue(key, out Client client);
            if (!client.IsConnected)
                return;

            int packeID = BitConverter.ToInt32(dgram, 1);
            if (client.ReliablePacketsToReceive.Remove(packeID, out ReliablePacket packet))
            {
                packet.ClientsLeftToReceive.Remove(client.IpEndpoint);

                if (packet.ClientsLeftToReceive.Count <= 0)
                    ReliablePackets.Remove(packet.PacketID, out var p);
            }
        }

        public void SendAll(byte[] dgram, params string[] dontSendTo)
        {
            byte[] dataGramToSend = new byte[dgram.Length + 1];
            dataGramToSend[0] = (byte)PacketTypes.PACKET;
            Array.Copy(dgram, 0, dataGramToSend, 1, dgram.Length);

            foreach (var client in Clients)
            {
                if (!dontSendTo.Contains(client.Value.ID) && client.Value.IsConnected)
                    s.SendTo(dataGramToSend, client.Value.IpEndpoint);
            }
        }

        public bool Send(byte[] dgram, string client)
        {
            if (dgram.Length >= DATAGRAM_MAX_SIZE)
                return false;
            
            Clients.TryGetValue(client, out Client clientData);
            if (!clientData.IsConnected)
                return false;

            //Adicionar o header PACKET na frente da mensagem
            byte[] dataGramToSend = new byte[dgram.Length + 1];
            dataGramToSend[0] = (byte)PacketTypes.PACKET;
            Array.Copy(dgram, 0, dataGramToSend, 1, dgram.Length);

            s.SendTo(dataGramToSend, clientData.IpEndpoint);

            return true;
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
        public bool SendAllReliable(byte[] dgram, params string[] dontSendTo)
        {
            if (dgram.Length >= DATAGRAM_MAX_SIZE)
                return false;

            ReliablePacket packet = CreateReliablePacket(dgram);
            
            foreach (var client in Clients)
            {
                if (!dontSendTo.Contains(client.Value.ID) && client.Value.IsConnected)
                {
                    client.Value.ReliablePacketsToReceive.Add(packet.PacketID, packet);
                    packet.ClientsLeftToReceive.Add(client.Value.IpEndpoint);

                    SendReliable(dgram, packet.PacketID, client.Value.IpEndpoint);
                }
            }
            
            if (packet.ClientsLeftToReceive.Count > 0)
                ReliablePackets.TryAdd(packet.PacketID, packet);

            return true;
        }
        public bool SendReliable(byte[] dgram, string client)
        {
            if (!Clients.TryGetValue(client, out Client clientData) || dgram.Length >= DATAGRAM_MAX_SIZE)
                return false;

            ReliablePacket packet = CreateReliablePacket(dgram);

            clientData.ReliablePacketsToReceive.Add(packet.PacketID, packet);
            packet.ClientsLeftToReceive.Add(clientData.IpEndpoint);

            ReliablePackets.TryAdd(packet.PacketID, packet);

            SendReliable(dgram, packet.PacketID, clientData.IpEndpoint);
            return true;
        }
        //Client -> Server -> Client
        private void SendReliable(byte[] dgram, int packetID, IPEndPoint clientIP)
        {
            byte[] dataGramToSend = new byte[dgram.Length + 1 + 4];
            dataGramToSend[0] = (byte)PacketTypes.RELIABLE_SEND; // Header
            Array.Copy(BitConverter.GetBytes(packetID), 0, dataGramToSend, 1, 4); // Packet ID para reliable packet

            Array.Copy(dgram, 0, dataGramToSend, 5, dgram.Length);
            s.SendTo(dataGramToSend, clientIP);
        }

        public void Stop()
        {
            if (!Listening)
                return;
            Listening = false;

            byte[] disconnectionBuffer = BitConverter.GetBytes((byte)PacketTypes.DISCONNECTION);

            foreach (var client in Clients)
                s.SendTo(disconnectionBuffer, client.Value.IpEndpoint);

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