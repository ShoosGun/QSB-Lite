using System;
using System.Net;
using System.Threading;

namespace SNet_Client.Sockets
{
    public struct ReliablePacket
    {
        public int PacketID;
        public byte[] Data;
        public ReliablePacket(int PacketID, byte[] Data)
        {
            this.PacketID = PacketID;
            this.Data = Data;
        }
    }

    public class Server
    {
        private readonly ReaderWriterLockSlim _lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        private EndPoint ServerEndPoint;
        private DateTime TimeOfLastReceivedMessage;
        private bool Connecting;
        private bool Connected;

        public Server()
        {
            TimeOfLastReceivedMessage = DateTime.UtcNow;
            Connecting = false;
            Connected = false;
        }

        public void SetServerEndPoint(EndPoint ServerEndPoint)
        {
            _lock.EnterWriteLock();
            try
            {
                this.ServerEndPoint = ServerEndPoint;
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }
        public void SetTimeOfLastReceivedMessage(DateTime time)
        {
            _lock.EnterWriteLock();
            try
            {
                TimeOfLastReceivedMessage = time;
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }
        public void SetConnecting(bool b)
        {
            _lock.EnterWriteLock();
            try
            {
                Connecting = b;
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }
        public void SetConnected(bool b)
        {
            _lock.EnterWriteLock();
            try
            {
                Connected = b;
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitWriteLock();
            }
        }

        public EndPoint GetServerEndPoint()
        {
            _lock.EnterReadLock();
            try
            {
                return ServerEndPoint;
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitReadLock();
            }
        }
        public DateTime GetTimeOfLastReceivedMessage()
        {
            _lock.EnterReadLock();
            try
            {
                return TimeOfLastReceivedMessage;
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitReadLock();
            }
        }
        public bool GetConnecting()
        {
            _lock.EnterReadLock();
            try
            {
                return Connecting;
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitReadLock();
            }
        }
        public bool GetConnected()
        {
            _lock.EnterReadLock();
            try
            {
                return Connected;
            }
            finally
            {
                if (_lock.IsWriteLockHeld) _lock.ExitReadLock();
            }
        }
    }
}
