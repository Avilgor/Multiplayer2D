using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public struct ClientSideData
{
    public ClientSideData(string user,Color c,ushort cId,uint playerEntity)
    {
        username = user;
        col = c;
        ID = cId;
        entityID = playerEntity;
    }

    public string GetUsername() { return username; }
    public Color GetColor() { return col; }
    public ushort GetID() { return ID; }
    public uint GetPlayerEntityID() { return entityID; }

    string username;
    Color col;
    ushort ID;
    uint entityID;
}

public class ClientGame : MonoBehaviour
{
    /*public bool jitter = true;
    public bool packetLoss = true;
    public int minJitt = 0;
    public int maxJitt = 800;
    public int lossThreshold = 90;*/
    public bool conected;
    public float pingInterval = 3.0f;
    public float timeout = 20.0f;
    public ushort playerId;
    public GameObject playerTank;
    public GameObject waitConnectionScreen;
    public ClientWait waitScript;
    public CameraFollow camera;
    public string username;
    public Dictionary<ushort, ClientSideData> gamePlayers;
    public Transform[] Spawnpoints;
    public GameObject winScreen, defeatScreen;
    NetworkingClient client;
    DateTime timestamp;
    //float deltaTime;

    private void Awake()
    {
        GLOBALS.clientGame = this;
        conected = false;
        client = new NetworkingClient();
        gamePlayers = new Dictionary<ushort, ClientSideData>();

        /*client.jitter = jitter;
        client.packetLoss = packetLoss;
        client.minJitt = minJitt;
        client.maxJitt = maxJitt;
        client.lossThreshold = lossThreshold;*/
        client.Start();
    }
    
    void Start()
    {
        //waitConnectionScreen.SetActive(true);
        timestamp = DateTime.Now;
        client.InitClient(GLOBALS.roomName);
        waitConnectionScreen.SetActive(true);
        winScreen.SetActive(false);
        defeatScreen.SetActive(false);       
        Debug.Log("Client game start");
    }

    private void OnDestroy()
    {
        if(client != null) client.onDisconnect();
    }

    void Update()
    {
        /*float current;
        current = Time.frameCount / Time.time;
        deltaTime = (int)current;
        Debug.Log("FPS: "+deltaTime.ToString());*/
        client.OnUpdate();      

        if ((DateTime.Now - timestamp).TotalSeconds > timeout)
        {
            Debug.Log("Server response timeout.");
            client.onDisconnect();
            DisconnectedFromServer();
        }     
        if(Input.GetKeyDown(KeyCode.Escape)) SceneManager.LoadScene(0);
    }

    private void StartConnection()
    {
        Packet pak = new Packet();
        pak.Write(username);
        client.ToSendPacket(pak.ToArray(),ServerMSG.SM_CLIENT_CONNECTION);
        StartCoroutine(ConnectToServerTimeout());
        pak = null;
    }

    public NetworkingClient GetClient()
    {
        return client;
    }


    public void ProcessPacket(Packet pak)
    {
        if (pak != null && pak.HasBufer())
        {
            ClientMSG msg = (ClientMSG)pak.ReadByte();
            timestamp = DateTime.Now;
            //Debug.Log("Client got message: " + msg);
            switch (msg)
            {
                case ClientMSG.CM_PONG:
                    //Debug.Log("Ping: "+ping.TotalSeconds);
                    break;

                case ClientMSG.CM_PING:
                    client.ToSendPacket(ServerMSG.SM_PONG);
                    break;

                case ClientMSG.CM_CLIENT_ACTION:
                    GLOBALS.playerActions.ReceivedActions(pak);
                    break;

                case ClientMSG.CM_CREATE_GO:
                    GLOBALS.networkGO.SpawnGo(pak.ReadInt(), pak.ReadUInt(),
                        new Vector3(pak.ReadFloat(), pak.ReadFloat(), pak.ReadFloat()),
                        new Quaternion(pak.ReadFloat(), pak.ReadFloat(), pak.ReadFloat(), pak.ReadFloat()));
                    break;

                case ClientMSG.CM_DESTROY_GO:
                    GLOBALS.networkGO.DestroyGo(pak.ReadUInt());
                    break;

                case ClientMSG.CM_CONECTION_SUCCES:
                    if (!client.connected)
                    {
                        playerId = pak.ReadUShort();
                        if (!gamePlayers.ContainsKey(playerId))
                        {
                            //Read all player data
                            uint tankID = pak.ReadUInt();
                            uint canonID = pak.ReadUInt();
                            Color userColor = pak.ReadColor();
                            Vector3 pos = pak.ReadVector3();
                            Quaternion rot = pak.ReadQuaternion();

                            //Create client
                            ClientSideData clDat = new ClientSideData(username, userColor, playerId, tankID);
                            gamePlayers.Add(playerId, clDat);

                            //Create player entity
                            GameObject go = GLOBALS.networkGO.SpawnGo(0, tankID, pos, rot);
                            go.GetComponent<TankController>().locked = true;
                            go.GetComponent<TankController>().SetCanonID(canonID);
                            go.GetComponent<TankController>().SetColor(userColor);
                            client.ToSendPacket(ServerMSG.SM_CLIENT_READY, true);
                            conected = true;
                            client.connected = true;
                            camera.target = go.transform;
                            GLOBALS.playerEntity = go.GetComponent<NetworkEntity>();
                            GLOBALS.playerTank = go.GetComponent<TankController>();
                            waitConnectionScreen.SetActive(false);
                            waitScript.StartClient();
                            waitScript.AddPlayer();
                            StartCoroutine(SendPing());
                        }
                    }
                    break;

                case ClientMSG.CM_CLIENT_DISCONNECTED:
                    ushort id = pak.ReadUShort();
                    if (id == playerId)
                    {
                        Debug.Log("Disconnecting client...");
                        client.onDisconnect();
                        DisconnectedFromServer();
                    }
                    else
                    {
                        Debug.Log("Removing client...");
                        GLOBALS.networkGO.DestroyGo(gamePlayers[id].GetPlayerEntityID());
                        gamePlayers.Remove(id);
                        waitScript.RemovePlayer();
                    }
                    break;

                case ClientMSG.CM_PLAYER_JOIN:
                    //Read all player data
                    ushort TempPlayerId = pak.ReadUShort();
                    if (!gamePlayers.ContainsKey(TempPlayerId))
                    {
                        uint TempTankID = pak.ReadUInt();
                        uint TempCannonID = pak.ReadUInt();
                        Color TempColor = new Color(pak.ReadFloat(), pak.ReadFloat(), pak.ReadFloat());
                        string tempName = pak.ReadString();
                        Vector3 TempPos = new Vector3(pak.ReadFloat(), pak.ReadFloat(), pak.ReadFloat());
                        Quaternion TempRot = new Quaternion(pak.ReadFloat(), pak.ReadFloat(), pak.ReadFloat(), pak.ReadFloat());

                        //Create client
                        ClientSideData newClient = new ClientSideData(tempName, TempColor, TempPlayerId, TempTankID);
                        gamePlayers.Add(TempPlayerId, newClient);

                        //Create player entity
                        GameObject TempGo = GLOBALS.networkGO.SpawnGo(0, TempTankID, TempPos, TempRot);
                        TempGo.GetComponent<TankController>().locked = true;
                        TempGo.GetComponent<TankController>().SetCanonID(TempCannonID);
                        TempGo.GetComponent<TankController>().SetColor(TempColor);
                        waitScript.AddPlayer();
                    }
                    break;
                case ClientMSG.CM_ACK:
                    byte ammount = pak.ReadByte();
                    for (int i = 0; i < ammount; i++)
                    {
                        GLOBALS.clientPakManager.OnACK(pak.ReadUInt());
                    }
                    break;

                case ClientMSG.CM_WORLD_STATE_UPDATE:
                    //Debug.Log("Update world");
                    UpdateWorld(pak);
                    break;

                case ClientMSG.CM_VERIFY_ACTIONS:
                    //GLOBALS.playerActions.VerifyInput(pak.ReadUInt(), pak.ReadVector3(), pak.ReadQuaternion());
                    break;

                case ClientMSG.CM_COUNTDOWN:
                    waitScript.SetCountdown((int)pak.ReadByte());
                    break;

                case ClientMSG.CM_GAME_START:
                    waitScript.StartGame();
                    GLOBALS.playerTank.locked = false;
                    break;

                case ClientMSG.CM_DEFEATED:
                    defeatScreen.SetActive(true);
                    if (GLOBALS.playerTank != null) GLOBALS.playerTank.locked = true;
                    break;

                case ClientMSG.CM_WINNER:
                    GLOBALS.playerTank.locked = true;
                    winScreen.SetActive(true);
                    break;

                case ClientMSG.CM_SAVE_SERVER:
                    Debug.Log("Server recon");
                    client.SetServer(pak.sender);                  
                    StartConnection();
                    break;

                default:
                    break;
            }
        }
        else Debug.Log("Packet null error.");
    }

    private void UpdateWorld(Packet pak)
    {     
        byte entities = pak.ReadByte();
        //Debug.Log("Entities to update: "+entities.ToString());
        for (int i = 0; i < entities; i++)
        {
            uint netID = pak.ReadUInt();
            if (GLOBALS.networkGO.HasEntity(netID) /*&& netID != GLOBALS.playerEntity.netID*/)
            {
                byte posUp = pak.ReadByte();
                byte rotUp = pak.ReadByte();
                byte scaleUp = pak.ReadByte();

                if (posUp == 2) GLOBALS.networkGO.GetEntity(netID).UpdatePosition(pak.ReadVector3());
                if (rotUp == 2) GLOBALS.networkGO.GetEntity(netID).UpdateRotation(pak.ReadQuaternion());
                if (scaleUp == 2) GLOBALS.networkGO.GetEntity(netID).UpdateScale(pak.ReadVector3());
            }
        }
    }

    public void DisconnectClient()
    {        
        SendPacket(ServerMSG.SM_DISCONNECT_CLIENT,false);
        StartCoroutine(DisconnectClientWait());
    }

    public void SendPacket(Packet pak,ServerMSG msg,bool esential)
    {
        client.ToSendPacket(pak.ToArray(), msg, esential);
    }

    public void SendPacket(ServerMSG msg, bool esential)
    {
        client.ToSendPacket(msg, esential);
    }

    private void DisconnectedFromServer()
    {
        //Return to menu
        conected = false;
        Debug.Log("Client disconnected from server.");
        SceneManager.LoadScene(0);
    }

    IEnumerator SendPing()
    {
        yield return new WaitForSeconds(pingInterval);
        if (client.connected)
        {
            client.ToSendPacket(ServerMSG.SM_PING);
        }
        
        StartCoroutine(SendPing());
    }

    IEnumerator DisconnectClientWait()
    {
        yield return new WaitForSeconds(1.0f);
        if (client.connected)
        {
            Debug.Log("Disconnecting client...");
            client.onDisconnect();
            DisconnectedFromServer();
        }
    }

    IEnumerator ConnectToServerTimeout()
    {
        yield return new WaitForSeconds(10);
        if (!client.connected)
        {
            Debug.Log("Server connection error.");
            client.onDisconnect();
            DisconnectedFromServer();
        }      
    }
}