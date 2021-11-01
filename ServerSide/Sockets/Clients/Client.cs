using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;

namespace ServerSide.Sockets.Clients
{
    public class Client
    {
        public string ID
        {
            get;
            private set;
        }
        public IPEndPoint EndPoint
        {
            get;
            private set;
        }

        private Socket reliableSocket;
        private UdpClient unreliableSocket;
        
        public Client(Socket accepted)
        {
            ID = Guid.NewGuid().ToString();
            reliableSocket = accepted;
            EndPoint = (IPEndPoint)reliableSocket.RemoteEndPoint;

            unreliableSocket = new UdpClient(EndPoint);

            reliableSocket.BeginReceive(new byte[] { 0 }, 0, 0, 0, ReceiveCallback, null);

            unreliableSocket.BeginReceive(UDPReceiveCallback, EndPoint);
        }
        
        private void ReceiveCallback(IAsyncResult ar)
        {
            try
            {
                reliableSocket.EndReceive(ar);
                byte[] buffer = new byte[4];
                reliableSocket.Receive(buffer, 0, 4, 0);
                int dataSize = BitConverter.ToInt32(buffer, 0);
                if (dataSize <= 0)
                    throw new SocketException();
                buffer = new byte[dataSize];
                int received = reliableSocket.Receive(buffer, 0, buffer.Length, 0);
                while (received < dataSize)
                {
                    received += reliableSocket.Receive(buffer, received, dataSize - received, 0);
                }
                Received?.Invoke(this, buffer);
                reliableSocket.BeginReceive(new byte[] { 0 }, 0, 0, 0,ReceiveCallback, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro no callback TCP de receber dados no Client {0}: {1}", ID, ex.Message);
                Disconnected?.Invoke(this);
            }
        }
        private void UDPReceiveCallback(IAsyncResult ar)
        {
            try
            {
                IPEndPoint endPoint = (IPEndPoint)ar.AsyncState;

                byte[] buffer = unreliableSocket.EndReceive(ar, ref endPoint);

                if (buffer.Length <= 0)
                    throw new SocketException();
                
                Received?.Invoke(this, buffer);

                unreliableSocket.BeginReceive(UDPReceiveCallback, endPoint);
            }
            catch (Exception ex)
            {
                Console.WriteLine("(Nao Importante) Erro no callback UDP de receber dados no Client {0}: {1}", ID, ex.Message);
            }
        }

        public void UnreliableSend(byte[] buffer) => unreliableSocket.Send(buffer, buffer.Length);

        public void Send(byte[] buffer) 
        {
            lock (this)
            {
                try
                {
                    byte[] sizeBuffer = BitConverter.GetBytes(buffer.Length);
                    reliableSocket.Send(sizeBuffer, 0, sizeBuffer.Length, 0);
                    reliableSocket.Send(buffer, 0, buffer.Length, 0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erro enquanto ao enviar dados para {0} >> {1}", ID, ex.Message);
                }
            }
        }
        public void Close()
        {
            reliableSocket.Close();
            reliableSocket.Dispose();
            
            unreliableSocket.Close();
            unreliableSocket.Dispose();
        }

        public event ClientReceivedHandler Received;
        public event ClientDisconnectedHandler Disconnected;
        public delegate void ClientReceivedHandler(Client sender, byte[] data);
        public delegate void ClientDisconnectedHandler(Client sender);
    }
}

