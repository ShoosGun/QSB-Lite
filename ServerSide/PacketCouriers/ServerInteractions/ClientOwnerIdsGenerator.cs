using System.Collections.Generic;

namespace ServerSide.PacketCouriers
{
    public class ClientOwnerIdsGenerator
    {
        public int MaxAmount { get; private set; }
        private int nextGeneratedID;
        private List<int> RemovedIDs;

        public ClientOwnerIdsGenerator(int maxAmount)
        {
            MaxAmount = maxAmount;
            nextGeneratedID = 0;
            RemovedIDs = new List<int>();
        }
        public bool TryGetID(out int id)
        {
            id = -1;
            if (RemovedIDs.Count > 0)
            {
                id = RemovedIDs[0];
                RemovedIDs.RemoveAt(0);
                return true;
            }
            else if(nextGeneratedID < MaxAmount)
            {
                id = nextGeneratedID;
                nextGeneratedID++;
                return true;
            }
            return false;
        }
        public void ReturnID(int id)
        {
            if(id< MaxAmount)
                RemovedIDs.Add(id);
        }
    }
}
