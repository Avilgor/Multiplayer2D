using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NetworkEntity))]
public class TankController : MonoBehaviour
{
    public AudioClip tankExplosion, tankShot, tankMove, tankIdle;
    public bool locked = false;
    public float speed = 5.0f;
    public float turnSpeed = 1.0f;
    public float cannonSpeed = 2.0f;
    public float shootCD = 2.0f;
    public Transform cannon;
    public Transform shootPoint;
    public ParticleSystem explosionFX;
    public GameObject tankBody;
    NetworkEntity entity;
    AudioSource source;
    bool shootReady;
    bool[] inputs;
    Vector3 lastPos;
    bool idle,changeSound;
    //Rigidbody2D rb;
    //float mov = 0;
    //float rot = 0;

    private void Awake()
    {
        //rb = GetComponent<Rigidbody2D>();
        entity = GetComponent<NetworkEntity>();
        source = GetComponent<AudioSource>();
    }

    void Start()
    {
        idle = true;
        changeSound = false;
        shootReady = true;
    }

    void Update()
    {
        /*if (idle && changeSound)
        {
            changeSound = false;
            source.Stop();
            source.clip = tankIdle;
            source.Play();
        }
        else if(changeSound)
        {
            changeSound = false;
            source.Stop();
            source.clip = tankMove;
            source.Play();
        }

        if (lastPos == transform.position && !idle)
        {
            changeSound = true;
            idle = true;
        }
        else if (idle)
        { 
            changeSound = true;
            idle = false;
        }*/

        if (locked) return;
        bool gotInput = false;
        inputs = new bool[16];
        //mov = 0;
        //rot = 0;

        if (Input.GetKey(KeyCode.W))
        {
            //transform.position = transform.position + (transform.up * (speed / 100));
            //Vector2 res = transform.up * (speed / 100);
            //rb.position += res;
            inputs[(int)PlayerInputsENUM.INPUT_MOVE_FRONT] = true;
            gotInput = true;
        }

        if (Input.GetKey(KeyCode.S))
        {
            //transform.position = transform.position + (transform.up * (-speed / 100));
            //Vector2 res = transform.up * (-speed / 100);
            //rb.position += res;
            inputs[(int)PlayerInputsENUM.INPUT_MOVE_BACK] = true;
            gotInput = true;
        }

        if (Input.GetKey(KeyCode.A))
        {
            //transform.Rotate(new Vector3(0, 0, turnSpeed / 100));
            //rb.rotation += turnSpeed / 100;
            inputs[(int)PlayerInputsENUM.INPUT_ROTATE_LEFT] = true;
            gotInput = true;
        }

        if (Input.GetKey(KeyCode.D))
        {
            //transform.Rotate(new Vector3(0, 0, -turnSpeed / 100));
            //rb.rotation += -turnSpeed / 100;
            inputs[(int)PlayerInputsENUM.INPUT_ROTATE_RIGHT] = true;
            gotInput = true;
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            inputs[(int)PlayerInputsENUM.INPUT_SHOOT] = true;
            gotInput = true;
        }

        if (Input.GetKey(KeyCode.K))
        {
            //cannon.Rotate(new Vector3(0, 0, cannonSpeed / 100));
            inputs[(int)PlayerInputsENUM.INPUT_CANNON_ROTATE_LEFT] = true;
            gotInput = true;
        }

        if (Input.GetKey(KeyCode.L))
        {
            //cannon.Rotate(new Vector3(0, 0, -cannonSpeed / 100));
            inputs[(int)PlayerInputsENUM.INPUT_CANNON_ROTATE_RIGHT] = true;
            gotInput = true;
        }

        //if (mov != 0) transform.position = transform.position + (transform.up * mov);
        //if (rot != 0) transform.Rotate(new Vector3(0,0, rot));

        //MouseLook
        //var dir = Input.mousePosition - Camera.main.WorldToScreenPoint(cannon.position);
        //float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        //cannon.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        //Vector3 vec = Camera.main.ScreenToWorldPoint(Input.mousePosition) - cannon.position;
        //Vector2 vec90 = new Vector2(-vec.y, vec.x);
        //float sign = (Vector2.Dot(vec90, cannon.up) < 0) ? -1.0f : 1.0f;

        /*if (sign < 0)
        {
            //cannon.Rotate(new Vector3(0, 0, cannonSpeed / 100));
            inputs[(int)PlayerInputsENUM.INPUT_CANNON_ROTATE_LEFT] = true;
            gotInput = true;
            //GLOBALS.playerActions.NewAction(entity.netID, ClientActions.ACTION_CANNON_ROTATE_LEFT, transform.position, transform.rotation);
        }
        else if (sign > 0)
        {
            //cannon.Rotate(new Vector3(0, 0, -cannonSpeed / 100));
            inputs[(int)PlayerInputsENUM.INPUT_CANNON_ROTATE_RIGHT] = true;
            gotInput = true;
        }*/

        if (gotInput) GLOBALS.playerActions.NewInput(entity.netID, inputs, transform.position, transform.rotation);
        lastPos = transform.position;
    }

    public void SetCanonID(uint id)
    {
        cannon.GetComponent<NetworkEntity>().SetEntity(id);
    }

    public uint GetCannonID()
    {
        return cannon.GetComponent<NetworkEntity>().netID;
    }

    public Transform GetCannonTrans()
    {
        return cannon.transform;
    }

    public bool Canshoot()
    {
        return shootReady;
    }

    /*public Rigidbody2D GetRigidbody()
    {
        return rb;
    }*/

    /*public void FixedUpdate()
    {
        if (locked) return;

        if (Input.GetKey(KeyCode.W))
        {
            Vector2 res = transform.up * (speed / 100);
            rb.position += res;
            GLOBALS.playerActions.NewAction(entity.netID, ClientActions.ACTION_MOVE_FRONT, transform.position, transform.rotation);
        }

        if (Input.GetKey(KeyCode.S))
        {
            Vector2 res = transform.up * (-speed / 100);
            rb.position += res;
            GLOBALS.playerActions.NewAction(entity.netID, ClientActions.ACTION_MOVE_BACK, transform.position, transform.rotation);
        }

        if (Input.GetKey(KeyCode.A))
        {
            rb.rotation += turnSpeed / 100;
            GLOBALS.playerActions.NewAction(entity.netID, ClientActions.ACTION_ROTATE_LEFT, transform.position, transform.rotation);
        }

        if (Input.GetKey(KeyCode.D))
        {
            rb.rotation += -turnSpeed / 100;
            GLOBALS.playerActions.NewAction(entity.netID, ClientActions.ACTION_ROTATE_RIGHT, transform.position, transform.rotation);
        }

        if (mov != 0)
        {
            Vector2 res = transform.up * mov;
            rb.position += res;
        }
        if (rot != 0) rb.rotation += rot; 
    }*/

    private void ServerShoot()
    {
        if (shootReady)
        {        
            shootReady = false;
            StartCoroutine(ShootReload());
        }
    }

    public void ClientShoot()
    {       
        //Particles FX

    }

    public Transform GetShootPoint()
    {
        return shootPoint;
    }

    public void PerformAction(PlayerInputsENUM input)
    {
        //Vector2 res;
        switch (input)
        {
            case PlayerInputsENUM.INPUT_SHOOT:
                ServerShoot();
                break;
            case PlayerInputsENUM.INPUT_ROTATE_RIGHT:
                transform.Rotate(new Vector3(0, 0, -turnSpeed / 100));
                //rb.rotation += (-turnSpeed / 100);
                break;
            case PlayerInputsENUM.INPUT_ROTATE_LEFT:
                transform.Rotate(new Vector3(0, 0, turnSpeed / 100));
                //rb.rotation += (turnSpeed / 100);
                break;
            case PlayerInputsENUM.INPUT_MOVE_FRONT:
                transform.position = transform.position + (transform.up * (speed / 100));
                //res = transform.up * speed / 100;
                //rb.position += res;
                break;
            case PlayerInputsENUM.INPUT_MOVE_BACK:
                transform.position = transform.position + (transform.up * (-speed / 100));
                //res = transform.up * -speed / 100;
                //rb.position += res;
                break;
            case PlayerInputsENUM.INPUT_CANNON_ROTATE_RIGHT:
                cannon.Rotate(new Vector3(0, 0, -cannonSpeed / 100));
                break;
            case PlayerInputsENUM.INPUT_CANNON_ROTATE_LEFT:
                cannon.Rotate(new Vector3(0, 0, cannonSpeed / 100));
                break;
            default:
                break;
        }
    }

    /*public void PerformReconciliationAction(bool[] inputs,uint seq)
    {
        //Vector2 res;
        PlayerInputsENUM doInput;
        for (int i = 0; i < inputs.Length; i++)
        {
            if (inputs[i])
            {
                doInput = (PlayerInputsENUM)i;
                switch (doInput)
                {
                    case PlayerInputsENUM.INPUT_SHOOT:

                        break;
                    case PlayerInputsENUM.INPUT_ROTATE_RIGHT:
                        transform.Rotate(new Vector3(0, 0, -turnSpeed / 100));
                        //rb.rotation += (-turnSpeed / 100);                        
                        break;
                    case PlayerInputsENUM.INPUT_ROTATE_LEFT:
                        transform.Rotate(new Vector3(0, 0, turnSpeed / 100));
                        //rb.rotation += (turnSpeed / 100);
                        break;
                    case PlayerInputsENUM.INPUT_MOVE_FRONT:
                        transform.position = transform.position + (transform.up * (speed / 100));
                        //res = transform.up * speed / 100;
                        //rb.position += res;                      
                        break;
                    case PlayerInputsENUM.INPUT_MOVE_BACK:
                        transform.position = transform.position + (transform.up * (-speed / 100));
                        //res = transform.up * -speed / 100;
                        //rb.position += res;
                        break;
                    case PlayerInputsENUM.INPUT_CANNON_ROTATE_RIGHT:
                        cannon.Rotate(new Vector3(0, 0, -cannonSpeed / 100));                      
                        break;
                    case PlayerInputsENUM.INPUT_CANNON_ROTATE_LEFT:
                        cannon.Rotate(new Vector3(0, 0, cannonSpeed / 100));
                        break;

                    default:
                        break;
                }
            }
        }
        GLOBALS.playerActions.ReconciliatedInput(entity.netID,inputs,transform.position,transform.rotation,seq);
    }*/

    private void ResetTank(Vector3 pos,Quaternion rot)
    {
        shootReady = true;
        transform.position = pos;
        transform.rotation = rot;
        tankBody.SetActive(true);
    }

    public void TankDestroyed(Vector3 pos, Quaternion rot)
    {
        StartCoroutine(MoveTank(pos,rot));
        //FX
        PlayDestroyedFX();
        explosionFX.Play();
        tankBody.SetActive(false);
    }

    public void PlayShotFX()
    {
        source.PlayOneShot(tankShot);
    }

    public void PlayDestroyedFX()
    {
        source.PlayOneShot(tankExplosion);   
    }

    public void TakeDamage()
    {
        ServerClient player = GLOBALS.serverGame.GetClientByTankID(GetComponent<NetworkEntity>().netID);
        if (player != null)
        {          
            player.lifes--;
            if (player.lifes <= 0)
            {
                //Player defeated
                GLOBALS.serverGame.PlayerDefeated(player.ep);
            }
            else
            {
                Transform t = GLOBALS.serverGame.GetRandomSpawnPoint(player.lastSpawn);
                player.lastSpawn = t.position;
                Packet pak = new Packet();
                pak.Write(GetComponent<NetworkEntity>().netID);
                pak.Write((byte)ClientActions.ACTION_TANK_DESTROYED);
                pak.Write(t.position);
                pak.Write(t.rotation);
                GLOBALS.serverGame.BroadcastPacket(pak,ClientMSG.CM_CLIENT_ACTION,true);
                TankDestroyed(t.position,t.rotation);                
            }
        }
    }

    IEnumerator MoveTank(Vector3 pos, Quaternion rot)
    {
        yield return new WaitForSeconds(2);
        ResetTank(pos,rot);
    }

    IEnumerator ShootReload()
    {
        yield return new WaitForSeconds(shootCD);
        shootReady = true;
    }
}