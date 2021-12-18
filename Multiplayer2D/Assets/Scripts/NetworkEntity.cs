using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class NetworkEntity : MonoBehaviour
{
    public uint netID = 0;
    public bool lockPosition = false, lockRotation = false, lockScale = false;

    bool updatePos,updateRot,updateScale;
    public float interpolationDuration = 0.2f;
    bool updatingPos, updatingRot, updatingScale;
    Vector3 statePos, stateScale;
    Quaternion stateRot;

    Vector3 startPos,endPos, startScale,endScale;
    Quaternion startRot,endRot;

    float posTimer, rotTimer, scaleTimer;

    void Start()
    {
        SaveState();
    }

    public void Update()
    {        
        if (updatingPos) InterpolatePosition();
        if (updatingRot) InterpolateRotation();
        if (updatingScale) InterpolateScale();
    }

    private void OnDestroy()
    {
        GLOBALS.networkGO.RemoveEntity(netID);
    }

    public void SetEntity(uint id)
    {
        netID = id;
        GLOBALS.networkGO.AddEntity(this);
    }

    public void SetPosition(Vector3 pos) { transform.position = pos; }
    public void SetRotation(Quaternion rot) { transform.rotation = rot; }
    public void SetScale(Vector3 scale) { transform.localScale = scale; }

    public void UpdatePosition(Vector3 pos)
    {
        updatingPos = true;
        startPos = transform.position;
        endPos = pos;
        posTimer = 0;
    }

    public void UpdateScale(Vector3 scale)
    {
        updatingScale = true;
        startScale = transform.localScale;
        endScale = scale;
        scaleTimer = 0;
    }

    public void UpdateRotation(Quaternion quat)
    {
        updatingRot = true;
        startRot = transform.rotation;
        endRot = quat;
        rotTimer = 0;
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

    private void InterpolatePosition()
    {
        //Debug.Log("Entity: " + netID.ToString() + " interpolate position.");
        if (posTimer < interpolationDuration)
        {
            transform.position = Vector3.Lerp(startPos, endPos, posTimer / interpolationDuration);
            posTimer += Time.deltaTime;
        }
        else
        {
            transform.position = endPos;
            updatingPos = false;
        }
    }

    private void InterpolateRotation()
    {
        //Debug.Log("Entity: " + netID.ToString() + " interpolate rotation.");
        if (rotTimer < interpolationDuration)
        {
            transform.rotation = Quaternion.Lerp(startRot, endRot, rotTimer / interpolationDuration);
            rotTimer += Time.deltaTime;
        }
        else
        {
            transform.rotation = endRot;
            updatingRot = false;
        }
    }

    private void InterpolateScale()
    {
        if (scaleTimer < interpolationDuration)
        {
            transform.localScale = Vector3.Lerp(startScale, endScale, scaleTimer / interpolationDuration);
            scaleTimer += Time.deltaTime;
        }
        else
        {
            transform.localScale = endScale;
            updatingScale = false;
        }
    }
}