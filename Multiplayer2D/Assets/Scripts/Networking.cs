using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public interface Networking 
{
    void Start();

    void OnUpdate();

    void onPacketReceived(byte[] inputPacket, EndPoint fromAddress);

    void onConnectionReset(Socket fromAddress);

    void SendPackets();

    void onDisconnect();

    void reportError();
}
