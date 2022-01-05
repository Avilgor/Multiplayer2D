using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NetworkEntity))]
public class TankController : MonoBehaviour
{
    public AudioClip tankExplosion, tankShot;
    public bool locked = false;
    public float speed = 5.0f;
    public float turnSpeed = 1.0f;
    public float cannonSpeed = 2.0f;
    public float shootCD = 2.0f;
    public Transform cannon;
    public Transform shootPoint;
    public ParticleSystem explosionFX;
    public GameObject tankBody;
    public SpriteRenderer bodySprite, cannonSprite;
    NetworkEntity entity;
    AudioSource source;
    bool shootReady;
    bool[] inputs;
    bool client;
    bool destroyed;
    Rigidbody2D rb;
    
    //float mov = 0;
    //float rot = 0;

    private void Awake()
    {
        client = GLOBALS.isclient;
        rb = GetComponent<Rigidbody2D>();
        entity = GetComponent<NetworkEntity>();
        source = GetComponent<AudioSource>();
    }

    void Start()
    {
        shootReady = true;
        destroyed = false;
    }

    void Update()
    {
        if (locked) return;
        bool gotInput = false;
        inputs = new bool[16];
        //mov = 0;
        //rot = 0;
        Vector2 res;
        if (Input.GetKey(KeyCode.W))
        {
            //Vector2 res = transform.up * (speed / 100);
            //rb.position += res;
            if (client)
            {
                inputs[(int)PlayerInputsENUM.INPUT_MOVE_FRONT] = true;
                gotInput = true;
            }
            else
            {
                //transform.position = transform.position + (transform.up * speed * Time.deltaTime);
                res = transform.up * speed * Time.deltaTime;
                rb.position += res;
            }
        }

        if (Input.GetKey(KeyCode.S))
        {
            //Vector2 res = transform.up * (-speed / 100);
            //rb.position += res;
            if (client)
            {
                inputs[(int)PlayerInputsENUM.INPUT_MOVE_BACK] = true;
                gotInput = true;
            }
            else
            {
                //transform.position = transform.position + (transform.up * (-speed) * Time.deltaTime);
                res = transform.up * -speed * Time.deltaTime;
                rb.position += res;
            }
        }

        if (Input.GetKey(KeyCode.A))
        {
            //rb.rotation += turnSpeed / 100;
            if (client)
            {
                inputs[(int)PlayerInputsENUM.INPUT_ROTATE_LEFT] = true;
                gotInput = true;
            }
            else
            {
                //transform.Rotate(new Vector3(0, 0, turnSpeed * Time.deltaTime));
                rb.rotation += (turnSpeed * Time.deltaTime);
            }
        }

        if (Input.GetKey(KeyCode.D))
        {
            //rb.rotation += -turnSpeed / 100;
            if (client)
            {
                inputs[(int)PlayerInputsENUM.INPUT_ROTATE_RIGHT] = true;
                gotInput = true;
            }
            else
            {
                //transform.Rotate(new Vector3(0, 0, -turnSpeed * Time.deltaTime));
                rb.rotation += (-turnSpeed * Time.deltaTime);
            }
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (client)
            {
                inputs[(int)PlayerInputsENUM.INPUT_SHOOT] = true;
                gotInput = true;
            }
            else HostTankShoot();
            
        }

        if (Input.GetKey(KeyCode.K))
        {
            if (client)
            {
                inputs[(int)PlayerInputsENUM.INPUT_CANNON_ROTATE_LEFT] = true;
                gotInput = true;
            }
            else
            {
                cannon.Rotate(new Vector3(0, 0, cannonSpeed * Time.deltaTime));
            }
        }

        if (Input.GetKey(KeyCode.L))
        {
            if (client)
            {
                inputs[(int)PlayerInputsENUM.INPUT_CANNON_ROTATE_RIGHT] = true;
                gotInput = true;
            }
            else
            {
                cannon.Rotate(new Vector3(0, 0, -cannonSpeed * Time.deltaTime));
            }
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

        if (gotInput && client) GLOBALS.playerActions.NewInput(entity.netID, inputs, transform.position, transform.rotation);
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

    private void HostTankShoot()
    {
        if (shootReady)
        {
            shootReady = false;
            StartCoroutine(ShootReload());

            uint id = GLOBALS.serverGame.GetNewNetID();
            GLOBALS.networkGO.SpawnGo(1, id, shootPoint.position, shootPoint.rotation);
            Packet pak = new Packet();
            pak.Write(entity.netID);
            pak.Write((byte)ClientActions.ACTION_SHOOT);
            pak.Write(transform.position);
            pak.Write(transform.rotation);
            pak.Write(cannon.position);
            pak.Write(cannon.rotation);
            pak.Write(id);
            GLOBALS.serverGame.BroadcastPacket(pak, ClientMSG.CM_CLIENT_ACTION, true);
        }
    }

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
        Vector2 res;
        switch (input)
        {
            case PlayerInputsENUM.INPUT_SHOOT:
                if(!destroyed) ServerShoot();
                break;
            case PlayerInputsENUM.INPUT_ROTATE_RIGHT:
                //transform.Rotate(new Vector3(0, 0, -turnSpeed * Time.deltaTime));
                rb.rotation += (-turnSpeed * Time.deltaTime);
                break;
            case PlayerInputsENUM.INPUT_ROTATE_LEFT:
                //transform.Rotate(new Vector3(0, 0, turnSpeed * Time.deltaTime));
                rb.rotation += (turnSpeed * Time.deltaTime);
                break;
            case PlayerInputsENUM.INPUT_MOVE_FRONT:
                //transform.position = transform.position + (transform.up * speed * Time.deltaTime);
                res = transform.up * speed * Time.deltaTime;
                rb.position += res;
                break;
            case PlayerInputsENUM.INPUT_MOVE_BACK:
                //transform.position = transform.position + (transform.up * (-speed) * Time.deltaTime);
                res = transform.up * -speed * Time.deltaTime;
                rb.position += res;
                break;
            case PlayerInputsENUM.INPUT_CANNON_ROTATE_RIGHT:
                cannon.Rotate(new Vector3(0, 0, -cannonSpeed * Time.deltaTime));
                break;
            case PlayerInputsENUM.INPUT_CANNON_ROTATE_LEFT:
                cannon.Rotate(new Vector3(0, 0, cannonSpeed * Time.deltaTime));
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
        destroyed = false;
    }

    public void TankDestroyed(Vector3 pos, Quaternion rot)
    {
        StartCoroutine(MoveTank(pos,rot));
        //FX
        PlayDestroyedFX();
        explosionFX.Play();
        tankBody.SetActive(false);
        destroyed = true;
    }

    public void PlayShotFX()
    {
        source.PlayOneShot(tankShot);
    }

    public void PlayDestroyedFX()
    {
        source.PlayOneShot(tankExplosion);   
    }

    public void SetColor(Color co)
    {
        bodySprite.color = co;
        cannonSprite.color = co;
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