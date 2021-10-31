using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using ClientSide.Sockets;

namespace ClientSide.PacketCouriers
{
    public interface IPacketCourier
    {
       void Receive(int latency, DateTime sentPacketTime, byte[] data);

    }
}
