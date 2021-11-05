using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace ServerSide.Sockets.Servers
{
    //1 - Adicionar o mandar e enviar de mensagens para saber se o cliente está mesmo conectado (FEITO)
    //2 - Adicionar o mandar e receber de mensagens para saber se o cliente está mesmo desconectado (FEITO)
    //TODO 3 - Adicionar uma forma de descobrir se o cliente desconectou sem avisar tal
    public class Listener
    {
        private Dictionary<string, IPEndPoint> Clients;

        private List<IPEndPoint> PendingConnectionVerifications;
        private const int MAX_WAITING_TIME_FOR_VERIFICATION = 2000;

        private object PendingConnectionVerifications_LOCK = new object();

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
            PendingConnectionVerifications = new List<IPEndPoint>();
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
                    Console.WriteLine("Server IP = {0} <<", localIPv4);
                else
                    Console.WriteLine("Não conseguimos o IPv4");
            }
            else
                AllowedClients = new IPEndPoint(IPAddress.Parse("127.1.0.0"), 0);

            Listening = true;

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
                        case PacketTypes.DISCONNECTION:
                            Disconnections(datagramBuffer, (IPEndPoint)sender);
                            break;
                    }
                }
                //TODO fazer maneira de verificar que o cliente na lista ainda está conectado
                //E enviar pedido de confirmação disso

                byte[] nextDatagramBuffer = new byte[DATAGRAM_MAX_SIZE];
                s.BeginReceiveFrom(nextDatagramBuffer, 0, nextDatagramBuffer.Length, SocketFlags.None, ref AllowedClients, ReceiveCallback, nextDatagramBuffer);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro no callback ao receber dados do Client {0}: {1}", ((IPEndPoint)sender).Address, ex.Message);
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
                        OnClientConnection?.Invoke(id);
                    }
                    PendingConnectionVerifications.Remove(sender);
                    //Enviar que está sim conectado o sender
                    //TODO Enviar que está sim conectado o sender
                    return;
                }
                //Quer dizer que ele enviou sem necessidade
                if (Clients.ContainsValue(sender))
                    return;

                //Quer dizer que é um novo cliente, fazer o processo de enviar e receber o pedido
                PendingConnectionVerifications.Add(sender);
            }
            //Enviar que precisamos da confirmação
            byte[] awnserBuffer = BitConverter.GetBytes((byte)PacketTypes.CONNECTION);
            s.SendTo(awnserBuffer, sender);

            //Usar aqui possiveis dados que vieram com o dgram

            //O limite de tempo para a confirmação da verificação
            new Thread(() =>
            {
                IPEndPoint pendingVerificationSender = sender;
                Thread.Sleep(MAX_WAITING_TIME_FOR_VERIFICATION);

                //Ignorar o pedido caso não tenha sido tirado da lista até então
                lock (PendingConnectionVerifications_LOCK)
                {
                    if (PendingConnectionVerifications.Contains(pendingVerificationSender))
                        PendingConnectionVerifications.Remove(pendingVerificationSender);
                }

            }).Start();
        }

        //Cliente -> Servidor -> Cliente
        private void Disconnections(byte[] dgram, IPEndPoint sender)
        {
            //Se o cliente está mesmo conectado então enviar que desconectou mesmo e uma verificação de tal fato
            if (Clients.ContainsValue(sender))
            {
                var keyValuePair = Clients.First((pair) => pair.Value == sender);
                Clients.Remove(keyValuePair.Key);
                OnClientDisconnection?.Invoke(keyValuePair.Key);

                byte[] awnserBuffer = BitConverter.GetBytes((byte)PacketTypes.DISCONNECTION);
                s.SendTo(awnserBuffer, sender);
            }            
        }

        private void Receive(byte[] dgram, IPEndPoint sender)
        {
            //Se o cliente está mesmo conectado então podemos receber dados dele
            if (Clients.ContainsValue(sender))
            {
                var keyValuePair = Clients.First((pair) => pair.Value == sender);
                byte[] treatedDGram = new byte[dgram.Length - 1];
                Array.Copy(dgram, 1, treatedDGram, 0, treatedDGram.Length);
                OnClientReceivedData?.Invoke(treatedDGram, keyValuePair.Key);
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
        public delegate void ClientReceivedData(byte[] dgram, string id);

        public event ClientConnection OnClientConnection;
        public event ClientConnection OnClientDisconnection;
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
        DISCONNECTION
    }
} 