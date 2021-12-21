using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Collections.Concurrent;

public class NetworkingClient : Networking
{
    //Jitter and packet loss simulation
    public bool jitter;
    public bool packetLoss;
    public int minJitt;
    public int maxJitt;
    public int lossThreshold;

    Socket sok = null;
    IPEndPoint localEP,serverEP;

    Thread clientListen = null;
    Thread clientSend = null;

    public bool connected;
    public uint sentID;

    bool close;
    string localIP = "";
    string serverIP = "";

    Queue<string> threadStrings;
    ConcurrentQueue<Packet> toSendData;

    System.Random r = new System.Random();

    public void Start()
    {
        sentID = 1;    
        connected = false;
        close = false;
        threadStrings = new Queue<string>();
        toSendData = new ConcurrentQueue<Packet>();
    }

    ~NetworkingClient()
    {
        close = true;
        connected = false;
        if (clientListen != null && clientListen.IsAlive) clientListen.Abort();
        if (clientSend != null && clientSend.IsAlive) clientSend.Abort();

        toSendData = null;
        threadStrings = null;

        if (sok != null)
        {
            try
            {
                sok.Shutdown(SocketShutdown.Both);
                sok.Close();
                sok = null;
            } catch (Exception e)
            {
                Debug.Log(e);
            }
        }
    }

    public void InitClient(string IP)
    {
        try
        {
            serverIP = IP;
            //Generate random IP
            localIP = "127."+ UnityEngine.Random.Range(0, 255) + "."+
                UnityEngine.Random.Range(0, 255) + "."+ UnityEngine.Random.Range(0, 255);

            sok = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            localEP = new IPEndPoint(IPAddress.Parse(localIP), 5000);
            serverEP = new IPEndPoint(IPAddress.Parse(serverIP), 5000);
            sok.Bind(localEP);
            sok.ReceiveTimeout = 50;
            close = false;
            Debug.Log("Client IP: " + localIP);
            clientListen = new Thread(ListenClient);
            clientListen.Start();
            clientSend = new Thread(SendPackets);
            clientSend.Start();          
        } catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    public void OnUpdate()
    {
        if (threadStrings.Count > 0)
        {
            Debug.Log(threadStrings.Dequeue());
        }
    }

    public void ToSendPacket(byte[] array, ServerMSG msg,bool es = false)
    {
        if (!close)
        {
            //Debug.Log("Send: " + msg.ToString());
            Packet temp = new Packet();
            temp.esential = es;
            temp.Write(sentID);
            temp.Write((byte)msg);
            temp.Write(array);
            temp.pakID = sentID;
            temp.sender = localEP;
            temp.remote = serverEP;
            //Send multiple times for packet loss
            SimulatePacket(temp);
            SimulatePacket(temp);
            SimulatePacket(temp);
            GLOBALS.clientPakManager.SentPacket(temp);
            sentID++;
            //Debug.Log("Packet to simulate");
        }
    }

    public void ToSendPacket(ServerMSG msg, bool es = false)
    {
        if (!close)
        {
            //Debug.Log("Send: "+msg.ToString());
            Packet temp = new Packet();
            temp.esential = es;
            temp.Write(sentID);
            temp.Write((byte)msg);
            temp.remote = serverEP;
            temp.sender = localEP;
            temp.pakID = sentID;
            //Send multiple times for packet loss
            SimulatePacket(temp);
            SimulatePacket(temp);
            SimulatePacket(temp);
            GLOBALS.clientPakManager.SentPacket(temp);
            sentID++;
            //Debug.Log("Packet to simulate");
        }
    }

    public void onConnectionReset(Socket fromAddress)
    {

    }

    private void ListenClient()
    {
        threadStrings.Enqueue("Client listening...");
        int size;
        byte[] data;
        do
        {
            data = new byte[1000];      
            try
            {
                size = sok.Receive(data);
                if (size > 0)
                {                  
                    Packet pak = new Packet(data);
                    GLOBALS.clientPakManager.GotPacket(pak);
                }
            }
            catch (Exception e)
            {
                //Debug.Log(e);
            }
        } while (!close);
    }

    public void onDisconnect()
    {
        connected = false;
        close = true;
        if (clientListen != null && clientListen.IsAlive) clientListen.Abort();
        if (clientSend != null && clientSend.IsAlive) clientSend.Abort();

        toSendData = null;
        threadStrings = null;

        if (sok != null)
        {
            try
            {
                sok.Shutdown(SocketShutdown.Both);
                sok.Close();
                sok = null;
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        }
    }

    public void reportError()
    {
        
    }

    private void SimulatePacket(Packet pak)
    {
        if (!packetLoss)
        {
            if (jitter)
            {
                pak.timestamp = DateTime.Now.AddMilliseconds(r.Next(minJitt, maxJitt)); // delay the message sending according to parameters
            }
            else pak.timestamp = DateTime.Now;

            pak.remote = serverEP;
            toSendData.Enqueue(pak);
        }
        else if (r.Next(0, 100) > lossThreshold) // Don't schedule the message with certain probability
        {
            if (jitter)
            {
                pak.timestamp = DateTime.Now.AddMilliseconds(r.Next(minJitt, maxJitt)); // delay the message sending according to parameters
            }
            else pak.timestamp = DateTime.Now;

            pak.remote = serverEP;
            toSendData.Enqueue(pak);
        }
        //Debug.Log("To send data: "+toSendData.Count);
    }

    //Thread only
    public void SendPackets()
    {
        threadStrings.Enqueue("Client waiting to send...");
        do
        {
            try
            {
                if (toSendData.Count > 0)
                {
                    if (toSendData.TryDequeue(out Packet pak))
                    {
                        if (pak.timestamp < DateTime.Now)
                        {
                            //Debug.Log(pak.ToBitArray());
                            sok.SendTo(pak.ToArray(), pak.Length(), SocketFlags.None, pak.remote);
                        }
                        else toSendData.Enqueue(pak);
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log(e);
            }
        } while (!close);
    }  
}