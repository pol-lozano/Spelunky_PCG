using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ManagerLoader : MonoBehaviour
{
    //public GameObject gameManager;
    public GameObject soundManager;

    //Load in game managers if they are missing from the scene
    void Awake()
    {
        //if (GameManager.instance == null) Instantiate(gameManager);
        if (SoundManager.instance == null) Instantiate(soundManager);     
    }
}

