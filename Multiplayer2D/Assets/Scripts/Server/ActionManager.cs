using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;

public class ActionManager : MonoBehaviour
{
    public int maxFrameActions = 10;

    int maxInputs = 10;
    NetworkGameobjects netGos;
    Queue<InputInfo> pendingInputs;

    private void Awake()
    {
        pendingInputs = new Queue<InputInfo>();       
    }

    void Start()
    {
        netGos = GLOBALS.networkGO;
    }
   
    void Update()
    {
        if (pendingInputs.Count > 0) ProcessInputs();
    }

    public void AddInputs(uint sequence, bool[] inputs, uint entity, IPEndPoint player)
    {
        //Debug.Log(string.Join(", ", inputs.Select(b => b.ToString()).ToArray()));
        InputInfo action = new InputInfo(sequence, inputs, entity, player);
        pendingInputs.Enqueue(action);
    }

    private void ProcessInputs()
    {
        for (int i = 0; i < pendingInputs.Count && i < maxInputs; i++)
        {
            InputInfo info = pendingInputs.Dequeue();
            NetworkEntity entity = netGos.GetEntity(info.GetID());
            if (entity != null)
            {
                if (info.GetSequence() > GLOBALS.serverGame.GetClient(info.GetPlayer()).lastActionSequence)
                {
                    bool[] inputs = info.GetInputs();
                    Packet pak;
                    for (int a = 0; a < inputs.Length; a++)
                    {
                        if (inputs[a])
                        {                        
                            PlayerInputsENUM doInput = (PlayerInputsENUM)a;
                            switch (doInput)
                            {
                                case PlayerInputsENUM.INPUT_SHOOT:
                                    TankController controller = entity.GetComponent<TankController>();
                                    if (controller.Canshoot())
                                    {
                                        uint id = GLOBALS.serverGame.GetNewNetID();
                                        Transform t = controller.GetShootPoint();
                                        Transform canT = controller.GetCannonTrans();
                                        GLOBALS.networkGO.SpawnGo(1, id, t.position, t.rotation);
                                        controller.PerformAction(doInput);
                                        pak = new Packet();
                                        pak.Write(entity.netID);
                                        pak.Write((byte)ClientActions.ACTION_SHOOT);
                                        pak.Write(entity.transform.position);
                                        pak.Write(entity.transform.rotation);
                                        pak.Write(canT.position);
                                        pak.Write(canT.rotation);
                                        pak.Write(id);
                                        GLOBALS.serverGame.BroadcastPacket(pak, ClientMSG.CM_CLIENT_ACTION, true);
                                    }
                                    break;

                                case PlayerInputsENUM.INPUT_MOVE_FRONT:
                                    entity.GetComponent<TankController>().PerformAction(doInput);
                                    break;

                                case PlayerInputsENUM.INPUT_MOVE_BACK:
                                    entity.GetComponent<TankController>().PerformAction(doInput);
                                    break;

                                case PlayerInputsENUM.INPUT_ROTATE_LEFT:
                                    entity.GetComponent<TankController>().PerformAction(doInput);
                                    break;

                                case PlayerInputsENUM.INPUT_ROTATE_RIGHT:
                                    entity.GetComponent<TankController>().PerformAction(doInput);
                                    break;

                                case PlayerInputsENUM.INPUT_CANNON_ROTATE_LEFT:
                                    entity.GetComponent<TankController>().PerformAction(doInput);
                                    break;

                                case PlayerInputsENUM.INPUT_CANNON_ROTATE_RIGHT:
                                    entity.GetComponent<TankController>().PerformAction(doInput);
                                    break;

                                default:
                                    Debug.Log("Action not found");
                                    break;
                            }
                        }
                    }
                    GLOBALS.serverGame.GetClient(info.GetPlayer()).newActionSequence = info.GetSequence();
                }
            }
        }
        //GLOBALS.serverGame.SendActionVerify();
    }
}
