using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum PlayerInputsENUM
{
    INPUT_MOVE_FRONT = 0,
    INPUT_MOVE_BACK,
    INPUT_ROTATE_LEFT,
    INPUT_ROTATE_RIGHT,
    INPUT_CANNON_ROTATE_LEFT,
    INPUT_CANNON_ROTATE_RIGHT,
    INPUT_SHOOT,
    INPUT_MAX = 15,
}

public enum ClientActions : byte
{
    ACTION_TANK_DESTROYED,
    ACTION_SHOOT,
    ACITON_MAX
}

public class PlayerActionsManager : MonoBehaviour
{
    public float actionsSendTime = 0.05f;

    Queue<InputInfo> playerInputs;
    Queue<InputInfo> unverifiedInputs;
    float counter;
    uint sequenceNum;

    private void Awake()
    {
        GLOBALS.playerActions = this;
        playerInputs = new Queue<InputInfo>();
        unverifiedInputs = new Queue<InputInfo>();
    }
   
    void Start()
    {
        counter = 0;
        sequenceNum = 0;
    }

    void Update()
    {
        if (counter < actionsSendTime) counter += Time.deltaTime;
        else
        {
            if (playerInputs.Count > 0)
            {
                InputsBundle();
                counter = 0;
            }
        }
    }

    public void NewInput(uint id,bool[] inputs,Vector3 pos, Quaternion rot)
    {
        //Debug.Log(string.Join(", ", inputs.Select(b => b.ToString()).ToArray()));
        InputInfo info = new InputInfo(sequenceNum, inputs, id,pos,rot);
        playerInputs.Enqueue(info);
        sequenceNum++;
    }

    public void ReconciliatedInput(uint id, bool[] inputs, Vector3 pos, Quaternion rot,uint seq)
    {
        InputInfo info = new InputInfo(seq, inputs, id, pos, rot);
        unverifiedInputs.Enqueue(info);
    }

    public void VerifyInput(uint seq,Vector3 pos, Quaternion rot)
    {
        //bool update = false;
        InputInfo[] array = unverifiedInputs.ToArray();
        unverifiedInputs.Clear();
        GLOBALS.playerEntity.SetPosition(pos);
        GLOBALS.playerEntity.SetRotation(rot);
        for (int i = 0; i < array.Length; i++)
        {
            /*if (array[i].GetSequence() == seq)
            {
                float res = Mathf.Abs(pos.magnitude - array[i].GetPosition().magnitude);
                float angle = Mathf.Abs(Quaternion.Angle(rot, array[i].GetRotation()));
                if (res > 1)
                {
                    GLOBALS.playerEntity.SetPosition(pos);
                    update = true;
                }
                if (angle > 5)
                {
                    GLOBALS.playerEntity.SetRotation(rot);
                    update = true;
                }                
            }
            else */if (array[i].GetSequence() > seq)
            {
                /*if (update) */GLOBALS.playerTank.PerformReconciliationAction(array[i].GetInputs(),array[i].GetSequence());
                //else unverifiedActions.Enqueue(array[i]);
            }
        }      
    }

    public void ReceivedActions(Packet pak)
    {
        uint netID = pak.ReadUInt();
        ClientActions act = (ClientActions)pak.ReadByte();
        NetworkEntity entity = GLOBALS.networkGO.GetEntity(netID);
        if (entity != null)
        {
            switch (act)
            {
                case ClientActions.ACTION_SHOOT:
                    Vector3 tPos = pak.ReadVector3();
                    Quaternion tQuat = pak.ReadQuaternion();
                    Vector3 cPos = pak.ReadVector3();
                    Quaternion cQuat = pak.ReadQuaternion();
                    uint bulletID = pak.ReadUInt();
                    TankController controller = entity.GetComponent<TankController>();
                    entity.UpdatePosition(tPos);
                    entity.UpdateRotation(tQuat);
                    controller.GetCannonTrans().GetComponent<NetworkEntity>().UpdatePosition(cPos);
                    controller.GetCannonTrans().GetComponent<NetworkEntity>().UpdateRotation(cQuat);
                    GLOBALS.networkGO.SpawnGo(1, bulletID, controller.GetShootPoint().position, controller.GetShootPoint().rotation);
                    break;

                case ClientActions.ACTION_TANK_DESTROYED:
                    Vector3 resPos = pak.ReadVector3();
                    Quaternion resQuat = pak.ReadQuaternion();
                    entity.GetComponent<TankController>().TankDestroyed(resPos,resQuat);
                    break;

                default:
                    break;
            }
        }
    }

    private void InputsBundle()
    {
        Packet pak =  new Packet();
        InputInfo[] array = playerInputs.ToArray();
        playerInputs.Clear();
        pak.Write((byte)array.Length);

        for (int i = 0;i < array.Length;i++)
        {
            pak.Write(array[i].GetID());
            pak.Write(array[i].GetSequence());
            pak.Write(new BitArray(array[i].GetInputs()));
            //unverifiedInputs.Enqueue(array[i]);
        }
        GLOBALS.clientGame.SendPacket(pak,ServerMSG.SM_PLAYER_INPUT,false);
    }
}
