using System;
using ServerSide.Sockets.Servers;
using ServerSide.Utils;

namespace ServerSide.PacketCouriers
{
    public class ServerInteraction
    {
        const string SI_LOCALIZATION_STRING = "ServerInteraction";
        public int HeaderValue { get; private set; }
        private Server server;

        private const int MAX_CLIENT_AMOUT = 10;
        private ClientOwnerIdsGenerator ownerIdsGenerator;

        private ConnectedClientDataDictionary clientDataDictionary;

        public event Action<ConnectedClientData> OnAfterSendOwnerID;

        public ServerInteraction(Server server)
        {
            this.server = server;

            server.OnNewClient += Server_NewConnectionID;
            server.OnClientDisconnection += Server_DisconnectionID;
            
            HeaderValue = server.packetReceiver.AddPacketReader(SI_LOCALIZATION_STRING, ReadPacket);

            ownerIdsGenerator = new ClientOwnerIdsGenerator(MAX_CLIENT_AMOUT);

            clientDataDictionary = new ConnectedClientDataDictionary();
        }
        public bool TryGetOwnerID(string socketID, out int id)
        {
            id = -1;
            if (clientDataDictionary.TryGetValue(socketID, out ConnectedClientData data))
            {
                id = data.OwnerID;
                return true;
            }
            return false;
        }
        
        private void Server_NewConnectionID(string clientID)
        {
            Console.WriteLine("Novo Cliente Com OwnerID");
            if (ownerIdsGenerator.TryGetID(out int newID))
            {
                Console.WriteLine("Novo OwnerID {0}",newID);
                ConnectedClientData clientData = new ConnectedClientData(clientID, newID);
                clientDataDictionary.Add(clientData);
                SendOwnerIDToClient(newID, clientID);
                OnAfterSendOwnerID?.Invoke(clientData);
            }
        }
        private void Server_DisconnectionID(string clientID)
        {
            if (clientDataDictionary.Contains(clientID))
            {
                ConnectedClientData clientData = clientDataDictionary[clientID];
                ownerIdsGenerator.ReturnID(clientData.OwnerID);
                clientDataDictionary.Remove(clientData);
                SendRemovedOwnerID(clientData.OwnerID);
            }
        }
        private void SendOwnerIDToClient(int id, string clientID)
        {
            Console.WriteLine("Enviando ID {0} para {1}", id, clientID);
            PacketWriter writer = new PacketWriter();
            writer.Write((byte)SIHeaders.CONNECTED);
            writer.Write(id);

            server.Send(writer.GetBytes(), HeaderValue, clientID);
        }
        private void SendRemovedOwnerID(int id)
        {
            PacketWriter writer = new PacketWriter();
            writer.Write((byte)SIHeaders.OWNER_ID_REMOVED);
            writer.Write(id);

            server.SendAll(writer.GetBytes(), HeaderValue);
        }

        private void ReadPacket(ref PacketReader reader, ReceivedPacketData receivedPacketData)
        {
        }

        enum SIHeaders : byte
        {
            CONNECTED = 0,
            OWNER_ID_REMOVED = 1,
        }
    }
}
