using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net;
using System.Linq;

[RequireComponent(typeof(ServerGame))]
public class ServerPacketManager : MonoBehaviour
{
    ServerGame server;   
    Dictionary<uint, Packet> sentPackets;
    Queue<Packet> receivedPackets;
    bool check;
    bool ack;

    private void Awake()
    {
        GLOBALS.serverPakManager = this;
        server = GetComponent<ServerGame>();
        receivedPackets = new Queue<Packet>();
        sentPackets = new Dictionary<uint, Packet>();
    }
    
    void Start()
    {
        check = false;
        ack = false;
        StartCoroutine(ACKNotification());
        StartCoroutine(PacketsCheck());
    }
    
    void Update()
    {
        if (receivedPackets.Count > 0) NextPacket();
        if (check) CheckPackets();
        if (ack) SendACK();
    }

    public void SentPacket(Packet pak)
    {
        if (!sentPackets.ContainsKey(pak.pakID))
        {
            sentPackets.Add(pak.pakID, pak);
        }
    }

    public void OnACK(uint ack)
    {
        //Debug.Log("Sent packets to ACK: "+sentPackets.Count);
        if (sentPackets.ContainsKey(ack))
        {
            //Debug.Log("Packet acknowledge");
            sentPackets.Remove(ack);
        }
    }

    public void GotPacket(Packet pak)
    {
        pak.ReadID();

        if (receivedPackets.Count > 100) receivedPackets.Dequeue();

        ServerClient client = server.GetClient(pak.sender);
        if (client != null)
        {
            client.lastTimestamp = DateTime.Now;
            if (pak.pakID >= client.expectedID)
            {
                receivedPackets.Enqueue(pak);
                client.packetsACK.Enqueue(pak.pakID);
                client.expectedID = pak.pakID + 1;
            }
        }
        else if((ServerMSG)pak.ReadByte(false) == ServerMSG.SM_CLIENT_CONNECTION)
        {
            receivedPackets.Enqueue(pak);           
        }
    }

    private void NextPacket()
    {
        Packet[] packets = receivedPackets.ToArray();
        receivedPackets.Clear();
        
        for (int i = 0; i < packets.Length; i++) server.ProcessPacket(packets[i]);
    }

    private void SendACK()
    {
        ack = false;
        Dictionary<IPEndPoint, ServerClient> clients = server.GetClients();
        foreach (KeyValuePair<IPEndPoint, ServerClient> client in clients)
        {
            if (client.Value.packetsACK.Count > 0)
            {
                Packet pak = new Packet();
                Queue<uint> acks = client.Value.packetsACK;
                pak.Write((byte)acks.Count);
                for (int i = 0; i < acks.Count; i++)
                {
                    pak.Write(acks.Dequeue());
                }
                client.Value.packetsACK.Clear();
                server.SendPacket(pak, ClientMSG.CM_ACK, client.Value, false);
            }
        }
        StartCoroutine(ACKNotification());
    }

    IEnumerator ACKNotification()
    {
        yield return new WaitForSeconds(0.07f);
        ack = true;   
    }

    private void CheckPackets()
    {
        check = false;
        //Debug.Log("Packets: " + sentPackets.Count);
        if (sentPackets.Count > 0)
        {
            List<Packet> packets = sentPackets.Select(kvp => kvp.Value).ToList();
            sentPackets.Clear();
            for (int i = 0; i < packets.Count; i++)
            {
                packets[i].ToArray();
                if (packets[i].esential)
                {
                    if (GLOBALS.serverGame.HasClient(packets[i].remote))
                    {
                        TimeSpan diff = DateTime.Now - packets[i].timestamp;
                        if (diff.TotalMilliseconds > 300)
                        {
                            //Debug.Log(BitConverter.ToString(pak.Value.ToArray()));
                            //Packet not ACK                     
                            packets[i].ReadUInt();
                            ClientMSG msg = (ClientMSG)packets[i].ReadByte();
                            packets[i].RemoveHeader();
                            server.SendPacket(packets[i], msg, packets[i].remote, true);
                        }
                        else sentPackets.Add(packets[i].pakID, packets[i]);
                    }
                }
            }          
        }
        StartCoroutine(PacketsCheck());      
    }

    IEnumerator PacketsCheck()
    {
        yield return new WaitForSeconds(0.1f);
        check = true;
    }
}
