using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System;
using System.Collections.Concurrent;
using System.IO;

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
    /*public bool jitter;
    public bool packetLoss;
    public int minJitt;
    public int maxJitt;
    public int lossThreshold;*/

    Socket sok = null;
    public IPEndPoint localEP = null, matchmakingEP = null;

    Thread serverListen = null;
    Thread serverSend = null;
    public string localPublicIP;

    public bool close;
    uint pakID;
    public List<EndPoint> connectedClients;
    Queue<string> threadStrings;
    ConcurrentQueue<Packet> toSendData;
    List<string> NATpunchAdresses;

    System.Random r = new System.Random();

    public void Start()
    {     
        close = true;
        pakID = 1;
        connectedClients = new List<EndPoint>();
        threadStrings = new Queue<string>();
        toSendData = new ConcurrentQueue<Packet>();
        NATpunchAdresses = new List<string>();
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
            string localIP = GetLocalIPAddress(); //= "127." + UnityEngine.Random.Range(0, 255) + "." +
                                                  //UnityEngine.Random.Range(0, 255) + "." + UnityEngine.Random.Range(0, 255);
            localPublicIP = GetPublicIPAddress();
            sok = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            localEP = new IPEndPoint(IPAddress.Parse(localIP), 0);
            matchmakingEP = new IPEndPoint(IPAddress.Parse(GLOBALS.matchmakingServerIP), GLOBALS.matchmakingServerPort);
            sok.Bind(localEP);
            sok.ReceiveTimeout = 50;
            close = false;
            serverListen = new Thread(ListenServer);
            serverListen.Start();
            serverSend = new Thread(SendPackets);
            serverSend.Start();
            threadStrings.Enqueue("Server ready.");
            CreateRoom();
        }
        catch (Exception e)
        {
            Debug.Log(e);
        }
    }

    private void CreateRoom()
    {
        Packet pak = new Packet();
        pak.Write(GLOBALS.roomName);
        pak.remote = matchmakingEP;
        toSendData.Enqueue(pak);
        Debug.Log("Created room: "+GLOBALS.roomName);
    }

    public void OnUpdate()
    {
        if (threadStrings != null && threadStrings.Count > 0) Debug.Log(threadStrings.Dequeue());    
    }

    public int GetLocalPort()
    {
        if (localEP != null) return localEP.Port;
        else return 0;
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
            toSendData.Enqueue(temp);
            toSendData.Enqueue(temp);
            toSendData.Enqueue(temp);
            //Debug.Log("Packet sent: " + msg.ToString());
            //Debug.Log("Esential: " + esential);
            GLOBALS.serverPakManager.SentPacket(temp);
            pakID++;
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
            toSendData.Enqueue(temp);
            toSendData.Enqueue(temp);
            toSendData.Enqueue(temp);
            //Debug.Log("Packet sent: " + msg.ToString());
            //Debug.Log("Esential: " + esential);
            GLOBALS.serverPakManager.SentPacket(temp);
            pakID++;
        }
    }

    public void NATHolePunch(string address, int port)
    {
        if (port != localEP.Port)
        {
            IPEndPoint remote = new IPEndPoint(IPAddress.Parse(address), port);
            Packet temp = new Packet();
            temp.remote = remote;
            toSendData.Enqueue(temp);
            NATpunchAdresses.Add(address);

            //Send server recognition msg
            if (!GLOBALS.serverGame.HasClient(remote))
            {
                Packet aux = new Packet();
                aux.esential = true;
                aux.Write(pakID);
                aux.Write((byte)ClientMSG.CM_SAVE_SERVER);
                aux.sender = localEP;
                aux.remote = remote;
                aux.pakID = pakID;

                toSendData.Enqueue(aux);
                toSendData.Enqueue(aux);
                toSendData.Enqueue(aux);

                GLOBALS.serverPakManager.SentPacket(aux);
                pakID++;
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
    }*/

    //Thred only
    private void ListenServer()
    {
        int size;
        byte[] data;
        threadStrings.Enqueue("Server started listening...");
        IPEndPoint remote;
        do
        {
            remote = new IPEndPoint(IPAddress.Any, 0);
            EndPoint sender = remote;
            data = new byte[1000];
            try
            {
                size = sok.ReceiveFrom(data, ref sender);
                
                if (size > 0)
                {
                    //Debug.Log("Packet size: " + size.ToString());
                    Packet pak = new Packet(data);
                    pak.size = size;
                    pak.sender = (IPEndPoint)sender;
                    //Debug.Log("From: " + pak.sender.Address.ToString());
                    if (pak.sender.Address.ToString().Equals(GLOBALS.matchmakingServerIP)) pak.externalServer = true;
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
                        sok.SendTo(pak.ToArray(), pak.remote);
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