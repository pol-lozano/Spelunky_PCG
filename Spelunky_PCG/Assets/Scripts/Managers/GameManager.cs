using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager instance = null;
    private LevelGenerator levelGenerator;
    public bool doingSetup;
    public Player player;

    private void Awake()
    {
        if (instance == null) instance = this;
        else if (instance != this) Destroy(gameObject);
        DontDestroyOnLoad(gameObject);

        levelGenerator = GetComponent<LevelGenerator>();
        player = FindObjectOfType<Player>();
        Initialize();
    }

    void Initialize()
    {
        LoadLevel();
    }

    public void LoadLevel()
    {
        doingSetup = true;
        //Keep track of time it takes to generate levels
        var watch = System.Diagnostics.Stopwatch.StartNew();

        //Generate level
        levelGenerator.GenerateLevel();

        //Spawn player in
        player.transform.position = levelGenerator.spawnPos;

        //Stop timer and print time elapsed
        watch.Stop();
        var elapsedMs = watch.ElapsedMilliseconds;
        Debug.Log("Generation Time: " + elapsedMs + "ms");
        doingSetup = false;
    }
}
