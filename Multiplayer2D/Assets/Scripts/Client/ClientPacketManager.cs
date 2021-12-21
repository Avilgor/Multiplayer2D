using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum ClientMSG : byte
{
    CM_PING = 0,
    CM_PONG,
    CM_CONECTION_SUCCES,
    CM_CONNECTION_ERROR,
    CM_PLAYER_JOIN,
    CM_CLIENT_DISCONNECTED,
    CM_CREATE_GO,
    CM_DESTROY_GO,
    CM_WORLD_STATE_UPDATE,
    CM_PLAYER_DESTROY,
    CM_CLIENT_ACTION,
    CM_ACK,
    CM_GAME_START,
    CM_VERIFY_ACTIONS,
    CM_COUNTDOWN,
    CM_DEFEATED,
    CM_WINNER,
    CM_MAX
}


[RequireComponent(typeof(ClientGame))]
public class ClientPacketManager : MonoBehaviour
{
    ClientGame client;
    uint expectedID;
    Dictionary<uint, Packet> sentPackets;
    Queue<Packet> receivedPackets;
    Queue<uint> acks;
    bool check;
    bool ack;

    private void Awake()
    {
        GLOBALS.clientPakManager = this;
        sentPackets = new Dictionary<uint, Packet>();
        client = GetComponent<ClientGame>();
        receivedPackets = new Queue<Packet>();
        acks = new Queue<uint>();
    }

    void Start()
    {
        ack = false;
        check = false;
        expectedID = 0;
        StartCoroutine(ACKNotification());
        StartCoroutine(PacketsCheck());
    }

    void Update()
    {
        if (receivedPackets.Count > 0)NextPacket();                    
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
        if (sentPackets.ContainsKey(ack)) sentPackets.Remove(ack);
    }

    public void GotPacket(Packet pak)
    {
        pak.ReadID();//pak id
        if (pak.pakID >= expectedID)
        {
            receivedPackets.Enqueue(pak);
            acks.Enqueue(pak.pakID);
            expectedID = pak.pakID + 1;
        }
        else if ((ClientMSG)pak.ReadByte(false) == ClientMSG.CM_ACK)
        {
            receivedPackets.Enqueue(pak);
            acks.Enqueue(pak.pakID);
        }
    }

    private void NextPacket()
    {
        try
        {
            Packet[] packets = receivedPackets.ToArray(); 
            receivedPackets.Clear();
            for (int i = 0; i < packets.Length; i++) client.ProcessPacket(packets[i]);
        } catch (Exception e)
        {
            
        }
    }

    private void CheckPackets()
    {
        check = false;
        //Debug.Log("Sent packets: " + sentPackets.Count);
        if (sentPackets.Count > 0)
        {
            List<Packet> packets = sentPackets.Select(kvp => kvp.Value).ToList();
            sentPackets.Clear();           
            for (int i = 0; i < packets.Count; i++)
            {
                if (packets[i].esential)
                {
                    packets[i].ToArray();
                    TimeSpan diff = DateTime.Now - packets[i].timestamp;
                    if (diff.TotalMilliseconds > 200)
                    {
                        //Packet not ACK                      
                        //Debug.Log(BitConverter.ToString(packets[i].ToArray()));
                        uint id = packets[i].ReadUInt();
                        //Debug.Log("ID: "+id);
                        ServerMSG msg = (ServerMSG)packets[i].ReadByte();
                        packets[i].RemoveHeader();
                        client.SendPacket(packets[i], msg, true);
                    }
                    else sentPackets.Add(packets[i].pakID,packets[i]);
                }
            }           
        }
        StartCoroutine(PacketsCheck());
    }

    private void SendACK()
    {
        if (acks.Count > 0)
        {
            Packet pak = new Packet();
            pak.Write((byte)acks.Count);
            for (int i = 0; i < acks.Count; i++)
            {
                pak.Write(acks.Dequeue());
            }
            client.SendPacket(pak, ServerMSG.SM_ACK, false);
        }
        ack = false;
        StartCoroutine(ACKNotification());
    }

    IEnumerator ACKNotification()
    {
        yield return new WaitForSeconds(0.1f);
        //Debug.Log("ACKS: "+acks.Count);
        ack = true;
    }

    IEnumerator PacketsCheck()
    {      
        yield return new WaitForSeconds(0.2f);
        check = true;
    }
}
