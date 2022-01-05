using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class StateObject
{
    public Socket workSocket = null;
    public EndPoint remote;
    public const int BUFFER_SIZE = 1024;
    public byte[] buffer = new byte[BUFFER_SIZE];
}

public interface Networking 
{
    void Start();

    void OnUpdate();

    //void onPacketReceivedCallback(IAsyncResult result);

    //void onConnectionReset(Socket fromAddress);

    void SendPackets();

    void onDisconnect();

    //void reportError();
}
