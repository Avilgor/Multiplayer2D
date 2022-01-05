using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Collections.Concurrent;
using System.IO;

public class NetworkingClient : Networking
{
    //Jitter and packet loss simulation
    /*public bool jitter;
    public bool packetLoss;
    public int minJitt;
    public int maxJitt;
    public int lossThreshold;*/

    Socket sok = null;
    IPEndPoint localEP,serverEP,matchmakingEP;

    Thread clientListen = null;
    Thread clientSend = null;

    public bool connected;
    public uint sentID;

    bool close;
    string localIP = "";
    string serverIP = "";
    public string localPublicIP;

    Queue<string> threadStrings;
    ConcurrentQueue<Packet> toSendData;
    List<string> NATpunchAdresses;

    System.Random r = new System.Random();

    public void Start()
    {
        sentID = 1;    
        connected = false;
        close = false;
        threadStrings = new Queue<string>();
        toSendData = new ConcurrentQueue<Packet>();
        NATpunchAdresses = new List<string>();
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
            //Generate random IP
            localIP = GetLocalIPAddress();
            //= "127."+ UnityEngine.Random.Range(0, 255) + "."+
                                          //UnityEngine.Random.Range(0, 255) + "."+ UnityEngine.Random.Range(0, 255);
            localPublicIP = GetPublicIPAddress();
            sok = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            localEP = new IPEndPoint(IPAddress.Parse(localIP), 0);
            matchmakingEP = new IPEndPoint(IPAddress.Parse(GLOBALS.matchmakingServerIP),GLOBALS.matchmakingServerPort);
            sok.Bind(localEP);
            sok.ReceiveTimeout = 50;
            close = false;
            clientListen = new Thread(ListenClient);
            clientListen.Start();
            clientSend = new Thread(SendPackets);
            clientSend.Start();
            EnterRoom();
        } catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    public int GetLocalPort()
    {
        if (localEP != null) return localEP.Port;
        else return 0;
    }

    public void SetServer(IPEndPoint server)
    {
        serverIP = server.Address.ToString();
        serverEP = server;
    }

    public void OnUpdate()
    {
        if (threadStrings.Count > 0)
        {
            Debug.Log(threadStrings.Dequeue());
        }
    }

    private string GetPublicIPAddress()
    {
        string address = "";
        WebRequest request = WebRequest.Create("http://checkip.dyndns.org/");
        using (WebResponse response = request.GetResponse())
        using (StreamReader stream = new StreamReader(response.GetResponseStream()))
        {
            address = stream.ReadToEnd();
        }

        int first = address.IndexOf("Address: ") + 9;
        int last = address.LastIndexOf("</body>");
        address = address.Substring(first, last - first);

        return address;
    }

    private string GetLocalIPAddress()
    {
        var host = Dns.GetHostEntry(Dns.GetHostName());
        foreach (var ip in host.AddressList)
        {
            if (ip.AddressFamily == AddressFamily.InterNetwork)
            {
                return ip.ToString();
            }
        }
        throw new Exception("No network adapters with an IPv4 address in the system!");
    }

    private void EnterRoom()
    {
        Packet pak = new Packet();
        pak.Write(GLOBALS.roomName);
        pak.remote = matchmakingEP;
        toSendData.Enqueue(pak);
        Debug.Log("Enter to room: "+GLOBALS.roomName);
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
            toSendData.Enqueue(temp);
            toSendData.Enqueue(temp);
            toSendData.Enqueue(temp);
            GLOBALS.clientPakManager.SentPacket(temp);
            sentID++;
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
            toSendData.Enqueue(temp);
            toSendData.Enqueue(temp);
            toSendData.Enqueue(temp);
            GLOBALS.clientPakManager.SentPacket(temp);
            sentID++;
        }
    }

    public void NATHolePunch(string address,int port)
    {
        if (port != localEP.Port)
        {
            IPEndPoint remote = new IPEndPoint(IPAddress.Parse(address), port);
            Packet temp = new Packet();
            temp.Write((byte)0);
            temp.remote = remote;
            toSendData.Enqueue(temp);
        }
    }

    private void ListenClient()
    {
        threadStrings.Enqueue("Client listening...");
        int size;
        byte[] data;
        IPEndPoint remote;
        do
        {
            remote = new IPEndPoint(IPAddress.Any, 0);
            EndPoint sender = remote;
            data = new byte[1000];
            try
            {
                size = sok.ReceiveFrom(data, ref sender);
                //Debug.Log("Packet from: " + sender.ToString());
                if (size > 0)
                {                  
                    Packet pak = new Packet(data);
                    pak.size = size;
                    pak.sender = (IPEndPoint)sender;
                    if (pak.sender.Address.ToString().Equals(GLOBALS.matchmakingServerIP)) pak.externalServer = true;
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


    /*private void SimulatePacket(Packet pak)
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
    }*/

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
                        sok.SendTo(pak.ToArray(),pak.remote);
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