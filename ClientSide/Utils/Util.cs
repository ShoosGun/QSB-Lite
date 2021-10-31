using UnityEngine;
using ClientSide.PacketCouriers.GameRelated.Entities;

namespace ClientSide.Utils
{
    public static class Util
    {
        // Código de Cpp em C# 0_0
        public static int GerarHashInt(string s)
        {
            const int p = 53;
            const int m = 1000000000 + 9; //10e9 + 9
            int hash_value = 0;
            int p_pow = 1;
            foreach (char c in s)
            {
                hash_value = (hash_value + (c - 'a' + 1) * p_pow) % m;
                p_pow = p_pow * p % m;
            }
            return hash_value;
        }

        public static NetworkedEntity GetAttachedNetworkedEntity(this GameObject gameObject)
        {
            return gameObject.GetComponent<NetworkedEntity>();
        }
        public static NetworkedEntity GetAttachedNetworkedEntity(this Component component)
        {
            return component.GetComponent<NetworkedEntity>();
        }
    }
}
