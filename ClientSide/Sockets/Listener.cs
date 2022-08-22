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
        private LobbyManager lobbyManager;
        private UserManager userManager;

        private const long CLIENT_ID = 1008107594516283493;

        public long CurrentlyConnectedLobby  { get; private set; }
        public User CurrentUser { get; private set; }

        private Dictionary<long, User> ConnectedUsers;

        public Listener(string discordSDK = "0")
        {
            Environment.SetEnvironmentVariable("DISCORD_INSTANCE_ID", discordSDK);
            discord = new Discord.Discord(CLIENT_ID, (UInt64)CreateFlags.Default);

            lobbyManager = discord.GetLobbyManager();
            userManager = discord.GetUserManager();

            ConnectedUsers = new Dictionary<long, User>();
            SetDiscordEvents();
        }
        private void SetDiscordEvents()
        {
            userManager.OnCurrentUserUpdate += () => 
            {
                CurrentUser = userManager.GetCurrentUser();
            };
            lobbyManager.OnMemberConnect += (lobbyId, userId) =>
            {
                ConnectedUsers.Add(userId, lobbyManager.GetMemberUser(lobbyId, userId));
                OnMemberConnect?.Invoke(userId);
            };
            lobbyManager.OnMemberDisconnect += (lobbyId, userId) =>
            {
                ConnectedUsers.Remove(userId);
            };
            lobbyManager.OnNetworkMessage += (lobbyId, userId, channelId, data) =>
            {
                ClientMod.LogSource.LogMessage($"Tamanho: {data.Length}");
                OnReceiveData?.Invoke(userId, data);
            };

        }

        private void WhenConnectedToLobby(ref Lobby lobby)
        {
            CurrentlyConnectedLobby = lobby.Id; 
            lobbyManager.ConnectNetwork(lobby.Id);
            lobbyManager.OpenNetworkChannel(lobby.Id, 0, true);
            lobbyManager.OpenNetworkChannel(lobby.Id, 1, false);

            OnConnection?.Invoke();

            //Get all clients already connected to the lobby
            var members = lobbyManager.GetMemberUsers(lobby.Id);
            foreach (var member in members)
            {
                if (member.Id != CurrentUser.Id)
                {
                    ConnectedUsers.Add(member.Id, member);
                    OnMemberConnect?.Invoke(member.Id);
                }
            }
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
                try
                {
                    WhenConnectedToLobby(ref lobby);
                }
                catch (Exception ex)
                {
                    ClientMod.LogSource.LogError($"Exception: {ex.Message} : {ex.StackTrace} - {ex.Source}");
                    OnFailConnection?.Invoke();
                }
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
            lobbyManager.FlushNetwork();
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
                lobbyManager.SendNetworkMessage(CurrentlyConnectedLobby, user.Value.Id, (byte)channel, data);
            }
        }
        public bool Send(byte[] data, long receivingId,bool reliable)
        {
            if (ConnectedUsers.TryGetValue(receivingId, out var user))
            {
                int channel = reliable ? 1 : 0;
                lobbyManager.SendNetworkMessage(CurrentlyConnectedLobby, user.Id, (byte)channel, data);
                return true;
            }
            return false;
        }

        public void Disconnect()
        {
            lobbyManager.DisconnectLobby(CurrentlyConnectedLobby, (result) =>
            {
                OnDisconnection?.Invoke();
                //ConnectedUsersPeerIdTable.Clear();
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
