using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Collections.Concurrent;

public enum ServerMSG : byte
{
    SM_PING = 0,
    SM_PONG,
    SM_CLIENT_CONNECTION,
    SM_DISCONNECT_CLIENT,
    SM_CLIENT_READY,
    SM_UPDATE_ENTITY_POS,
    SM_UPDATE_ENTITY_ROT,
    SM_UPDATE_ENTITY_SCALE,
    //SM_REQUEST_ACTION,
    SM_PLAYER_INPUT,
    SM_ACK,
    SM_MAX
}

public class NetworkingServer : Networking
{
    //Jitter and packet loss simulation
    public bool jitter;
    public bool packetLoss;
    public int minJitt;
    public int maxJitt;
    public int lossThreshold;

    Socket sok = null;
    IPEndPoint localEP = null;

    Thread serverListen = null;
    Thread serverSend = null;

    public bool close;
    uint pakID;
    public List<EndPoint> connectedClients;
    Queue<string> threadStrings;
    ConcurrentQueue<Packet> toSendData;

    System.Random r = new System.Random();

    public void Start()
    {     
        close = true;
        pakID = 1;
        connectedClients = new List<EndPoint>();
        threadStrings = new Queue<string>();
        toSendData = new ConcurrentQueue<Packet>();
    }

    ~NetworkingServer()
    {
        close = true;

        if (serverListen != null && serverListen.IsAlive) serverListen.Abort();
        if (serverSend != null && serverSend.IsAlive) serverSend.Abort();

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

    public void InitServer()
    {       
        try
        {
            sok = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            localEP = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 5000);
            sok.Bind(localEP);
            sok.ReceiveTimeout = 50;
            close = false;
            serverListen = new Thread(ListenServer);
            serverListen.Start();
            serverSend = new Thread(SendPackets);
            serverSend.Start();
            threadStrings.Enqueue("Server ready.");
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    public void OnUpdate()
    {
        if (threadStrings != null && threadStrings.Count > 0) Debug.Log(threadStrings.Dequeue());    
    }

    public void ToSendPacket(byte[] array, ClientMSG msg, ServerClient client,bool esential)
    {
        if (!close)
        {
            //Add header
            Packet temp = new Packet();
            temp.esential = esential;
            temp.Write(pakID);
            temp.Write((byte)msg);
            temp.Write(array);
            temp.sender = localEP;
            temp.remote = client.ep;
            temp.pakID = pakID;
            //Send multiple times for packet loss
            SimulatePacket(temp);
            SimulatePacket(temp);
            SimulatePacket(temp);
            //Debug.Log("Packet sent: " + msg.ToString());
            //Debug.Log("Esential: " + esential);
            GLOBALS.serverPakManager.SentPacket(temp);
            pakID++;
            //Debug.Log("Server packet to simulate");
        }
    }

    public void ToSendPacket(ClientMSG msg, ServerClient client,bool esential)
    {
        if (!close)
        {
            Packet temp = new Packet();
            temp.esential = esential;
            temp.Write(pakID);
            temp.Write((byte)msg);
            temp.sender = localEP;
            temp.remote = client.ep;
            temp.pakID = pakID;
            //Send multiple times for packet loss      
            SimulatePacket(temp);
            SimulatePacket(temp);
            SimulatePacket(temp);
            //Debug.Log("Packet sent: " + msg.ToString());
            //Debug.Log("Esential: " + esential);
            GLOBALS.serverPakManager.SentPacket(temp);
            pakID++;
            //Debug.Log("Server packet to simulate");
        }
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

            toSendData.Enqueue(pak);
        }
        else if (r.Next(0, 100) > lossThreshold) // Don't schedule the message with certain probability
        {
            if (jitter)
            {
                pak.timestamp = DateTime.Now.AddMilliseconds(r.Next(minJitt, maxJitt)); // delay the message sending according to parameters
            }
            else pak.timestamp = DateTime.Now;
            toSendData.Enqueue(pak);
        }
        //Debug.Log("Total packets sent: " + sent);
    }

    public void onConnectionReset(Socket fromAddress)
    {

    }

    //Thred only
    private void ListenServer()
    {
        int size;
        byte[] data;
        threadStrings.Enqueue("Server started listening...");
        IPEndPoint remote = new IPEndPoint(IPAddress.Any, 5000);
        do
        {
            remote = new IPEndPoint(IPAddress.Any, 5000);
            EndPoint sender = (EndPoint)remote;
            data = new byte[1000];
            try
            {
                size = sok.ReceiveFrom(data, ref sender);
                if (size > 0)
                {
                    Packet pak = new Packet(data);
                    //Debug.Log(pak.ToBitArray());
                    pak.sender = (IPEndPoint)sender;
                    GLOBALS.serverPakManager.GotPacket(pak);
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
        close = true;
        if (serverListen != null && serverListen.IsAlive) serverListen.Abort();
        if (serverSend != null && serverSend.IsAlive) serverSend.Abort();

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

    public void SendPackets()
    {
        Debug.Log("Server send started...");
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
                            sok.SendTo(pak.ToArray(), pak.remote);
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