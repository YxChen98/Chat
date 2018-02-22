using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace Network
{
    //encapsulating TCP/IP protocol
    //server and client are similar in many functions, so encapsulate them into one class
    public class TCPPeer
    {
        public bool isServer { set; get; }
        //indicating server or client
        //if server then listen, else connect to server

        public Socket socket;
        NetworkManager networkMgr;

        public TCPPeer(NetworkManager netMgr)
        {
            networkMgr = netMgr;
        }

        //adding internal message to NetworkManager like connection accepted, failed
        private void AddInternalPacket(string msg, Socket socket)
        {
            NetPacket np = new NetPacket();
            np.socket = socket;
            np.BeginWrite(msg);
            networkMgr.AddPacket(np);
        }


        ////////////////////////////////////////////Listening as server:

        //callback function of "void Listen"
        void ListenCallback(System.IAsyncResult ar)
        {
            //get server socket
            Socket listener = (Socket)ar.AsyncState;
            try
            {
                //get client socket
                Socket client = listener.EndAccept(ar);

                //adding internal message to server, connection accepted
                AddInternalPacket("OnAccepted", client);

                //create new packet to receive data from remote client
                NetPacket packet = new NetPacket();
                packet.socket = client;
                client.BeginReceive(packet.bytes, 0, NetPacket.headerLength, SocketFlags.None, new System.AsyncCallback(ReceiveHeader), packet);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }

            //begin new connection
            listener.BeginAccept(new System.AsyncCallback(ListenCallback), listener);
        }

        //start listening as server
        public void Listen(string ip, int port, int backlog = 1000)
        {
            isServer = true;
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), port);
            //create socket
            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            try
            {
                socket.Bind(ipe);
                socket.Listen(backlog);

                //accepting async. connection
                socket.BeginAccept(new System.AsyncCallback(ListenCallback), socket);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        ////////////////////////////////////////////Connecting as client:
        public void Connect(string ip, int port)
        {
            isServer = false;
            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(ip), port);

            try
            {
                socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                socket.BeginConnect(ipe, new System.AsyncCallback(ConnectionCallback), socket);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

        public void ConnectionCallback(System.IAsyncResult ar)
        {
            Socket client = (Socket)ar.AsyncState;
            try
            {
                client.EndConnect(ar);
                AddInternalPacket("OnConnected", client);
                NetPacket packet = new NetPacket();
                packet.socket = client;
                client.BeginReceive(packet.bytes, 0, NetPacket.headerLength, SocketFlags.None, new System.AsyncCallback(ReceiveHeader), packet);
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
                AddInternalPacket("OnConnectFailed", client);
            }
        }

        ////////////////////////////////////////////No matter client or server socket, it will receive data, containing header and body:
        void ReceiveHeader(System.IAsyncResult ar)
        {
            NetPacket packet = (NetPacket)ar.AsyncState;
            try
            {
                //return received data length
                int read = packet.socket.EndReceive(ar);
                if (read < 1)//connection lost
                {
                    AddInternalPacket("OnLost", packet.socket);
                    return;
                }
                packet.readLength += read;

                //header must read 4 byts
                if (packet.readLength < NetPacket.headerLength)
                {
                    //receiving header
                    packet.socket.BeginReceive(packet.bytes,
                                               packet.readLength, //offset = already read length
                                               NetPacket.headerLength - packet.readLength,//size=remaining number of bytes
                                               SocketFlags.None,
                                               new System.AsyncCallback(ReceiveHeader),//recursive callback until 4 bytes
                                               packet);

                }
                //when 4 bytes read
                else
                {
                    packet.DecodeHeader();//get message length from decoding header
                    packet.readLength = 0;

                    //receiving body
                    packet.socket.BeginReceive(packet.bytes,
                                               NetPacket.headerLength, //offset=headerLength
                                               packet.bodyLength, //size=bodyLength
                                               SocketFlags.None,
                                               new System.AsyncCallback(ReceiveBody),
                                               packet);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ReceiveHeader: " + e.Message);
            }
        }

        void ReceiveBody(System.IAsyncResult ar)
        {
            NetPacket packet = (NetPacket)ar.AsyncState;
            try
            {
                //return received data length
                int read = packet.socket.EndReceive(ar);
                if (read < 1)//connection lost
                {
                    AddInternalPacket("OnLost", packet.socket);
                    return;
                }
                packet.readLength += read;

                //must read all bytes of the body
                if (packet.readLength < packet.bodyLength)
                {
                    packet.socket.BeginReceive(packet.bytes,
                                               NetPacket.headerLength + packet.readLength, //offset=headerlength+already read bytes
                                               packet.bodyLength - packet.readLength, //remaining size
                                               SocketFlags.None,
                                               new System.AsyncCallback(ReceiveBody), //recursive callback as ReceiveHeader
                                               packet);
                }
                else
                {
                    //add received message to NetworkManager, then reset
                    networkMgr.AddPacket(packet);
                    packet.Reset();

                    packet.socket.BeginReceive(packet.bytes,
                                               0,
                                               NetPacket.headerLength,
                                               SocketFlags.None,
                                               new System.AsyncCallback(ReceiveHeader), //call ReceiveHeader for next round of receive
                                               packet);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("ReceiveBody: " + e.Message);
            }
        }

        ////////////////////////////////////////////Also they both need to send data to remote terminal:
        public void Send(Socket sk, NetPacket packet)
        {
            NetworkStream ns; //NetworkStream is a class to handle TCP data stream, sending data async.
            lock (sk)//critical region
            {
                ns = new NetworkStream(sk);
                if (ns.CanWrite)
                {
                    try
                    {
                        ns.BeginWrite(packet.bytes, 0, packet.length, new System.AsyncCallback(SendCallback), ns);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
                    }
                }
            }
        }

        public void SendCallback(System.IAsyncResult ar)
        {
            NetworkStream ns = (NetworkStream)ar.AsyncState;
            try
            {
                ns.EndWrite(ar);
                ns.Flush();
                ns.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
        }

    }
}

namespace Chat
{
    [System.Serializable]
    public class ChatProto
    {
        public string userName;
        public string chatMsg;
    }
}
