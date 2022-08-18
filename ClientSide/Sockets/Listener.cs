using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using Discord;

using SNet_Client.Utils;


namespace SNet_Client.Sockets
{
    public class Listener
    {
        private Discord.Discord discord;
        private NetworkManager networkManager;
        private LobbyManager lobbyManager;
        private UserManager userManager;

        private const long APPLICATION_ID = 1008107594516283493;
        private const long CLIENT_ID = 1008107594516283493;


        public long currentlyConnectedLobby { get; private set; }
        public User currentUser { get; private set; }

        private SNETConcurrentDictionary<long, ConnectedUser> ConnectedUsers;
        private SNETConcurrentDictionary<ulong, long> ConnectedUsersPeerIdTable;

        public Listener()
        {
            var clientID = Environment.GetEnvironmentVariable("DISCORD_CLIENT_ID");
            if (clientID == null)
            {
                clientID = "418559331265675294";
            }
            //discord = new Discord.Discord(Int64.Parse(clientID), (UInt64)Discord.CreateFlags.Default);
            discord = new Discord.Discord(CLIENT_ID, (UInt64)Discord.CreateFlags.Default);
            networkManager = discord.GetNetworkManager();
            lobbyManager = discord.GetLobbyManager();
            userManager = discord.GetUserManager();

            ConnectedUsers = new SNETConcurrentDictionary<long, ConnectedUser>();
            ConnectedUsersPeerIdTable = new SNETConcurrentDictionary<ulong, long>();

            SetDiscordEvents();
        }
        private void SetDiscordEvents()
        {
            //Adds our route uptade
            networkManager.OnRouteUpdate += route =>
            {
                var txn = lobbyManager.GetMemberUpdateTransaction(currentlyConnectedLobby, currentUser.Id);
                txn.SetMetadata("route", route);
                lobbyManager.UpdateMember(currentlyConnectedLobby, currentUser.Id, txn, (result =>
                {
                    ClientMod.LogSource.LogWarning("Updated member route with result " + result);
                }));
            };

            lobbyManager.OnMemberDisconnect += (lobbyId, userId) =>
            {
                if (ConnectedUsers.TryGetValue(userId, out ConnectedUser user))
                {
                    ConnectedUsers.Remove(userId);
                    ConnectedUsersPeerIdTable.Remove(user.PeerId);
                }
            };
            //Receives the new route from connected user and updates accordenly
            lobbyManager.OnMemberUpdate += (lobbyId, userId) =>
            {
                var rawPeerId = lobbyManager.GetMemberMetadataValue(lobbyId, userId, "peer_id");
                var peerId = Convert.ToUInt64(rawPeerId);
                var newRoute = lobbyManager.GetMemberMetadataValue(lobbyId, userId, "route");
                networkManager.UpdatePeer(peerId, newRoute);

                if (!ConnectedUsers.ContainsKey(userId))
                {
                    ConnectedUsers.Add(userId,
                        new ConnectedUser() { User = lobbyManager.GetMemberUser(lobbyId, userId), PeerId = peerId });
                    OnMemberConnect?.Invoke(userId);
                    ConnectedUsersPeerIdTable.Add(peerId, userId);
                }
            };
            networkManager.OnMessage += (peerId, channel, data) =>
            {
                if (ConnectedUsersPeerIdTable.TryGetValue(peerId, out long userId))
                {
                    OnReceiveData?.Invoke(userId, data);
                }
            };

        }
        private void WhenConnectedToLobby(ref Lobby lobby)
        {
            currentlyConnectedLobby = lobby.Id;

            var localPeerId = Convert.ToString(networkManager.GetPeerId());
            var txn = lobbyManager.GetMemberUpdateTransaction(lobby.Id, currentUser.Id);
            txn.SetMetadata("peer_id", localPeerId);
            lobbyManager.UpdateMember(lobby.Id, currentUser.Id, txn, (result) =>
            {
                ClientMod.LogSource.LogWarning("Updated member peer id with result " + result);
            });

            //Get all clients already connected to the lobby
            var members = lobbyManager.GetMemberUsers(lobby.Id);
            foreach (var member in members)
            {
                long userId = member.Id;
                string rawPeerId = lobbyManager.GetMemberMetadataValue(lobby.Id, userId, "peer_id");
                ulong userPeerId = Convert.ToUInt64(rawPeerId);
                string route = lobbyManager.GetMemberMetadataValue(lobby.Id, userId, "route");

                networkManager.OpenPeer(userPeerId, route);
                networkManager.OpenChannel(userPeerId, 0, false);// Not realiable
                networkManager.OpenChannel(userPeerId, 1, true); // Realiable

                ConnectedUsers.Add(userId, new ConnectedUser() { User = member, PeerId = userPeerId });
                ConnectedUsersPeerIdTable.Add(userPeerId, userId);
                OnMemberConnect?.Invoke(userId);
            }
            OnConnection?.Invoke();
        }
        public void TryCreatingLobby(uint capacity = 5)
        {
            var txn = lobbyManager.GetLobbyCreateTransaction();
            txn.SetCapacity(capacity);
            txn.SetType(LobbyType.Private);

            lobbyManager.CreateLobby(txn, (Result result, ref Lobby lobby) =>
            {
                if (result != Result.Ok)
                {
                    OnFailConnection?.Invoke();
                    return;
                }
                var secret = lobbyManager.GetLobbyActivitySecret(lobby.Id);
                ClientMod.LogSource.LogInfo("Secret: " + secret);
                WhenConnectedToLobby(ref lobby);
            });
        } 
        public void TryConnect(string activitySecret)
        {
            lobbyManager.ConnectLobbyWithActivitySecret(activitySecret, (Result result, ref Lobby lobby) =>
            {
                if(result != Result.Ok) 
                {
                    OnFailConnection?.Invoke();
                    return;
                }
                WhenConnectedToLobby(ref lobby);
            });
        }
        public void FlushAllMessages() 
        {
            networkManager.Flush();
        }
        public void CheckForDiscordInformation() 
        {
            discord.RunCallbacks();
        }

        public void SendToAllCients(byte[] data, bool reliable) 
        {
            int channel = reliable ? 1 : 0; 
            foreach (var user in ConnectedUsers) 
            {                
                networkManager.SendMessage(user.Value.PeerId, (byte)channel, data);
            }
        }
        public bool Send(byte[] data, long receivingId,bool reliable)
        {
            if (ConnectedUsers.TryGetValue(receivingId, out var user))
            {
                int channel = reliable ? 1 : 0;
                networkManager.SendMessage(user.PeerId, (byte)channel, data);
                return true;
            }
            return false;
        }

        public void Disconnect()
        {
            lobbyManager.DisconnectLobby(currentlyConnectedLobby, (result) =>
            {
                OnDisconnection?.Invoke();

                ConnectedUsersPeerIdTable.Clear();
                ConnectedUsers.Clear();
            });
        }
        
        public delegate void ReceivedData(long sendingPeer, byte[] data);
        public delegate void MemberUpdate(long userId);

        public event Action OnConnection;
        public event Action OnFailConnection;
        public event Action OnDisconnection;
        public event ReceivedData OnReceiveData;

        public event MemberUpdate OnMemberConnect;
        public event MemberUpdate OnMemberDisconnect;
    }
}
