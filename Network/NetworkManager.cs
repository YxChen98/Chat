using System;
using System.Collections.Generic;
using System.Text;

namespace Network
{
    public class NetworkManager
    {
        //a thread independent of network to handle logic
        System.Threading.Thread myThread;

        //data queue
        private System.Collections.Queue Packets = new System.Collections.Queue();

        //delegate callback function, associating each message to an OnReceive function
        public delegate void OnReceive(NetPacket packet);
        public Dictionary<string, OnReceive> handlers;

        public void AddHandler(string msgID, OnReceive handler)
        {
            handlers.Add(msgID, handler);//message as a key, different handler as value, delegating to OnReceive
        }

        //server accepts connection from client
        public virtual void OnAccepted(NetPacket packet) { }
        //client connect to server
        public virtual void OnConnected(NetPacket packet) { }
        //client connect to server failed
        public virtual void OnConnectFailed(NetPacket packet) { }
        //conection lost
        public virtual void OnLost(NetPacket packet) { }

        //constructor
        public NetworkManager()
        {
            handlers = new Dictionary<string, OnReceive>();

            AddHandler("OnAccepted", OnAccepted);
            AddHandler("OnConnected", OnConnected);
            AddHandler("OnConnectFailed", OnConnectFailed);
            AddHandler("OnLost", OnLost);

        }

        ////////////////////queue handling:

        //add packet to queue
        //using lock to ensure threads do not fight with each other
        public void AddPacket(NetPacket packet)
        {
            lock (Packets)
            {
                Packets.Enqueue(packet);
            }
        }

        //get packet from queue
        public NetPacket GetPacket()
        {
            lock (Packets)
            {
                if (Packets.Count == 0)
                    return null;
                return (NetPacket)Packets.Dequeue();
            }
        }

        /////////////////////myThread doing things:

        //getting packet untill null
        //read message and call corresponding delegate function
        public void Update()
        {
            NetPacket packet = null;
            for (packet = GetPacket(); packet != null;)
            {
                string msg = "";
                packet.BeginRead(out msg);
                OnReceive handler = null;

                if (handlers.TryGetValue(msg, out handler))
                {
                    //call corresponding delegate function according to message
                    if (handler != null)
                        handler(packet);
                }

                packet = null;

            }
        }

        //using myThread to call update() function, and sleep every 30ms to save CPU
        protected void ThreadUpdate()
        {
            while (true)//keeps checking if new packets are incoming
            {
                System.Threading.Thread.Sleep(30);
                Update();
            }
        }

        //start myThread
        public void StartThreadUpdate()
        {
            myThread = new System.Threading.Thread(new System.Threading.ThreadStart(ThreadUpdate));
            myThread.Start();
        }

    }
}