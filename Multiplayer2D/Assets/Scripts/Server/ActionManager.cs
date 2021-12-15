using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ActionManager : MonoBehaviour
{
    public int maxFrameActions = 10;

    NetworkGameobjects netGos;
    Queue<ActionInfo> actions;

    private void Awake()
    {        
        actions = new Queue<ActionInfo>();       
    }

    void Start()
    {
        netGos = GLOBALS.networkGO;
    }
   
    void Update()
    {
        if (actions.Count > 0) ProcessActions();
    }

    public void AddAction(uint sequence,ClientActions act,uint entity)
    {
        //Debug.Log("Action: " + act.ToString());
        //Debug.Log("Net ID: " + entity);
        //Debug.Log("Sequence: " + sequence);
        ActionInfo action = new ActionInfo(sequence,act,entity);
        actions.Enqueue(action);
    }

    private void ProcessActions()
    {
        for (int i = 0; i < actions.Count; i++)
        {
            ActionInfo info = actions.Dequeue();
            NetworkEntity entity = netGos.GetEntity(info.GetID());
            if (entity != null)
            {
                ClientActions action = info.GetAction();
                Packet actionBundleResponse = null;
                //Debug.Log("Client action: " + action.ToString());
                switch (action)
                {
                    case ClientActions.ACTION_SHOOT:
                        uint id = GLOBALS.serverGame.GetNewNetID();
                        Transform t = entity.GetComponent<TankController>().GetShootPoint();
                        GLOBALS.networkGO.SpawnGo(1, id, t.position, t.rotation);
                        entity.GetComponent<TankController>().PerformAction(action);

                        actionBundleResponse.Write(entity.netID);
                        actionBundleResponse.Write((byte)ClientActions.ACTION_SHOOT);
                        actionBundleResponse.Write(id);
                        GLOBALS.serverGame.BroadcastPacket(actionBundleResponse, ClientMSG.CM_CLIENT_ACTION, true);
                        break;

                    case ClientActions.ACTION_MOVE_FRONT:
                        entity.GetComponent<TankController>().PerformAction(action);
                        break;

                    case ClientActions.ACTION_MOVE_BACK:
                        entity.GetComponent<TankController>().PerformAction(action);
                        break;

                    case ClientActions.ACTION_ROTATE_LEFT:
                        entity.GetComponent<TankController>().PerformAction(action);
                        break;

                    case ClientActions.ACTION_ROTATE_RIGHT:
                        entity.GetComponent<TankController>().PerformAction(action);
                        break;

                    case ClientActions.ACTION_CANNON_ROTATE_LEFT:
                        entity.GetComponent<TankController>().PerformAction(action);
                        break;

                    case ClientActions.ACTION_CANNON_ROTATE_RIGHT:
                        entity.GetComponent<TankController>().PerformAction(action);
                        break;

                    default:
                        Debug.Log("Action not found");
                        break;
                }
            }
        }
    }
}
