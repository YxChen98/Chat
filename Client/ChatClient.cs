using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Network;

//this is a standard Unity script
//bind to a game object, with simple UI

public class ChatClient : MonoBehaviour {

    ChatManager clientPeer;

    public string recvString = "";
    protected string inputString = "";

	// Use this for initialization
	void Start () {
        //instantiate ChatManager, register "chat" in Network Manager
        clientPeer = new ChatManager();
        clientPeer.AddHandler("chat", OnChat);
        clientPeer.Start();
	}
	
	// Update is called once per frame
	void Update () {
        clientPeer.Update();
	}

    void OnGUI()
    {
        //display received message
        GUI.Label(new Rect(5, 5, 200, 30), recvString);

        //type input message
        inputString = GUI.TextField(new Rect(Screen.width * 0.5f - 200, Screen.height * 0.5f - 20, 400,40 ), inputString);

        //send message
        if(GUI.Button(new Rect(Screen.width*0.5f-100, Screen.height*0.6f, 200, 30), "send message"))
        {
            SendChat();
        }
    }

    //send chat message
    public void SendChat()
    {
        //pack message in a packet and send
        Chat.ChatProto proto= new Chat.ChatProto();
        proto.userName = "client";
        proto.chatMsg = inputString;
        NetPacket p = new NetPacket();
        p.BeginWrite("chat");
        p.WriteObject<Chat.ChatProto>(proto);
        p.EncodeHeader();
        clientPeer.send(p);

        inputString = "";
    }

    //display received message
    public void OnChat(NetPacket packet)
    {
        Debug.Log("message received");
        Chat.ChatProto proto = packet.ReadObject<Chat.ChatProto>();
        recvString = proto.userName + ": " + proto.chatMsg;
    }
}
