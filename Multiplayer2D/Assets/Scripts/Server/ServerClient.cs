using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System;

public class ServerClient
{
    public ushort id;
    public bool defeated;
    public uint expectedID;
    public Color col;
    public string username;
    public DateTime lastTimestamp;
    public int lifes,ping;
    public IPEndPoint ep;
    public NetworkEntity clientTank;
    public Queue<uint> packetsACK;
    public uint lastActionSequence,newActionSequence;
    public Vector3 lastSpawn;

    public ServerClient(ushort clientID, string name, Color color, IPEndPoint endPoint)
    {
        defeated = false;
        id = clientID;
        col = color;
        lifes = 3;
        ep = endPoint;
        username = name;
        expectedID = 0;
        packetsACK = new Queue<uint>();
    }

    public byte[] GetPlayerInfoPacket()
    {
        byte[] array;
        Packet pak = new Packet();
        pak.Write(id); //Client ID
        pak.Write(clientTank.netID); //Tank network entity ID
        pak.Write(clientTank.GetComponent<TankController>().GetCannonID()); //Cannon network entity ID
        //player Color
        pak.Write(col);
        //Player username
        pak.Write(username);
        //Player entity position
        pak.Write(clientTank.transform.position);
        //Player entity rotation
        pak.Write(clientTank.transform.rotation);
        array = pak.ToArray();
        return array;
    }
}
