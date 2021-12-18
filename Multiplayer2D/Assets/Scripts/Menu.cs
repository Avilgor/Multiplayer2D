using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class Menu : MonoBehaviour
{
    public GameObject joinScreen;
    public TMP_InputField usernameJoin,joinIp;

    private void Awake()
    {
        //Application.targetFrameRate = 60;
    }

    void Start()
    {
        usernameJoin.text = "player";
        joinIp.text = "127.0.0.1";
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

    public void StartHost()
    {      
        SceneManager.LoadScene(1);
    }

    public void StartJoin()
    {
        if (!string.IsNullOrWhiteSpace(usernameJoin.text) && !string.IsNullOrWhiteSpace(joinIp.text))
        {
            GLOBALS.IP = joinIp.text;
            GLOBALS.username = usernameJoin.text;
            SceneManager.LoadScene(2);
        }
    }
}
