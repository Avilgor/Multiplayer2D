using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Menu : MonoBehaviour
{
    public GameObject joinScreen,hostScreen;
    public TMP_InputField usernameJoin,roomJoin,userHost,roomHost;

    private void Awake()
    {
        //Application.targetFrameRate = 60;
    }

    void Start()
    {
        GLOBALS.isclient = true;
        usernameJoin.text = userHost.text = "Player";
        roomJoin.text = roomHost.text = "MyRoom";
        joinScreen.SetActive(false);
    }

    public void CloseGame()
    {
        Application.Quit();
    }

    public void OpenJoin()
    {
        joinScreen.SetActive(true);
    }

    public void CloseJoin()
    {
        joinScreen.SetActive(false);
    }

    public void OpenHost()
    {
        hostScreen.SetActive(true);
    }

    public void CloseHost()
    {
        hostScreen.SetActive(false);
    }

    public void StartHost()
    {
        if (!string.IsNullOrWhiteSpace(userHost.text) && !string.IsNullOrWhiteSpace(roomHost.text))
        {
            GLOBALS.isclient = false;
            GLOBALS.roomName = roomHost.text;
            GLOBALS.username = userHost.text;
            SceneManager.LoadScene(1);
        }
    }

    public void StartJoin()
    {
        if (!string.IsNullOrWhiteSpace(usernameJoin.text) && !string.IsNullOrWhiteSpace(roomJoin.text))
        {
            GLOBALS.roomName = roomJoin.text;
            GLOBALS.username = usernameJoin.text;
            SceneManager.LoadScene(2);
        }
    }
}
