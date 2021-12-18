using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ClientWait : MonoBehaviour
{
    public TextMeshProUGUI players, header;
    public GameObject waitScreen;

    int totalPlayers = 0;

    private void Start()
    {
        header.text = "Waiting for players...";
    }

    public void StartServer()
    {
        waitScreen.SetActive(true);
        players.text = totalPlayers.ToString() + "/10";
    }

    public void StartClient()
    {
        waitScreen.SetActive(true);
        players.text = totalPlayers.ToString() + "/10";
    }

    public void AddPlayer()
    {
        totalPlayers++;
        players.text = totalPlayers.ToString() + "/10";
    }

    public void RemovePlayer()
    {
        totalPlayers--;
        players.text = totalPlayers.ToString() + "/10";
    }

    public void StartGame()
    {
        waitScreen.SetActive(false);
    }

    public void SetCountdown(int value)
    {
        header.text = "Starting in..."+value.ToString();
    }
}
