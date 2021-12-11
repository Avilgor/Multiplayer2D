using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target;

    [SerializeField] Vector3 offset;
    [SerializeField] float smoothTime = 0.3f;
    Vector3 vel = Vector3.zero;
    Rigidbody2D rb;

    public void Awake()
    {
        GLOBALS.cameraFollow = this;
        rb = GetComponent<Rigidbody2D>();
    }


    public void FixedUpdate()
    {
        if(target != null) rb.MovePosition(Vector3.SmoothDamp(transform.position, target.position + offset, ref vel, smoothTime));
    }

    /*void LateUpdate()
    {
        if (target != null)
        {          
            rb.transform.position = Vector3.SmoothDamp(transform.position, target.position + offset, ref vel, smoothTime);
        }
    }*/
}
