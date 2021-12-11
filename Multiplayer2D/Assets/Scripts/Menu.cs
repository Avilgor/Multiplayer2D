using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Menu : MonoBehaviour
{
    public GameObject hostScreen,joinScreen;
    public TMP_InputField usernameHost, usernameJoin,joinIp;

    void Start()
    {
        joinScreen.SetActive(false);
        hostScreen.SetActive(false);
    }

    public void CloseGame()
    {
        Application.Quit();
    }


    public void OpenHost()
    {
        hostScreen.SetActive(true);
    }

    public void CloseHost()
    {
        hostScreen.SetActive(false);
    }

    public void OpenJoin()
    {
        joinScreen.SetActive(true);
    }

    public void CloseJoin()
    {
        joinScreen.SetActive(false);
    }

    public void StartHost()
    {
        if (!string.IsNullOrWhiteSpace(usernameHost.text))
        {
            GLOBALS.isclient = false;
            GLOBALS.username = usernameHost.text;
            SceneManager.LoadScene(1);
        }
    }

    public void StartJoin()
    {
        if (!string.IsNullOrWhiteSpace(usernameJoin.text) && !string.IsNullOrWhiteSpace(joinIp.text))
        {
            GLOBALS.IP = joinIp.text;
            GLOBALS.username = usernameJoin.text;
            GLOBALS.isclient = true;
            SceneManager.LoadScene(1);
        }
    }
}
