using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NetworkGameobjects : MonoBehaviour
{
    [SerializeField] GameObject[] netGos;
    Dictionary<uint, NetworkEntity> networkEntities;

    private void Awake()
    {
        GLOBALS.networkGO = this;
        networkEntities = new Dictionary<uint, NetworkEntity>();
    }

    public GameObject SpawnGo(int index, uint id, Vector3 pos, Quaternion rot)
    {
        GameObject go = null;

        if (index >= 0 && index < netGos.Length)
        {
            go = Instantiate(netGos[index],pos,rot);
            go.GetComponent<NetworkEntity>().SetEntity(id);           
        }
        return go;
    }

    public bool HasEntity(uint netID)
    {
        if (networkEntities.ContainsKey(netID)) return true;
        else return false;
    }

    public void AddEntity(NetworkEntity ent)
    {
        if (!networkEntities.ContainsKey(ent.netID)) networkEntities.Add(ent.netID,ent);
    }

    public void RemoveEntity(uint id)
    {
        if (!networkEntities.ContainsKey(id)) networkEntities.Remove(id);
    }

    public NetworkEntity GetEntity(uint netId)
    {
        if (networkEntities.ContainsKey(netId)) return networkEntities[netId];
        return null;
    }

    public Dictionary<uint, NetworkEntity> GetEntities()
    {
        return networkEntities;
    }

    public bool DestroyGo(uint netId)
    {
        if (networkEntities.ContainsKey(netId))
        {
            Destroy(networkEntities[netId].gameObject);
            networkEntities.Remove(netId);
            return true;
        }

        Debug.Log("Could not destroy, network entity not found.");
        return false;
    }
}
