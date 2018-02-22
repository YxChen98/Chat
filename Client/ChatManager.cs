using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network;

public class ChatManager : NetworkManager {

    TCPPeer client;

	// Use this for initialization
	public void Start () {
        client = new TCPPeer(this);
        client.Connect("127.0.0.1", 10001);
	}
	
    public void send(NetPacket packet)
    {
        client.Send(client.socket, packet);
    }

    public override void OnLost(NetPacket packet)
    {
        Debug.Log("connection to server lost");
    }

    public override void OnConnected(NetPacket packet)
    {
        Debug.Log("connection completed");
    }

    public override void OnConnectFailed(NetPacket packet)
    {
        Debug.Log("connection to server failed, exit");
    }

}
