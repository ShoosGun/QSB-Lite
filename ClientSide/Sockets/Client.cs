using System;
using SNet_Client.Utils;


namespace SNet_Client.Sockets
{
    public class Client
    {
        Listener l;

        public bool Connected { private set; get; }
        public bool Connecting { get; private set; }

        public long GetUserId() => l.CurrentUser.Id;
        public PacketReceiver packetReceiver { get; private set; }

        private static Client CurrentClient = null;
        public static Client GetClient()
        {
            return CurrentClient;
        }

        public Client(bool useCanary = false)
        {
            if (CurrentClient != null)
                return;

            packetReceiver = new PacketReceiver();
            CurrentClient = this;

            Connected = false;
            Connecting = false;

            l = new Listener(useCanary?"1":"0");
            l.OnConnection += (() =>
            {
                Connecting = false;
                Connected = true;
                ClientMod.LogSource.LogInfo("Connected!");
            });
            l.OnFailConnection += (() =>
            {
                Connecting = false;
                Connected = false;
                ClientMod.LogSource.LogError("Connection Failed");
            });
            l.OnDisconnection += (() =>
            {
                Connected = false;
                ClientMod.LogSource.LogWarning("Disconnected");
            });
            l.OnReceiveData += ((sendingId, data) =>
            {
                ReceiveData(data, sendingId);
            });
            l.OnMemberConnect += (userId =>
            {
                MemberConnection?.Invoke(userId);
                ClientMod.LogSource.LogInfo($"Client {userId} on lobby !");
            });
            l.OnMemberDisconnect += (userId =>
            {
                MemberDisconnection?.Invoke(userId);
                ClientMod.LogSource.LogInfo($"Client disconnected from lobby: {userId}");
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

        private void ReceiveData(byte[] data, long sendingId)
        {
            PacketReader packet = new PacketReader(data);
            try
            {
                int Header = packet.ReadInt32();
                DateTime sendTime = packet.ReadDateTime();
                ReceivedPacketData receivedPacketData = new ReceivedPacketData(sendingId, sendTime, (int)(DateTime.UtcNow - sendTime).TotalMilliseconds);

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
