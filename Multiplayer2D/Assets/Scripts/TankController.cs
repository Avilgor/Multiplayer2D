using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(NetworkEntity))]
public class TankController : MonoBehaviour
{
    public GameObject shell;
    public bool locked = false;
    public float speed = 5.0f;
    public float turnSpeed = 1.0f;
    public float cannonSpeed = 2.0f;
    public float shootCD = 2.0f;
    public Transform cannon;
    public Transform shootPoint;
    NetworkEntity entity;

    bool shootReady;
    Rigidbody2D rb;
    float mov = 0;
    float rot = 0;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        entity = GetComponent<NetworkEntity>();
    }

    void Start()
    {
        shootReady = true;
        //if (entity.clientControlled) StartCoroutine(PlayerActionsBundle());
    }
    
    void Update()
    {
        if (locked) return;
        
        mov = 0;
        rot = 0;

        if (Input.GetKey(KeyCode.W))
        {
            //mov = speed/100;
            GLOBALS.playerActions.NewAction(entity.netID,ClientActions.ACTION_MOVE_FRONT);
        }

        if (Input.GetKey(KeyCode.S))
        {
            //mov = -speed/100;
            GLOBALS.playerActions.NewAction(entity.netID, ClientActions.ACTION_MOVE_BACK);
        }

        if (Input.GetKey(KeyCode.A))
        {
            //rot = turnSpeed/100;
            GLOBALS.playerActions.NewAction(entity.netID, ClientActions.ACTION_ROTATE_LEFT);
        }

        if (Input.GetKey(KeyCode.D))
        {
            //rot = -turnSpeed/100;
            GLOBALS.playerActions.NewAction(entity.netID, ClientActions.ACTION_ROTATE_RIGHT);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            GLOBALS.playerActions.NewAction(entity.netID, ClientActions.ACTION_SHOOT);
        }
        
        //if (mov != 0) transform.position = transform.position + (transform.up * mov);
        //if (rot != 0) transform.Rotate(new Vector3(0,0, rot));

        //MouseLook
        Vector3 vec = Camera.main.ScreenToWorldPoint(Input.mousePosition) - cannon.position;
        Vector2 vec90 = new Vector2(-vec.y, vec.x);
        float sign = (Vector2.Dot(vec90, cannon.up) < 0) ? -1.0f : 1.0f;

        if (sign < 0)
        {
            //cannon.Rotate(new Vector3(0, 0, cannonSpeed/100));
            //GLOBALS.playerActions.NewAction(entity.netID, ClientActions.ACTION_CANNON_ROTATE_LEFT);
        }
        else if (sign > 0)
        {
            //cannon.Rotate(new Vector3(0, 0, -cannonSpeed/100));
            //GLOBALS.playerActions.NewAction(entity.netID, ClientActions.ACTION_CANNON_ROTATE_RIGHT);
        }

        /*Vector3 diff = Camera.main.ScreenToWorldPoint(Input.mousePosition) - cannon.position;
        diff.Normalize();
        float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        cannon.rotation = Quaternion.Euler(0f, 0f, rot_z - 90);*/                       
    }

    public void FixedUpdate()
    {
        if (mov != 0)
        {
            Vector2 res = transform.up * mov;
            rb.position += res;
        }
        if (rot != 0) rb.rotation += rot;             
    }

    private void ServerShoot()
    {
        if (shootReady)
        {        
            shootReady = false;
            StartCoroutine(ShootReload());
            GLOBALS.networkGO.SpawnGo(1,215,shootPoint.position,shootPoint.rotation);
            //Particles FX

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

    public void PerformAction(ClientActions action)
    {
        Vector2 res;
        switch (action)
        {
            case ClientActions.ACTION_SHOOT:
                ServerShoot();
                break;
            case ClientActions.ACTION_ROTATE_RIGHT:
                rb.rotation += (-turnSpeed / 100);
                break;
            case ClientActions.ACTION_ROTATE_LEFT:
                rb.rotation += (turnSpeed / 100);
                break;
            case ClientActions.ACTION_MOVE_FRONT:
                res = transform.up * speed / 100;
                rb.position += res;
                break;
            case ClientActions.ACTION_MOVE_BACK:
                res = transform.up * -speed / 100;
                rb.position += res;
                break;
            case ClientActions.ACTION_CANNON_ROTATE_RIGHT:
                cannon.Rotate(new Vector3(0, 0, -cannonSpeed/100));
                break;
            case ClientActions.ACTION_CANNON_ROTATE_LEFT:
                cannon.Rotate(new Vector3(0, 0, cannonSpeed/100));
                break;
            default:
                break;
        }
    }


    public void ResetTank()
    {
        shootReady = true;
        //Move to new spawnPoint
    }

    public void TakeDamage()
    {
        ResetTank();
    }

    public void PlayerDeath()
    {
        
    }

    IEnumerator ShootReload()
    {
        yield return new WaitForSeconds(shootCD);
        shootReady = true;
    }

    /*IEnumerator PlayerActionsBundle()
    {
        //Each 20ms pack requested player actions and send
        Packet pak;
        Queue<ClientActions> act;
        
        yield return new WaitForSeconds(0.02f);
        if (tankActions.Count > 0)
        {
            pak = new Packet();
            act = new Queue<ClientActions>(tankActions);
            tankActions.Clear();
            int i = act.Count;
            pak.Write((byte)i);
            for (int a = 0; a < i; a++)
            {
                pak.Write((byte)act.Dequeue());
            }
            //Save sended state            
            GLOBALS.clientGame.SendActionsBundle(pak);
        }
        StartCoroutine(PlayerActionsBundle());
    }*/
}