using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System;

public struct InputInfo
{
    public InputInfo(uint sequence, bool[] inp, uint id,Vector3 pos,Quaternion quat)
    {
        netID = id;
        sequenceNum = sequence;
        inputs = inp;
        player = null;
        seqPos = pos;
        seqRot = quat;
    }

    public InputInfo(uint sequence, bool[] inp, uint id,IPEndPoint pl)
    {
        netID = id;
        sequenceNum = sequence;
        inputs = inp;
        player = pl;
        seqPos = new Vector3();
        seqRot = new Quaternion();
    }
    public uint GetID() { return netID; }
    public uint GetSequence() { return sequenceNum; }
    public bool[] GetInputs() { return inputs; }
    public IPEndPoint GetPlayer() { return player; }
    public Vector3 GetPosition() { return seqPos; }
    public Quaternion GetRotation() { return seqRot; }

    uint netID;
    bool[] inputs;
    uint sequenceNum;
    IPEndPoint player;
    Vector3 seqPos;
    Quaternion seqRot;
}

public static class GLOBALS 
{
    public static bool isclient;
    public static string IP = "";
    public static string username = "";
   
    public static ClientGame clientGame = null;
    public static ServerGame serverGame = null;
    public static NetworkGameobjects networkGO = null;
    public static PlayerActionsManager playerActions = null;
    public static ClientPacketManager clientPakManager = null;
    public static ServerPacketManager serverPakManager = null;

    public static CameraFollow cameraFollow = null;
    public static NetworkEntity playerEntity = null;
    public static TankController playerTank = null;
}

/*public struct PlayerInput
{
    //public const byte maxBits = 16;//short
    public PlayerInput(bool[] inputs)
    {
        //bitInputs = new BitArray(inputs);
        array = inputs;
        //bitInputs.CopyTo(array,0);
    }
    public bool[] GetInputs()
    {
        return array;  
    }

    bool[] array;
    //BitArray bitInputs;
}*/