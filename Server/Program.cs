using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Network;

namespace ChatServer
{
    class Program
    {
        static void Main(string[] args)
        {
            ChatServer server = new ChatServer();
            server.StartServer("127.0.0.1", 10001);
        }
    }

    public class ChatServer: NetworkManager
    {
        List<Socket> peerList;
        TCPPeer server;

        public ChatServer()
        {
            //a list to save sockets of all clients
            peerList = new List<Socket>();
        }

        public void StartServer(string ip, int port)
        {
            AddHandler("chat", OnChat);
            server = new TCPPeer(this);
            server.Listen(ip, port);

            //start the independent myThread to handle message queue
            this.StartThreadUpdate();
            Console.WriteLine("starting chat server");
        }

        //connection successful, add to peer list
        public override void OnAccepted(NetPacket packet)
        {
            Console.WriteLine("accepting new connection");
            peerList.Add(packet.socket);
        }

        //if lost, remove from list
        public override void OnLost(NetPacket packet)
        {
            Console.WriteLine("connection lost");
            peerList.Remove(packet.socket);
        }

        //send message to peers
        public void OnChat(NetPacket packet)
        {
            //display chat
            Chat.ChatProto proto = packet.ReadObject<Chat.ChatProto>();
            if (proto != null)
                Console.WriteLine(proto.userName + ": " + proto.chatMsg);

            packet.BeginWrite("chat");
            packet.WriteObject<Chat.ChatProto>(proto);
            packet.EncodeHeader();
                
            foreach(Socket sk in peerList)
            {
                server.Send(sk, packet);
            }
            
        }
    }

}
