using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bullet : MonoBehaviour
{
    public float lifetime = 3.0f;
    public float shellSpeed = 1.0f;

    void Start()
    {
        StartCoroutine(TimeToDie());
    }

    void Update()
    {
        transform.position = transform.position + (transform.up * (shellSpeed/100));
    }

    public void DestroyBullet()
    {
        GLOBALS.networkGO.DestroyGo(GetComponent<NetworkEntity>().netID);
        Packet pak = new Packet();
        pak.Write(GetComponent<NetworkEntity>().netID);
        GLOBALS.serverGame.BroadcastPacket(pak, ClientMSG.CM_DESTROY_GO, true);
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Tank")
        {
            collision.gameObject.GetComponent<TankController>().TakeDamage();
            DestroyBullet();
        }
    }

    IEnumerator TimeToDie()
    {
        yield return new WaitForSeconds(lifetime);
        DestroyBullet();
    }
}