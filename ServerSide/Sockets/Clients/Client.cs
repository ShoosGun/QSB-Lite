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

        private Socket sck;

        //private DateTime startedReceivingTime;
        //private readonly int receivingLimit;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="accepted"></param>
        /// <param name="debugger"></param>
        /// <param name="receivingLimit"> In packets/s </param>
        public Client(Socket accepted /*, int receivingLimit =  100*/)
        {
            ID = Guid.NewGuid().ToString();
            sck = accepted;
            EndPoint = (IPEndPoint)sck.RemoteEndPoint;

            //startedReceivingTime = DateTime.UtcNow;
            //this.receivingLimit = receivingLimit;

            sck.BeginReceive(new byte[] { 0 }, 0, 0, 0, callback, null);
        }

        private int amountOfReceivedPackets = 0;
        private void callback(IAsyncResult ar)
        {
            try
            {
                sck.EndReceive(ar);
                byte[] buffer = new byte[4];
                sck.Receive(buffer, 0, 4, 0);
                int dataSize = BitConverter.ToInt32(buffer, 0);
                if (dataSize <= 0)
                    throw new SocketException();
                buffer = new byte[dataSize];
                int received = sck.Receive(buffer, 0, buffer.Length, 0);
                while (received < dataSize)
                {
                    received += sck.Receive(buffer, received, dataSize - received, 0);
                }
                Received?.Invoke(this, buffer);
                //TODO reimplementar a maneira de evitar "spam" de pacotes
                //if( (DateTime.UtcNow - startedReceivingTime).Milliseconds >= 1000)
                //    amountOfReceivedPackets = 0;

                //else if(amountOfReceivedPackets > receivingLimit)
                //{
                //    Thread.Sleep(1000 - (DateTime.UtcNow - startedReceivingTime).Milliseconds); // Esperar até dar um segundo
                //}
                sck.BeginReceive(new byte[] { 0 }, 0, 0, 0,callback, null);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro no callback de receber dados no Client {0}: {1}", ID, ex.Message);
                Disconnected?.Invoke(this);
            }
        }
        public void Send(byte[] buffer) 
        {
            lock (this)
            {
                try
                {
                    byte[] sizeBuffer = BitConverter.GetBytes(buffer.Length);
                    sck.Send(sizeBuffer, 0, sizeBuffer.Length, 0);
                    sck.Send(buffer, 0, buffer.Length, 0);
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Erro enquanto ao enviar dados para {0} >> {1}", ID, ex.Message);
                }
            }
        }
        public void Close()
        {
            sck.Close();
        }

        public event ClientReceivedHandler Received;
        public event ClientDisconnectedHandler Disconnected;
        public delegate void ClientReceivedHandler(Client sender, byte[] data);
        public delegate void ClientDisconnectedHandler(Client sender);
    }
}

