using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NetworkEntity : MonoBehaviour
{
    public uint netID = 0;
    //public bool hasAuthority;
    //public bool clientControlled;

    bool updatePos,updateRot,updateScale;
    public float interpolationDuration = 0.1f;
    bool updatingPos, updatingRot, updatingScale;
    Vector3 statePos, stateScale;
    Quaternion stateRot;

    void Start()
    {
        SaveState();
    }

    public void SetPosition(Vector3 pos) { transform.position = pos; }
    public void SetRotation(Quaternion rot) { transform.rotation = rot; }
    public void SetScale(Vector3 scale) { transform.localScale = scale; }

    public void UpdatePosition(Vector3 pos)
    {
        if (updatingPos) StopCoroutine(InterpolatePosition(pos,interpolationDuration));
        StartCoroutine(InterpolatePosition(pos, interpolationDuration));
    }

    public void UpdateScale(Vector3 scale)
    {
        if (updatingScale) StopCoroutine(InterpolateScale(scale, interpolationDuration));
        StartCoroutine(InterpolateScale(scale, interpolationDuration));
    }

    public void UpdateRotation(Quaternion quat)
    {
        if (updatingRot) StopCoroutine(InterpolateRotation(quat, interpolationDuration));
        StartCoroutine(InterpolateRotation(quat, interpolationDuration));
    }

    public void SaveState()
    {
        statePos = transform.position;
        stateRot = transform.rotation;
        stateScale = transform.localScale;
    }

    public bool CheckUpdate()
    {
        bool res = false;
        if (statePos != transform.position)
        {
            updatePos = true;
            res = true;
        }
        if (stateRot != transform.rotation)
        {
            updateRot = true;
            res = true;
        }
        if (stateScale != transform.localScale)
        {
            updateScale = true;
            res = true;
        }
        
        return res;
    }

    public byte[] GetUpdateState()
    {
        byte[] res;
        Packet pak = new Packet();
        pak.Write(netID);
        if (updatePos) pak.Write((byte)2);
        else pak.Write((byte)1);

        if (updateRot) pak.Write((byte)2);
        else pak.Write((byte)1);

        if (updateScale) pak.Write((byte)2);
        else pak.Write((byte)1);

        if (updatePos)
        {         
            pak.Write(transform.position);
        }
        if (updateRot)
        {
            pak.Write(transform.rotation);
        }
        if (updateScale)
        {
            pak.Write(transform.localScale);
        }
        updateRot = false;
        updatePos = false;
        updateScale = false;
        res = pak.ToArray();
        return res;
    }

    IEnumerator InterpolatePosition(Vector3 newPos, float duration)
    {
        updatingPos = true;
        float time = 0;
        Vector2 startPos = transform.position;
        while (time < duration)
        {
            transform.position = Vector3.Lerp(startPos,newPos,time/duration);
            time += Time.deltaTime;
            yield return null;
        }
        transform.position = newPos;
        updatingPos = false;
    }

    IEnumerator InterpolateRotation(Quaternion newRot, float duration)
    {
        updatingRot = true;
        float time = 0;
        Quaternion startRot = transform.rotation;
        while (time < duration)
        {
            transform.rotation = Quaternion.Lerp(startRot, newRot, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        transform.rotation = newRot;
        updatingRot = false;
    }

    IEnumerator InterpolateScale(Vector3 newScale, float duration)
    {
        updatingScale = true;
        float time = 0;
        Vector2 startScale = transform.localScale;
        while (time < duration)
        {
            transform.localScale = Vector3.Lerp(startScale, newScale, time / duration);
            time += Time.deltaTime;
            yield return null;
        }
        transform.localScale = newScale;
        updatingScale = false;
    }
}