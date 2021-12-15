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
        Destroy(gameObject);
    }

    public void OnCollisionEnter2D(Collision2D collision)
    {
        if(collision.gameObject.tag == "Tank")
        {
            DestroyBullet();
        }
    }

    IEnumerator TimeToDie()
    {
        yield return new WaitForSeconds(lifetime);
        Destroy(gameObject);
    }
}