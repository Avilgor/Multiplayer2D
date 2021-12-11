using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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
    public NetworkEntity entity;

    bool shootReady;
    Queue<ClientActions> tankActions;
    //Rigidbody2D rb;

    private void Awake()
    {
        //rb = GetComponent<Rigidbody2D>();
        tankActions = new Queue<ClientActions>();
    }

    void Start()
    {
        locked = false;
        shootReady = true;
        if (entity.clientControlled) StartCoroutine(PlayerActionsBundle());
    }
    
    void Update()
    {
        if (locked) return;
        
        float mov = 0;
        float rot = 0;

        if (Input.GetKey(KeyCode.W))
        {
            mov = speed;
            tankActions.Enqueue(ClientActions.ACTION_MOVE_FRONT);
        }

        if (Input.GetKey(KeyCode.S))
        {
            mov = -speed;
            tankActions.Enqueue(ClientActions.ACTION_MOVE_BACK);
        }

        if (Input.GetKey(KeyCode.A))
        {
            rot = turnSpeed;
            tankActions.Enqueue(ClientActions.ACTION_ROTATE_LEFT);
        }

        if (Input.GetKey(KeyCode.D))
        {
            rot = -turnSpeed;
            tankActions.Enqueue(ClientActions.ACTION_ROTATE_RIGHT);
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            tankActions.Enqueue(ClientActions.ACTION_SHOOT);
        }

        //if (mov != 0) rb.MovePosition(transform.position + (transform.up * mov * Time.deltaTime));
        transform.position = transform.position + (transform.up * mov * Time.deltaTime);
        if (rot != 0) transform.Rotate(new Vector3(0,0, rot * Time.deltaTime));

        //MouseLook
        Vector3 vec = Camera.main.ScreenToWorldPoint(Input.mousePosition) - cannon.position;
        Vector2 vec90 = new Vector2(-vec.y, vec.x);
        float sign = (Vector2.Dot(vec90, cannon.up) < 0) ? -1.0f : 1.0f;

        if (sign < 0)
        {
            cannon.Rotate(new Vector3(0, 0, cannonSpeed * Time.deltaTime));
            tankActions.Enqueue(ClientActions.ACTION_ROTATE_LEFT);
        }
        else if (sign > 0)
        {
            cannon.Rotate(new Vector3(0, 0, -cannonSpeed * Time.deltaTime));
            tankActions.Enqueue(ClientActions.ACTION_CANNON_ROTATE_RIGHT);
        }

        /*Vector3 diff = Camera.main.ScreenToWorldPoint(Input.mousePosition) - cannon.position;
        diff.Normalize();
        float rot_z = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;
        cannon.rotation = Quaternion.Euler(0f, 0f, rot_z - 90);*/                       
    }

    private void ServerShoot()
    {
        if (shootReady)
        {        
            shootReady = false;
            StartCoroutine(ShootReload());

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
        switch (action)
        {
            case ClientActions.ACTION_SHOOT:
                ServerShoot();
                break;
            case ClientActions.ACTION_ROTATE_RIGHT:
                transform.Rotate(new Vector3(0, 0, -turnSpeed * Time.deltaTime));
                break;
            case ClientActions.ACTION_ROTATE_LEFT:
                transform.Rotate(new Vector3(0, 0, turnSpeed * Time.deltaTime));
                break;
            case ClientActions.ACTION_MOVE_FRONT:
                transform.position = transform.position + (transform.up * speed * Time.deltaTime);
                break;
            case ClientActions.ACTION_MOVE_BACK:
                transform.position = transform.position + (transform.up * -speed * Time.deltaTime);
                break;
            case ClientActions.ACTION_CANNON_ROTATE_RIGHT:
                cannon.Rotate(new Vector3(0, 0, -cannonSpeed * Time.deltaTime));
                break;
            case ClientActions.ACTION_CANNON_ROTATE_LEFT:
                cannon.Rotate(new Vector3(0, 0, cannonSpeed * Time.deltaTime));
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

    IEnumerator PlayerActionsBundle()
    {
        //Each 30ms pack requested player actions and send
        //This will by nature add 30ms ping
        bool data = false;
        Packet pak = null;
        if (tankActions.Count > 0)
        {
            data = true;
            pak = new Packet();
            byte i = (byte)tankActions.Count;
            pak.Write(i);//Save amount of actions
            for (int a = 0; a < i; a++)
            {
                pak.Write((byte)tankActions.Dequeue());
            }
            //Save sended state
            entity.UpdateLastState(transform.position,transform.rotation,transform.localScale);
        }
        yield return new WaitForSeconds(0.03f);
        if (data) GLOBALS.clientGame.SendActionsBundle(pak);
    }
}