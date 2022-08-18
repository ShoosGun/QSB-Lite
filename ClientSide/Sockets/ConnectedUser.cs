using Discord;

namespace SNet_Client.Sockets
{
    public class ConnectedUser
    {
        public User User;
        public ulong PeerId = ulong.MaxValue;
    }
}
