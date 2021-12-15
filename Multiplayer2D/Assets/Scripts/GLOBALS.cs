using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System;

public struct ActionInfo
{
    public ActionInfo(uint num, ClientActions act, uint id)
    {
        netID = id;
        sequenceNum = num;
        action = act;
    }
    public uint GetID() { return netID; }
    public uint GetSequence() { return sequenceNum; }
    public ClientActions GetAction() { return action; }
    uint netID;
    ClientActions action;
    uint sequenceNum;
}

public static class GLOBALS 
{
    public static bool isclient;
    public static string IP = "";
    public static string username = "";
    public static CameraFollow cameraFollow = null;
    public static ClientGame clientGame = null;
    public static ServerGame serverGame = null;
    public static NetworkGameobjects networkGO = null;
    public static PlayerActionsManager playerActions = null;
    public static ClientPacketManager clientPakManager = null;
    public static ServerPacketManager serverPakManager = null;
}
