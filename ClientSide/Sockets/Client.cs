using System;
using SNet_Client.Utils;


namespace SNet_Client.Sockets
{
    public class Client
    {
        Listener l;

        public bool Connected { private set; get; }
        public bool Connecting { get; private set; }

        public long GetUserId() => l.currentUser.Id;

        private SNETConcurrentQueue<QueuedData> receivedData = new SNETConcurrentQueue<QueuedData>();
        private SNETConcurrentQueue<ConnectedMemberStatus> receivedMemberStatus = new SNETConcurrentQueue<ConnectedMemberStatus>();
        private const int MAX_PACKETS_TO_LOOK_EACH_LOOP = 30;
        private const int MAX_WAITING_TIME_TO_READ_PACKET = 10;

        private struct QueuedData
        {
            public byte[] data;
            public long sendingId;
        }
        private struct ConnectedMemberStatus
        {
            public long memberId;
            public bool connectionStatus; //0 - Disconnected, 1 - Connected
        }

        public PacketReceiver packetReceiver { get; private set; }

        private static Client CurrentClient = null;
        public static Client GetClient()
        {
            return CurrentClient;
        }

        public Client()
        {
            if (CurrentClient != null)
                return;

            packetReceiver = new PacketReceiver();
            CurrentClient = this;

            Connected = false;
            Connecting = false;

            l = new Listener();
            l.OnConnection += (() =>
            {
                Connecting = false;
                Connected = true;
            });
            l.OnFailConnection += (() =>
            {
                Connecting = false;
                Connected = false;
            });
            l.OnDisconnection += (() =>
            {
                Connected = false;
            });
            l.OnReceiveData += ((sendingId, data) =>
            {
                receivedData.Enqueue(new QueuedData() { data = data, sendingId = sendingId });
            });
            l.OnMemberConnect += (userId =>
            {
                receivedMemberStatus.Enqueue(new ConnectedMemberStatus() { connectionStatus = true, memberId = userId });
            });
            l.OnMemberDisconnect += (userId =>
            {
                receivedMemberStatus.Enqueue(new ConnectedMemberStatus() { connectionStatus = false, memberId = userId });
            });
        }

        public void ConnectToLobby(string activitySecret)
        {
            if (!Connecting && !Connected)
            {
                Connecting = true;
                l.TryConnect(activitySecret);
            }
        }
        public void OpenLobby(uint capacity = 5)
        {
            if (!Connecting && !Connected)
            {
                Connecting = true;
                l.TryCreatingLobby(capacity);
            }
        }

        private bool wasConnected = false;
        public void Update()
        {
            l.CheckForDiscordInformation();
            if (Connected)
            {
                if (!wasConnected)
                {
                    UnityEngine.Debug.Log("Conectados!");
                    wasConnected = true;
                    Connection?.Invoke();
                }
                ReadNewMemberStatus();
                ReadReceivedPackets();
            }
            else if (wasConnected)
            {
                UnityEngine.Debug.Log("Desconectados!");
                wasConnected = false;
                Disconnection?.Invoke();
            }
        }
        public void LateUpdate() 
        {
            l.FlushAllMessages();
        }
        private void ReadReceivedPackets() 
        {
            int amountOfPacketDequeued = 0;
            while (receivedData.TryDequeue(out QueuedData data, MAX_WAITING_TIME_TO_READ_PACKET) && amountOfPacketDequeued < MAX_PACKETS_TO_LOOK_EACH_LOOP)
            {
                ReceiveData(data);
                amountOfPacketDequeued++;
            }
        }
        private void ReadNewMemberStatus()
        {
            //What if there are both the connection and disconnection messages? welp, who knows!
            //making it so connection messges happen before could fix this, but let's ignore it for now
            while (receivedMemberStatus.TryDequeue(out ConnectedMemberStatus memberStatus, MAX_WAITING_TIME_TO_READ_PACKET))
            {
                if (memberStatus.connectionStatus)
                {
                    MemberConnection?.Invoke(memberStatus.memberId);
                }
                else
                {
                    MemberDisconnection?.Invoke(memberStatus.memberId);
                }
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
        public bool Send(long receivingId, byte[] data, int header, bool reliable) => l.Send(MakeDataWithHeader(data, header), receivingId, reliable);
        public void SendToAll(byte[] data, int header, bool reliable) => l.SendToAllCients(MakeDataWithHeader(data, header), reliable);

        private void ReceiveData(QueuedData data)
        {
            PacketReader packet = new PacketReader(data.data);
            try
            {
                int Header = packet.ReadInt32();
                DateTime sendTime = packet.ReadDateTime();
                ReceivedPacketData receivedPacketData = new ReceivedPacketData(data.sendingId, sendTime, (int)(DateTime.UtcNow - sendTime).TotalMilliseconds);

                packetReceiver.ReadReceivedPacket(ref packet, Header, receivedPacketData);
            }
            catch (Exception ex)
            {
                UnityEngine.Debug.Log($"Erro ao ler dados de um outro cliente : {ex.Source} | {ex.Message}");                
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

        public event MemberHandler MemberConnection;
        public event MemberHandler MemberDisconnection;
        public delegate void MemberHandler(long memberId);
    }
}
