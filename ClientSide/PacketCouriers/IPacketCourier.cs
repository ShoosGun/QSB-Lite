using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using SNet_Client.Sockets;

namespace SNet_Client.PacketCouriers
{
    public interface IPacketCourier
    {
       void Receive(int latency, DateTime sentPacketTime, byte[] data);

    }
}
