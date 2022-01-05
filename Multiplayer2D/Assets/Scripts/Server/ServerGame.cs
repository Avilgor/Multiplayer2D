using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Net;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

[RequireComponent(typeof (ActionManager))]
public class ServerGame : MonoBehaviour
{
    public List<Transform> freeSpawns,usedSpwans;
    public ClientWait waitScript;
    public GameObject winScreen, defeatScreen;
    public int maxClients = 10;
    public ushort clientIDs;
    public int timeout = 20;
    public int pingMS = 2000;
    /*public bool jitter = true;
    public bool packetLoss = true;
    public int minJitt = 0;
    public int maxJitt = 800;
    public int lossThreshold = 10;*/
    public float updateWorldTime = 0.05f;
    public int secondsToStartGame = 11;
    public int maxEntityUpdatePacket = 30;
    public CameraFollow camera;

    ActionManager actManager;
    NetworkingServer server;
    uint netIDS;
    bool gameStarted, updateWorld,sendPing;
    int currentPlayers;

    Dictionary<IPEndPoint, ServerClient> clients;
    List<Color> usedColors, freeColors;
    Queue<ServerClient> toRemoveClients;
    //float deltaTime;

    private void Awake()
    {
        gameStarted = false;
        updateWorld = false;
        sendPing = false;
        netIDS = 0;
        clientIDs = 0;
        GLOBALS.serverGame = this;
        actManager = GetComponent<ActionManager>();
        server = new NetworkingServer();
        clients = new Dictionary<IPEndPoint, ServerClient>();
        usedColors = new List<Color>();
        freeColors = new List<Color>();
        toRemoveClients = new Queue<ServerClient>();
        /*server.maxJitt = maxJitt;
        server.minJitt = minJitt;
        server.lossThreshold = lossThreshold;
        server.jitter = jitter;
        server.packetLoss = packetLoss;*/
        server.Start();       
    }

    void Start()
    {
        waitScript.StartServer();
        winScreen.SetActive(false);
        defeatScreen.SetActive(false);
        for (int i = 0; i < 20; i++)
        {
            freeColors.Add(new Color(UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f), UnityEngine.Random.Range(0f, 1f)));
        }
        server.InitServer();
        StartCoroutine(PingCountdown());
        StartCoroutine(UpdateWorldTimer());
        CreateServerTank();
        Debug.Log("Server game start");
    }

    void Update()
    {
        /*float current;
        current = Time.frameCount / Time.time;
        deltaTime = (int)current;
        Debug.Log("FPS: " + deltaTime.ToString());*/
        server.OnUpdate();     
        if (toRemoveClients.Count > 0) DisconnectClient(toRemoveClients.Dequeue());
        if (updateWorld)
        {
            //SendActionVerify();
            UpdateWorldState();
        }
        if (sendPing) PingClients();
        if (Input.GetKeyDown(KeyCode.Escape)) Application.Quit();
    }

    private void OnDestroy()
    {
        if(server != null) server.onDisconnect();
    }

    public Dictionary<IPEndPoint, ServerClient> GetClients()
    {
        return clients;
    }

    public void ProcessPacket(Packet data)
    {
        if (data != null && data.HasBufer())
        {
            Packet temp;

            try
            {
                ServerMSG msg = (ServerMSG)data.ReadByte();
                //Debug.Log("Server got message: "+msg);
                if (clients.ContainsKey(data.sender))
                {
                    clients[data.sender].lastTimestamp = DateTime.Now;
                    switch (msg)
                    {
                        case ServerMSG.SM_PING:
                            server.ToSendPacket(ClientMSG.CM_PONG, clients[data.sender], false);
                            //Debug.Log("Ping from: " + clients[data.remote].username);
                            data = null;
                            break;

                        case ServerMSG.SM_PONG:
                            data = null;
                            //Debug.Log("Pong to: "+clients[data.remote].username);
                            break;

                        case ServerMSG.SM_ACK:
                            byte num = data.ReadByte();
                            for (int i = 0; i < num; i++)
                            {
                                GLOBALS.serverPakManager.OnACK(data.ReadUInt());
                            }
                            data = null;
                            break;

                        case ServerMSG.SM_CLIENT_READY:
                            //Send current players info
                            if (clients.Count > 1)
                            {
                                foreach (KeyValuePair<IPEndPoint, ServerClient> client in clients)
                                {
                                    if (client.Key != data.sender)
                                    {
                                        server.ToSendPacket(client.Value.GetPlayerInfoPacket(), ClientMSG.CM_PLAYER_JOIN, clients[data.sender], true);
                                    }
                                }
                            }
                            data = null;
                            break;

                        case ServerMSG.SM_DISCONNECT_CLIENT:
                            if (clients.ContainsKey(data.sender)) DisconnectClient(data);
                            //if (clients.Count < 2) StopCoroutine(CountdownToStart());
                            data = null;
                            break;

                        /*case ServerMSG.SM_REQUEST_ACTION:
                            //Debug.Log(data.ToBitArray());
                            byte actionsCount = data.ReadByte();
                            uint netID;
                            byte act;
                            for (int i = 0; i < actionsCount; i++)
                            {
                                netID = data.ReadUInt();
                                seq = data.ReadUInt();
                                act = data.ReadByte();
                                actManager.AddAction(seq, (ClientActions)act, netID, data.sender);
                            }
                            data = null;
                            break;*/

                        case ServerMSG.SM_PLAYER_INPUT:
                            byte inputsNum = data.ReadByte();
                            uint seq;
                            uint netID;
                            bool[] inputs = new bool[16];
                            for (int i = 0; i < inputsNum; i++)
                            {
                                netID = data.ReadUInt();
                                seq = data.ReadUInt();
                                data.ReadBits(16).CopyTo(inputs,0);
                                actManager.AddInputs(seq, inputs, netID, data.sender);
                            }
                            data = null;
                            break;

                        default:
                            break;
                    }
                }
                else
                {
                    //Check if new client request
                    if (msg == ServerMSG.SM_CLIENT_CONNECTION)
                    {
                        if (!gameStarted)
                        {
                            if (clients.Count < maxClients)
                            {
                                if (!clients.ContainsKey(data.sender))
                                {
                                    uint tankID = GetNewNetID();
                                    uint canonID = GetNewNetID();
                                    string name = data.ReadString();
                                    int col = UnityEngine.Random.Range(0, freeColors.Count);
                                    Color co = freeColors[col];
                                    usedColors.Add(co);
                                    freeColors.RemoveAt(col);
                                    ServerClient newC = new ServerClient(clientIDs, name, co, data.sender);
                                    GameObject go = CreatePlayerEntity(newC, tankID, canonID);

                                    clients.Add(data.sender, newC);


                                    //Notify client                          
                                    temp = new Packet();
                                    temp.Write(clientIDs);
                                    temp.Write(tankID);
                                    temp.Write(canonID);
                                    temp.Write(co);
                                    temp.Write(go.transform.position);
                                    temp.Write(go.transform.rotation);
                                    server.ToSendPacket(temp.ToArray(), ClientMSG.CM_CONECTION_SUCCES, clients[data.sender], true);
                                    server.connectedClients.Add(data.sender);

                                    newC.packetsACK.Enqueue(data.pakID);
                                    newC.expectedID = data.pakID + 1;
                                    clientIDs++;
                                    temp = null;
                                    SendPlayerJoined(newC);
                                    waitScript.AddPlayer();
                                    if (clients.Count >= 2) StartCoroutine(CountdownToStart());
                                }
                            }
                        }
                    }
                    data = null;
                }
            }
            catch (System.Exception e)
            {
                Debug.Log(e);
            }
        }
    }

    private void CreateServerTank()
    {
        uint tankID = GetNewNetID();
        uint canonID = GetNewNetID();
        string name = GLOBALS.username;
        int col = UnityEngine.Random.Range(0, freeColors.Count);
        Color co = freeColors[col];
        usedColors.Add(co);
        freeColors.RemoveAt(col);
        ServerClient newC = new ServerClient(clientIDs, name, co, server.localEP);
        GameObject go = CreatePlayerEntity(newC, tankID, canonID);
        GLOBALS.playerTank = go.GetComponent<TankController>();
        go.GetComponent<TankController>().locked = true;
        GLOBALS.playerTank.SetColor(co);
        newC.host = true;
        clients.Add(newC.ep, newC);
        waitScript.AddPlayer();
        camera.target = go.transform;
        clientIDs++;
    }

    private GameObject CreatePlayerEntity(ServerClient player,uint tankID,uint cannonID)
    {
        Transform t = freeSpawns[UnityEngine.Random.Range(0,freeSpawns.Count)];
        usedSpwans.Add(t);
        freeSpawns.Remove(t);
        player.lastSpawn = t.position;
        GameObject go = GLOBALS.networkGO.SpawnGo(0, tankID, t.position,t.rotation);
        go.GetComponent<TankController>().locked = true;
        go.GetComponent<TankController>().SetCanonID(cannonID);
        player.clientTank = go.GetComponent<NetworkEntity>();
        go.GetComponent<TankController>().SetColor(player.col);
        return go;
    }

    private void SendPlayerJoined(ServerClient player)
    {
        byte[] pak = player.GetPlayerInfoPacket();
        if (clients.Count > 0)
        {
            foreach (KeyValuePair<IPEndPoint, ServerClient> client in clients)
            {
                if (client.Key != player.ep) server.ToSendPacket(pak, ClientMSG.CM_PLAYER_JOIN, client.Value, true);
            }
        }
    }

    public uint GetNewNetID()
    {
        uint t = netIDS;
        netIDS++;
        return t;
    }

    public void SendActionVerify()
    {
        Packet pak;
        foreach (KeyValuePair<IPEndPoint,ServerClient> player in clients)
        {
            if (player.Value.newActionSequence > player.Value.lastActionSequence)
            {
                pak = new Packet();
                pak.Write(player.Value.newActionSequence);
                pak.Write(player.Value.clientTank.transform.position);
                pak.Write(player.Value.clientTank.transform.rotation);
                SendPacket(pak,ClientMSG.CM_VERIFY_ACTIONS,player.Key,false);
                player.Value.lastActionSequence = player.Value.newActionSequence;
            }
        }
    }

    public void SendPacket(Packet pak,ClientMSG msg,ServerClient client,bool esential)
    {
        server.ToSendPacket(pak.ToArray(), msg, client, esential);
    }

    public void SendPacket(Packet pak, ClientMSG msg, IPEndPoint client, bool esential)
    {
        if(clients.ContainsKey(client)) server.ToSendPacket(pak.ToArray(), msg, clients[client], esential);
    }

    public void SendPacket(ClientMSG msg, ServerClient client, bool esential)
    {
        server.ToSendPacket(msg, client, esential);
    }

    public void SendPacket(ClientMSG msg, IPEndPoint client, bool esential)
    {
        if (clients.ContainsKey(client)) server.ToSendPacket(msg, clients[client], esential);
    }

    public void BroadcastPacket(Packet pak,ClientMSG msg,bool esential)
    {
        //Send to all except server client
        foreach(KeyValuePair<IPEndPoint, ServerClient> player in clients)
        {
            if(!player.Value.host) server.ToSendPacket(pak.ToArray(), msg, player.Value,esential);
        }
    }

    public NetworkingServer GetServer()
    {
        return server;
    }

    public void BroadcastPacket(ClientMSG msg, bool esential)
    {
        //Send to all except server client
        foreach (KeyValuePair<IPEndPoint, ServerClient> player in clients)
        {
            server.ToSendPacket(msg, player.Value, esential);
        }
    }

    public ServerClient GetClient(IPEndPoint remote)
    {
        if (clients.ContainsKey(remote)) return clients[remote];
        else return null;
    }

    public ServerClient GetClientByTankID(uint ID)
    {  
        foreach (KeyValuePair<IPEndPoint,ServerClient> player in clients)
        {
            if (player.Value.clientTank.netID == ID) return player.Value;
        }
         
        return null;
    }

    public bool HasClient(IPEndPoint client)
    {
        if (clients.ContainsKey(client)) return true;
        else return false;
    }

    public int GetNumberOfClients()
    {
        return clients.Count;
    }

    public Transform GetRandomSpawnPoint(Vector3 lastSpawn)
    {
        Transform res;
        do
        {
            res = freeSpawns[UnityEngine.Random.Range(0, freeSpawns.Count)];
        } while (res.position == lastSpawn);

        return res;
    }

    public void PlayerDefeated(IPEndPoint player)
    {
        currentPlayers--;
        clients[player].defeated = true;
        GLOBALS.networkGO.DestroyGo(clients[player].clientTank.netID);
        Packet pak = new Packet();
        pak.Write(clients[player].clientTank.netID);
        BroadcastPacket(pak,ClientMSG.CM_DESTROY_GO,true);
        if (clients[player].host)
        {
            defeatScreen.SetActive(false);
            GLOBALS.playerTank.locked = true;
        }
        else server.ToSendPacket(ClientMSG.CM_DEFEATED,clients[player],true);
        if (currentPlayers == 1)
        {
            //Got winner
            foreach (KeyValuePair<IPEndPoint, ServerClient> client in clients)
            {
                if (!client.Value.defeated)
                {
                    if (!client.Value.host) server.ToSendPacket(ClientMSG.CM_WINNER, client.Value, true);
                    else
                    {
                        GLOBALS.playerTank.locked = true;
                        winScreen.SetActive(true);
                    }
                    
                    StartCoroutine(AutoDisconnect());
                    break;
                }
            }
        }
    }

    private void DisconnectClient(Packet data)
    {
        Debug.Log("Disconnect client: " + clients[data.sender].username);
        freeColors.Add(clients[data.sender].col);
        usedColors.Remove(clients[data.sender].col);

        Packet temp = new Packet();
        temp.Write(clients[data.sender].id);
        BroadcastPacket(temp, ClientMSG.CM_CLIENT_DISCONNECTED,true);
        StartCoroutine(RemoveClient(data.sender));
        server.connectedClients.Remove(data.sender);
        GLOBALS.networkGO.DestroyGo(clients[data.sender].clientTank.netID);
        waitScript.RemovePlayer();
    }

    private void DisconnectClient(ServerClient player)
    {
        Debug.Log("Disconnect client: " + player.username);
        freeColors.Add(player.col);
        usedColors.Remove(player.col);

        Packet temp = new Packet();
        temp.Write(player.id);
        BroadcastPacket(temp, ClientMSG.CM_CLIENT_DISCONNECTED,true);
        StartCoroutine(RemoveClient(player.ep));
        GLOBALS.networkGO.DestroyGo(player.clientTank.netID);
        server.connectedClients.Remove(player.ep);
    }

    private void PingClients()
    {
        if (!server.close)
        {
            sendPing = false;
            if (clients.Count > 0)
            {
                foreach (KeyValuePair<IPEndPoint, ServerClient> client in clients)
                {
                    if (!client.Value.host)
                    {
                        TimeSpan diff = DateTime.Now - client.Value.lastTimestamp;
                        if (diff.TotalSeconds > timeout) //Disconnect client if more than timeout
                        {
                            toRemoveClients.Enqueue(client.Value);
                        }
                        else //Send ping
                        {
                            server.ToSendPacket(ClientMSG.CM_PING, client.Value, false);
                        }
                    }
                }
            }
            StartCoroutine(PingCountdown());
        }
    }

    private void UpdateWorldState()
    {
        if (!server.close)
        {
            //Dictionary<uint, NetworkEntity> entities = GLOBALS.networkGO.GetEntities();
            List<NetworkEntity> entities = GLOBALS.networkGO.GetEntities().Select(kvp => kvp.Value).ToList();
            int totalEntities = entities.Count;
            byte updatedEntities = 0;

            //Debug.Log("Total entities: "+ totalEntities);
            if (totalEntities > 0)
            {
                do
                {
                    Packet temp = new Packet();
                    Packet pak = new Packet();
                    int updated = 0;
                    for (int i = 0; i < maxEntityUpdatePacket && updatedEntities < totalEntities; i++)
                    {
                        NetworkEntity entity = entities[updatedEntities];
                        if (entity != null)
                        {
                            if (entity.CheckUpdate())
                            {
                                temp.Write(entity.GetUpdateState());
                                updated++;
                            }
                            entity.SaveState();
                        }
                        updatedEntities++;
                    }
                    pak.Write(updatedEntities);
                    pak.Write(temp.ToArray());
                    BroadcastPacket(pak, ClientMSG.CM_WORLD_STATE_UPDATE, false);
                } while (updatedEntities < totalEntities);
            }
            updateWorld = false;
            StartCoroutine(UpdateWorldTimer());
        }      
    }

    public void DisconnectHost()
    {
        BroadcastPacket(ClientMSG.CM_CLIENT_DISCONNECTED, false);
        server.onDisconnect();
        SceneManager.LoadScene(0);
    }

    IEnumerator RemoveClient(IPEndPoint player)
    {
        yield return new WaitForSeconds(1);
        clients.Remove(player);
    }

    IEnumerator CountdownToStart()
    {
        Debug.Log("Started countdown");
        int counter = secondsToStartGame;
        do
        {
            yield return new WaitForSeconds(1);
            counter--;
            Packet pak = new Packet();
            pak.Write((byte)counter);
            waitScript.SetCountdown(counter);
            BroadcastPacket(pak,ClientMSG.CM_COUNTDOWN,false);
        } while (counter > 0);
        if (clients.Count >= 2)//Start game
        {
            gameStarted = true;                    
            for (int i = 0;i < usedSpwans.Count;i++) freeSpawns.Add(usedSpwans[i]);
            usedSpwans.Clear();
            BroadcastPacket(ClientMSG.CM_GAME_START,true);
            currentPlayers = clients.Count;
            waitScript.StartGame();
            GLOBALS.playerTank.locked = false;
        }
    }

    IEnumerator UpdateWorldTimer()
    {
        yield return new WaitForSeconds(updateWorldTime);
        updateWorld = true;
    }

    IEnumerator PingCountdown()
    {
        yield return new WaitForSeconds(2);
        sendPing = true;
    }

    IEnumerator AutoDisconnect()
    {
        yield return new WaitForSeconds(5);
        server.onDisconnect();
        SceneManager.LoadScene(0);
    }
}
